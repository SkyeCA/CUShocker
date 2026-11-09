using System;
using System.Globalization;
using System.Text;

namespace CUShocker.Protocol;

public static class ProtocolInfo
{
	public const int Version = 1;

	public const string Host = "127.0.0.1";
	public const int DefaultPort = 37412;

	public const int HeartbeatIntervalMs = 1000;
	public const int ConnectionTimeoutMs = 3000;
}

public static class MessageTypes
{
	public const string State = "state";
}

public sealed class PainState
{
	public int V { get; set; } = ProtocolInfo.Version;
	public string Type { get; set; } = MessageTypes.State;

	public string Session { get; set; } = "";
	public long Seq { get; set; }

	public bool InWorld { get; set; }
	public bool Alive { get; set; }
	public bool Conscious { get; set; }
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
