using TqTool.Features.Price.Models;

namespace TqTool.Features.Price;

public class PriceViewModelFactory : IPriceViewModelFactory
{
	public PriceSummaryViewModel CreateModel(PriceInfo priceInfo, int hours)
	{
		var now = DateTime.Now;
		var referenceDateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

		var todayPrices = priceInfo.Today.ToList();
		var tomorrowPrices = priceInfo.Tomorrow.ToList();

		var currentPrice = todayPrices.FirstOrDefault(x => x.StartsAt == referenceDateTime);
		var comingPrices = todayPrices.Where(x => x.StartsAt > DateTime.Now).Take(hours).ToList();

		CompleteMissingPrices(comingPrices, tomorrowPrices, hours);

		return GetPriceSummary(currentPrice, comingPrices);
	}

	private void CompleteMissingPrices(List<EnergyPrice> comingPrices, List<EnergyPrice> tomorrowPrices, int hours)
	{
		var noOfComingPrices = comingPrices.Count;

		if (noOfComingPrices < hours && tomorrowPrices.Any())
		{
			var missing = hours - noOfComingPrices;
			var energyPrices = tomorrowPrices.Take(missing).ToList();

			if (energyPrices.Any())
			{
				comingPrices.AddRange(energyPrices);
			}
		}
	}

	private PriceSummaryViewModel GetPriceSummary(EnergyPrice? currentPrice, IEnumerable<EnergyPrice?> prices)
	{
		var upcomingPrices = prices.Select(GetPriceViewModel);
		var priceSummaryViewModel = new PriceSummaryViewModel(GetPriceViewModel(currentPrice), upcomingPrices);

		return priceSummaryViewModel;
	}

	private PriceViewModel GetPriceViewModel(EnergyPrice? currentPrice)
	{
		if (currentPrice == null) return new PriceViewModel(0, 0, DateTime.Now);

		var price = currentPrice.Total * 100;
		var roundedPrice = (int)decimal.Round(price, MidpointRounding.ToEven);
		var taxPrice = currentPrice.Tax * 100;
		var roundedTax = (int)decimal.Round(taxPrice, MidpointRounding.ToEven);

		return new PriceViewModel(roundedPrice, roundedTax, currentPrice.StartsAt);
	}
}