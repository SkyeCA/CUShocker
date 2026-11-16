using System.Text.Json;
using System.Text.Json.Serialization;
using CUShocker.Core.Logic;
using Microsoft.Extensions.Logging;

namespace CUShocker.MultiShock.Services;

public sealed class MultiShockConfigStore
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		Converters = { new JsonStringEnumConverter() },
	};

	private readonly string _path;
	private readonly ILogger<MultiShockConfigStore> _log;

	public MultiShockConfig Config { get; }

	public MultiShockConfigStore(string path, ControlLimits limits, ILogger<MultiShockConfigStore> log)
	{
		_path = path;
		_log = log;
		Config = Load();
		Config.ClampTo(limits);
	}

	public void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
			File.WriteAllText(_path, JsonSerializer.Serialize(Config, JsonOptions));
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			_log.LogError(ex, "Couldn't save config to {Path}", _path);
		}
	}

	private MultiShockConfig Load()
	{
		if (!File.Exists(_path))
			return new MultiShockConfig();

		try
		{
			return JsonSerializer.Deserialize<MultiShockConfig>(File.ReadAllText(_path), JsonOptions) ?? new MultiShockConfig();
		}
		catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
		{
			_log.LogError(ex, "Couldn't load config from {Path}, using defaults", _path);
			return new MultiShockConfig();
		}
	}
}
