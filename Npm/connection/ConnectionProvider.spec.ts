import {ConnectionOptions} from './ConnectionOptions'
import {ConnectionProvider} from './ConnectionProvider'
import {SinonStub, stub} from 'sinon'
import {IRetryPolicy} from '@microsoft/signalr'

describe('ConnectionProvider', () => {
	let connectionProvider: ConnectionProvider

	it('constructs correctly the database with the specified options', () => {
		// Given some options
		const options: ConnectionOptions = {
			url: 'http://localhost:40801',
			getToken: () => 'some-token'
		}

		const hubConnectionBuilder = {
			withAutomaticReconnect: stub(),
			withUrl()
		}

		connectionProvider = new ConnectionProvider(hubConnectionBuilder as any)

		// When asked for a new connection
		const connection = connectionProvider.createConnection(options)

		// Then the connection should be correct
		const hubConnectionBuilderStub = hubConnectionBuilder.withAutomaticReconnect as SinonStub
		expect(hubConnectionBuilderStub.calledOnce).toBeTruthy()
		expect((hubConnectionBuilderStub.firstCall.args[0] as IRetryPolicy).nextRetryDelayInMilliseconds({} as any))
			.toBe(1000)
	})
})