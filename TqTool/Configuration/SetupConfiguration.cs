using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using TqTool.Features.Consumption;
using TqTool.Features.Owner;
using TqTool.Features.Price;
using TqTool.Infrastructure;

namespace TqTool.Configuration;

public static class SetupConfiguration
{
	public static IConfigurationRoot InitConfiguration()
	{
		var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
		var location = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program))?.Location);
		if (string.IsNullOrEmpty(location)) throw new InvalidOperationException("Could not determine application location.");

		// Later sources win. The file shipped beside the executable is only a default, so anything
		// stored by `tqtool config` overrides it; an environment specific file and then environment
		// variables still override that, which keeps debugging and one-off runs working as before.
		var configuration = new ConfigurationBuilder()
			.SetBasePath(location)
			.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
			.AddInMemoryCollection(ReadUserSettings())
			.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
			.AddEnvironmentVariables()
			.Build();
		return configuration;
	}

	private static IEnumerable<KeyValuePair<string, string?>> ReadUserSettings()
	{
		var (endpoint, token) = UserSettings.Default.Read();
		var settings = new Dictionary<string, string?>();

		// Blank entries are skipped so a half filled file cannot wipe out another source.
		if (!string.IsNullOrWhiteSpace(endpoint)) settings["apiEndpoint"] = endpoint;
		if (!string.IsNullOrWhiteSpace(token)) settings["apiToken"] = token;

		return settings;
	}

	public static IServiceCollection ConfigureServices(IConfigurationRoot configuration)
	{
		var logLevel = configuration["logLevel"] ?? "Debug";

		var services = new ServiceCollection();

		services.AddSingleton(TimeProvider.System);

		// Built on first resolve rather than eagerly, so --help works without any api credentials.
		services.AddScoped<IGraphQLClient>(_ => CreateGraphQlClient(configuration));
		services.AddScoped<IOwnerService, OwnerService>();
		services.AddScoped<IPriceService, PriceService>();
		services.AddScoped<IConsumptionService, ConsumptionService>();
		services.AddScoped<ICommandLineHandler, CommandLineHandler>();
		services.AddScoped<IPriceViewModelFactory, PriceViewModelFactory>();
		services.AddScoped<IConsumptionViewModelFactory, ConsumptionViewModelFactory>();
		services.AddScoped<IGraphClientWrapper, GraphClientWrapper>();
		services.AddLogging(configure => configure.AddConsole());
		SetLogLevel(logLevel, services);

		return services;
	}

	private static IGraphQLClient CreateGraphQlClient(IConfiguration configuration)
	{
		var endpoint = configuration["apiEndpoint"];
		var token = configuration["apiToken"];

		if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(token))
		{
			throw new InvalidOperationException(
				"apiEndpoint and apiToken are not configured. Run: tqtool config -token <token> -endpoint <url>  " +
				"(or set the apiEndpoint and apiToken environment variables).");
		}

		var graphQlHttpClient = new GraphQLHttpClient(endpoint, new NewtonsoftJsonSerializer());
		graphQlHttpClient.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

		return graphQlHttpClient;
	}

	private static void SetLogLevel(string logLevel, IServiceCollection services)
	{
		switch (logLevel)
		{
			case "Error":
				services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Error);
				break;

			case "Debug":
				services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);
				break;

			case "Information":
				services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information);
				break;

			default:
				services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Error);
				break;
		}
	}
}