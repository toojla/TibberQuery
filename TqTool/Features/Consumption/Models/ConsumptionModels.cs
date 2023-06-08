namespace TqTool.Features.Consumption.Models;

public record ConsumptionWrapper(ConsumptionResult Viewer);

public record ConsumptionResult(IEnumerable<HomeWrapper> Homes);

public record HomeWrapper(Consumption Consumption);

public record Consumption(IEnumerable<Node> Nodes);

public record Node(DateTime From, DateTime To, decimal? Cost, decimal? UnitPrice, decimal? UnitPriceVat,
	decimal? Consumption, string ConsumptionUnit);

public record ConsumptionViewModel(int NumberOfDaysBack, IEnumerable<ConsumptionDay> ConsumptionDays);

public record ConsumptionDay(DateTime Day, int Cost, decimal AveragePrice, int Consumption, string ConsumptionUnit);