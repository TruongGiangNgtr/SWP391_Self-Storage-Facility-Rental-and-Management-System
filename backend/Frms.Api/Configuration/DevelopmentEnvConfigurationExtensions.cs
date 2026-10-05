using DotNetEnv;
using DotNetEnv.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Memory;

namespace Frms.Api.Configuration;

public static class DevelopmentEnvConfigurationExtensions
{
    public static IConfigurationBuilder AddDevelopmentEnvFile(this IConfigurationBuilder configuration, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment()) return configuration;

        var path = Path.Combine(environment.ContentRootPath, ".env");
        if (!File.Exists(path)) return configuration;

        MemoryConfigurationSource source;
        try
        {
            // Parse with the package provider without changing process environment variables.
            using var localConfiguration = new ConfigurationManager();
            localConfiguration.AddDotNetEnv(path, LoadOptions.NoEnvVars());
            source = new MemoryConfigurationSource { InitialData = localConfiguration.AsEnumerable().ToArray() };
        }
        catch (Exception)
        {
            // Parser exceptions can contain file contents; do not expose credentials in startup logs.
            throw new InvalidOperationException("Unable to load backend .env configuration. Check file access and dotenv syntax.");
        }

        // Keep the default unprefixed environment and command-line providers above the local file.
        var index = 0;
        while (index < configuration.Sources.Count &&
               configuration.Sources[index] is not EnvironmentVariablesConfigurationSource { Prefix: null or "" })
        {
            index++;
        }
        configuration.Sources.Insert(index, source);
        return configuration;
    }
}
