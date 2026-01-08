using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using TqTool.Configuration;

namespace TqTool;

public class Program
{
	private static IConfigurationRoot? _configuration;
	private static ServiceProvider? _serviceProvider;

	private static async Task Main(string[] args)
	{
		_configuration = SetupConfiguration.InitConfiguration();
		_serviceProvider = SetupConfiguration.ConfigureServices(_configuration).BuildServiceProvider();

		var runner = CommandLineBuilderFactory.BuildRootCommand(_serviceProvider)
			.UseHost(_ => SetupConfiguration.CreateHostBuilder(args), (builder) => builder
				.UseSerilog()
				.UseDefaultServiceProvider((context, options) =>
				{
					options.ValidateScopes = true;
				}))
			.UseDefaults().Build();

		await runner.InvokeAsync(args);
	}
}