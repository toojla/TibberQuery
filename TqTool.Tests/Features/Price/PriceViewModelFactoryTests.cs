using TqTool.Features.Price;
using TqTool.Features.Price.Models;

namespace TqTool.Tests.Features.Price;

public class PriceViewModelFactoryTests
{
	// Sweden in summer, so prices arrive with a +02:00 offset.
	private static readonly TimeSpan _swedishSummer = TimeSpan.FromHours(2);
	private static readonly DateTimeOffset _midday = new(2026, 8, 9, 12, 0, 0, _swedishSummer);

	private sealed class FixedClock(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}

	private static IPriceViewModelFactory At(DateTimeOffset now) => new PriceViewModelFactory(new FixedClock(now));

	private static EnergyPrice Price(decimal total, DateTimeOffset startsAt, decimal tax = 0) =>
		new(total, total - tax, tax, startsAt, "SEK");

	[Fact]
	public void CreateModel_ShouldReturnViewModel()
	{
		// Arrange - 20 minutes into the midday hour
		var sut = At(_midday.AddMinutes(20));
		var today = new List<EnergyPrice> { new(100, 70, 30, _midday, "SEK") };
		var tomorrow = new List<EnergyPrice> { new(1000, 700, 300, _midday.AddDays(1), "SEK") };

		// Act
		var actual = sut.CreateModel(new PriceInfo(today, tomorrow), 1);

		// Assert
		actual.CurrentPrice.Price.ShouldBe(10000);
		actual.CurrentPrice.Tax.ShouldBe(3000);
		actual.UpcomingPrices.ShouldHaveSingleItem();
	}

	[Fact]
	public void CreateModel_ShouldConvertKronorToOreRoundedToEven()
	{
		// Arrange - 0.185 kr is 18.5 öre, which rounds to the even 18
		var sut = At(_midday.AddMinutes(20));

		// Act
		var actual = sut.CreateModel(new PriceInfo([Price(0.185m, _midday, tax: 0.125m)], []), 1);

		// Assert
		actual.CurrentPrice.Price.ShouldBe(18);
		actual.CurrentPrice.Tax.ShouldBe(12);
	}

	[Fact]
	public void CreateModel_ShouldTreatTheHourContainingNowAsCurrent()
	{
		// Arrange - 59 minutes in, still the same hour
		var sut = At(_midday.AddMinutes(59));
		var today = new List<EnergyPrice> { Price(1, _midday.AddHours(-1)), Price(2, _midday), Price(3, _midday.AddHours(1)) };

		// Act
		var actual = sut.CreateModel(new PriceInfo(today, []), 1);

		// Assert
		actual.CurrentPrice.Price.ShouldBe(200);
	}

	[Fact]
	public void CreateModel_ShouldFindTheCurrentHourWhenTheApiOffsetIsNotTheMachineOffset()
	{
		// Arrange - the same instants written in a half hour zone. This pins that matching is done on
		// the instant rather than on how the offset happens to be written. It does not reproduce the
		// related machine-zone bug: truncating to a local hour only fails when TimeZoneInfo.Local
		// itself has a half hour offset, which a unit test cannot set.
		var sut = At(_midday.AddMinutes(20));
		var indianOffset = TimeSpan.FromMinutes(330);
		var today = new List<EnergyPrice>
		{
			Price(1, _midday.ToOffset(indianOffset)),
			Price(2, _midday.AddHours(1).ToOffset(indianOffset))
		};

		// Act
		var actual = sut.CreateModel(new PriceInfo(today, []), 1);

		// Assert - the window containing now, regardless of how it is written down
		actual.CurrentPrice.Price.ShouldBe(100);
	}

	[Fact]
	public void CreateModel_ShouldPickTheRightHourAcrossADaylightSavingFold()
	{
		// Arrange - 25 October 2026, when 02:00-03:00 happens twice in Sweden. Both hours read
		// 02:xx on a wall clock and are only told apart by their offset.
		var firstPass = new DateTimeOffset(2026, 10, 25, 2, 0, 0, TimeSpan.FromHours(2));
		var secondPass = new DateTimeOffset(2026, 10, 25, 2, 0, 0, TimeSpan.FromHours(1));
		var today = new List<EnergyPrice> { Price(1, firstPass), Price(2, secondPass) };

		// Act - half an hour into the repeated hour
		var actual = At(secondPass.AddMinutes(30)).CreateModel(new PriceInfo(today, []), 1);

		// Assert - the second 02:00, not the first one that shares its wall clock reading
		actual.CurrentPrice.Price.ShouldBe(200);
	}

	[Fact]
	public void CreateModel_ShouldBackFillFromTomorrowWhenTodayRunsOut()
	{
		// Arrange - only one hour left today, but three requested
		var sut = At(_midday.AddMinutes(20));
		var today = new List<EnergyPrice> { Price(1, _midday), Price(2, _midday.AddHours(1)) };
		var tomorrow = new List<EnergyPrice>
		{
			Price(3, _midday.AddDays(1)),
			Price(4, _midday.AddDays(1).AddHours(1)),
			Price(5, _midday.AddDays(1).AddHours(2))
		};

		// Act
		var actual = sut.CreateModel(new PriceInfo(today, tomorrow), 3);

		// Assert - one from today, topped up with the first two from tomorrow
		actual.UpcomingPrices.Select(x => x.Price).ShouldBe([200, 300, 400]);
	}

	[Fact]
	public void CreateModel_ShouldNotBackFillWhenTodayCoversTheWindow()
	{
		// Arrange
		var sut = At(_midday.AddMinutes(20));
		var today = new List<EnergyPrice>
		{
			Price(1, _midday), Price(2, _midday.AddHours(1)),
			Price(3, _midday.AddHours(2)), Price(4, _midday.AddHours(3))
		};
		var tomorrow = new List<EnergyPrice> { Price(99, _midday.AddDays(1)) };

		// Act
		var actual = sut.CreateModel(new PriceInfo(today, tomorrow), 2);

		// Assert
		actual.UpcomingPrices.Select(x => x.Price).ShouldBe([200, 300]);
	}

	[Fact]
	public void CreateModel_ShouldTolerateTomorrowBeingEmpty()
	{
		// Arrange - tomorrow's prices are only published in the afternoon
		var sut = At(_midday.AddMinutes(20));
		var today = new List<EnergyPrice> { Price(1, _midday), Price(2, _midday.AddHours(1)) };

		// Act
		var actual = sut.CreateModel(new PriceInfo(today, []), 5);

		// Assert - returns what it has rather than throwing
		actual.UpcomingPrices.ShouldHaveSingleItem().Price.ShouldBe(200);
	}

	[Fact]
	public void CreateModel_ShouldReturnZeroCurrentPriceWhenTheHourIsMissing()
	{
		// Arrange - nothing covering the current hour
		var sut = At(_midday.AddMinutes(20));
		var today = new List<EnergyPrice> { Price(7, _midday.AddHours(2)) };

		// Act
		var actual = sut.CreateModel(new PriceInfo(today, []), 1);

		// Assert
		actual.CurrentPrice.Price.ShouldBe(0);
		actual.CurrentPrice.Tax.ShouldBe(0);
	}
}
