using TqTool.Features.Price;
using TqTool.Features.Price.Models;

namespace TqTool.Tests.Features.Price;

public class PriceViewModelFactoryTests
{
	private readonly IPriceViewModelFactory _sut = new PriceViewModelFactory();

	// The factory reads DateTime.Now internally, so fixtures are built relative to the current hour.
	private static DateTime CurrentHour()
	{
		var now = DateTime.Now;
		return new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
	}

	private static EnergyPrice Price(decimal total, DateTime startsAt, decimal tax = 0) =>
		new(total, total - tax, tax, startsAt, "SEK");

	[Fact]
	public void CreateModel_ShouldReturnViewModel()
	{
		// Arrange
		const int hours = 1;
		var currentHour = CurrentHour();
		var todayPrices = new List<EnergyPrice> { new(100, 70, 30, currentHour, "SEK") };
		var tomorrowPrices = new List<EnergyPrice> { new(1000, 700, 300, currentHour.AddDays(1), "SEK") };
		var priceInfo = new PriceInfo(todayPrices, tomorrowPrices);

		// Act
		var actual = _sut.CreateModel(priceInfo, hours);

		// Assert
		actual.ShouldNotBeNull();
		actual.CurrentPrice.Price.ShouldBe(10000);
		actual.CurrentPrice.Tax.ShouldBe(3000);
		actual.UpcomingPrices.ShouldHaveSingleItem();
	}

	[Fact]
	public void CreateModel_ShouldConvertKronorToOreRoundedToEven()
	{
		// Arrange - 0.185 kr is 18.5 öre, which rounds to the even 18
		var currentHour = CurrentHour();
		var priceInfo = new PriceInfo([Price(0.185m, currentHour, tax: 0.125m)], []);

		// Act
		var actual = _sut.CreateModel(priceInfo, 1);

		// Assert
		actual.CurrentPrice.Price.ShouldBe(18);
		actual.CurrentPrice.Tax.ShouldBe(12);
	}

	[Fact]
	public void CreateModel_ShouldBackFillFromTomorrowWhenTodayRunsOut()
	{
		// Arrange - only one hour left today, but three requested
		var currentHour = CurrentHour();
		var today = new List<EnergyPrice> { Price(1, currentHour), Price(2, currentHour.AddHours(1)) };
		var tomorrow = new List<EnergyPrice>
		{
			Price(3, currentHour.AddDays(1)),
			Price(4, currentHour.AddDays(1).AddHours(1)),
			Price(5, currentHour.AddDays(1).AddHours(2))
		};

		// Act
		var actual = _sut.CreateModel(new PriceInfo(today, tomorrow), 3);

		// Assert - one from today, topped up with the first two from tomorrow
		var upcoming = actual.UpcomingPrices.ToList();
		upcoming.Count.ShouldBe(3);
		upcoming.Select(x => x.Price).ShouldBe([200, 300, 400]);
	}

	[Fact]
	public void CreateModel_ShouldNotBackFillWhenTodayCoversTheWindow()
	{
		// Arrange
		var currentHour = CurrentHour();
		var today = new List<EnergyPrice>
		{
			Price(1, currentHour),
			Price(2, currentHour.AddHours(1)),
			Price(3, currentHour.AddHours(2)),
			Price(4, currentHour.AddHours(3))
		};
		var tomorrow = new List<EnergyPrice> { Price(99, currentHour.AddDays(1)) };

		// Act
		var actual = _sut.CreateModel(new PriceInfo(today, tomorrow), 2);

		// Assert
		actual.UpcomingPrices.Select(x => x.Price).ShouldBe([200, 300]);
	}

	[Fact]
	public void CreateModel_ShouldTolerateTomorrowBeingEmpty()
	{
		// Arrange - tomorrow's prices are only published in the afternoon
		var currentHour = CurrentHour();
		var today = new List<EnergyPrice> { Price(1, currentHour), Price(2, currentHour.AddHours(1)) };

		// Act
		var actual = _sut.CreateModel(new PriceInfo(today, []), 5);

		// Assert - returns what it has rather than throwing
		actual.UpcomingPrices.ShouldHaveSingleItem().Price.ShouldBe(200);
	}

	[Fact]
	public void CreateModel_ShouldReturnZeroCurrentPriceWhenTheHourIsMissing()
	{
		// Arrange - nothing starting exactly on the current hour
		var currentHour = CurrentHour();
		var today = new List<EnergyPrice> { Price(7, currentHour.AddHours(2)) };

		// Act
		var actual = _sut.CreateModel(new PriceInfo(today, []), 1);

		// Assert
		actual.CurrentPrice.Price.ShouldBe(0);
		actual.CurrentPrice.Tax.ShouldBe(0);
	}
}
