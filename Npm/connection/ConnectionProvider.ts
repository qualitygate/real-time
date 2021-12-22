import {HubConnection, HubConnectionBuilder, IRetryPolicy} from '@microsoft/signalr'
import {ConnectionOptions} from './ConnectionOptions'

/**
 * An object that can create new database connections given database options.
 */
export class ConnectionProvider {
	private _retryPolicy: IRetryPolicy = {
		nextRetryDelayInMilliseconds(): number | null {
			return 1000
		}
	}
	private readonly _builder: HubConnectionBuilder

	/**
	 * Initializes a new instance of {@link ConnectionProvider} given an optional connection builder.
	 *
	 * @param builder {HubConnectionBuilder}: Builder use to build the database connection.
	 */
	constructor(builder?: HubConnectionBuilder) {
		this._builder = builder ?? new HubConnectionBuilder()
	}

	/**
	 * Creates a new database connection with given options.
	 *
	 * @param options {Partial<ConnectionOptions>}: Options to use to configure the database connection.
	 * @returns {HubConnection} A SignalR connection to the SignalR endpoint on the backend through which this client and
	 * the backend will exchange data change notifications
	 */
	createConnection(options: Partial<ConnectionOptions>): HubConnection {
		return this._builder
			.withAutomaticReconnect(this._retryPolicy)
			.withUrl(options.url, {
				withCredentials: false,
				headers: {Authorization: `Bearer ${options.getToken()}`}
			})
			.build()
	}
}