namespace WindowWatcher;

public sealed class MonitorService : IDisposable
{
    private readonly ScreenCaptureService _capture = new();
    private readonly OcrService _ocr = new();
    private readonly Dictionary<RuleConfig, RuleState> _states = new();

    private AppConfig? _config;
    private CancellationTokenSource? _cancellation;
    private Task? _loopTask;

    public event Action<string>? Log;

    public bool IsRunning => _loopTask is { IsCompleted: false };

    public void Start(AppConfig config)
    {
        if (IsRunning)
        {
            return;
        }

        _config = config;
        _states.Clear();
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        _loopTask = Task.Run(() => LoopAsync(token));
    }

    public async Task StopAsync()
    {
        var cancellation = _cancellation;
        if (cancellation == null)
        {
            return;
        }

        _cancellation = null;
        try
        {
            cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        var loopTask = _loopTask;
        _loopTask = null;
        if (loopTask != null)
        {
            try
            {
                await loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
        }

        cancellation.Dispose();
    }

    public void Dispose()
    {
        try
        {
            _cancellation?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        _cancellation?.Dispose();
        _cancellation = null;
    }

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        var config = _config;
        if (config == null)
        {
            return;
        }

        var nextRun = new Dictionary<RuleConfig, DateTime>();

        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var rule in config.Rules)
            {
                if (!rule.Enabled)
                {
                    continue;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                if (nextRun.TryGetValue(rule, out var due) && now < due)
                {
                    continue;
                }

                nextRun[rule] = now.AddMilliseconds(Math.Max(100, rule.CheckIntervalMs));

                try
                {
                    await ProcessRuleAsync(rule, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    var state = GetState(rule);
                    state.Matched = false;
                    LogRule(rule, state, $"处理失败：{ex.Message}");
                }
            }

            try
            {
                await Task.Delay(Math.Max(30, config.SchedulerIntervalMs), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task ProcessRuleAsync(RuleConfig rule, CancellationToken cancellationToken)
    {
        var state = GetState(rule);
        var hWnd = WindowService.FindWindow(rule);

        if (hWnd == IntPtr.Zero || !WindowService.IsWindow(hWnd))
        {
            state.Matched = false;
            LogRule(rule, state, "未找到目标窗口");
            return;
        }

        if (WindowService.IsIconic(hWnd))
        {
            WindowService.Activate(hWnd);
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        }

        if (rule.ActivateBeforeCapture)
        {
            WindowService.Activate(hWnd);
            await Task.Delay(80, cancellationToken).ConfigureAwait(false);
        }

        var client = WindowService.GetClientScreenRectangle(hWnd);
        if (client.Width <= 0 || client.Height <= 0)
        {
            state.Matched = false;
            LogRule(rule, state, "无法获取目标窗口客户区");
            return;
        }

        var region = Rectangle.Intersect(
            new Rectangle(
                client.X + rule.RegionX,
                client.Y + rule.RegionY,
                rule.RegionWidth,
                rule.RegionHeight),
            client);

        if (region.Width < 2 || region.Height < 2)
        {
            state.Matched = false;
            LogRule(rule, state, "监控区域超出窗口客户区或尺寸过小");
            return;
        }

        using var bitmap = _capture.Capture(region);
        _ocr.EnsureEngine(rule.OcrLanguage);
        var text = await _ocr.RecognizeAsync(bitmap, rule.OcrScale).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (rule.LogRecognizedText)
        {
            Log?.Invoke($"[{rule.Name}] 识别：{Summarize(text)}");
        }

        var matched = ConditionMatcher.IsMatch(rule, text);
        var wasMatched = state.Matched;
        state.Matched = matched;

        if (!matched)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var shouldFire = !wasMatched || rule.TriggerMode == TriggerMode.Repeat;
        if (!shouldFire)
        {
            return;
        }

        if ((now - state.LastFireUtc).TotalMilliseconds < rule.CooldownMs)
        {
            return;
        }

        state.LastFireUtc = now;
        Log?.Invoke($"[{rule.Name}] 条件命中，执行 {rule.Steps.Count} 个动作步骤");

        if (rule.ActivateBeforeAction && WindowService.GetForegroundWindow() != hWnd)
        {
            WindowService.Activate(hWnd);
            await Task.Delay(150, cancellationToken).ConfigureAwait(false);
        }

        await InputService.ExecuteAsync(rule.Steps, cancellationToken).ConfigureAwait(false);
    }

    private RuleState GetState(RuleConfig rule)
    {
        if (!_states.TryGetValue(rule, out var state))
        {
            state = new RuleState();
            _states[rule] = state;
        }

        return state;
    }

    private void LogRule(RuleConfig rule, RuleState state, string message)
    {
        var now = DateTime.UtcNow;
        if (state.LastMessage == message && (now - state.LastMessageUtc).TotalSeconds < 5)
        {
            return;
        }

        state.LastMessage = message;
        state.LastMessageUtc = now;
        Log?.Invoke($"[{rule.Name}] {message}");
    }

    private static string Summarize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "（空）";
        }

        var value = text.Replace("\r", " ").Replace("\n", " ").Trim();
        return value.Length <= 300 ? value : value[..300] + "…";
    }

    private sealed class RuleState
    {
        public bool Matched;
        public DateTime LastFireUtc = DateTime.MinValue;
        public string LastMessage = "";
        public DateTime LastMessageUtc = DateTime.MinValue;
    }
}
