using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowWatcher;

public static class InputService
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;

    private const ushort VkTab = 0x09;
    private const ushort VkReturn = 0x0D;
    private const ushort VkShift = 0xA0;
    private const ushort VkControl = 0xA2;
    private const ushort VkAlt = 0xA4;
    private const ushort VkWin = 0x5B;
    private const ushort VkEscape = 0x1B;
    private const ushort VkBack = 0x08;
    private const ushort VkDelete = 0x2E;
    private const ushort VkSpace = 0x20;

    private static readonly Dictionary<string, Keys> KeyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = Keys.Enter,
        ["Return"] = Keys.Enter,
        ["Esc"] = Keys.Escape,
        ["Escape"] = Keys.Escape,
        ["Space"] = Keys.Space,
        ["Spacebar"] = Keys.Space,
        ["Tab"] = Keys.Tab,
        ["Backspace"] = Keys.Back,
        ["Back"] = Keys.Back,
        ["Del"] = Keys.Delete,
        ["Delete"] = Keys.Delete,
        ["Up"] = Keys.Up,
        ["Down"] = Keys.Down,
        ["Left"] = Keys.Left,
        ["Right"] = Keys.Right,
        ["Home"] = Keys.Home,
        ["End"] = Keys.End,
        ["PgUp"] = Keys.PageUp,
        ["PageUp"] = Keys.PageUp,
        ["PgDn"] = Keys.PageDown,
        ["PageDown"] = Keys.PageDown,
        ["Ins"] = Keys.Insert,
        ["Insert"] = Keys.Insert,
        ["Plus"] = Keys.Oemplus,
        ["Minus"] = Keys.OemMinus,
        ["Comma"] = Keys.Oemcomma,
        ["Period"] = Keys.OemPeriod,
        ["Slash"] = Keys.OemQuestion,
        ["Semicolon"] = Keys.OemSemicolon,
        ["Quote"] = Keys.OemQuotes,
        ["Backslash"] = Keys.OemPipe,
        ["BracketLeft"] = Keys.OemOpenBrackets,
        ["BracketRight"] = Keys.OemCloseBrackets,
        ["Tilde"] = Keys.Oemtilde
    };

    public static async Task ExecuteAsync(IEnumerable<ActionStep> steps, CancellationToken cancellationToken)
    {
        foreach (var step in steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (step.Type)
            {
                case StepType.Text:
                    SendText(Unescape(step.Value));
                    break;
                case StepType.Keys:
                    foreach (var combo in step.Value.Split(
                                 ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        SendKeyCombo(combo);
                    }

                    break;
                case StepType.Delay:
                    await Task.Delay(Math.Max(0, step.DelayAfterMs), cancellationToken).ConfigureAwait(false);
                    continue;
            }

            if (step.DelayAfterMs > 0)
            {
                await Task.Delay(step.DelayAfterMs, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public static void SendText(string text)
    {
        var inputs = new List<INPUT>();
        foreach (var character in text)
        {
            switch (character)
            {
                case '\r':
                    continue;
                case '\n':
                    AddKeyPress(inputs, VkReturn);
                    continue;
                case '\t':
                    AddKeyPress(inputs, VkTab);
                    continue;
            }

            inputs.Add(UnicodeInput(character, false));
            inputs.Add(UnicodeInput(character, true));
        }

        Send(inputs);
    }

    public static void SendKeyCombo(string combo)
    {
        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var modifiers = new List<ushort>();
        for (var i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers.Add(VkControl);
                    break;
                case "shift":
                    modifiers.Add(VkShift);
                    break;
                case "alt":
                    modifiers.Add(VkAlt);
                    break;
                case "win":
                case "windows":
                    modifiers.Add(VkWin);
                    break;
                default:
                    throw new InvalidOperationException($"未知修饰键：{parts[i]}");
            }
        }

        var key = ParseKey(parts[^1]);
        var inputs = new List<INPUT>();

        foreach (var modifier in modifiers)
        {
            inputs.Add(KeyInput(modifier, false));
        }

        inputs.Add(KeyInput((ushort)key, false));
        inputs.Add(KeyInput((ushort)key, true));

        for (var i = modifiers.Count - 1; i >= 0; i--)
        {
            inputs.Add(KeyInput(modifiers[i], true));
        }

        Send(inputs);
    }

    private static void AddKeyPress(List<INPUT> inputs, ushort virtualKey)
    {
        inputs.Add(KeyInput(virtualKey, false));
        inputs.Add(KeyInput(virtualKey, true));
    }

    private static Keys ParseKey(string name)
    {
        var value = name.Trim();
        if (value.Length == 1)
        {
            var character = char.ToUpperInvariant(value[0]);
            if (character >= 'A' && character <= 'Z')
            {
                return (Keys)character;
            }

            if (character >= '0' && character <= '9')
            {
                return (Keys)((int)Keys.D0 + (character - '0'));
            }
        }

        if (KeyAliases.TryGetValue(value, out var alias))
        {
            return alias;
        }

        if (Enum.TryParse<Keys>(value, true, out var parsed) && parsed != Keys.None)
        {
            return parsed;
        }

        throw new InvalidOperationException($"无法识别的按键：{name}");
    }

    private static string Unescape(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\' || i + 1 >= value.Length)
            {
                builder.Append(value[i]);
                continue;
            }

            var next = value[++i];
            switch (next)
            {
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case '\\':
                    builder.Append('\\');
                    break;
                default:
                    builder.Append(next);
                    break;
            }
        }

        return builder.ToString();
    }

    private static void Send(List<INPUT> inputs)
    {
        if (inputs.Count == 0)
        {
            return;
        }

        var array = inputs.ToArray();
        var sent = SendInput((uint)array.Length, array, Marshal.SizeOf<INPUT>());
        if (sent != array.Length)
        {
            throw new InvalidOperationException(
                $"SendInput 失败：{new Win32Exception(Marshal.GetLastWin32Error()).Message}");
        }
    }

    private static INPUT KeyInput(ushort virtualKey, bool keyUp) => new()
    {
        Type = InputKeyboard,
        Data = new InputUnion
        {
            Keyboard = new KEYBDINPUT
            {
                VirtualKey = virtualKey,
                ScanCode = 0,
                Flags = keyUp ? KeyEventKeyUp : 0,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            }
        }
    };

    private static INPUT UnicodeInput(char character, bool keyUp) => new()
    {
        Type = InputKeyboard,
        Data = new InputUnion
        {
            Keyboard = new KEYBDINPUT
            {
                VirtualKey = 0,
                ScanCode = character,
                Flags = (keyUp ? KeyEventKeyUp : 0) | KeyEventUnicode,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            }
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT Mouse;
        [FieldOffset(0)] public KEYBDINPUT Keyboard;
        [FieldOffset(0)] public HARDWAREINPUT Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint Message;
        public ushort ParameterLow;
        public ushort ParameterHigh;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, INPUT[] inputs, int inputSize);
}
