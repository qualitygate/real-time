import * as React from 'react'
import {isNil} from 'lodash'
import {createDatabase, Database} from '../database'

interface Databases {
	[key: string]: Database
}

const databases: Databases = {}

let databasesContext = React.createContext<Databases>(databases)

export interface DatabaseProviderProps {
	children?: any
}

// noinspection JSUnusedGlobalSymbols
/**
 * Gets a reference to the nearest <DatabaseProvider/> element provided database instance.
 * @return {Database} instance provided by the nearest <DatabaseProvider/> up in the tree of the caller component.
 */
export function useDatabase(databaseName: string): Database {
	const databases = React.useContext<Databases>(databasesContext)
	let database = databases[databaseName]

	if (isNil(database)) {
		database = databases[databaseName] = createDatabase(databaseName)
	}

	return database
}

// noinspection JSUnusedGlobalSymbols
/**
 * Include this component on the component tree where children will require and instance of a database.
 */
export class DatabaseProvider extends React.Component<DatabaseProviderProps> {
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
