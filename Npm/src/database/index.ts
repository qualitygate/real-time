import {Database, DatabaseImpl} from './Database'
import {DatabaseListeners} from './DatabaseListeners'
import {ConnectionProvider} from '../connection/ConnectionProvider'
import {LoggerImpl} from '../utils/Logger'

export * from './Database'
export * from '../connection/ConnectionOptions'
export * from './DatabaseStatus'
export * from './DatabaseStatusListener'

/**
 * Creates a new database instance.
 *
 * @param name {string}: Name of the database.
 * @returns {Database} being the new database created and ready to use.
 */
export function createDatabase(name: string): Database {
	const listeners = new DatabaseListeners()
	const connectionProvider = new ConnectionProvider()

	return new DatabaseImpl(name, listeners, connectionProvider, new LoggerImpl(name))
}