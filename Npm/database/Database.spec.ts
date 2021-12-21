import {DatabaseImpl} from './Database'
import {DatabaseListeners} from './DatabaseListeners'
import {Equal, PageInfo, Query} from '../contracts'
import {ConnectionOptions} from '../connection/ConnectionOptions'
import {ConnectionProvider} from '../connection/Connection'
import * as sinon from 'sinon'
import {SinonStub} from 'sinon'
import {range} from 'lodash'
import {HubConnection, HubConnectionState} from '@microsoft/signalr'
import {Logger} from '../utils/Logger'
import {AddPageQuery, AddQuery, Change, Delete, EntityChanged, PageChanged, Upsert} from './protocol'
import {CONNECTED, DISCONNECTED} from './DatabaseStatus'

describe('DatabaseImpl tests', () => {
	const AGE = 'Age'
	const NAME = 'Name'
	const QUERY_NAME = 'Database: #1'
	const CONNECTION_ID = 'Connection: #1'

	const doNothing = (...args: any[]) => args
	const query: Query = {
		page: 0,
		name: 'Query-1',
		orderBy: {fields: [NAME], ascending: false},
		fields: [NAME],
		conditions: [
			{field: NAME, operator: Equal, value: 'John', joinUsing: 'and'},
			{field: AGE, operator: Equal, value: 30}
		],
		table: 'Users',
		size: 8
	}
	const connectionOptions: ConnectionOptions = {
		url: 'http://localhost:49798',
		getToken: () => 'some-token'
	}
	let connectionProvider: ConnectionProvider = new ConnectionProvider()
	let connection: HubConnection
	let listeners: DatabaseListeners
	let logger: Logger

	let database: DatabaseImpl

	beforeEach(() => {
		connection = setupHubConnection(CONNECTION_ID)
		logger = setupLogger()

		const createConnection = sinon.stub(connectionProvider, 'createConnection')
		createConnection.withArgs(connectionOptions).returns(connection)

		listeners = new DatabaseListeners()
		listeners.notify = sinon.stub()

		database = new DatabaseImpl(QUERY_NAME, listeners, connectionProvider, logger)
	})

	afterEach(() => {
		sinon.restore()
	})

	it('have a property called: name', () => {
		// When
		const databaseName = database.name

		// Then
		expect(databaseName).toBe(QUERY_NAME)
	})

	it('starts in Not Ready state', () => {
		expect(database.ready).toBeFalsy()
	})

	describe('Initialization', () => {
		it('establishes the connection to the backend', async () => {
			// Given, there are substituted internal functions for stubs
			const entityChangedStub = sinon.stub(database, '_onEntityChanged' as any)
			const pageChangedStub = sinon.stub(database, '_onPageChanged' as any)
			const reconnectQueriesStub = sinon.stub(database, '_reconnectQueries' as any)

			// When
			await database.initialize(connectionOptions)

			// Then the connection must have been established
			expect((connection.start as SinonStub).calledOnce).toBeTruthy()

			// And it notifies the listeners about the connection established event
			expect((listeners.notify as SinonStub).calledOnceWithExactly(CONNECTED)).toBeTruthy()

			// And signals the backend it wants to receive changes from entities, check the registered callback is the proper
			// on
			let on = connection.on as SinonStub
			expect(on.calledWithExactly(EntityChanged, sinon.match.func)).toBeTruthy()
			const registeredEntityChangedFunction = on.args[0][1]
			const newEntities = [{id: 1}]
			registeredEntityChangedFunction(query.name, newEntities)
			expect(entityChangedStub.calledOnceWithExactly(query.name, newEntities)).toBeTruthy()

			// And also signals the backend it wants to receive changes from paged queries, check the registered callback is
			// correct
			expect(on.calledWithExactly(PageChanged, sinon.match.func)).toBeTruthy()
			const registeredPageChangedFunction = on.args[1][1]
			registeredPageChangedFunction(query.name, query)
			expect(pageChangedStub.calledOnceWithExactly(query.name, query)).toBeTruthy()

			// And also makes sure that queries get reconnected when the connection comes back on
			let onreconnected = connection.onreconnected as SinonStub
			expect(onreconnected.calledOnceWithExactly(sinon.match.func)).toBeTruthy()
			const reconnectQueriesFunction = onreconnected.args[0][0]
			reconnectQueriesFunction()
			expect(reconnectQueriesStub.calledOnce).toBeTruthy()

			// And it becomes ready
			expect(database.ready).toBeTruthy()
		})

		it('ignores duplicated initializations, and warn developer', async () => {
			// Given the database is already initialized
			const startStub = connection.start as SinonStub
			const notifyStub = listeners.notify as SinonStub
			const onStub = connection.on as SinonStub

			await database.initialize(connectionOptions)

			// When
			await database.initialize(connectionOptions)

			// Then, it must not do anything of the initialization
			expect(startStub.calledOnce).toBeTruthy()
			expect(notifyStub.calledOnce).toBeTruthy()
			expect(onStub.calledTwice).toBeTruthy()

			// And warn the user
			expect((logger.warn as SinonStub).calledOnceWithExactly('Cannot start twice the database')).toBeTruthy()
		})

		it('lets the developer know about a errors found on the process', async () => {
			const error = new Error('Something got really wrong!');
			(connection.start as SinonStub).throws(error)

			// When the initialization fails
			await database.initialize(connectionOptions)

			// Then
			let loggerError = logger.error as SinonStub
			expect(loggerError.calledOnceWith('Error connecting to database', sinon.match.instanceOf(Error))).toBeTruthy()
			expect(loggerError.firstCall.args[1]).toBe(error)
		})
	})

	describe('Paged query management', () => {
		it('throws an error if not connected', async () => {
			await expect(() => database.addPageQuery<any>(query, doNothing))
				.rejects
				.toThrow('Database must be connected to register queries. Please initialize it first, and make sure it\'s so.')
		})

		describe('and it\'s connect', () => {
			beforeEach(async () => {
				await database.initialize(connectionOptions)
			})

			it('does nothing if registered query with the same name twice, just warn about it', async () => {
				// Given a scenario
				const setPageInfo = sinon.stub()

				// When a query is already registered with a name
				await database.addPageQuery<any>(query, setPageInfo);
				(connection.send as SinonStub).reset()

				// And attempted to add the same query with the same name
				await database.addPageQuery<any>(query, setPageInfo)

				// Then, no registration should happen
				expect((connection.send as SinonStub).notCalled).toBeTruthy()

				// And a warning notified to the developer
				let expectedWarning = `Page query: ${query.name} already exists. They can only be added once.`
				expect((logger.warn as SinonStub).calledOnceWithExactly(expectedWarning)).toBeTruthy()
			})

			it('registers the query locally and also in the backend', async () => {
				// Given a new page info
				const setPageInfo = sinon.stub()

				// When a query is registered
				await database.addPageQuery<any>(query, setPageInfo)

				// Then, the notification must have come
				expect((connection.send as SinonStub).calledOnceWithExactly(AddPageQuery, query)).toBeTruthy()
			})

			it('receives data change notifications when they arrive', async () => {
				// Given a new page info
				const setPageInfo = sinon.stub()
				const newPageInfo: PageInfo<any> = {
					page: 1,
					size: 10,
					items: range(0, 10),
					total: 20
				}

				// And a query is already registered listening for pages of such entities
				await database.addPageQuery<any>(query, setPageInfo);

				// When, the new page of entities arrives
				(database as any)._onPageChanged(query.name, newPageInfo)

				// Then, the notification must have come
				setPageInfo.calledOnceWithExactly(newPageInfo)
			})
		})
	})

	describe('Normal query management', () => {
		it('throws an error if not connected', async () => {
			await expect(() => database.addQuery<any>(query, doNothing))
				.rejects
				.toThrow('Database must be connected to register queries. Please initialize it first, and make sure it\'s so.')
		})

		describe('and it\'s connect', () => {
			beforeEach(async () => {
				await database.initialize(connectionOptions)
			})

			it('does nothing if registered query with the same name twice, just warn about it', async () => {
				// Given a scenario
				const setEntities = sinon.stub()

				// When a query is already registered with a name
				await database.addQuery<any>(query, setEntities);
				(connection.send as SinonStub).reset()

				// And attempted to add the same query with the same name
				await database.addQuery<any>(query, setEntities)

				// Then, no registration should happen
				expect((connection.send as SinonStub).notCalled).toBeTruthy()

				// And a warning notified to the developer
				let expectedWarning = `Query: ${query.name} already exists. They can only be added once.`
				expect((logger.warn as SinonStub).calledOnceWithExactly(expectedWarning)).toBeTruthy()
			})

			it('registers the query locally and also in the backend', async () => {
				// Given a new page info
				const setEntities = sinon.stub()

				// When a query is registered
				await database.addQuery<any>(query, setEntities)

				// Then, the notification must have come
				expect((connection.send as SinonStub).calledOnceWithExactly(AddQuery, query)).toBeTruthy()
			})

			it('receives data change notifications when they arrive', async () => {
				// Given a new page info
				const setEntities = sinon.stub()
				const entity1 = {id: '1', name: 'E1'}
				const entity2 = {id: '2', name: 'E2'}
				const entity3 = {id: '3', name: 'E3'}
				let changes: Change[] = [{entity: entity1, type: Delete}, {entity: entity2, type: Upsert}]

				// And a query is already registered listening for pages of such entities
				await database.addQuery<any>(query, setEntities);

				// When, the new page of entities arrives
				(database as any)._onEntityChanged(query.name, changes)

				// Then, the notification must have come
				expect(setEntities.calledWithExactly([entity2])).toBeTruthy()

				// When done a second notification
				changes = [{entity: entity3, type: Upsert}, {entity: {...entity2, name: 'E22'}, type: Upsert}];
				(database as any)._onEntityChanged(query.name, changes)

				// Then
				expect(setEntities.calledWithExactly([{...entity2, name: 'E22'}, entity3])).toBeTruthy()
			})
		})
	})

	describe('Disposal', () => {
		beforeEach(() => {
			database.initialize(connectionOptions)
		})

		it('destroys the connection and notifies the listeners about the disconnection', async () => {
			// When
			await database.dispose()

			// Then
			expect((connection.stop as SinonStub).calledOnce).toBeTruthy()

			// And notifies listeners about the disconnection
			expect((listeners.notify as SinonStub).calledOnceWithExactly(DISCONNECTED))
		})

		it('still notifies the listeners about the disconnection', async () => {
			// Given
			const errorMessage = 'Critical error';
			(connection.stop as SinonStub).throws(new Error(errorMessage))

			// When
			await database.dispose()

			// Then
			expect(
				(logger.error as SinonStub).calledOnceWithExactly('Error disconnecting database', sinon.match.instanceOf(Error))
			).toBeTruthy()

			// And notifies listeners about the disconnection
			expect((listeners.notify as SinonStub).calledOnceWithExactly(DISCONNECTED))
		})
	})
})

function setupHubConnection(connectionId: string): HubConnection {
	// noinspection JSUnusedLocalSymbols
	const connection = {
		state: HubConnectionState.Connected,

		start(): Promise<void> {
			return Promise.resolve()
		},

		get connectionId(): string | null {
			return connectionId
		},

		onreconnected: (callback: () => void) => callback(),

		on(name: string, callback: () => void) {
		},

		send(methodName: string, ...args: any[]): Promise<void> {
			return Promise.resolve()
		}
	} as HubConnection

	const connectionStart = sinon.stub(connection, 'start')
	connectionStart.callsFake(async () => {
		const connectionState = sinon.stub(connection, 'state')
		connectionState.returns(HubConnectionState.Connected)
	})

	return {
		...connection,
		on: sinon.stub(),
		onreconnected: sinon.stub(),
		start: connectionStart,
		stop: sinon.stub(),
		send: sinon.stub(connection, 'send')
	} as any
}

function setupLogger(): Logger {
	return {
		warn: sinon.stub(),
		debug: sinon.stub(),
		info: sinon.stub(),
		error: sinon.stub()
	}
}