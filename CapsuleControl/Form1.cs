using System;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using CapsuleControl.Properties;

namespace CapsuleControl;

public class Form1 : Form
{
	private readonly QRValidator _validator;

	private readonly RelayController _relay;

	private readonly CapsuleService _service;

	private readonly GlobalKeyboardHook _globalHook;

	private bool[] Digi = new bool[8];

	private bool flagAction;

	private IContainer components;

	private Label lblTimerL;

	private Label lblStatus;

	private Label lblTimerR;

	private Timer timer1;

	public Form1()
	{
		InitializeComponent();
		BackColor = Color.FromArgb(10, 10, 10);
		Text = "Переговорная Капсула";
		string relayIp = ConfigurationManager.AppSettings["RelayIP"] ?? "192.168.1.200";
		_validator = new QRValidator();
		_relay = new RelayController(relayIp);
		_service = new CapsuleService(_validator, _relay, this);
		_globalHook = new GlobalKeyboardHook(this);
	}

	public void ProcessQRCode(string qrText)
	{
		_service.ProcessQR(qrText);
	}

	public void UpdateUI(string status, string timerText)
	{
		lblStatus.Text = status;
		lblTimerL.Text = timerText.Remove(2);
		lblTimerR.Text = timerText.Remove(0, 3);
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		_globalHook?.Dispose();
		base.OnFormClosing(e);
	}

	private void lblTimer_Click(object sender, EventArgs e)
	{
	}

	private void lblTimerR_Click(object sender, EventArgs e)
	{
	}

	private void button1_Click(object sender, EventArgs e)
	{
	}

	private void timer1_Tick(object sender, EventArgs e)
	{
		Digi = _relay.ReadDigitalVal();
		for (int i = 0; i < Digi.Length; i++)
		{
			if (Digi[i])
			{
				if (!flagAction && !_service.GetActionSession())
				{
					flagAction = true;
					_relay.StartSession();
				}
				return;
			}
		}
		if (flagAction && !_service.GetActionSession())
		{
			_relay.EndSession();
			flagAction = false;
		}
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		this.lblTimerL = new System.Windows.Forms.Label();
		this.lblStatus = new System.Windows.Forms.Label();
		this.lblTimerR = new System.Windows.Forms.Label();
		this.timer1 = new System.Windows.Forms.Timer(this.components);
		base.SuspendLayout();
		this.lblTimerL.BackColor = System.Drawing.Color.Transparent;
		this.lblTimerL.Font = new System.Drawing.Font("Max Sans Medium", 185f, System.Drawing.FontStyle.Bold);
		this.lblTimerL.ForeColor = System.Drawing.Color.White;
		this.lblTimerL.Location = new System.Drawing.Point(51, 231);
		this.lblTimerL.Name = "lblTimerL";
		this.lblTimerL.RightToLeft = System.Windows.Forms.RightToLeft.No;
		this.lblTimerL.Size = new System.Drawing.Size(632, 317);
		this.lblTimerL.TabIndex = 1;
		this.lblTimerL.Text = "00";
		this.lblTimerL.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.lblTimerL.Click += new System.EventHandler(lblTimer_Click);
		this.lblStatus.BackColor = System.Drawing.Color.Transparent;
		this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 24f);
		this.lblStatus.ForeColor = System.Drawing.Color.White;
		this.lblStatus.Location = new System.Drawing.Point(229, 562);
		this.lblStatus.Name = "lblStatus";
		this.lblStatus.Size = new System.Drawing.Size(800, 120);
		this.lblStatus.TabIndex = 0;
		this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblStatus.Visible = false;
		this.lblTimerR.BackColor = System.Drawing.Color.Transparent;
		this.lblTimerR.Font = new System.Drawing.Font("Max Sans Medium", 185f, System.Drawing.FontStyle.Bold);
		this.lblTimerR.ForeColor = System.Drawing.Color.White;
		this.lblTimerR.Location = new System.Drawing.Point(619, 230);
		this.lblTimerR.Name = "lblTimerR";
		this.lblTimerR.RightToLeft = System.Windows.Forms.RightToLeft.No;
		this.lblTimerR.Size = new System.Drawing.Size(688, 317);
		this.lblTimerR.TabIndex = 3;
		this.lblTimerR.Text = "00";
		this.lblTimerR.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.lblTimerR.Click += new System.EventHandler(lblTimerR_Click);
		this.timer1.Enabled = true;
		this.timer1.Interval = 1000;
		this.timer1.Tick += new System.EventHandler(timer1_Tick);
		this.BackColor = System.Drawing.SystemColors.Control;
		this.BackgroundImage = CapsuleControl.Properties.Resources.bg;
		base.ClientSize = new System.Drawing.Size(1280, 720);
		base.Controls.Add(this.lblTimerL);
		base.Controls.Add(this.lblTimerR);
		base.Controls.Add(this.lblStatus);
		this.ForeColor = System.Drawing.SystemColors.Control;
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		base.Name = "Form1";
		this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		base.ResumeLayout(false);
	}
}
