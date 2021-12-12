import {each, has, valuesIn} from 'lodash'

import {DatabaseStatus, DISCONNECTED} from '../database'
import {DatabaseStatusListener} from './DatabaseStatusListener'

/**
 * An object that is associated to each database, to which its clients will register/unregister listeners to get
 * notified about database status changes.
 */
export class DatabaseListeners {
	private readonly _listeners: { [id: string]: (status: DatabaseStatus) => void } = {}
	private _status: DatabaseStatus = DISCONNECTED

	/**
	 * Register the given listener if it doesn't exist already. It also invokes it notifying the current state of the
	 * database.
	 * @param listener {DatabaseStatusListener} The listener to register.
	 */
	public register(listener: DatabaseStatusListener) {
		if (has(this._listeners, listener.id)) return

		const fn = this._listeners[listener.id] = listener.fn
		fn(this._status)
	}

	/**
	 * Unregister the listener matches the provided id.
	 * @param listenerId {string} The identifier of the listener to unregister.
	 */
	public unregister(listenerId: string) {
		delete this._listeners[listenerId]
	}

	/**
	 * DO NOT USE THIS METHOD YOURSELF. It's intended for the associated database to invoke it, letting the listeners
	 * know about its status.
	 * @param status {DatabaseStatus} Current database status.
	 */
	public notify(status: DatabaseStatus) {
		this._status = status
		each(valuesIn(this._listeners), fn => fn(status))
	}
}