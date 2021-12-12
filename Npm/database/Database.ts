import {HubConnection, HubConnectionBuilder, HubConnectionState, IRetryPolicy} from '@microsoft/signalr'
import {Entity, Query} from '../contracts'
import {each, filter, find, has, indexOf, isNil} from 'lodash'
import {AddQuery, Change, ClientFunction, Delete, EntityChanged, RemoveQuery, ServerFunction, Upsert} from './protocol'
import {DatabaseListeners} from './DatabaseListeners'
import {DatabaseOptions} from './DatabaseOptions'
import {CONNECTED, DISCONNECTED} from './DatabaseStatus'

export interface Database {
	/**
	 * Determines whether the database has been completely initialized and ready to use.
	 */
	ready: boolean

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
	 * @param options {Partial<DatabaseOptions>}: Partial options to pass to the initialization process.
	 * @returns {Promise} that does the initialization process.
	 */
	initialize: (options: Partial<DatabaseOptions>) => Promise<void>

	/**
	 * Gets the associated listener collection.
	 */
	listeners: DatabaseListeners

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
	notifyChanges: (items: any[]) => void
	cache: any[]
}

export class DatabaseImpl implements Database {
	private _connection: HubConnection = null
	private _queries: { [query: string]: QueryEntry } = {}
	private readonly _inspector: DatabaseListeners
	private _logger = 'Database'
	private readonly _name: string
	private _retryPolicy: IRetryPolicy = {
		nextRetryDelayInMilliseconds(): number | null {
			return 1000
		}
	}

	constructor(name: string, inspector: DatabaseListeners) {
		this._name = name
		this._inspector = inspector
	}

	public get listeners(): DatabaseListeners {
		return this._inspector
	}

	public get name(): string {
		return this._name
	}

	public get ready(): boolean {
		return this._connection?.state === HubConnectionState.Connected
	}

	async addQuery<T>(query: Query, setItems: (i: T[]) => void): Promise<void> {
		if (!isNil(find(this._queries, q => q.name === query.name))) {
			this._warn(`Query: ${query.name} already exists. They can only be added once`)
			return
		}

		this._debug(`Adding query: ${query.name}, definition: ${JSON.stringify(query)}`)
		const connectionId = this._connection.connectionId
		this._queries[query.name] = {
			cache: [],
			connectionId: connectionId,
			name: query.name,
			notifyChanges: setItems,
			query
		}

		await this._sendMessage(AddQuery, query)
		this._debug(`Query: ${query.name}, connectionId: ${connectionId} added`)
	}

	async dispose(): Promise<void> {
		try {
			this._debug('Closing connection')
			await this._connection.stop()
			this._debug('Connection closed')
		} catch (e) {
			console.error('Error disconnecting database')
		} finally {
			this.listeners.notify(DISCONNECTED)
			this._queries = {}
		}
	}

	async initialize(options: Partial<DatabaseOptions>): Promise<void> {
		this._debug('Initializing database')

		if (this.ready) {
			this._warn('Cannot start twice the database')
			return
		}

		options ??= {}

		// Handle 401 responses
		try {
			this._debug('Creating connection builder')
			this._connection = new HubConnectionBuilder()
				.withAutomaticReconnect(this._retryPolicy)
				.withUrl(options.url, {
					withCredentials: false,
					headers: {Authorization: `Bearer ${options.getToken()}`}
				})
				.build()

			this._connection.onreconnected(() => this._reconnectQueries())
			this._on(EntityChanged, (queryName, entities) => this._onEntityChange(queryName, entities))

			this._debug('Starting connection')
			await this._connection.start()
			this.listeners.notify(CONNECTED)
			this._debug('Connection established')
		} catch (e) {
			console.log('Error connecting to database', e)
		}
	}

	async removeQuery(name: string): Promise<void> {
		if (!has(this._queries, name)) return
		delete this._queries[name]
		await this._connection.send(RemoveQuery, name)
	}

	private _debug(message) {
		console.debug(`${this._logger} - ${message}`)
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

	private _onEntityChange(queryName: string, changes: Change[]) {
		this._debug(`Received entities ${changes?.length ?? 0} change(s) for query: ${queryName}`)
		const query = find(this._queries, e => e.name === queryName)

		this._debug('Merging entities cache')
		query.cache = this._mergeEntities(query.cache, changes)

		this._debug('Notifying listening clients')
		query.notifyChanges(query.cache)
	}

	private async _reconnectQueries() {
		const entries = {...this._queries}

		this._queries = {}

		this._debug('Reconnecting queries')
		for (let queryName in entries) {
			const entry = entries[queryName]
			const query: Query = entry.query
			await this.addQuery(query, entry.notifyChanges)
		}
	}

	private async _sendMessage(message: ServerFunction, ...args: any[]): Promise<void> {
		await this._connection.send(message, ...args)
	}

	private _warn(message) {
		console.warn(`${this._logger} - ${message}`)
	}
}