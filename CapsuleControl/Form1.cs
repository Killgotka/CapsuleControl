using System;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using CapsuleControl.Properties;

namespace CapsuleControl;

public enum StatusKind { Ready, Active, Error }

public class Form1 : Form
{
    private readonly QRValidator         _validator;
    private readonly RelayController     _relay;
    private readonly CapsuleService      _service;
    private readonly GlobalKeyboardHook  _globalHook;

    private Label lblTimerL = null!;
    private Label lblTimerR = null!;
    private Label lblStatus = null!;

    public Form1()
    {
        string relayIp = ConfigurationManager.AppSettings["RelayIP"] ?? "192.168.1.200";

        _validator  = new QRValidator();
        _relay      = new RelayController(relayIp);
        _service    = new CapsuleService(_validator, _relay, this);
        _globalHook = new GlobalKeyboardHook(this);

        _service.RelayConnectionChanged += OnRelayConnectionChanged;

        FontManager.Load();
        InitializeComponent();
        ApplyRelayStatus(_relay.IsConnected);

        Logger.Info("Form1 initialized");
    }

    // ── public API для CapsuleService ────────────────────────────────────

    public void UpdateTimer(string timerText)
    {
        if (InvokeRequired) { Invoke(() => UpdateTimer(timerText)); return; }
        lblTimerL.Text = timerText[..2];
        lblTimerR.Text = timerText[3..];
    }

    public void SetStatus(string message, StatusKind kind)
    {
        if (InvokeRequired) { Invoke(() => SetStatus(message, kind)); return; }
        lblStatus.Text      = message;
        lblStatus.Visible   = !string.IsNullOrEmpty(message);
        lblStatus.ForeColor = kind switch
        {
            StatusKind.Active => Color.FromArgb(100, 220, 130),
            StatusKind.Error  => Color.FromArgb(255, 90, 90),
            _                 => Color.FromArgb(200, 200, 200),
        };
    }

    public void ProcessQRCode(string qrText) => _service.ProcessQR(qrText);

    // ─────────────────────────────────────────────────────────────────────

    private void OnRelayConnectionChanged(bool connected)
    {
        if (InvokeRequired) { Invoke(() => OnRelayConnectionChanged(connected)); return; }
        ApplyRelayStatus(connected);
    }

    private void ApplyRelayStatus(bool connected)
    {
        if (_service.SessionActive) return;

        if (!connected)
            SetStatus("Реле недоступно", StatusKind.Error);
        else
            SetStatus("", StatusKind.Ready); // чистый экран когда всё ок
    }

    // ── lifecycle ─────────────────────────────────────────────────────────

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _globalHook?.Dispose();
        _service?.Dispose();
        _relay?.Dispose();
        base.OnFormClosing(e);
    }

    // ── UI ────────────────────────────────────────────────────────────────

    private void InitializeComponent()
    {
        SuspendLayout();

        BackColor       = Color.FromArgb(10, 10, 10);
        BackgroundImage = Resources.bg;
        ClientSize      = new Size(1280, 720);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition   = FormStartPosition.CenterScreen;
        Text            = "Переговорная Капсула";

        lblTimerL = new Label
        {
            Text      = "00",
            Font      = FontManager.Get("Max Sans Medium", 185f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Location  = new Point(51, 231),
            Size      = new Size(632, 317),
            TextAlign = ContentAlignment.MiddleRight,
        };

        lblTimerR = new Label
        {
            Text      = "00",
            Font      = FontManager.Get("Max Sans Medium", 185f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Location  = new Point(619, 230),
            Size      = new Size(688, 317),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        lblStatus = new Label
        {
            Text      = "",
            Font      = new Font("Segoe UI", 24f),
            ForeColor = Color.FromArgb(200, 200, 200),
            BackColor = Color.Transparent,
            Location  = new Point(229, 562),
            Size      = new Size(800, 80),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible   = false,
        };

        Controls.Add(lblTimerL);
        Controls.Add(lblTimerR);
        Controls.Add(lblStatus);

        ResumeLayout(false);
    }
}
