using FluentAssertions;
using GraphQL;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TqTool.Features.Price;
using TqTool.Features.Price.Models;
using TqTool.Infrastructure;

namespace TqTool.Tests.Features.Price;

public class PriceTests
{
	private readonly IPriceService _sut;
	private readonly IGraphClientWrapper _graphClientMock = Substitute.For<IGraphClientWrapper>();
	private readonly IMemoryCache _memoryCacheMock = Substitute.For<IMemoryCache>();
	private readonly IPriceViewModelFactory _priceViewModelFactoryMock = Substitute.For<IPriceViewModelFactory>();
	private readonly ILogger<PriceService> _logger = Substitute.For<ILogger<PriceService>>();

	public PriceTests()
	{
		_sut = new PriceService(_graphClientMock, _memoryCacheMock, _priceViewModelFactoryMock, _logger);
	}

	[Fact]
	public async Task GetPriceAsync_ShouldReturnCurrentPrices()
	{
		// Arrange
		const int hours = 5;
		const int price = 10;
		var cacheEntry = Substitute.For<ICacheEntry>();

		var priceResultWrapper = new PriceResultWrapper(new PriceResult(new List<HomeWrapper>
		{
			new (new CurrentSubscriptionWrapper(
				new PriceInfo(new List<EnergyPrice>
				{
					new (price, 1, 1, new DateTime(), "")
				},new List<EnergyPrice>
				{
					new (1,1,1, new DateTime(),"")
				})))
		}));

		var priceSummaryViewModel = new PriceSummaryViewModel(new PriceViewModel(price, 1, new DateTime()), new List<PriceViewModel>
		{
			new (price, 1, new DateTime())
		});

		var result = new GraphQLResponse<PriceResultWrapper> { Data = priceResultWrapper };

		_memoryCacheMock.CreateEntry(Arg.Any<object>()).Returns(cacheEntry);
		_graphClientMock.SendQueryAsync<PriceResultWrapper>(Arg.Any<GraphQLRequest>()).Returns(result);
		_priceViewModelFactoryMock.CreateModel(Arg.Any<PriceInfo>(), Arg.Any<int>()).Returns(priceSummaryViewModel);

		// Act
		var actual = await _sut.GetPriceAsync(hours);

		// Assert
		actual.Should().NotBeNull();
		actual.CurrentPrice.Price.Should().Be(price);
		actual.UpcomingPrices.Should().HaveCount(1);
		_priceViewModelFactoryMock.Received(1).CreateModel(Arg.Any<PriceInfo>(), Arg.Any<int>());
	}
}