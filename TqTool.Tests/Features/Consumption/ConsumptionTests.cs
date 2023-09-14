using AutoFixture;
using FluentAssertions;
using GraphQL;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TqTool.Features.Consumption;
using TqTool.Features.Consumption.Models;
using TqTool.Infrastructure;
using HomeWrapper = TqTool.Features.Consumption.Models.HomeWrapper;

namespace TqTool.Tests.Features.Consumption;

public class ConsumptionTests
{
	private readonly IGraphClientWrapper _graphClientMock = Substitute.For<IGraphClientWrapper>();
	private readonly ILogger<ConsumptionService> _logger = Substitute.For<ILogger<ConsumptionService>>();
	private readonly IConsumptionViewModelFactory _consumptionViewModelFactoryMock = Substitute.For<IConsumptionViewModelFactory>();
	private readonly IConsumptionService _sut;
	private readonly Fixture _fixture = new();

	public ConsumptionTests()
	{
		_sut = new ConsumptionService(_graphClientMock, _consumptionViewModelFactoryMock, _logger);
	}

	[Fact]
	public async Task GetConsumption_ShouldReturnConsumption()
	{
		// Arrange
		const int numberOfDays = 5;
		var nodes = GetNodes(numberOfDays);

		var consumptionWrapper = new ConsumptionWrapper(new ConsumptionResult(new List<HomeWrapper>
		{
			new HomeWrapper(new TqTool.Features.Consumption.Models.Consumption(nodes))
		}));
		var result = new GraphQLResponse<ConsumptionWrapper> { Data = consumptionWrapper };

		_graphClientMock.SendQueryAsync<ConsumptionWrapper>(Arg.Any<GraphQLRequest>()).Returns(result);
		_consumptionViewModelFactoryMock.CreateModel(nodes).Returns(_fixture.Create<ConsumptionViewModel>());

		// Act
		var actual = await _sut.GetConsumptionAsync(numberOfDays);

		// Assert
		actual.Should().NotBeNull();
		actual.ConsumptionDays.Should().HaveCountGreaterThan(1);
		_consumptionViewModelFactoryMock.Received(1).CreateModel(nodes);
	}

	private List<Node> GetNodes(int numberOfNodes)
	{
		var enumerable = _fixture.CreateMany<Node>(numberOfNodes);
		return enumerable.ToList();
	}
}