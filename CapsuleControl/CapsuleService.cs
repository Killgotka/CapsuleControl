using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Windows.Forms;

namespace CapsuleControl;

public class CapsuleService : IDisposable
{
    private readonly QRValidator    _validator;
    private readonly RelayController _relay;
    private readonly Form1          _form;

    private readonly int _sessionDuration;

    private SessionData? _currentSession;
    private Timer?       _sessionTimer;
    private Timer?       _inputPollTimer;
    private Timer?       _reconnectTimer;
    private bool         _physicalOverride;

    // QR-коды использованные сегодня — персистентны (выживают перезапуск)
    private readonly HashSet<string> _usedQRs = new();
    private static readonly string UsedQRFile =
        Path.Combine(AppPaths.Data, $"used_qr_{DateTime.Now:yyyy-MM-dd}.txt");

    public event Action<bool>? RelayConnectionChanged;

    public bool SessionActive => _currentSession != null;

    public CapsuleService(QRValidator validator, RelayController relay, Form1 form)
    {
        _validator = validator;
        _relay     = relay;
        _form      = form;

        _sessionDuration = int.TryParse(
            ConfigurationManager.AppSettings["SessionDurationMinutes"], out int d) ? d : 25;

        LoadUsedQRs();
        StartInputPolling();
        StartReconnectWatcher();

        Logger.Info($"CapsuleService started. SessionDuration={_sessionDuration}min, " +
                    $"Relay={(_relay.IsConnected ? "connected" : "disconnected")}");
    }

    // ── QR processing ──────────────────────────────────────────────────

    public void ProcessQR(string qrText)
    {
        Logger.Info($"QR scanned: {qrText[..Math.Min(12, qrText.Length)]}...");

        if (_usedQRs.Contains(qrText))
        {
            Logger.Warn("QR already used — ignored");
            return;
        }

        SessionData? session = _validator.Validate(qrText);
        if (session == null)
        {
            Logger.Warn("QR validation failed");
            _form.SetStatus("Неверный QR-код", StatusKind.Error);
            return;
        }

        // проверка даты целиком (год + месяц + день)
        if (session.StartTime.Date != DateTime.Today)
        {
            Logger.Warn($"QR date mismatch: {session.StartTime:yyyy-MM-dd} vs today {DateTime.Today:yyyy-MM-dd}");
            _form.SetStatus("QR недействителен сегодня", StatusKind.Error);
            return;
        }

        double secondsUntilEnd = (session.StartTime - DateTime.Now).TotalSeconds + _sessionDuration * 60;
        if (secondsUntilEnd < 1)
        {
            Logger.Warn("QR expired");
            MarkUsed(qrText);
            _form.SetStatus("Время сессии истекло", StatusKind.Error);
            return;
        }

        // всё ок — стартуем
        MarkUsed(qrText);
        session.EndTime = DateTime.Now.AddSeconds(secondsUntilEnd);
        _currentSession = session;

        Logger.Info($"Session started: capsule={session.Capsule}, " +
                    $"extension={session.IsExtension}, ends={session.EndTime:HH:mm:ss}");

        _relay.StartSession();
        _form.SetStatus(session.IsExtension ? "Продление сессии" : "Сессия активна", StatusKind.Active);
        StartSessionTimer();
    }

    // ── session lifecycle ──────────────────────────────────────────────

    private void StartSessionTimer()
    {
        _sessionTimer?.Stop();
        _sessionTimer?.Dispose();
        _sessionTimer = new Timer { Interval = 1000 };
        _sessionTimer.Tick += OnSessionTick;
        _sessionTimer.Start();
    }

    private void OnSessionTick(object? sender, EventArgs e)
    {
        if (_currentSession == null) return;

        TimeSpan left = _currentSession.EndTime - DateTime.Now;
        if (left.TotalSeconds <= 0)
        {
            EndCurrentSession();
            return;
        }

        _form.UpdateTimer($"{(int)left.TotalMinutes:D2}:{left.Seconds:D2}");
    }

    public void EndCurrentSession()
    {
        _sessionTimer?.Stop();
        _sessionTimer?.Dispose();
        _sessionTimer = null;

        _physicalOverride = false;
        _relay.EndSession();
        _currentSession = null;

        Logger.Info("Session ended");
        _form.UpdateTimer("00:00");
        _form.SetStatus("Готово к работе", StatusKind.Ready);
    }

    // ── digital input polling (физическая кнопка/датчик) ──────────────

    private void StartInputPolling()
    {
        _inputPollTimer = new Timer { Interval = 1000 };
        _inputPollTimer.Tick += OnInputPoll;
        _inputPollTimer.Start();
    }

    private void OnInputPoll(object? sender, EventArgs e)
    {
        if (!_relay.IsConnected) return;

        bool[] inputs = _relay.ReadDigitalInputs();
        bool anyActive = Array.Exists(inputs, v => v);

        if (anyActive && !SessionActive && !_physicalOverride)
        {
            _physicalOverride = true;
            _relay.StartSession();
            Logger.Info("Physical input triggered relay ON");
        }
        else if (!anyActive && _physicalOverride && !SessionActive)
        {
            _physicalOverride = false;
            _relay.EndSession();
            Logger.Info("Physical input released, relay OFF");
        }
    }

    // ── reconnect watcher ─────────────────────────────────────────────

    private void StartReconnectWatcher()
    {
        _reconnectTimer = new Timer { Interval = 10_000 };
        _reconnectTimer.Tick += OnReconnectTick;
        _reconnectTimer.Start();
    }

    private void OnReconnectTick(object? sender, EventArgs e)
    {
        bool was = _relay.IsConnected;
        if (!_relay.IsConnected)
        {
            Logger.Info("Attempting relay reconnect...");
            _relay.TryReconnect();
        }
        if (was != _relay.IsConnected)
            RelayConnectionChanged?.Invoke(_relay.IsConnected);
    }

    // ── used QR persistence ───────────────────────────────────────────

    private void LoadUsedQRs()
    {
        try
        {
            if (!File.Exists(UsedQRFile)) return;
            foreach (string line in File.ReadAllLines(UsedQRFile))
            {
                string qr = line.Trim();
                if (!string.IsNullOrEmpty(qr)) _usedQRs.Add(qr);
            }
            Logger.Info($"Loaded {_usedQRs.Count} used QRs from disk");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load used QRs", ex);
        }
    }

    private void MarkUsed(string qrText)
    {
        _usedQRs.Add(qrText);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(UsedQRFile)!);
            File.AppendAllText(UsedQRFile, qrText + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to persist used QR", ex);
        }
    }

    // ─────────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _sessionTimer?.Dispose();
        _inputPollTimer?.Dispose();
        _reconnectTimer?.Dispose();
    }
}
