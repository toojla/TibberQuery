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

		priceCommand.SetAction((parseResult, _) =>
		{
			var hours = CalculateHours(parseResult.GetValue(inputHoursOption), parseResult.GetValue(maxInputOptions));
			return RunAsync(serviceProvider, handler => handler.GetPriceAsync(hours));
		});

		ownerCommand.SetAction((_, _) => RunAsync(serviceProvider, handler => handler.GetOwnerAsync()));

		homesCommand.SetAction((_, _) => RunAsync(serviceProvider, handler => handler.GetHomesAsync()));

		costCommand.SetAction((parseResult, _) =>
		{
			var days = parseResult.GetValue(inputDaysOption) ?? _defaultDaysConst;
			return RunAsync(serviceProvider, handler => handler.GetConsumptionAsync(days));
		});

		return rootCommand;
	}

	public static int CalculateHours(int? hours, bool maxInput)
	{
		if (hours is > _maxDefaultHoursConst or < 1)
		{
			hours = _maxDefaultHoursConst;
		}

		if (maxInput)
		{
			hours = _maxDefaultHoursConst;
		}

		return hours ?? _maxDefaultHoursConst;
	}

	// Resolving the handler builds the api client, so a missing or invalid configuration surfaces
	// here rather than inside CommandLineHandler's own try/catch.
	private static async Task<int> RunAsync(ServiceProvider serviceProvider, Func<ICommandLineHandler, Task> run)
	{
		try
		{
			var commandLineHandler = serviceProvider.GetRequiredService<ICommandLineHandler>();
			await run(commandLineHandler);
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
			return 1;
		}
	}
}
