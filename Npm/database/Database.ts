import {HubConnection, HubConnectionState} from '@microsoft/signalr'
import {Entity, PageInfo, Query} from '../contracts'
import {each, filter, find, has, indexOf, isNil} from 'lodash'
import {
	AddPageQuery,
	AddQuery,
	Change,
	ClientFunction,
	Delete,
	EntityChanged,
	ModifyQuery,
	PageChanged,
	RemoveQuery,
	ServerFunction,
	Upsert
} from './protocol'
import {DatabaseListeners} from './DatabaseListeners'
import {ConnectionOptions} from '../connection/ConnectionOptions'
import {CONNECTED, DISCONNECTED} from './DatabaseStatus'
import {ConnectionProvider} from '../connection/Connection'
import {Logger, LoggerImpl} from '../utils/Logger'

export interface Database {
	/**
	 * Determines whether the database has been completely initialized and ready to use.
	 */
	ready: boolean

	addPageQuery: <T>(pageQuery: Query, setPageInfo: (pageInfo: PageInfo<T>) => void) => Promise<void>

	/**
	 * Register the given query for real-time synchronization.
	 *
	 * @param query {Query}: Definition of a query, which represent the criteria to select a portion of entities of
	 * a certain type at which the current app is interested on.
	 * @param setItems {Function}: Invoked each time the result of the query evaluation changes, used to notify the new
	 * query resultant items. This function must be able to receive an array of elements as single parameter.
	 * @returns {Promise} that does the addition process.
	 */
	addQuery: <T>(query: Query, setItems: (items: T[]) => void) => Promise<void>

	/**
	 * Stops all queries from being synchronized and closes the connection to the remote SignalR endpoint.
	 * @returns {Promise} that does the disposal process.
	 */
	dispose: () => Promise<void>

	/**
	 * Initializes the database and establishes the connection with the remote SignalR endpoint from which queries data
	 * changes notifications will come.
	 * @param options {Partial<ConnectionOptions>}: Partial options to pass to the initialization process.
	 * @returns {Promise} that does the initialization process.
	 */
	initialize: (options: Partial<ConnectionOptions>) => Promise<void>

	/**
	 * Gets the associated listener collection.
	 */
	listeners: DatabaseListeners

	/**
	 * Modifies an existing query given its new definition.
	 *
	 * @param query: Query to modify.
	 * @return {Promise<void>} that executes the modification of the query.
	 */
	modifyQuery: (query: Query) => Promise<void>

	/**
	 * Gets the name of the database.
	 */
	name: string

	/**
	 * Stops the synchronization of data of the query named as specified.
	 * @param name {string}: Name of the query which data changes will stop getting notified.
	 * @returns {Promise} that does the initialization process.
	 */
	removeQuery: (name: string) => Promise<void>
}

interface QueryEntry {
	name: string
	query: Query,
	connectionId: string
	notifyChanges: (items: any) => void
	cache: any
	isPaged: boolean
}

export class DatabaseImpl implements Database {
	private _connection: HubConnection = null
	private _connectionProvider: ConnectionProvider
	private _queries: { [query: string]: QueryEntry } = {}
	private readonly _listeners: DatabaseListeners
	private readonly _logger: Logger
	private readonly _name: string

	constructor(name: string,
	            listeners: DatabaseListeners,
	            connectionProvider?: ConnectionProvider,
	            logger?: Logger) {
		this._logger = logger ?? new LoggerImpl(name)
		this._connectionProvider = connectionProvider ?? new ConnectionProvider()
		this._name = name
		this._listeners = listeners
	}

	public get listeners(): DatabaseListeners {
		return this._listeners
	}

	public get name(): string {
		return this._name
	}

	public get ready(): boolean {
		return this._connection?.state === HubConnectionState.Connected
	}

	async addPageQuery<T>(pageQuery: Query, setPageInfo: (pageInfo: PageInfo<T>) => void) {
		this._checkConnected()

		if (!isNil(find(this._queries, q => q.name === pageQuery.name))) {
			this._logger.warn(`Page query: ${pageQuery.name} already exists. They can only be added once.`)
			return
		}

		this._logger.debug(`Adding page query: ${pageQuery.name}, definition: ${JSON.stringify(pageQuery)}`)
		const connectionId = this._connection.connectionId
		this._queries[pageQuery.name] = {
			cache: {page: 0, size: 0, items: [], total: 0},
			connectionId: connectionId,
			isPaged: true,
			name: pageQuery.name,
			notifyChanges: setPageInfo,
			query: pageQuery
		}

		await this._sendMessage(AddPageQuery, pageQuery)
		this._logger.debug(`Page query: ${pageQuery.name}, connectionId: ${connectionId} added`)
	}

	async addQuery<T>(query: Query, setItems: (i: T[]) => void): Promise<void> {
		this._checkConnected()

		if (!isNil(find(this._queries, q => q.name === query.name))) {
			this._logger.warn(`Query: ${query.name} already exists. They can only be added once.`)
			return
		}

		this._logger.debug(`Adding query: ${query.name}, definition: ${JSON.stringify(query)}`)
		const connectionId = this._connection.connectionId
		this._queries[query.name] = {
			cache: [],
			connectionId: connectionId,
			isPaged: false,
			name: query.name,
			notifyChanges: setItems,
			query
		}

		await this._sendMessage(AddQuery, query)
		this._logger.debug(`Query: ${query.name}, connectionId: ${connectionId} added`)
	}

	async dispose(): Promise<void> {
		try {
			this._logger.debug('Closing connection')
			await this._connection.stop()
			this._logger.debug('Connection closed')
		} catch (e) {
			this._logger.error('Error disconnecting database', e)
		} finally {
			this.listeners.notify(DISCONNECTED)
			this._queries = {}
		}
	}

	async initialize(options: Partial<ConnectionOptions>): Promise<void> {
		this._logger.debug('Initializing database')

		if (this.ready) {
			this._logger.warn('Cannot start twice the database')
			return
		}

		options ??= {}

		try {
			this._logger.debug('Creating connection builder')
			this._connection = this._connectionProvider.createConnection(options)

			this._connection.onreconnected(() => this._reconnectQueries())
			this._on(EntityChanged, (queryName, entities) => this._onEntityChanged(queryName, entities))
			this._on(PageChanged, (queryName, pageInfo) => this._onPageChanged(queryName, pageInfo))

			this._logger.debug('Starting connection')
			await this._connection.start()
			this.listeners.notify(CONNECTED)
			this._logger.debug('Connection established')
		} catch (e) {
			// TODO Handle 401 responses
			this._logger.error('Error connecting to database', e)
		}
	}

	async modifyQuery(query: Query): Promise<void> {
		if (isNil(find(this._queries, q => q.name === query.name))) {
			this._logger.warn(`Query: ${query.name} does not exist.`)
			return
		}

		this._logger.debug(`Modifying query name: ${query.name}, with the new definition: ${JSON.stringify(query)}`)
		await this._sendMessage(ModifyQuery, query)
		this._queries[query.name].query = query
	}

	async removeQuery(name: string): Promise<void> {
		if (!has(this._queries, name)) return
		delete this._queries[name]
		await this._connection.send(RemoveQuery, name)
	}

	private _checkConnected() {
		if (this.ready) return

		throw new Error(
			'Database must be connected to register queries. Please initialize it first, and make sure it\'s so.'
		)
	}

	private _deleteById(entities: Entity[], change: Change) {
		const entityToRemove = find(entities, e => e.id === change.entity.id)
		if (isNil(entityToRemove)) return
		const index = indexOf(entities, entityToRemove)
		entities.splice(index, 1)
	}

	private _mergeEntities(entities: Entity[], changes: Change[]): Entity[] {
		// Removing deleted entities
		const entitiesCache = [...entities]
		const deletionChanges = filter(changes, c => c.type === Delete)
		each(deletionChanges, dc => this._deleteById(entitiesCache, dc))

		// Update the current entities
		const addChanges = filter(changes, c => c.type === Upsert)
		each(addChanges, ac => {
			const existingEntity = find(entitiesCache, e => e.id === ac.entity.id)
			const index = indexOf(entitiesCache, existingEntity)
			if (isNil(existingEntity))
				entitiesCache.push(ac.entity)
			else
				entitiesCache[index] = {...ac.entity}
		})

		return entitiesCache
	}

	private _on(clientMethod: ClientFunction, fn: (...args: any[]) => void) {
		this._connection.on(clientMethod, fn)
	}

	private _onEntityChanged(queryName: string, changes: Change[]) {
		this._logger.debug(`Received entities ${changes?.length ?? 0} change(s) for query: ${queryName}`)
		const query = find(this._queries, e => e.name === queryName)

		this._logger.debug('Merging entities cache')
		query.cache = this._mergeEntities(query.cache, changes)

		this._logger.debug('Notifying listening clients')
		query.notifyChanges(query.cache)
	}

	private _onPageChanged(queryName: string, pageInfo: PageInfo<any>) {
		this._logger.debug(`Received entities ${pageInfo.items?.length ?? 0} change(s) for query: ${queryName}`)
		const query = find(this._queries, e => e.name === queryName)

		this._logger.debug('Notifying listening clients')
		query.notifyChanges(pageInfo)
	}

	private async _reconnectQueries() {
		const entries = {...this._queries}

		this._queries = {}

		this._logger.debug('Reconnecting queries')
		for (let queryName in entries) {
			const entry = entries[queryName]
			const query: Query = entry.query
			if (entry.isPaged) {
				await this.addPageQuery(query, entry.notifyChanges)
			} else {
				await this.addQuery(query, entry.notifyChanges)
			}
		}
	}

	private async _sendMessage(message: ServerFunction, ...args: any[]): Promise<void> {
		await this._connection.send(message, ...args)
	}
}