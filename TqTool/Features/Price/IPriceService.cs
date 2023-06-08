using TqTool.Features.Price.Models;

namespace TqTool.Features.Price;

public interface IPriceService
{
	Task<PriceSummaryViewModel> GetPriceAsync(int hours);
}