import {DatabaseStatus} from './DatabaseStatus'

/**
 * Describes a database status listener
 */
export interface DatabaseStatusListener {
	/**
	 * Identifies the listener.
	 */
	id: string

	/**
	 * The actual registered function by the client wanting to be notified of database status changes.
	 * @param status {DatabaseStatus} Current database status.
	 */
	fn: (status: DatabaseStatus) => void
}