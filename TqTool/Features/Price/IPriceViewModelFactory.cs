using TqTool.Features.Price.Models;

namespace TqTool.Features.Price;

public interface IPriceViewModelFactory
{
	PriceSummaryViewModel CreateModel(PriceInfo priceInfo, int hours);
}