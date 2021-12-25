import {ConnectionOptions} from './ConnectionOptions'
import {ConnectionProvider} from './ConnectionProvider'
import {SinonStub, stub, restore} from 'sinon'
import {HubConnection, HubConnectionBuilder, IRetryPolicy} from '@microsoft/signalr'

describe('ConnectionProvider', () => {
	let options: ConnectionOptions
	let hubConnectionBuilder: HubConnectionBuilder
	let hubConnection: HubConnection

	let connectionProvider: ConnectionProvider

	beforeEach(() => {
		options = {
			url: 'http://localhost:40801',
			getToken: () => 'some-token'
		}

		hubConnection = {} as any
		const withAutomaticReconnect = stub()
		const withUrl = stub()
		const build = stub()
		hubConnectionBuilder = {
			withAutomaticReconnect,
			withUrl,
			build
		} as any
		withAutomaticReconnect.returns({withUrl})
		withUrl.returns({build})
		build.returns(hubConnection)

		connectionProvider = new ConnectionProvider()
		const getHubConnectionBuilderStub = stub(connectionProvider, 'getHubConnectionBuilder' as any)
		getHubConnectionBuilderStub.returns(hubConnectionBuilder as any)
	})

	afterEach(() => {
		restore()
	})

	it('constructs correctly the database with the specified options', () => {
		// When asked for a new connection
		const connection = connectionProvider.createConnection(options)

		// Then the connection should have reconnection logic
		let stubUnderAsserts = hubConnectionBuilder.withAutomaticReconnect as SinonStub
		expect(stubUnderAsserts.calledOnce).toBeTruthy()
		expect((stubUnderAsserts.firstCall.args[0] as IRetryPolicy).nextRetryDelayInMilliseconds({} as any))
			.toBe(10000)

		stubUnderAsserts = hubConnectionBuilder.withUrl as SinonStub
		expect(stubUnderAsserts.calledOnceWithExactly(options.url, {
			withCredentials: false,
			headers: {Authorization: `Bearer ${options.getToken()}`}
		})).toBeTruthy()

		stubUnderAsserts = hubConnectionBuilder.build as SinonStub
		expect(stubUnderAsserts.calledOnce).toBeTruthy()

		expect(connection).toBe(hubConnection)
	})

	it('constructs correctly the database without authentication token, if getToken options is undefined', () => {
		// Given options that have no getToken() function
		options = {...options, getToken: null}

		// When asked for a new connection
		const connection = connectionProvider.createConnection(options)

		// Then the connection should have reconnection logic
		let stubUnderAsserts = hubConnectionBuilder.withUrl as SinonStub
		expect(stubUnderAsserts.calledOnceWithExactly(options.url, {withCredentials: false, headers: {}})).toBeTruthy()

		expect(connection).toBe(hubConnection)
	})
})