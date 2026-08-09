using TqTool.Configuration;

namespace TqTool.Tests.Configuration;

public class UserSettingsTests : IDisposable
{
	private readonly string _directory = Path.Combine(Path.GetTempPath(), $"tqtool-tests-{Guid.NewGuid():N}");
	private readonly UserSettings _sut;

	public UserSettingsTests()
	{
		_sut = new UserSettings(Path.Combine(_directory, "appsettings.json"));
	}

	public void Dispose()
	{
		if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
	}

	[Fact]
	public void Read_ShouldReturnNothingWhenTheFileDoesNotExist()
	{
		// Act
		var (endpoint, token) = _sut.Read();

		// Assert
		endpoint.ShouldBeNull();
		token.ShouldBeNull();
	}

	[Fact]
	public void Save_ShouldCreateTheDirectoryAndRoundTrip()
	{
		// Act - the profile directory does not exist on a first run
		_sut.Save("https://api.tibber.com/v1-beta/gql", "a-token");
		var (endpoint, token) = _sut.Read();

		// Assert
		File.Exists(_sut.FilePath).ShouldBeTrue();
		endpoint.ShouldBe("https://api.tibber.com/v1-beta/gql");
		token.ShouldBe("a-token");
	}

	[Fact]
	public void Save_ShouldKeepTheTokenWhenOnlyTheEndpointIsSupplied()
	{
		// Arrange
		_sut.Save("https://old", "keep-me");

		// Act
		_sut.Save("https://new", null);

		// Assert - updating one value must not blank the other
		var (endpoint, token) = _sut.Read();
		endpoint.ShouldBe("https://new");
		token.ShouldBe("keep-me");
	}

	[Fact]
	public void Save_ShouldKeepTheEndpointWhenOnlyTheTokenIsSupplied()
	{
		// Arrange
		_sut.Save("https://keep-me", "old-token");

		// Act
		_sut.Save(null, "new-token");

		// Assert
		var (endpoint, token) = _sut.Read();
		endpoint.ShouldBe("https://keep-me");
		token.ShouldBe("new-token");
	}

	[Fact]
	public void Read_ShouldNameTheFileWhenItIsNotValidJson()
	{
		// Arrange
		Directory.CreateDirectory(_directory);
		File.WriteAllText(_sut.FilePath, "{ this is not json");

		// Act
		var actual = Should.Throw<InvalidOperationException>(() => _sut.Read());

		// Assert - the path matters more than the parser's complaint
		actual.Message.ShouldContain(_sut.FilePath);
	}

	[Fact]
	public void DefaultFilePath_ShouldLiveUnderTheUserProfile()
	{
		// Assert - not beside the executable, which dotnet replaces on every tool update
		UserSettings.DefaultFilePath.ShouldContain("tqtool");
		Path.IsPathRooted(UserSettings.DefaultFilePath).ShouldBeTrue();
	}
}
