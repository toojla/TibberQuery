using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using TqTool.Configuration;

namespace TqTool;

public class Program
{
	private static IConfigurationRoot? _configuration;
	private static ServiceProvider? _serviceProvider;

	private static async Task<int> Main(string[] args)
	{
		_configuration = SetupConfiguration.InitConfiguration();
		_serviceProvider = SetupConfiguration.ConfigureServices(_configuration).BuildServiceProvider();

		var rootCommand = CommandLineBuilderFactory.BuildRootCommand(_serviceProvider);

		return await rootCommand.Parse(args).InvokeAsync();
	}
}
