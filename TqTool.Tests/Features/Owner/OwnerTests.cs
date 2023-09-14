using FluentAssertions;
using GraphQL;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TqTool.Features.Owner;
using TqTool.Features.Owner.Models;
using TqTool.Infrastructure;

namespace TqTool.Tests.Features.Owner;

public class OwnerTests
{
	private const string _name = "Test Name";
	private readonly OwnerService _sut;
	private readonly IGraphClientWrapper _graphClientMock = Substitute.For<IGraphClientWrapper>();
	private readonly ILogger<OwnerService> _logger = Substitute.For<ILogger<OwnerService>>();

	public OwnerTests()
	{
		_sut = new OwnerService(_logger, _graphClientMock);
	}

	[Fact]
	public async Task GetOwnerAsync_ShouldReturnOwnerName()
	{
		// Arrange
		var owner = new OwnerWrapper(new OwnerModel(_name, "loginnametest", null));
		var ownerResponse = new GraphQLResponse<OwnerWrapper>
		{
			Data = owner
		};

		_graphClientMock.SendQueryAsync<OwnerWrapper>(Arg.Any<GraphQLRequest>()).Returns(ownerResponse);

		// Act
		var actual = await _sut.GetOwnerAsync();

		// Assert
		actual.Should().NotBeNull();
		actual.Name.Should().Be(_name);
	}

	[Fact]
	public async Task GetOwnerAsync_ShouldReturnEmptyOwnerNameIfErrors()
	{
		// Arrange
		var returnValueSetup = new GraphQLResponse<OwnerWrapper>
		{
			Data = new OwnerWrapper(new OwnerModel(string.Empty, string.Empty, null)),
			Errors = new[] {
				new GraphQLError { Message = "TestError" },
				new GraphQLError { Message = "TestError2" } }
		};

		_graphClientMock.SendQueryAsync<OwnerWrapper>(Arg.Any<GraphQLRequest>()).Returns(returnValueSetup);

		// Act
		var actual = await _sut.GetOwnerAsync();

		// Assert
		actual.Should().NotBeNull();
		actual.Name.Should().Be(string.Empty);
		await _graphClientMock.Received(1).SendQueryAsync<OwnerWrapper>(Arg.Any<GraphQLRequest>());
	}

	[Fact]
	public async Task GetOwnerHomesAsync_ShouldReturnOwnerHomes()
	{
		// Arrange
		var returnValueSetup = new GraphQLResponse<HomeWrapper>
		{
			Data = new HomeWrapper(new OwnerModel(_name, "loginmock",
				new List<Home>
				{
					new(100, 3, "test", "testtype",
						new Address("Adr1", "Adr2", "Adr3", "Test city", 12345, "Sweden"))
				}))
		};

		_graphClientMock.SendQueryAsync<HomeWrapper>(Arg.Any<GraphQLRequest>()).Returns(returnValueSetup);

		// Act
		var actual = await _sut.GetOwnerHomesAsync();

		// Assert
		actual.Should().NotBeNullOrEmpty();
		actual.Should().HaveCountGreaterThanOrEqualTo(1);
	}
}