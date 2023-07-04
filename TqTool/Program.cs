using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using TqTool.Configuration;

namespace TqTool;

public class Program
{
	private static IConfigurationRoot _configuration;
	private static ServiceProvider _serviceProvider;
	private const int _maxDefaultHoursConst = 12;
	private const int _defaultDaysConst = 5;

	private static async Task Main(string[] args)
	{
		_configuration = SetupConfiguration.InitConfiguration();

		var runner = BuildRootCommand()
			.UseHost(_ => SetupConfiguration.CreateHostBuilder(args), (builder) => builder
				.UseSerilog()
				.ConfigureServices((c, s) =>
				{
					_serviceProvider = SetupConfiguration.ConfigureServices(_configuration).BuildServiceProvider();
				})
				.UseDefaultServiceProvider((context, options) =>
				{
					options.ValidateScopes = true;
				}))
			.UseDefaults().Build();

		await runner.InvokeAsync(args);
	}

	private static CommandLineBuilder BuildRootCommand()
	{
		var inputHoursOption = new Option<int?>(
			name: "-hrs",
			getDefaultValue: () => _maxDefaultHoursConst,
			description: $"Get price for n number of hours forward (maximum {_maxDefaultHoursConst}hrs)");

		var maxInputOptions = new Option<bool>(
			name: "-max",
			description: "Get price for maximum number of hours forward");

		var inputDaysOption = new Option<int?>(
			name: "-days",
			getDefaultValue: () => _defaultDaysConst,
			description: "Get consumption for given number of days");

		var priceCommand = new Command("price", "Gets the price") { inputHoursOption, maxInputOptions };
		var ownerCommand = new Command("owner", "Gets owner information");
		var homesCommand = new Command("homes", "Gets homes information");
		var costCommand = new Command("cost", "Gets consumption information") { inputDaysOption };

		var rootCommand = new RootCommand("Gets information from Tibber api");

		rootCommand.AddCommand(priceCommand);
		rootCommand.AddCommand(ownerCommand);
		rootCommand.AddCommand(homesCommand);
		rootCommand.AddCommand(costCommand);

		priceCommand.SetHandler(async (hours, max) =>
		{
			await GetPriceAsync(hours!, max);
		}, inputHoursOption, maxInputOptions);

		ownerCommand.SetHandler(async () =>
		{
			await GetOwnerAsync();
		});

		homesCommand.SetHandler(async () =>
		{
			await GetHomesAsync();
		});

		costCommand.SetHandler(async days =>
		{
			await GetConsumptionAsync(days);
		}, inputDaysOption);

		return new CommandLineBuilder(rootCommand);
	}

	private static async Task GetOwnerAsync()
	{
		var commandLineHandler = _serviceProvider.GetRequiredService<ICommandLineHandler>();
		await commandLineHandler.GetOwnerAsync();
	}

	private static async Task GetHomesAsync()
	{
		var commandLineHandler = _serviceProvider.GetRequiredService<ICommandLineHandler>();
		await commandLineHandler.GetHomesAsync();
	}

	private static async Task GetConsumptionAsync(int? days)
	{
		var commandLineHandler = _serviceProvider.GetRequiredService<ICommandLineHandler>();
		var daysInt = days ?? _defaultDaysConst;
		await commandLineHandler.GetConsumptionAsync(daysInt);
	}

	private static async Task GetPriceAsync(int? hours, bool maxInput)
	{
		if (hours is > _maxDefaultHoursConst or < 1)
		{
			hours = _maxDefaultHoursConst;
		}

		if (maxInput)
		{
			hours = _maxDefaultHoursConst;
		}

		var calculatedHours = hours ?? _maxDefaultHoursConst;

		var commandLineHandler = _serviceProvider.GetRequiredService<ICommandLineHandler>();
		await commandLineHandler.GetPriceAsync(calculatedHours);
	}
}