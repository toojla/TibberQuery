using FluentAssertions;
using TqTool.Features.Price;
using TqTool.Features.Price.Models;

namespace TqTool.Tests.Features.Price;

public class PriceViewModelFactoryTests
{
	private readonly IPriceViewModelFactory _sut;

	public PriceViewModelFactoryTests()
	{
		_sut = new PriceViewModelFactory();
	}

	[Fact]
	public async Task CreateModel_ShouldReturnViewModel()
	{
		// Arrange
		const int hours = 1;
		const decimal decimalPrice = 100;
		const int intPrice = 10000;
		const decimal decimalTax = 30;
		const int intTax = 3000;
		var now = DateTime.Now;
		var referenceDateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
		var todayPrices = new List<EnergyPrice> { new EnergyPrice(decimalPrice, 70, decimalTax, referenceDateTime, "SEK") };
		var tomorrowPrices = new List<EnergyPrice> { new EnergyPrice(1000, 700, 300, referenceDateTime.AddDays(1), "SEK") };
		var priceInfo = new PriceInfo(todayPrices, tomorrowPrices);

		// Act
		var actual = _sut.CreateModel(priceInfo, hours);

		// Assert
		actual.Should().NotBeNull();
		actual.CurrentPrice.Price.Should().Be(intPrice);
		actual.CurrentPrice.Tax.Should().Be(intTax);
		actual.UpcomingPrices.Should().HaveCount(1);
	}
}