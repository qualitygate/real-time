import {HubConnection, HubConnectionBuilder, IRetryPolicy} from '@microsoft/signalr'
import {ConnectionOptions} from './ConnectionOptions'

export class ConnectionProvider {
	private _retryPolicy: IRetryPolicy = {
		nextRetryDelayInMilliseconds(): number | null {
			return 1000
		}
	}

	createConnection(options: Partial<ConnectionOptions>): HubConnection {
		return new HubConnectionBuilder()
			.withAutomaticReconnect(this._retryPolicy)
			.withUrl(options.url, {
				withCredentials: false,
				headers: {Authorization: `Bearer ${options.getToken()}`}
			})
			.build()
	}
}