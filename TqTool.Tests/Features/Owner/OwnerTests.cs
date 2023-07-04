using FluentAssertions;
using GraphQL;
using Microsoft.Extensions.Logging;
using Moq;
using TqTool.Features.Owner;
using TqTool.Features.Owner.Models;
using TqTool.Infrastructure;

namespace TqTool.Tests.Features.Owner;

public class OwnerTests
{
	private readonly IOwnerService _sut;
	private readonly Mock<IGraphClientWrapper> _graphClientMock = new();
	private readonly Mock<ILogger<OwnerService>> _logger = new();

	public OwnerTests()
	{
		//_logger.Setup(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(),
		//	It.IsAny<Func<object, Exception, string>>()));
		_sut = new OwnerService(_logger.Object, _graphClientMock.Object);
	}

	[Fact]
	public async Task GetOwnerAsync_ShouldReturnOwnerName()
	{
		// Arrange
		const string name = "Test Name";
		var owner = new OwnerWrapper(new OwnerModel(name, "loginnametest", null));
		var ownerResponse = new GraphQLResponse<OwnerWrapper>
		{
			Data = owner
		};

		_graphClientMock.Setup(x => x.SendQueryAsync<OwnerWrapper>(It.IsAny<GraphQLRequest>()))
			.ReturnsAsync(ownerResponse);

		// Act
		var actual = await _sut.GetOwnerAsync();

		// Assert
		actual.Should().NotBeNull();
		actual.Name.Should().Be(name);
	}

	[Fact]
	public async Task GetOwnerAsync_ShouldReturnEmptyOwnerNameIfErrors()
	{
		// Arrange
		var returnValue = new GraphQLResponse<OwnerWrapper>
		{
			Data = new OwnerWrapper(new OwnerModel(string.Empty, string.Empty, null)),
			Errors = new[] {
				new GraphQLError { Message = "TestError" },
				new GraphQLError { Message = "TestError2" } }
		};

		_graphClientMock.Setup(x => x.SendQueryAsync<OwnerWrapper>(It.IsAny<GraphQLRequest>()))
			.ReturnsAsync(returnValue);

		// Act
		var actual = await _sut.GetOwnerAsync();

		// Assert
		actual.Should().NotBeNull();
		actual.Name.Should().Be(string.Empty);
		_logger.Verify(l => l.Log(
			LogLevel.Error,
			It.IsAny<EventId>(),
			It.IsAny<It.IsAnyType>(),
			It.IsAny<Exception>(),
			(Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
	}
}