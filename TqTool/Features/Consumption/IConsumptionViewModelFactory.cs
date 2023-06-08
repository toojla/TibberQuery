using TqTool.Features.Consumption.Models;

namespace TqTool.Features.Consumption;

public interface IConsumptionViewModelFactory
{
	ConsumptionViewModel CreateModel(IEnumerable<Node> nodes);
}