using TqTool.Features.Consumption;
using TqTool.Features.Consumption.Models;

namespace TqTool.Tests.Features.Consumption;

public class ConsumptionViewModelFactoryTests
{
	private readonly IConsumptionViewModelFactory _sut = new ConsumptionViewModelFactory();

	private static Node Node(decimal? cost, decimal? unitPrice = 1, decimal? consumption = 1, DateTime? from = null) =>
		new(from ?? new DateTime(2026, 8, 1), new DateTime(2026, 8, 2), cost, unitPrice, 0, consumption, "kWh");

	[Fact]
	public void CreateModel_ShouldKeepNodesWithCost()
	{
		// Arrange
		var nodes = new[] { Node(cost: 12), Node(cost: 8) };

		// Act
		var actual = _sut.CreateModel(nodes);

		// Assert
		actual.ConsumptionDays.Count().ShouldBe(2);
		actual.NumberOfDaysBack.ShouldBe(2);
	}

	[Fact]
	public void CreateModel_ShouldSkipNodesWithoutCost()
	{
		// Arrange - a null cost is a day the api has no billing data for
		var nodes = new[] { Node(cost: null), Node(cost: 5), Node(cost: null) };

		// Act
		var actual = _sut.CreateModel(nodes);

		// Assert
		actual.ConsumptionDays.ShouldHaveSingleItem().Cost.ShouldBe(5);
		actual.NumberOfDaysBack.ShouldBe(1);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(0.99)]
	public void CreateModel_ShouldSkipNodesCostingLessThanOne(decimal cost)
	{
		// Arrange
		var nodes = new[] { Node(cost) };

		// Act
		var actual = _sut.CreateModel(nodes);

		// Assert
		actual.ConsumptionDays.ShouldBeEmpty();
		actual.NumberOfDaysBack.ShouldBe(0);
	}

	[Theory]
	[InlineData(2.5, 2)]   // banker's rounding: .5 goes to the even neighbour
	[InlineData(3.5, 4)]
	[InlineData(4.4, 4)]
	[InlineData(4.6, 5)]
	public void CreateModel_ShouldRoundCostToEven(decimal cost, int expected)
	{
		// Arrange
		var nodes = new[] { Node(cost) };

		// Act
		var actual = _sut.CreateModel(nodes);

		// Assert
		actual.ConsumptionDays.ShouldHaveSingleItem().Cost.ShouldBe(expected);
	}

	[Fact]
	public void CreateModel_ShouldRoundUnitPriceToTwoDecimals()
	{
		// Arrange
		var nodes = new[] { Node(cost: 10, unitPrice: 1.23456m) };

		// Act
		var actual = _sut.CreateModel(nodes);

		// Assert - kept in kr, matching the "kr/unit" the handler prints
		actual.ConsumptionDays.ShouldHaveSingleItem().AveragePrice.ShouldBe(1.23m);
	}

	[Fact]
	public void CreateModel_ShouldCarryThroughDayAndUnit()
	{
		// Arrange
		var day = new DateTime(2026, 8, 9);
		var nodes = new[] { Node(cost: 10, consumption: 21.4m, from: day) };

		// Act
		var actual = _sut.CreateModel(nodes);

		// Assert
		var consumptionDay = actual.ConsumptionDays.ShouldHaveSingleItem();
		consumptionDay.Day.ShouldBe(day);
		consumptionDay.Consumption.ShouldBe(21);
		consumptionDay.ConsumptionUnit.ShouldBe("kWh");
	}

	[Fact]
	public void CreateModel_ShouldReturnEmptyModelForNoNodes()
	{
		// Act
		var actual = _sut.CreateModel([]);

		// Assert
		actual.ConsumptionDays.ShouldBeEmpty();
		actual.NumberOfDaysBack.ShouldBe(0);
	}
}
