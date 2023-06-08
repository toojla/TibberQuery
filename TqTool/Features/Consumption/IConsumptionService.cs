using TqTool.Features.Consumption.Models;

namespace TqTool.Features.Consumption;

public interface IConsumptionService
{
	Task<ConsumptionViewModel> GetConsumptionAsync(int noOfDays);
}