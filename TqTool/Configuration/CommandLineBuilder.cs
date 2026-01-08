using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Builder;

namespace TqTool.Configuration;

public static class CommandLineBuilderFactory
{
	private const int _maxDefaultHoursConst = 12;
	private const int _defaultDaysConst = 5;

	public static CommandLineBuilder BuildRootCommand(ServiceProvider serviceProvider)
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
			await GetPriceAsync(serviceProvider, hours!, max);
		}, inputHoursOption, maxInputOptions);

		ownerCommand.SetHandler(async () =>
		{
			await GetOwnerAsync(serviceProvider);
		});

		homesCommand.SetHandler(async () =>
		{
			await GetHomesAsync(serviceProvider);
		});

		costCommand.SetHandler(async days =>
		{
			await GetConsumptionAsync(serviceProvider, days);
		}, inputDaysOption);

		return new CommandLineBuilder(rootCommand);
	}

	private static async Task GetOwnerAsync(ServiceProvider serviceProvider)
	{
		var commandLineHandler = serviceProvider.GetRequiredService<ICommandLineHandler>();
		await commandLineHandler.GetOwnerAsync();
	}

	private static async Task GetHomesAsync(ServiceProvider serviceProvider)
	{
		var commandLineHandler = serviceProvider.GetRequiredService<ICommandLineHandler>();
		await commandLineHandler.GetHomesAsync();
	}

	private static async Task GetConsumptionAsync(ServiceProvider serviceProvider, int? days)
	{
		var commandLineHandler = serviceProvider.GetRequiredService<ICommandLineHandler>();
		var daysInt = days ?? _defaultDaysConst;
		await commandLineHandler.GetConsumptionAsync(daysInt);
	}

	private static async Task GetPriceAsync(ServiceProvider serviceProvider, int? hours, bool maxInput)
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

		var commandLineHandler = serviceProvider.GetRequiredService<ICommandLineHandler>();
		await commandLineHandler.GetPriceAsync(calculatedHours);
	}
}