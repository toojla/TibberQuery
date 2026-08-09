using TqTool.Features.Consumption.Models;

namespace TqTool.Features.Consumption;

public class ConsumptionViewModelFactory : IConsumptionViewModelFactory
{
	public ConsumptionViewModel CreateModel(IEnumerable<Node> nodes)
	{
		var consumptionDays = new List<ConsumptionDay>();
		var countedNumberOfDays = 0;

		foreach (var node in nodes)
		{
			if (node.Cost is null or < 1)
			{
				continue;
			}

			var roundedCost = (int)decimal.Round(node.Cost ?? 0, MidpointRounding.ToEven);
			// Kept in kr to match the "kr/unit" the handler prints. The parentheses are load bearing:
			// `node.UnitPrice ?? 0 * 100` binds as `node.UnitPrice ?? (0 * 100)`, so the * 100 never applied.
			var unitPrice = node.UnitPrice ?? 0;
			var roundedUnitPrice = Math.Round(unitPrice, 2);
			var roundedConsumption = (int)decimal.Round(node.Consumption ?? 0, MidpointRounding.ToEven);

			consumptionDays.Add(new ConsumptionDay(node.From, roundedCost, roundedUnitPrice, roundedConsumption, node.ConsumptionUnit));
			countedNumberOfDays++;
		}

		return new ConsumptionViewModel(countedNumberOfDays, consumptionDays);
	}
}