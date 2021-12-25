# Real time web

This library is meant to be used paired with a ASPNET Core API service using that uses the
[QualityGate.RealTime](https://www.nuget.org/packages/QualityGate.RealTime) Nuget package. Along with mentioned package,
this library allows a web application to register Data queries, through which, changes on domain entities in the
mentioned ASPNET Core API service's database can be received as notifications in real-time, and in this way, the web
application can reflect those changes in its UI.

Please, read more information about the **QualityGate.RealTime** package, because it fuels the functionality of this
library.

**NOTE:** Please note that all the code examples below are using *Typescript* language.

## Requirements for usage

Inside your NPM project:

- Install this library's package using NPM: `npm install @qualitygate/real-time` or
  Yarn `yarn add @qualitygate/real-time`.
- That's it. See examples below to know how to use it concretely.

The `@qualitygate/real-time` package is hosted [here](https://www.npmjs.com/package/@qualitygate/real-time) in the NPM
registry.

## Requirements for contributions

To contribute to this library, the contributions are meant to be compiled and published to the Npm registry for web
applications to import it (whether it's a React, Angular, Vue or vanilla Javascript/Typescript app). Follow the steps
below to set the development environment:

- Make sure you have installed [nodejs](https://nodejs.org/en).
- An IDE like [WebStorm](https://www.jetbrains.com/webstorm) or Text Editor like
  [Visual Studio code](https://code.visualstudio.com).
- Clone the project, on a terminal, just issue: `git clone https://github.com/qualitygate/real-time.git`. Or use the IDE
  to do so.
- Go to the `Npm` directory where this project is.
- And install dependencies running on the terminal: `yarn install`.

## Testing the project

The project uses [Jest](https://jestjs.io). To run the tests, just standing in this project's directory, run: `jest`.

## How it works

The whole functionality of the current project revolves around the `Database` interface (with the `DatabaseImpl` as the
default implementation). It represents a database API to be used on the browser, through which, you can register the
queries to want to receive real-time data changes notifications from.

To use it, in your application at the initialization point, create a new `Database` instance:

```typescript
import {createDatabase, ConnectionOptions} from '@qualitygate/real-time'

// This is a convenience factory method that allows for easy instatiation of the Database interface's default implementation
const database = createDatabase('Database #1')

const options: ConnectionOptions = {
	// URL where the ASPNET Core API service exposing the database endpoint
	url: 'http://localhost:8080/some-path',
	// Completely optional. Place here the token that goes on Authorization header to authenticate the connection request
	getToken: () => 'some-authentication-token'
}
database.initialize(options) // Returns a Promise that resolves when the initialization finishes
```

The initialization sends a command to the backing ASPNET Core API service (having the **QualityGate.RealTime** package
installed, configured and exposing an expected SignalR endpoint). This command establishes the connection between both
ends and the data synchronization begins to flow. To avoid bringing the whole database content to the browser, this
library uses a concept called: `Query` which a JSON representation of what comes to be a SQL query on a standard
Relational Database. With those, the desired entities we want on the web-application is narrowed by relevance. In short,
think of them as normal Queries, which resultant entities you would like to display in the UI.

**NOTE:** Please, DO NOT register a query twice, or several queries with the same name, that's not allowed.

### Standard queries

To show how the query registration goes, lets do it using an example:

```typescript
// Registers a query that gets all users (an example entity, from the table 'users') named as 'John' and with age 30
// years, order by Age first and Name second in ascending order.
import {Query, Equal} from '@qualitygate/real-time'

const query: Query = {
	name: 'all-john-users', // Unique name all queries must have
	table: 'users',
	conditions: [
		{field: 'name', operator: Equal, value: 'John'},
		{field: 'nge', operator: Equal, value: 30}
	],
	orderBy: {fields: ['age', 'name'], ascending: true}
}

// Assuming a User entity exists in the project, this function will be invoked by the query when a User entity gets
// added, deleted or updated and that matches the query criteria. If so, the backend query sends all the results back
// again. Internally it does a merging algorithm that attempts not to recreate all the User entities its results have,
// and only adds, deletes, or modifies the changed entity. This makes for better use of Reach re-renders (for example)
// and avoid each time invalidating the whole list of users, when only one User was changed.
function updateUserResults(newUsers: User[]) {
	// Do some logic to update the UI, maybe update a visible list of users
	// ...
}

database.addQuery(query, userDateUserResults)
```

The previous code registers the query internally in the database (locally in the browser), and also in the backend
(by using the endpoint exposed by the backend service using the **QualityGate.RealTime** package). When on the backend,
a change on an entity matching the previous criteria is detected, then such changed entity gets pushed to the browser.
The registered query on the browser invokes its given function that receives the new results (in the example above, the
function: `updateUserResults`).

### Paginated queries

In case you want to render in your application a paginated list of entities (users in the examples before), you will be
in need of a Paginated query. Follow the example below:

```typescript
// Having done all from the previous examples:
// 1. Having a database created.
// 2. And successfully initialized

import {PageInfo} from '@qualitygate/real-time'

// Let's copy and mofify a bit the previous query and add the pagination parameters
const paginatedQuery = {...query, page: 1, size: 8}

// This function works as the `updateUserResults` in the example above, just notice that it receives instead of the raw
// array of users beign the result of the query, a PageInfo<User> object, which has the User entities but also
// pagination information, such as: page, size, total users and user instances of the current page.
function updatePageResults(newPage: PageInfo<User>) {
	// ...
}

// Register the paginated query and pass the function to receive the data change notifications
database.addPageQuery(paginatedQuery, updatePageResults)
```

### Reconnection

If at some point your application looses connection with the backend, the SignalR socket that gets opened when the
database gets initialized is configured to automatically reconnect (or attempt it every 10 seconds). When the connection
is back your queries get registered again (using the same process explained above) and their results get fetched again.
On the backend, the queries registered from this browser get removed if the connection breaks.

### Stop querying

If you are moving away from your page, and you need to destroy the current resources (the queries), they can be removed,
effectively breaking the synchronization cycle, and allowing you to re-register them back later.

Use `removeQuery(...)` function to do that:

```typescript
// Unregister a query, by its name
database.removeQuery(query.name)
```

## Usage in React projects

This library was designed to be imported in React projects. It provides the following members to allow easy usage:

* `DatabaseProvider`: A Context provider HoC to be placed in your component tree to allow then accessing database
  instances. You can request creating or using existing databases as many as you like, just make sure they have
  different names.
* `useDatabase`: A hook to be used on the functional components below a `DatabaseProvider` in the hierarchy.

See the following example for more details :

Complex example alert!!!

```typescript jsx
import React from 'react'

import {DatabaseProvider, DatabaseStatus, DatabaseOptions, useDatabase} from '@qualitygate/real-time'

const QUERY_NAME = 'all-users'

// Following the same example entity: User.
interface User {
	name: string
	age: string
}

function UserList() {
	// Let's connect to the database called "users". Here the database instance is created, or re-used if it exists already
	const database = useDatabase('users')

	// Let's use a state for a one time initialization round
	const [isInitializing, setIsInitializing] = React.useState<boolean>(true)

	// This state holds the current query results
	const [users, setUsers] = React.useState([])

	// This effect is meant to run only once, and it connects to the 'users' database and register the desired query
	React.useEffect(() => {
		if (!isInitializing) return

		(async () => {
			try {
				const databaseOptions: Partial<DatabaseOptions> = {
					// Random URL, but it should point to the exposed endpoint on the backend
					url: `http://localhost:27654/some-randome-path`,
					// Uncomment below if needing authentication on database connection request authentication, and pass the correct
					// token
					// getToken: () => ''
				}

				// Initialize the database connection
				await database.initialize(databaseOptions)
				console.debug('users database connected')

				async function onDatabaseStatusChanged(status: DatabaseStatus) {
					// Only add/remove queries when connection is up
					if (status !== CONNECTED) return

					// Connect the query and register the setUsers state setter to be updated with changes in the users
					// matching the the query criteria
					const query: Query = {
						table: 'users',
						name: QUERY_NAME,
						conditions: [
							{field: 'name', operator: Equal, value: 'John'},
							{field: 'age', operator: Equal, value: 30}
						],
						orderBy: {fields: ['age', 'name'], ascending: false}
					}

					try {
						await database.addQuery(query, setVouchers)
						setIsInitializing(true)
					} catch (error) {
						// Or deal with the error that prevented so
					}
				}

				// After the database is connected, we register a listener to get notified about when the database
				// connection gets successfully established
				await database.listeners.register({
					id: QUERY_NAME,
					// This callback is invoked when the database connection status changes
					fn: onDatabaseStatusChanged
				})
			} catch (error) {
				console.error(error)
				throw error
			}
		})()
	}, [isInitializing])

	// This effect cleans up the database connection when this component gets destroyed (not re-rendered, or too many
	// reconnections would trigger instead)
	React.useEffect(() => {
		return () => {
			(async () => {
				// Remove the database connection listener and the query
				await database.listeners.unregister(QUERY_NAME)
				await database.removeQuery(QUERY_NAME)

				// And destroy the database resources and backend connection
				await database.dispose()

				console.debug('users database disconnected')
			})()
		}
	}, []) // <--- Empty dependencies array to make the effect is executed on component's destruction

	return (
		<ul>
			{users.map(user => {
				<li key={Math.random()}>
					Name: {user.name}, Age: {user.age}
				</li>
			})}
		</ul>
	)
}

export function RootComponent() {
	// Let's place a DatabaseProvider at the top of the component tree, so every children can get access to the database
	return (
		<DatabaseProvider>
			<UserList/>
		</DatabaseProvider>
	)
}
```

## License

MIT License. Anyone can fork, use or contribute to this project at will. All the contributions are pretty much
appreciated.

## Changelog

Latest changes are tracked at the [CHANGELOG.md](https://github.com/qualitygate/real-time/blob/main/Npm/CHANGELOG.md)