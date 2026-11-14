using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowWatcher;

public sealed record WindowInfo(IntPtr Handle, string Title, string ProcessName);

public static class WindowService
{
    private const int SwRestore = 9;
    private const int DwmwaCloaked = 14;

    private static readonly ConcurrentDictionary<int, string> ProcessNameCache = new();

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", EntryPoint = "GetWindowRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRectNative(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll", EntryPoint = "GetClientRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRectNative(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint attachThreadId, uint attachToThreadId, [MarshalAs(UnmanagedType.Bool)] bool attach);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hWnd, int attribute, out int value, int valueSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public static List<WindowInfo> GetWindows()
    {
        var result = new List<WindowInfo>();
        var ownProcessId = (uint)Environment.ProcessId;

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd) || GetWindowTextLength(hWnd) == 0 || IsCloaked(hWnd))
            {
                return true;
            }

            GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == ownProcessId)
            {
                return true;
            }

            result.Add(new WindowInfo(hWnd, GetWindowTitle(hWnd), GetProcessName((int)processId)));
            return true;
        }, IntPtr.Zero);

        return result;
    }

    public static IntPtr FindWindow(RuleConfig rule)
    {
        var match = rule.WindowMatch?.Trim() ?? "";
        var processName = rule.ProcessName?.Trim() ?? "";
        if (match.Length == 0 && processName.Length == 0)
        {
            return IntPtr.Zero;
        }

        var found = IntPtr.Zero;
        var ownProcessId = (uint)Environment.ProcessId;

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd) || GetWindowTextLength(hWnd) == 0 || IsCloaked(hWnd))
            {
                return true;
            }

            GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == ownProcessId)
            {
                return true;
            }

            if (processName.Length > 0 &&
                !string.Equals(GetProcessName((int)processId), processName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (match.Length == 0)
            {
                found = hWnd;
                return false;
            }

            if (TitleMatches(GetWindowTitle(hWnd), match, rule.WindowMatchMode))
            {
                found = hWnd;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return found;
    }

    public static Rectangle GetWindowRectangle(IntPtr hWnd)
    {
        if (!GetWindowRectNative(hWnd, out var rect))
        {
            return Rectangle.Empty;
        }

        return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    public static Rectangle GetClientScreenRectangle(IntPtr hWnd)
    {
        if (!GetClientRectNative(hWnd, out var rect))
        {
            return Rectangle.Empty;
        }

        var origin = new POINT();
        if (!ClientToScreen(hWnd, ref origin))
        {
            return Rectangle.Empty;
        }

        return new Rectangle(origin.X, origin.Y, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public static bool Activate(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        if (IsIconic(hWnd))
        {
            ShowWindow(hWnd, SwRestore);
        }

        var foreground = GetForegroundWindow();
        var foregroundThread = foreground != IntPtr.Zero ? GetWindowThreadProcessId(foreground, out _) : 0;
        var targetThread = GetWindowThreadProcessId(hWnd, out _);
        var attached = false;

        try
        {
            if (foregroundThread != 0 && targetThread != 0 && foregroundThread != targetThread)
            {
                attached = AttachThreadInput(foregroundThread, targetThread, true);
            }

            BringWindowToTop(hWnd);
            SetForegroundWindow(hWnd);
        }
        finally
        {
            if (attached)
            {
                AttachThreadInput(foregroundThread, targetThread, false);
            }
        }

        return GetForegroundWindow() == hWnd;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length <= 0)
        {
            return "";
        }

        var builder = new StringBuilder(length + 1);
        GetWindowText(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetProcessName(int processId)
    {
        return ProcessNameCache.GetOrAdd(processId, static id =>
        {
            try
            {
                using var process = Process.GetProcessById(id);
                return process.ProcessName;
            }
            catch
            {
                return "";
            }
        });
    }

    private static bool IsCloaked(IntPtr hWnd)
    {
        try
        {
            return DwmGetWindowAttribute(hWnd, DwmwaCloaked, out var cloaked, sizeof(int)) == 0 && cloaked != 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TitleMatches(string title, string pattern, WindowMatchMode mode)
    {
        return mode switch
        {
            WindowMatchMode.Exact => string.Equals(title, pattern, StringComparison.OrdinalIgnoreCase),
            WindowMatchMode.Regex => SafeRegexMatch(title, pattern),
            _ => title.Contains(pattern, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static bool SafeRegexMatch(string input, string pattern)
    {
        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }
        catch
        {
            return false;
        }
    }
}
