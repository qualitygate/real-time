/**
 * Describe the options that you can pass to a database initialization.
 */
export interface DatabaseOptions {
	/**
	 * Url to which the client database driver will point to. This must point to a valid SignalR HUB endpoint.
	 */
	url: string,

	/**
	 * Function to call whenever there is needed an Authorization token to connect to the SignalR HUB.
	 */
	getToken: () => string
}