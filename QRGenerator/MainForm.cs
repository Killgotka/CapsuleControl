using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using QRCoder;

namespace QRGenerator;

public class MainForm : Form
{
    // ── controls ──
    private NumericUpDown nudCapsule = null!;
    private CheckBox chkExtension = null!;
    private DateTimePicker dtpStart = null!;
    private Button btnGenerate = null!;
    private Button btnSave = null!;
    private Button btnCopy = null!;
    private PictureBox picQR = null!;
    private Label lblCode = null!;
    private Panel pnlCard = null!;

    private Bitmap? _qrBitmap;
    private string _qrString = "";
    private Image? _logo;

    private static readonly Color BG = Color.FromArgb(18, 18, 18);
    private static readonly Color CARD = Color.FromArgb(30, 30, 30);
    private static readonly Color ACCENT = Color.FromArgb(99, 102, 241);
    private static readonly Color ACCENT_HOVER = Color.FromArgb(79, 82, 221);
    private static readonly Color TEXT = Color.FromArgb(240, 240, 240);
    private static readonly Color SUBTEXT = Color.FromArgb(140, 140, 140);
    private static readonly Color BORDER = Color.FromArgb(50, 50, 50);

    public MainForm()
    {
        BuildUI();
        LoadLogo();
    }

    private void LoadLogo()
    {
        string logoPath = Path.Combine(
            Path.GetDirectoryName(Application.ExecutablePath)!, "max_logo.png");
        if (File.Exists(logoPath))
            _logo = Image.FromFile(logoPath);
    }

    private void BuildUI()
    {
        SuspendLayout();

        BackColor = BG;
        ForeColor = TEXT;
        Font = new Font("Segoe UI", 10f);
        Text = "QR Generator — CapsuleControl";
        ClientSize = new Size(520, 620);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // ── title ──
        var lblTitle = MakeLabel("QR Generator", 0, 20, 520, 36);
        lblTitle.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
        lblTitle.TextAlign = ContentAlignment.MiddleCenter;
        lblTitle.ForeColor = TEXT;

        var lblSub = MakeLabel("Создание QR-кодов для переговорных капсул", 0, 58, 520, 22);
        lblSub.TextAlign = ContentAlignment.MiddleCenter;
        lblSub.ForeColor = SUBTEXT;
        lblSub.Font = new Font("Segoe UI", 9f);

        // ── card with inputs ──
        pnlCard = new Panel
        {
            Location = new Point(24, 92),
            Size = new Size(472, 200),
            BackColor = CARD
        };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(BORDER);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        // capsule id
        var lCapsule = MakeLabel("Номер капсулы", 16, 16, 160, 22, parent: pnlCard);
        nudCapsule = new NumericUpDown
        {
            Location = new Point(16, 40),
            Size = new Size(200, 30),
            Minimum = 1,
            Maximum = 999,
            Value = 1,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = TEXT,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 11f)
        };
        pnlCard.Controls.Add(nudCapsule);

        // extension
        chkExtension = new CheckBox
        {
            Text = "Продление сессии",
            Location = new Point(240, 44),
            Size = new Size(200, 26),
            ForeColor = TEXT,
            BackColor = CARD
        };
        pnlCard.Controls.Add(chkExtension);

        // start time
        var lDate = MakeLabel("Время начала сессии", 16, 88, 200, 22, parent: pnlCard);
        dtpStart = new DateTimePicker
        {
            Location = new Point(16, 112),
            Size = new Size(440, 30),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd.MM.yyyy   HH:mm",
            Value = DateTime.Now,
            CalendarForeColor = TEXT,
            CalendarTitleBackColor = ACCENT,
            CalendarTitleForeColor = Color.White,
            Font = new Font("Segoe UI", 11f)
        };
        pnlCard.Controls.Add(dtpStart);

        // quick buttons row
        var btnNow = MakeSmallButton("Сейчас", 16, 158, pnlCard);
        btnNow.Click += (s, e) => dtpStart.Value = DateTime.Now;

        var btnPlus30 = MakeSmallButton("+30 мин", 100, 158, pnlCard);
        btnPlus30.Click += (s, e) => dtpStart.Value = DateTime.Now.AddMinutes(30);

        var btnPlus1h = MakeSmallButton("+1 час", 190, 158, pnlCard);
        btnPlus1h.Click += (s, e) => dtpStart.Value = DateTime.Now.AddHours(1);

        // ── generate button ──
        btnGenerate = new Button
        {
            Text = "Сгенерировать QR",
            Location = new Point(24, 308),
            Size = new Size(472, 46),
            BackColor = ACCENT,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnGenerate.FlatAppearance.BorderSize = 0;
        btnGenerate.MouseEnter += (s, e) => btnGenerate.BackColor = ACCENT_HOVER;
        btnGenerate.MouseLeave += (s, e) => btnGenerate.BackColor = ACCENT;
        btnGenerate.Click += OnGenerate;

        // ── QR picture card ──
        var pnlQRCard = new Panel
        {
            Location = new Point(24, 368),
            Size = new Size(472, 200),
            BackColor = CARD
        };
        pnlQRCard.Paint += (s, e) =>
        {
            using var pen = new Pen(BORDER);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlQRCard.Width - 1, pnlQRCard.Height - 1);
        };

        picQR = new PictureBox
        {
            Location = new Point(16, 12),
            Size = new Size(176, 176),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White
        };
        pnlQRCard.Controls.Add(picQR);

        lblCode = new Label
        {
            Location = new Point(204, 12),
            Size = new Size(252, 100),
            ForeColor = SUBTEXT,
            Font = new Font("Consolas", 8f),
            Text = "QR-строка появится\nпосле генерации",
            AutoEllipsis = true
        };
        pnlQRCard.Controls.Add(lblCode);

        btnCopy = MakeSmallButton("Копировать строку", 204, 120, pnlQRCard);
        btnCopy.Size = new Size(252, 30);
        btnCopy.Enabled = false;
        btnCopy.Click += (s, e) =>
        {
            if (!string.IsNullOrEmpty(_qrString))
                Clipboard.SetText(_qrString);
        };

        btnSave = MakeSmallButton("Сохранить PNG", 204, 158, pnlQRCard);
        btnSave.Size = new Size(252, 30);
        btnSave.Enabled = false;
        btnSave.Click += OnSave;

        // ── assemble ──
        Controls.AddRange(new Control[]
        {
            lblTitle, lblSub, pnlCard, btnGenerate, pnlQRCard
        });

        ResumeLayout();
    }

    private void OnGenerate(object? sender, EventArgs e)
    {
        int capsuleId = (int)nudCapsule.Value;
        bool isExtension = chkExtension.Checked;
        DateTime startTime = dtpStart.Value;

        _qrString = QREncoder.Encode(capsuleId, isExtension, startTime);

        using var gen = new QRCodeGenerator();
        // ECCLevel.H — 30% коррекция ошибок, нужна для лого в центре
        using var data = gen.CreateQrCode(_qrString, QRCodeGenerator.ECCLevel.H);

        _qrBitmap?.Dispose();
        _qrBitmap = _logo != null
            ? StyledQRRenderer.Render(data, _logo)
            : StyledQRRenderer.Render(data, null!);

        picQR.Image = _qrBitmap;
        lblCode.ForeColor = TEXT;
        lblCode.Text = _qrString;
        btnSave.Enabled = true;
        btnCopy.Enabled = true;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_qrBitmap == null) return;
        using var dlg = new SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            FileName = $"capsule_{nudCapsule.Value}_{dtpStart.Value:yyyyMMdd_HHmm}.png"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
            _qrBitmap.Save(dlg.FileName, ImageFormat.Png);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _qrBitmap?.Dispose();
        _logo?.Dispose();
        base.OnFormClosed(e);
    }

    // ── helpers ──
    private static Label MakeLabel(string text, int x, int y, int w, int h, Control? parent = null)
    {
        var lbl = new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, h),
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Segoe UI", 9f),
            BackColor = Color.Transparent
        };
        parent?.Controls.Add(lbl);
        return lbl;
    }

    private static Button MakeSmallButton(string text, int x, int y, Control parent)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(80, 28),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.FromArgb(200, 200, 200),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
        parent.Controls.Add(btn);
        return btn;
    }
}
