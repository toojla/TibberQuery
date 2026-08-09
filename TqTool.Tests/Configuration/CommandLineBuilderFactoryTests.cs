using TqTool.Configuration;

namespace TqTool.Tests.Configuration;

public class CommandLineBuilderFactoryTests
{
	private const int _maxHours = 12;

	[Theory]
	[InlineData(1)]
	[InlineData(6)]
	[InlineData(12)]
	public void CalculateHours_ShouldKeepValuesInsideTheAllowedRange(int hours)
	{
		// Act
		var actual = CommandLineBuilderFactory.CalculateHours(hours, maxInput: false);

		// Assert
		actual.ShouldBe(hours);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(13)]
	[InlineData(int.MaxValue)]
	public void CalculateHours_ShouldClampValuesOutsideTheAllowedRange(int hours)
	{
		// Act - out of range silently becomes the maximum rather than erroring
		var actual = CommandLineBuilderFactory.CalculateHours(hours, maxInput: false);

		// Assert
		actual.ShouldBe(_maxHours);
	}

	[Fact]
	public void CalculateHours_ShouldFallBackToTheMaximumWhenNotSupplied()
	{
		// Act
		var actual = CommandLineBuilderFactory.CalculateHours(null, maxInput: false);

		// Assert
		actual.ShouldBe(_maxHours);
	}

	[Fact]
	public void CalculateHours_ShouldOverrideAnExplicitHourCountWhenMaxIsSet()
	{
		// Act - -max wins over -hrs
		var actual = CommandLineBuilderFactory.CalculateHours(3, maxInput: true);

		// Assert
		actual.ShouldBe(_maxHours);
	}
}
