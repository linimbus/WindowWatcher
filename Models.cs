namespace WindowWatcher;

public enum WindowMatchMode
{
    Contains = 0,
    Exact = 1,
    Regex = 2
}

public enum ConditionMode
{
    ContainsAny = 0,
    ContainsAll = 1,
    Regex = 2,
    NotContainsAny = 3
}

public enum TriggerMode
{
    Edge = 0,
    Repeat = 1
}

public enum StepType
{
    Text = 0,
    Keys = 1,
    Delay = 2
}

public sealed class ActionStep
{
    public StepType Type { get; set; } = StepType.Text;

    public string Value { get; set; } = "";

    public int DelayAfterMs { get; set; } = 50;
}

public sealed class RuleConfig
{
    public string Name { get; set; } = "新规则";

    public bool Enabled { get; set; } = true;

    public string ProcessName { get; set; } = "";

    public string WindowMatch { get; set; } = "";

    public WindowMatchMode WindowMatchMode { get; set; } = WindowMatchMode.Contains;

    public int RegionX { get; set; }

    public int RegionY { get; set; }

    public int RegionWidth { get; set; } = 400;

    public int RegionHeight { get; set; } = 120;

    public string OcrLanguage { get; set; } = "";

    public double OcrScale { get; set; } = 2.0;

    public bool IgnoreWhitespace { get; set; } = true;

    public ConditionMode ConditionMode { get; set; } = ConditionMode.ContainsAny;

    public string ConditionText { get; set; } = "";

    public bool IgnoreCase { get; set; } = true;

    public TriggerMode TriggerMode { get; set; } = TriggerMode.Edge;

    public int CooldownMs { get; set; } = 1000;

    public int CheckIntervalMs { get; set; } = 500;

    public bool ActivateBeforeCapture { get; set; }

    public bool ActivateBeforeAction { get; set; } = true;

    public bool LogRecognizedText { get; set; }

    public List<ActionStep> Steps { get; set; } = new();

    public override string ToString() => Enabled ? Name : $"{Name}（已禁用）";
}

public sealed class AppConfig
{
    public int SchedulerIntervalMs { get; set; } = 60;

    public List<RuleConfig> Rules { get; set; } = new();
}
