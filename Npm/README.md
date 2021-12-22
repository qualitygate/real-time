# Real time web

This library is meant to be used paired with a ASPNET Core API service using that uses the
[QualityGate.RealTime](https://www.nuget.org/packages/QualityGate.RealTime) Nuget package. Along with mentioned package,
this library allows a web application to register Data queries, through which, changes on domain entities in the
mentioned ASPNET Core API service's database can be received as notifications in real-time, and in this way, the web
application can reflect those changes in its UI.

Please, read more information about the
[QualityGate.RealTime](https://github.com/qualitygate/real-time/blob/main/README.md) Nuget package, as such it fuels the
functionality of this library.

**NOTE:** Please note that all the code examples below are using *Typescript* language.

## Requirements

This project represents a simple library that is to be compiled and published to the Npm registry for web applications
to import (whether it's a React, Angular, Vue or vanilla Javascript/Typescript app). To start contributing to this
library just:

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
import {createDatabase} from './index'
import {ConnectionOptions} from './connection/ConnectionOptions'

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
Relational Database. With those the desired entities we want on the web-application is narrowed by relevance. In short,
think of them as normal Queries, which resultant entities you would like to display in the UI.

**NOTE:** Please, DO NOT register a query twice, or several queries with the same name, that's not allowed.

### Standard queries

To show how the query registration goes, lets do it using an example:

```typescript
// Registers a query that gets all Users (an example entity, from the table 'Users') named as 'John' and with age 30
// years, order by Age first and Name second in ascending order.
import {Query} from './Query'
import {Equal} from './Operator'

const query: Query = {
	name: 'all-john-users', // Unique name all queries must have
	table: 'Users',
	conditions: [
		{field: 'Name', operator: Equal, value: 'John'},
		{field: 'Age', operator: Equal, value: 30}
	],
	orderBy: {fields: ['Age', 'Name'], ascending: true}
}

// Assuming a User entity exists in the project, this function will be invoked by the query when a User entity gets
// added, deleted or updated and that matches the query criteria. If so, the backend query sends all the results back
// again. Internally it does a merging algorithm that attempts not to recreate all the User entities its results have,
// and only adds, deletes, or modifies the changed entity. This makes for better use of Reach re-renders (for example)
// and avoid each time invalidate the whole list of Users, when only one User was changed.
function updateUserResults(newUsers: User[]) {
	// Do some logic to update the UI, maybe a visible list of Users
	// ...
}

database.addQuery(query, userDateUserResults)
```

The previous code registers the query internally in the database (locally in the browser), and also in the backend
(by using the endpoint exposed by the backend service using the QualityGate.RealTime package). When on the backend, a
change on an entity matching the previous criteria is detected, then such changed entity gets pushed. The registered
query on the browser invokes its given function that receives the new results (in the example above, the function:
`updateUserResults`.

### Paginated queries

In case you want to render in your application a paginated list of entities (Users in the examples before), you will be
in need of a Paginated query. Follow the example below:

```typescript
// Having dome all from the previous example:
// 1. Having a database created.
// 2. And successfully initialized

// Let's copy and mofify a bit the previous query and add the pagination parameters
import {PageInfo} from './PageInfo'

const paginatedQuery = {...query, page: 1, size: 8}

// This function works as the `updateUserResults` in the example above, just notice that it instead of receiving the raw
// array of Users beign the result of the query, it receives a PageInfo<User> object, which has the User entities but
// also pagination information, such as: page, size, total users and user instances of the current page.
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
On the backend, anyway the queries registered from this browser get removed if the connection breaks.

### Stop querying

If you are changing the current page, and you need to destroy the current resources, the queries, they can be removed,
effectively breaking the synchronization cycle, and allowing you to re-register them back later.

To do that, just:

```typescript
// Unregister a query, by its name
database.removeQuery(query.name)
```