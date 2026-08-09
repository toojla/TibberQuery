using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace TqTool.Configuration;

public static class CommandLineBuilderFactory
{
	private const int _maxDefaultHoursConst = 12;
	private const int _defaultDaysConst = 5;

	public static RootCommand BuildRootCommand(ServiceProvider serviceProvider)
	{
		var inputHoursOption = new Option<int?>("-hrs")
		{
			Description = $"Get price for n number of hours forward (maximum {_maxDefaultHoursConst}hrs)",
			DefaultValueFactory = _ => _maxDefaultHoursConst
		};

		var maxInputOptions = new Option<bool>("-max")
		{
			Description = "Get price for maximum number of hours forward"
		};

		var inputDaysOption = new Option<int?>("-days")
		{
			Description = "Get consumption for given number of days",
			DefaultValueFactory = _ => _defaultDaysConst
		};

		var priceCommand = new Command("price", "Gets the price") { inputHoursOption, maxInputOptions };
		var ownerCommand = new Command("owner", "Gets owner information");
		var homesCommand = new Command("homes", "Gets homes information");
		var costCommand = new Command("cost", "Gets consumption information") { inputDaysOption };

		var rootCommand = new RootCommand("Gets information from Tibber api");

		rootCommand.Add(priceCommand);
		rootCommand.Add(ownerCommand);
		rootCommand.Add(homesCommand);
		rootCommand.Add(costCommand);

		priceCommand.SetAction(async (parseResult, _) =>
		{
			var hours = parseResult.GetValue(inputHoursOption);
			var max = parseResult.GetValue(maxInputOptions);
			await GetPriceAsync(serviceProvider, hours, max);
		});

		ownerCommand.SetAction(async (_, _) =>
		{
			await GetOwnerAsync(serviceProvider);
		});

		homesCommand.SetAction(async (_, _) =>
		{
			await GetHomesAsync(serviceProvider);
		});

		costCommand.SetAction(async (parseResult, _) =>
		{
			await GetConsumptionAsync(serviceProvider, parseResult.GetValue(inputDaysOption));
		});

		return rootCommand;
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
