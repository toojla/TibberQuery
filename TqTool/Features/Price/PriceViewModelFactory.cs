using TqTool.Features.Price.Models;

namespace TqTool.Features.Price;

public class PriceViewModelFactory(TimeProvider timeProvider) : IPriceViewModelFactory
{
	public PriceViewModelFactory() : this(TimeProvider.System)
	{
	}

	public PriceSummaryViewModel CreateModel(PriceInfo priceInfo, int hours)
	{
		// Compared as instants, so the offset each side carries is irrelevant to the comparison.
		var now = timeProvider.GetUtcNow();

		var todayPrices = priceInfo.Today.ToList();
		var tomorrowPrices = priceInfo.Tomorrow.ToList();

		// Each entry covers the hour beginning at StartsAt, so the current price is whichever window
		// contains this instant. Matching on an exact local hour instead used to fail outright in zones
		// offset by a half hour, and to pick arbitrarily between the two repeated hours of a DST fold.
		var currentPrice = todayPrices.FirstOrDefault(x => x.StartsAt <= now && now < x.StartsAt.AddHours(1));
		var comingPrices = todayPrices.Where(x => x.StartsAt > now).Take(hours).ToList();

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
		if (currentPrice == null) return new PriceViewModel(0, 0, timeProvider.GetLocalNow());

		var price = currentPrice.Total * 100;
		var roundedPrice = (int)decimal.Round(price, MidpointRounding.ToEven);
		var taxPrice = currentPrice.Tax * 100;
		var roundedTax = (int)decimal.Round(taxPrice, MidpointRounding.ToEven);

		return new PriceViewModel(roundedPrice, roundedTax, currentPrice.StartsAt);
	}
}
