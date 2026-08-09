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

		var tokenOption = new Option<string?>("-token", "--token")
		{
			Description = "Api token from developer.tibber.com"
		};

		var endpointOption = new Option<string?>("-endpoint", "--endpoint")
		{
			Description = "Api endpoint url from developer.tibber.com"
		};

		var showOption = new Option<bool>("-show", "--show")
		{
			Description = "Show where settings are stored and which of them are set"
		};

		var priceCommand = new Command("price", "Gets the price") { inputHoursOption, maxInputOptions };
		var ownerCommand = new Command("owner", "Gets owner information");
		var homesCommand = new Command("homes", "Gets homes information");
		var costCommand = new Command("cost", "Gets consumption information") { inputDaysOption };
		var configCommand = new Command("config", "Stores the api credentials for the installed tool")
		{
			tokenOption, endpointOption, showOption
		};

		var rootCommand = new RootCommand("Gets information from Tibber api");

		rootCommand.Add(priceCommand);
		rootCommand.Add(ownerCommand);
		rootCommand.Add(homesCommand);
		rootCommand.Add(costCommand);
		rootCommand.Add(configCommand);

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

		// Deliberately not routed through ICommandLineHandler: resolving that builds the api client,
		// which refuses to start without credentials - the very thing this command exists to supply.
		configCommand.SetAction(parseResult => RunConfig(
			parseResult.GetValue(tokenOption),
			parseResult.GetValue(endpointOption),
			parseResult.GetValue(showOption)));

		return rootCommand;
	}

	private static int RunConfig(string? token, string? endpoint, bool show)
	{
		var settings = UserSettings.Default;

		try
		{
			if (show)
			{
				var (storedEndpoint, storedToken) = settings.Read();

				Console.WriteLine($"Settings file: {settings.FilePath}");
				Console.WriteLine($"apiEndpoint  : {storedEndpoint ?? "<not set>"}");
				Console.WriteLine($"apiToken     : {DescribeToken(storedToken)}");
				return 0;
			}

			if (token == null && endpoint == null)
			{
				Console.Error.WriteLine("Nothing to do. Pass -token and/or -endpoint to store them, or -show to see what is set.");
				return 1;
			}

			settings.Save(endpoint, token);
			Console.WriteLine($"Saved to {settings.FilePath}");
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
			return 1;
		}
	}

	// The token is never echoed back, so a shared terminal or a pasted transcript cannot leak it.
	private static string DescribeToken(string? token) =>
		string.IsNullOrWhiteSpace(token) ? "<not set>" : $"set ({token.Length} characters)";

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
