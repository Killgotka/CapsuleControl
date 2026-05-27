using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CapsuleControl;

public class CapsuleService
{
	private readonly QRValidator _validator;

	private readonly RelayController _relay;

	private readonly Form1 _form;

	private SessionData? _currentSession;

	private Timer? _timer;

	private int sessiontime = 25;

	private List<string> disabelKey = new List<string>();

	public CapsuleService(QRValidator validator, RelayController relay, Form1 form)
	{
		_validator = validator;
		_relay = relay;
		_form = form;
	}

	public bool GetActionSession()
	{
		if (_currentSession != null)
		{
			return true;
		}
		return false;
	}

	public void ProcessQR(string qrText)
	{
		if (disabelKey.Contains(qrText))
		{
			return;
		}
		SessionData sessionData = _validator.Validate(qrText);
		if (sessionData == null)
		{
			_form.UpdateUI("", "00:00");
			return;
		}
		DateTime now = DateTime.Now;
		if (now.Date.Day == sessionData.StartTime.Day && now.Date.Month == sessionData.StartTime.Month)
		{
			int num = (int)(sessionData.StartTime - now).TotalSeconds;
			string text = num.ToString();
			num += sessiontime * 60;
			_form.UpdateUI(text + " " + num, "00:00");
			if (num < 1)
			{
				disabelKey.Add(qrText);
				return;
			}
			sessionData.EndTime = sessionData.StartTime;
			sessionData.EndTime = sessionData.EndTime.AddSeconds(sessiontime * 60);
			disabelKey.Add(qrText);
			_currentSession = sessionData;
			_relay.StartSession();
			StartTimer();
		}
	}

	private void StartTimer()
	{
		_timer?.Stop();
		_timer = new Timer
		{
			Interval = 1000
		};
		_timer.Tick += Timer_Tick;
		_timer.Start();
	}

	private void Timer_Tick(object? sender, EventArgs e)
	{
		if (_currentSession != null)
		{
			TimeSpan timeSpan = _currentSession.EndTime - DateTime.Now;
			if (timeSpan.TotalSeconds <= 0.0)
			{
				EndCurrentSession();
				return;
			}
			_form.UpdateUI("", $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}");
		}
	}

	public void EndCurrentSession()
	{
		_timer?.Stop();
		_relay.EndSession();
		_currentSession = null;
		_form.UpdateUI("", "00:00");
	}

	private string GetTimeLeft(SessionData session)
	{
		TimeSpan timeSpan = session.EndTime - DateTime.Now;
		if (!(timeSpan.TotalSeconds > 0.0))
		{
			return "00:00";
		}
		return $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
	}
}
