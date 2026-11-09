using System;
using System.Net;
using System.Net.Sockets;
using CUShocker.Protocol;
using UnityEngine;

namespace CUShocker.Mod;

internal sealed class PainReporter : MonoBehaviour
{
	private const float SampleInterval = 0.1f;
	private const float MinPainDelta = 0.05f;

	private UdpClient? _udp;
	private IPEndPoint _endpoint = null!;
	private readonly string _session = Guid.NewGuid().ToString("N");
	private long _seq;

	private float _nextSample;
	private float _lastSentAt = float.NegativeInfinity;
	private PainState? _lastSent;
	private bool _sendErrorLogged;
	private bool _readErrorLogged;

	public void Init(int port)
	{
		_endpoint = new IPEndPoint(IPAddress.Loopback, port);
		try
		{
			_udp = new UdpClient(AddressFamily.InterNetwork);
		}
		catch (Exception e)
		{
			Plugin.Logger.LogError($"Couldn't create UDP socket: {e.Message}");
		}
	}

	private void Update()
	{
		float now = Time.unscaledTime;
		if (now < _nextSample)
			return;
		_nextSample = now + SampleInterval;

		PainState state;
		try
		{
			state = ReadState();
		}
		catch (Exception e)
		{
			if (!_readErrorLogged)
			{
				Plugin.Logger.LogError($"Failed to read player state: {e}");
				_readErrorLogged = true;
			}
			return;
		}

		bool heartbeat = (now - _lastSentAt) * 1000f >= ProtocolInfo.HeartbeatIntervalMs;
		if (heartbeat || Changed(state))
		{
			Send(state);
			_lastSentAt = now;
		}
	}

	private static PainState ReadState()
	{
		var camera = PlayerCamera.main;
		var body = camera != null ? camera.body : null;
		if (body == null)
			return new PainState { InWorld = false };

		return new PainState
		{
			InWorld = true,
			Alive = body.alive,
			Conscious = body.conscious,
			AveragePain = body.averagePain,
		};
	}

	private bool Changed(PainState state) =>
		_lastSent is not { } last ||
		last.InWorld != state.InWorld ||
		last.Alive != state.Alive ||
		last.Conscious != state.Conscious ||
		Math.Abs(last.AveragePain - state.AveragePain) >= MinPainDelta;

	private void Send(PainState state)
	{
		state.Session = _session;
		state.Seq = ++_seq;
		_lastSent = state;
		if (_udp is null)
			return;

		try
		{
			var bytes = state.ToUtf8Bytes();
			_udp.Send(bytes, bytes.Length, _endpoint);
		}
		catch (Exception e)
		{
			// windows sometimes throws here after an icmp port is unreachable, but next send is fine
			if (!_sendErrorLogged)
			{
				Plugin.Logger.LogWarning($"Send failed: {e.Message}");
				_sendErrorLogged = true;
			}
		}
	}

	private void OnDestroy()
	{
		Send(new PainState { InWorld = false });
		_udp?.Close();
		_udp = null;
	}
}
