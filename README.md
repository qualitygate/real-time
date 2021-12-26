# RealTime backend

This is a library for ASPNET Core API services that expose data to web-apps and want that data to become real-time
updated. It was design to back frontend apps using the [@qualityGate/real-time](https://www.npmjs.com/package/@qualitygate/real-time)
Npm package. Web apps using that package will send commands to this library establishing definitions of queries which
results they want to get updated in real time. Continue reading to understand further.

Please, read more information about the **@qualityGate/real-time** package, because this library exposes the API for
to consume by frontend apps using that package.

## Requirements for usage

Inside your AST.NET Core project:
- If using Visual Studio, use **Nuget Package Manager** to install the library, search for [QualityGate.RealTime](https://www.nuget.org/packages/QualityGate.RealTime)
and install it.
- Or by using the terminal, stand on your project's directory and issue the command: `dotnet add QualityGate.RealTime`.
- Once package is installed, find your *Startup.cs* script, and add the following setup code you the `ConfigureServices`
method:
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
        // Important to the pass the application configuration to the methods below. More on the subject later.
        services.AddDefaultCors(_configuration);
        services.AddRealTime(_configuration);
        
        // ...
    }
}
```


## Changelog

Latest changes are tracked at the [CHANGELOG.md](https://github.com/qualitygate/real-time/blob/main/CHANGELOG.md)