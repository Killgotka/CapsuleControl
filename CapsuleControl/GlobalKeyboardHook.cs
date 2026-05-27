using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CapsuleControl;

public class GlobalKeyboardHook : IDisposable
{
	private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

	private readonly Form1 _form;

	private nint _hookID = IntPtr.Zero;

	private readonly LowLevelKeyboardProc _proc;

	private string _buffer = "";

	private DateTime _lastCharTime = DateTime.MinValue;

	private bool _shiftDown;

	private const double STALE_BUFFER_SECONDS = 4.0;

	private const int WH_KEYBOARD_LL = 13;

	private const int WM_KEYDOWN = 256;

	private const int WM_KEYUP = 257;

	private const int WM_SYSKEYDOWN = 260;

	private const int WM_SYSKEYUP = 261;

	public GlobalKeyboardHook(Form1 form)
	{
		_form = form;
		_proc = HookCallback;
		_hookID = SetHook(_proc);
	}

	private nint SetHook(LowLevelKeyboardProc proc)
	{
		using Process process = Process.GetCurrentProcess();
		using ProcessModule processModule = process.MainModule;
		return SetWindowsHookEx(13, proc, GetModuleHandle(processModule?.ModuleName), 0u);
	}

	private nint HookCallback(int nCode, nint wParam, nint lParam)
	{
		if (nCode >= 0)
		{
			Keys keys = (Keys)Marshal.ReadInt32(lParam);
			switch (wParam)
			{
			case 256:
			case 260:
				switch (keys)
				{
				case Keys.ShiftKey:
				case Keys.LShiftKey:
				case Keys.RShiftKey:
					_shiftDown = true;
					break;
				case Keys.Return:
					if (string.IsNullOrWhiteSpace(_buffer))
					{
						break;
					}
					if ((DateTime.Now - _lastCharTime).TotalSeconds <= 4.0)
					{
						string qrText = _buffer.Trim();
						_buffer = "";
						_form.BeginInvoke(delegate
						{
							_form.ProcessQRCode(qrText);
						});
					}
					else
					{
						_buffer = "";
					}
					break;
				default:
				{
					char c = KeyToChar(keys, _shiftDown);
					if (c != 0)
					{
						ReadOnlySpan<char> readOnlySpan = _buffer;
						char reference = c;
						_buffer = string.Concat(readOnlySpan, new ReadOnlySpan<char>(ref reference));
						_lastCharTime = DateTime.Now;
					}
					break;
				}
				}
				break;
			case 257:
			case 261:
				if ((keys == Keys.ShiftKey || (uint)(keys - 160) <= 1u) ? true : false)
				{
					_shiftDown = false;
				}
				break;
			}
		}
		return CallNextHookEx(_hookID, nCode, wParam, lParam);
	}

	private static char KeyToChar(Keys key, bool shift)
	{
		if (key >= Keys.A && key <= Keys.Z)
		{
			if (!shift)
			{
				return (char)(97 + (key - 65));
			}
			return (char)(65 + (key - 65));
		}
		if (key >= Keys.D0 && key <= Keys.D9)
		{
			if (!shift)
			{
				return (char)(48 + (key - 48));
			}
			return key switch
			{
				Keys.D1 => '!', 
				Keys.D2 => '@', 
				Keys.D3 => '#', 
				Keys.D4 => '$', 
				Keys.D5 => '%', 
				Keys.D6 => '^', 
				Keys.D7 => '&', 
				Keys.D8 => '*', 
				Keys.D9 => '(', 
				Keys.D0 => ')', 
				_ => '\0', 
			};
		}
		if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
		{
			return (char)(48 + (key - 96));
		}
		return key switch
		{
			Keys.OemMinus => shift ? '_' : '-', 
			Keys.OemPeriod => shift ? '>' : '.', 
			Keys.Oemcomma => shift ? '<' : ',', 
			Keys.OemQuestion => shift ? '?' : '/', 
			_ => '\0', 
		};
	}

	public void Dispose()
	{
		if (_hookID != IntPtr.Zero)
		{
			UnhookWindowsHookEx(_hookID);
			_hookID = IntPtr.Zero;
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWindowsHookEx(nint hhk);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern nint GetModuleHandle(string? lpModuleName);
}
