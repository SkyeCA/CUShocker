// Shared between the BepInEx mod (net48) and the OpenShock Desktop module (net10).
// Keep this file free of dependencies: the mod side has no JSON library of its own.

using System;
using System.Globalization;
using System.Text;

namespace CUShocker.Protocol;

/// <summary>
/// Constants both sides must agree on.
/// The mod sends UDP datagrams to <see cref="Host"/>:<see cref="DefaultPort"/>, one JSON object per datagram.
/// </summary>
public static class ProtocolInfo
{
	/// <summary>Bump when a change would break the other side. Receivers ignore other versions.</summary>
	public const int Version = 1;

	public const string Host = "127.0.0.1";
	public const int DefaultPort = 37412;

	/// <summary>The mod re-sends its current state at least this often, so the module can tell the game is connected.</summary>
	public const int HeartbeatIntervalMs = 1000;

	/// <summary>The module treats the game as disconnected after this long without a message.</summary>
	public const int ConnectionTimeoutMs = 3000;
}

public static class MessageTypes
{
	/// <summary>The player's current condition. Sent when it changes, and as a heartbeat.</summary>
	public const string State = "state";
}

/// <summary>
/// A snapshot of the local player's condition. Serialized as camelCase JSON, e.g.
/// <c>{"v":1,"type":"state","session":"…","seq":42,"inWorld":true,"alive":true,"conscious":true,"averagePain":12.5}</c>
/// </summary>
public sealed class PainState
{
	/// <summary>Protocol version, see <see cref="ProtocolInfo.Version"/>.</summary>
	public int V { get; set; } = ProtocolInfo.Version;

	/// <summary>Message type, see <see cref="MessageTypes"/>.</summary>
	public string Type { get; set; } = MessageTypes.State;

	/// <summary>Random ID chosen when the game starts. A new session means <see cref="Seq"/> starts again.</summary>
	public string Session { get; set; } = "";

	/// <summary>Increases by one per message within a session, so stale or reordered datagrams can be dropped.</summary>
	public long Seq { get; set; }

	/// <summary>False in menus and loading screens, where there is no player body.</summary>
	public bool InWorld { get; set; }

	public bool Alive { get; set; }

	public bool Conscious { get; set; }

	/// <summary>The game's <c>Body.averagePain</c>. 0 when not in the world.</summary>
	public float AveragePain { get; set; }

	public string ToJson()
	{
		var json = new StringBuilder(160);
		json.Append("{\"v\":").Append(V.ToString(CultureInfo.InvariantCulture));
		json.Append(",\"type\":");
		AppendString(json, Type);
		json.Append(",\"session\":");
		AppendString(json, Session);
		json.Append(",\"seq\":").Append(Seq.ToString(CultureInfo.InvariantCulture));
		json.Append(",\"inWorld\":").Append(InWorld ? "true" : "false");
		json.Append(",\"alive\":").Append(Alive ? "true" : "false");
		json.Append(",\"conscious\":").Append(Conscious ? "true" : "false");
		json.Append(",\"averagePain\":").Append(FormatNumber(AveragePain));
		json.Append('}');
		return json.ToString();
	}

	public byte[] ToUtf8Bytes() => Encoding.UTF8.GetBytes(ToJson());

	// JSON has no NaN or Infinity, so those become 0
	private static string FormatNumber(float value) =>
		float.IsNaN(value) || float.IsInfinity(value) ? "0" : value.ToString("R", CultureInfo.InvariantCulture);

	private static void AppendString(StringBuilder json, string value)
	{
		json.Append('"');
		foreach (char c in value)
		{
			switch (c)
			{
				case '"': json.Append("\\\""); break;
				case '\\': json.Append("\\\\"); break;
				default:
					if (c < 0x20)
						json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
					else
						json.Append(c);
					break;
			}
		}
		json.Append('"');
	}

#if NET
	private static readonly System.Text.Json.JsonSerializerOptions JsonOptions =
		new(System.Text.Json.JsonSerializerDefaults.Web);

	/// <summary>Parses a datagram. Returns false if it is malformed, or from another protocol version.</summary>
	public static bool TryParse(ReadOnlySpan<byte> utf8Json, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out PainState? state)
	{
		state = null;
		try
		{
			var parsed = System.Text.Json.JsonSerializer.Deserialize<PainState>(utf8Json, JsonOptions);
			if (parsed is null || parsed.V != ProtocolInfo.Version || parsed.Type != MessageTypes.State)
				return false;
			state = parsed;
			return true;
		}
		catch (System.Text.Json.JsonException)
		{
			return false;
		}
	}
#endif
}
