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
import {AddPageQuery, EntityChanged, PageChanged} from './protocol'
import {CONNECTED} from './DatabaseStatus'

describe('DatabaseImpl tests', () => {
	const AGE = 'Age'
	const NAME = 'Name'
	const QUERY_NAME = 'Database: #1'
	const CONNECTION_ID = 'Connection: #1'

	const doNothing = (...args: any[]) => args
	const pageQuery: Query = {
		page: 0,
		name: 'Paged-Query-1',
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
		it('should establish the connection to the backend', async () => {
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
			registeredEntityChangedFunction(pageQuery.name, newEntities)
			expect(entityChangedStub.calledOnceWithExactly(pageQuery.name, newEntities)).toBeTruthy()

			// And also signals the backend it wants to receive changes from paged queries, check the registered callback is
			// correct
			expect(on.calledWithExactly(PageChanged, sinon.match.func)).toBeTruthy()
			const registeredPageChangedFunction = on.args[1][1]
			registeredPageChangedFunction(pageQuery.name, pageQuery)
			expect(pageChangedStub.calledOnceWithExactly(pageQuery.name, pageQuery)).toBeTruthy()

			// And also makes sure that queries get reconnected when the connection comes back on
			let onreconnected = connection.onreconnected as SinonStub
			expect(onreconnected.calledOnceWithExactly(sinon.match.func)).toBeTruthy()
			const reconnectQueriesFunction = onreconnected.args[0][0]
			reconnectQueriesFunction()
			expect(reconnectQueriesStub.calledOnce).toBeTruthy()

			// And it becomes ready
			expect(database.ready).toBeTruthy()
		})

		it('should ignore if initialized twice, and warn developer', async () => {
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
	})

	describe('Paged query management', () => {
		it('should throw an error if not connected', async () => {
			await expect(() => database.addPageQuery<any>(pageQuery, doNothing))
				.rejects
				.toThrow('Database must be connected to register queries. Please initialize it first, and make sure it\'s so.')
		})

		describe('and it\'s connect', () => {
			beforeEach(async () => {
				await database.initialize(connectionOptions)
			})

			it('should do nothing if registered query with the same name twice, just warn about it', async () => {
				// Given a scenario
				const setPageInfo = sinon.stub()

				// When a query is already registered with a name
				await database.addPageQuery<any>(pageQuery, setPageInfo);
				(connection.send as SinonStub).reset()

				// And attempted to add the same query with the same name
				await database.addPageQuery<any>(pageQuery, setPageInfo)

				// Then, no registration should happen
				expect((connection.send as SinonStub).notCalled).toBeTruthy()

				// And a warning notified to the developer
				let expectedWarning = `Page query: ${pageQuery.name} already exists. They can only be added once.`
				expect((logger.warn as SinonStub).calledOnceWithExactly(expectedWarning)).toBeTruthy()
			})

			it('should register the query locally and also in the backend', async () => {
				// Given a new page info
				const setPageInfo = sinon.stub()

				// When a query is registered
				await database.addPageQuery<any>(pageQuery, setPageInfo)

				// Then, the notification must have come
				expect((connection.send as SinonStub).calledOnceWithExactly(AddPageQuery, pageQuery)).toBeTruthy()
			})

			it('should receive data change notifications when they arrive', async () => {
				// Given a new page info
				const setPageInfo = sinon.stub()
				const newPageInfo: PageInfo<any> = {
					page: 1,
					size: 10,
					items: range(0, 10),
					total: 20
				}

				// And a query is already registered listening for pages of such entities
				await database.addPageQuery<any>(pageQuery, setPageInfo);

				// When, the new page of entities arrives
				(database as any)._onPageChanged(pageQuery.name, newPageInfo)

				// Then, the notification must have come
				setPageInfo.calledOnceWithExactly(newPageInfo)
			})
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