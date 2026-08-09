using GraphQL;
using TqTool.Infrastructure;

namespace TqTool.Tests.Infrastructure;

public class GraphQlErrorsTests
{
	[Fact]
	public void Describe_ShouldJoinEveryMessage()
	{
		// Arrange
		GraphQLError[] errors =
		[
			new() { Message = "invalid token" },
			new() { Message = "rate limited" }
		];

		// Act
		var actual = GraphQlErrors.Describe(errors);

		// Assert
		actual.ShouldBe("invalid token; rate limited");
	}

	[Fact]
	public void Describe_ShouldExplainWhenThereAreNoErrors()
	{
		// Act
		var actual = GraphQlErrors.Describe(null);

		// Assert - a null payload with no errors is its own diagnosis
		actual.ShouldBe("no data and no errors were returned");
	}

	[Fact]
	public void Describe_ShouldExplainWhenTheErrorArrayIsEmpty()
	{
		// Act
		var actual = GraphQlErrors.Describe([]);

		// Assert
		actual.ShouldBe("no data and no errors were returned");
	}
}
