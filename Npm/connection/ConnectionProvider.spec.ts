import {ConnectionOptions} from './ConnectionOptions'
import {ConnectionProvider} from './ConnectionProvider'
import {SinonStub, stub} from 'sinon'
import {HubConnection, IRetryPolicy} from '@microsoft/signalr'

describe('ConnectionProvider', () => {
	let connectionProvider: ConnectionProvider

	it('constructs correctly the database with the specified options', () => {
		// Given some options
		const options: ConnectionOptions = {
			url: 'http://localhost:40801',
			getToken: () => 'some-token'
		}

		const hubConnection: HubConnection = {} as any
		const hubConnectionBuilder = {withAutomaticReconnect: stub(), withUrl: stub(), build: stub()}
		hubConnectionBuilder.withAutomaticReconnect.returns(hubConnectionBuilder)
		hubConnectionBuilder.withUrl.returns(hubConnectionBuilder)
		hubConnectionBuilder.build.returns(hubConnection)

		connectionProvider = new ConnectionProvider(hubConnectionBuilder as any)

		// When asked for a new connection
		const connection = connectionProvider.createConnection(options)

		// Then the connection should have reconnection logic
		let stubUnderAsserts = hubConnectionBuilder.withAutomaticReconnect as SinonStub
		expect(stubUnderAsserts.calledOnce).toBeTruthy()
		expect((stubUnderAsserts.firstCall.args[0] as IRetryPolicy).nextRetryDelayInMilliseconds({} as any))
			.toBe(1000)

		stubUnderAsserts = hubConnectionBuilder.withUrl as SinonStub
		expect(stubUnderAsserts.calledOnceWithExactly(options.url, {
			withCredentials: false,
			headers: {Authorization: `Bearer ${options.getToken()}`}
		})).toBeTruthy()

		stubUnderAsserts = hubConnectionBuilder.build as SinonStub
		expect(stubUnderAsserts.calledOnce).toBeTruthy()

		expect(connection).toBe(hubConnection)
	})
})