import React, {useContext} from 'react'
import {isNil} from 'lodash'
import {Database, DatabaseImpl} from '../database/Database'
import {DatabaseListeners} from '../database/DatabaseListeners'

interface Databases {
	[key: string]: Database
}

const databases: Databases = {}

let databasesContext = React.createContext<Databases>(databases)

export interface DatabaseProviderProps {
	children?: any
}

interface DatabaseProviderState {
	initialized: boolean
}

/**
 * Gets a reference to the nearest <DatabaseProvider/> element provided database instance.
 * @return {Database} instance provided by the nearest <DatabaseProvider/> up in the tree of the caller component.
 */
export function useDatabase(databaseName: string): Database {
	const databases = useContext<Databases>(databasesContext)
	let database = databases[databaseName]

	if (isNil(database)) {
		const listeners = new DatabaseListeners()
		database = databases[databaseName] = new DatabaseImpl(databaseName, listeners)
	}

	return database
}

/**
 * Include this component on the component tree where children will require and instance of a database.
 */
export class DatabaseProvider extends React.Component<DatabaseProviderProps, DatabaseProviderState> {
	state = {
		initialized: false
	}

	constructor(props) {
		super(props)
	}

	render() {
		const {children} = this.props

		return (
			<databasesContext.Provider value={databases}>
				{children}
			</databasesContext.Provider>
		)
	}
}
