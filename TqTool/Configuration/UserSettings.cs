using System.Text.Json;

namespace TqTool.Configuration;

/// <summary>
/// Api credentials for an installed tool, kept in the user's profile rather than next to the
/// executable, because dotnet replaces the tool directory on every update and uninstall.
/// </summary>
public sealed class UserSettings(string filePath)
{
	private const string _endpointKey = "apiEndpoint";
	private const string _tokenKey = "apiToken";

	public static UserSettings Default { get; } = new(DefaultFilePath);

	public static string DefaultFilePath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"tqtool",
		"appsettings.json");

	public string FilePath { get; } = filePath;

	public (string? ApiEndpoint, string? ApiToken) Read()
	{
		if (!File.Exists(FilePath))
		{
			return (null, null);
		}

		Dictionary<string, string>? stored;

		try
		{
			stored = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath));
		}
		catch (JsonException ex)
		{
			throw new InvalidOperationException($"The settings file {FilePath} is not valid json: {ex.Message}", ex);
		}

		if (stored == null)
		{
			return (null, null);
		}

		stored.TryGetValue(_endpointKey, out var endpoint);
		stored.TryGetValue(_tokenKey, out var token);

		return (endpoint, token);
	}

	/// <summary>Updates only the values supplied, leaving anything already stored untouched.</summary>
	public void Save(string? apiEndpoint, string? apiToken)
	{
		var (existingEndpoint, existingToken) = Read();

		var settings = new Dictionary<string, string>
		{
			[_endpointKey] = apiEndpoint ?? existingEndpoint ?? string.Empty,
			[_tokenKey] = apiToken ?? existingToken ?? string.Empty
		};

		var directory = Path.GetDirectoryName(FilePath);

		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
		RestrictToCurrentUser();
	}

	private void RestrictToCurrentUser()
	{
		// The windows profile directory is already per user; elsewhere the file holds a secret in
		// a world readable location unless it is narrowed.
		if (OperatingSystem.IsWindows())
		{
			return;
		}

		File.SetUnixFileMode(FilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
	}
}
