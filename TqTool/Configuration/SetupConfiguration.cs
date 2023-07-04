using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
		var configuration = new ConfigurationBuilder()
			.SetBasePath(location)
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
			.AddEnvironmentVariables()
			.Build();
		return configuration;
	}

	public static IServiceCollection ConfigureServices(IConfigurationRoot configuration)
	{
		var token = configuration["apiToken"];
		var logLevel = configuration["logLevel"] ?? "Debug";
		var graphQlHttpClient = new GraphQLHttpClient(configuration["apiEndpoint"], new NewtonsoftJsonSerializer());
		graphQlHttpClient.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

		var services = new ServiceCollection()
			.AddMemoryCache();

		services.AddScoped<IGraphQLClient>(s => graphQlHttpClient);
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

	public static IHostBuilder CreateHostBuilder(string[] args)
	{
		var hostBuilder = Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((context, builder) =>
			{
				var location = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program))?.Location);
				builder.SetBasePath(location);
			});

		return hostBuilder;
	}
}