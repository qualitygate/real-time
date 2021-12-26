# RealTime backend

This is a library for ASPNET Core API services that expose data to web-apps and want that data to become real-time
updated. It was design to back frontend apps using the [@qualityGate/real-time](https://www.npmjs.com/package/@qualitygate/real-time)
Npm package. Web apps using that package will send commands to this library establishing definitions of queries which
results they want to get updated in real time. Continue reading to understand further.

Please, read more information about the **@qualityGate/real-time** package, because this library exposes the API for
to consume by frontend apps using that package.

**NOTE:** This library today works only with [RavenDB](https://ravendb.net/) as backend. All the content below assumes
you are using it to save and query the data.

## Requirements for usage

Inside your AST.NET Core project:
- If using Visual Studio, use **Nuget Package Manager** to install the library, search for [QualityGate.RealTime](https://www.nuget.org/packages/QualityGate.RealTime)
and install it.
- Or by using the terminal, stand on your project's directory and issue the command: `dotnet add QualityGate.RealTime`.
- Once package is installed, find your *Startup.cs* script, and add the following setup code to the `ConfigureServices`
method (or the one that plays this role):
```csharp
public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Important to pass application configuration to methods below. More details later.
        services.AddDefaultCors(_configuration);
        services.AddRealTime(_configuration);
        
        // ...
    }
}
```
- Update the application configuration, by changing your **appsettings.json** as below:
```json
{
  "Database": {
    "Url": "http://localhost:7080",
    "Name": "accounting"
  }
}
```
The settings are explained below.
- Run your application, and you should be ready to start seeing results in your web frontend (assuming that you already
defined the queries there). How to define queries on the frontend, is explained on the
[@qualitygate/real-time docs](https://github.com/qualitygate/real-time/tree/main/Npm#how-it-works).

## Settings

- `Database.Url`: Url where the database system is published.
- `Database.Name`: The name of the database in the backend to aim queries to.

## Requirements for contributions

To contribute to this library, the contributions are meant to be compiled and published to the Nuget registry for ASPNET
Core applications to import it. Follow the steps below to set the development environment:

- Make sure you have installed [dotnet](https://dotnet.microsoft.com/en-us/download).
- An IDE like [Visual Studio](https://visualstudio.microsoft.com/downloads/) or Text Editor like
  [Visual Studio code](https://code.visualstudio.com).
- Clone the project, on a terminal, just issue: `git clone https://github.com/qualitygate/real-time.git`. Or use the IDE
  to do so.
- And install dependencies running on the terminal (if not using the IDE): `dotnet restore`.
- Include your contribution.
- Submit a PR to this project.
- Notify this library's author for the new package's version publication.

## Testing the project

The project uses [MSTest](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest).
To run the tests, just standing in this project's directory, run: `dotnet test`. You will find in the **RealTime.Tests**
project all the written tests, specially some checking the RavenDB integration, which use a test driver provided by the RavenDB
authors that runs an in-memory database instance.

## How it works

When your ASPNET Core application starts (having all the above-mentioned set), it will expose through
[SignalR](https://dotnet.microsoft.com/en-us/apps/aspnet/signalr) a Hub that contains the API through which web-apps
will register/modify/delete queries, and receive query results change notifications in real-time. Also, on the
application start-up process, a change observer is registered on the RavenDB connection, to watch for document (or
domain entity) changes, so that these changes can be channelled towards the browser which defined queries where this
document is a relevant result. 

When a query gets registered, it's definition is saved locally, and it's calculated results (depending on the query type,
more on this below) will be sent to the client, for the first render of such results (domain entities or database rows
if you will) can take place. Modifications will also trigger data notifications, adding dynamic behavior to the query
definition and allow frontend developers do scenarios such as table pagination. When a query gets deleted, its
definition gets removed, and no more data notification for its results get sent to any client.

Queries are saved along with an identifier that allows to differentiate from which SignalR connection they got from
(you can think of it as a browser session). This allows for granularity in the way queries get modified, where for
example: two browser sessions can register the same paginated query, but be seeing different results, due to the `page`
and page `size` chosen.

When the connection to a browser session gets closed, all queries registered for that browser will be removed
therefore stopping data notifications. If the connection comes back, the registration process is started again with the
consequent query results notifications. With this, in the face of an outage, no further work is needed.

## Query types

Currently, there are supported two types of queries: *Paginated* and *Standard*.
- **Paginated queries**: Are those which results are sliced by using two arguments in the query definition `page` and
`size` (see more on this in the 
[@qualitygate/real-time docs](https://github.com/qualitygate/real-time/tree/main/Npm#paginated-queries)), effectively
sending, from the whole query result, only the elements from `(page * size)`th element to the
`(page * size + size)`th one.
- **Standard queries**: These are the most common ones, and will return their full results without any
slicing/pagination.

## Data notification

What happens when a document/entity changes? The algorithm is explained below.

### For standard queries

1. The database backend notifies the `ChangeObserver` about the change, it brings which entity (identified by its ID),
from which Table/Collection name the change came, and which operation it suffered (an upsert or deletion).
2. In case it's an upset operation, all the registered queries get asked whether the new version of the entity still
matches their criteria. If so, an *entity changed* notification is sent to the client along with the new version of the
entity. If the entity stops matching any criteria of some queries, a deletion notification is sent to the clients
forcing them to delete also the entity from their results in the frontend.
3. In case it's a deletion operation, all relevant query clients get notified about the deletion.

### For paginated queries

1. As above, the database backend notifies the `ChangeObserver` about the change.
2. Whatever the change the entity suffered, a *page changed* notification is sent to the paginated queries clients that
want to listen changes on the entities of the same type as the changed one. The notification carries the new pagination
result entirely, with the changed entity included (or not if the entity stop being part of the paginated query result).

## License

MIT License. Anyone can fork, use or contribute to this project at will. All contributions are very much appreciated
and welcome.

## Changelog

Latest changes are tracked at the [CHANGELOG.md](https://github.com/qualitygate/real-time/blob/main/CHANGELOG.md)