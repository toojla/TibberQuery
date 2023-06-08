using Microsoft.Extensions.Logging;
using TqTool.Features.Consumption;
using TqTool.Features.Owner;
using TqTool.Features.Price;

namespace TqTool;

public class CommandLineHandler : ICommandLineHandler
{
	private readonly ILogger<CommandLineHandler> _logger;
	private readonly IOwnerService _ownerService;
	private readonly IPriceService _priceService;
	private readonly IConsumptionService _consumptionService;

	public CommandLineHandler(IOwnerService ownerService,
		IPriceService priceService,
		IConsumptionService consumptionService,
		ILogger<CommandLineHandler> logger)
	{
		_ownerService = ownerService;
		_priceService = priceService;
		_consumptionService = consumptionService;
		_logger = logger;
	}

	public async Task GetHomesAsync()
	{
		try
		{
			_logger.LogDebug("Trying to get homes from service...");
			var homes = (await _ownerService.GetOwnerHomesAsync()).ToList();
			_logger.LogDebug($"Found {homes.Count} homes!");
			var firstHome = homes.FirstOrDefault();

			Console.WriteLine($"Showing first home (only): {firstHome?.Address.Address1} {firstHome?.Address.City}");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex.Message);
		}
	}

	public async Task GetOwnerAsync()
	{
		try
		{
			_logger.LogDebug("Trying to get owner from service...");
			var owner = await _ownerService.GetOwnerAsync();

			Console.WriteLine($"Showing the owner: {owner.Name}");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex.Message);
		}
	}

	public async Task GetPriceAsync(int hours)
	{
		try
		{
			_logger.LogDebug("Trying to get prices from service...");
			var priceSummary = await _priceService.GetPriceAsync(hours);

			Console.WriteLine("All prices are in öre");
			Console.WriteLine($"Current price: {priceSummary.CurrentPrice.Price} (tax: {priceSummary.CurrentPrice.Tax})");
			Console.WriteLine($"Upcoming prices for next {hours} hours...");

			foreach (var upcomingPrice in priceSummary.UpcomingPrices)
			{
				Console.WriteLine($"{upcomingPrice.StartsAt}, {upcomingPrice.Price} (tax: {upcomingPrice.Tax})");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex.Message);
		}
	}

	public async Task GetConsumptionAsync(int days)
	{
		try
		{
			_logger.LogDebug("Trying to get consumption from service...");
			var consumptionViewModel = await _consumptionService.GetConsumptionAsync(days);

			Console.WriteLine($"Found {consumptionViewModel.NumberOfDaysBack} consumption prices for the last {days} days");

			if (consumptionViewModel.ConsumptionDays.Any())
			{
				foreach (var consumptionDay in consumptionViewModel.ConsumptionDays)
				{
					Console.WriteLine($"{consumptionDay.Day.ToShortDateString()}, {consumptionDay.Cost} kr " +
									  $"(Avg. price {consumptionDay.AveragePrice} kr/{consumptionDay.ConsumptionUnit}), " +
									  $"consumed: {consumptionDay.Consumption} {consumptionDay.ConsumptionUnit}");
				}
			}
			else
			{
				Console.WriteLine($"Could not find any prices for the last {days} days");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex.Message);
		}
	}
}