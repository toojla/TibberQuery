using Microsoft.Extensions.Logging;
using TqTool.Features.Consumption;
using TqTool.Features.Owner;
using TqTool.Features.Price;

namespace TqTool.Configuration;

public class CommandLineHandler(
	IOwnerService ownerService,
	IPriceService priceService,
	IConsumptionService consumptionService,
	ILogger<CommandLineHandler> logger)
	: ICommandLineHandler
{
	public async Task GetHomesAsync()
	{
		try
		{
			logger.LogDebug("Trying to get homes from service...");
			var homes = (await ownerService.GetOwnerHomesAsync()).ToList();
			logger.LogDebug("Found {HomeCount} homes!", homes.Count);
			var firstHome = homes.FirstOrDefault();

			Console.WriteLine($"Showing first home (only): {firstHome?.Address.Address1} {firstHome?.Address.City}");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Could not get the homes");
		}
	}

	public async Task GetOwnerAsync()
	{
		try
		{
			logger.LogDebug("Trying to get owner from service...");
			var owner = await ownerService.GetOwnerAsync();

			Console.WriteLine($"Showing the owner: {owner.Name}");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Could not get the owner");
		}
	}

	public async Task GetPriceAsync(int hours)
	{
		try
		{
			logger.LogDebug("Trying to get prices from service...");
			var priceSummary = await priceService.GetPriceAsync(hours);

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
			logger.LogError(ex, "Could not get the prices");
		}
	}

	public async Task GetConsumptionAsync(int days)
	{
		try
		{
			logger.LogDebug("Trying to get consumption from service...");
			var consumptionViewModel = await consumptionService.GetConsumptionAsync(days);

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
			logger.LogError(ex, "Could not get the consumption");
		}
	}
}
