import {DatabaseListeners} from './DatabaseListeners'
import {DatabaseStatusListener} from './DatabaseStatusListener'
import {restore, SinonStub, stub} from 'sinon'
import {CONNECTED, DISCONNECTED} from './DatabaseStatus'

describe('DatabaseListeners', () => {
	let listener: DatabaseStatusListener = {
		id: 'Listener: #1',
		fn: stub()
	}

	let listeners: DatabaseListeners

	beforeEach(() => {
		listeners = new DatabaseListeners()
	})

	afterEach(() => {
		restore()
	})

	describe('Register listener', () => {
		it('registers the given listener once and only once, and invoke the listener with the current database status', () => {
			// When
			listeners.register(listener)

			// Then, the notification arrives to the listener, it should be DISCONNECTED since the database connection state
			// has not changed
			expect((listener.fn as SinonStub).calledOnceWithExactly(DISCONNECTED)).toBeTruthy()

			// When a second registration is done
			listeners.register(listener)

			// Then no other notification should have arrived
			expect((listener.fn as SinonStub).calledOnceWithExactly(DISCONNECTED)).toBeTruthy()
		})
	})

	describe('Unregister listener', () => {
		it('removes the given listener from the notifications, so they should not arrive to the listener ' +
			'anymore', () => {
			// Given a registered listener
			listeners.register(listener);
			(listener.fn as SinonStub).reset()

			// When, unregistering the listener and a notification comes
			listeners.unregister(listener.id)
			listeners.notify(CONNECTED)

			// Then, no notification should have been received by the listener
			expect((listener.fn as SinonStub).notCalled).toBeTruthy()
		})

		it('allows multiple un-registrations of the same listener without problems', () => {
			// Given a registered listener
			listeners.register(listener)

			// When, unregistering the listener several times should not end in an error
			listeners.unregister(listener.id)
			listeners.unregister(listener.id)
			listeners.unregister(listener.id)
			listeners.unregister(listener.id)
		})
	})

	describe('Notify listeners', () => {
		it('notifies all registered listeners of database state change', () => {
			// Given registered listeners
			const listener2: DatabaseStatusListener = {
				id: 'Listener: #2',
				fn: stub()
			}

			listeners.register(listener)
			listeners.register(listener2);
			(listener.fn as SinonStub).reset();
			(listener2.fn as SinonStub).reset()

			// When
			listeners.notify(CONNECTED)

			// Then
			expect((listener.fn as SinonStub).calledOnceWithExactly(CONNECTED)).toBeTruthy()
			expect((listener2.fn as SinonStub).calledOnceWithExactly(CONNECTED)).toBeTruthy()
		})
	})
})