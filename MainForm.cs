using System.Text.RegularExpressions;

namespace WindowWatcher;

public sealed class MainForm : Form
{
    private readonly AppConfig _config;
    private readonly List<string> _ocrLanguages;

    private readonly ListBox _ruleList = new();
    private readonly Panel _editorPanel = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly SplitContainer _mainSplit = new();
    private readonly SplitContainer _rightSplit = new();
    private readonly ListBox _logList = new();

    private readonly Button _btnStart = new() { Text = "启动监控", Location = new Point(16, 11), Width = 110, Height = 36 };
    private readonly Button _btnStop = new() { Text = "停止监控", Location = new Point(138, 11), Width = 110, Height = 36, Enabled = false };
    private readonly Button _btnSave = new() { Text = "保存配置", Location = new Point(260, 11), Width = 110, Height = 36 };
    private readonly Label _statusLabel = new();

    private readonly TextBox _txtName = new()
    {
        Location = new Point(88, 30), Width = 260, Height = 30, AutoSize = false
    };
    private readonly CheckBox _chkEnabled = new()
    {
        Text = "启用规则", Location = new Point(362, 31), Width = 92, Height = 30, AutoSize = false
    };
    private readonly NumericUpDown _numInterval = new()
    {
        Location = new Point(578, 30), Width = 100, Height = 30, Minimum = 100, Maximum = 60000,
        Increment = 100, Value = 500
    };
    private readonly TextBox _txtWindowMatch = new()
    {
        Location = new Point(88, 78), Width = 250, Height = 30, AutoSize = false
    };
    private readonly Button _btnPickWindow = new() { Text = "选择窗口…", Location = new Point(346, 76), Width = 110, Height = 34 };
    private readonly ComboBox _cmbWindowMatch = new()
    {
        Location = new Point(538, 78), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly TextBox _txtProcess = new()
    {
        Location = new Point(88, 126), Width = 250, Height = 30, AutoSize = false,
        ReadOnly = true, BackColor = SystemColors.Control
    };

    private readonly NumericUpDown _numX = new();
    private readonly NumericUpDown _numY = new();
    private readonly NumericUpDown _numW = new();
    private readonly NumericUpDown _numH = new();
    private readonly Button _btnSelectRegion = new() { Text = "框选区域", Location = new Point(544, 32), Width = 104, Height = 34 };
    private readonly Button _btnTestOcr = new() { Text = "预览/识别", Location = new Point(656, 32), Width = 116, Height = 34 };

    private readonly ComboBox _cmbLang = new()
    {
        Location = new Point(92, 34), Width = 190, DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly NumericUpDown _numScale = new()
    {
        Location = new Point(376, 34), Width = 78, Height = 30, Minimum = 1, Maximum = 4, Increment = 0.5m,
        DecimalPlaces = 1, Value = 2
    };
    private readonly CheckBox _chkIgnoreWhitespace = new()
    {
        Text = "忽略空白", Location = new Point(472, 35), Width = 90, Height = 30, AutoSize = false, Checked = true
    };
    private readonly CheckBox _chkLogOcr = new()
    {
        Text = "记录识别文本", Location = new Point(578, 35), Width = 120, Height = 30, AutoSize = false
    };

    private readonly ComboBox _cmbConditionMode = new()
    {
        Location = new Point(92, 34), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly CheckBox _chkIgnoreCase = new()
    {
        Text = "忽略大小写", Location = new Point(288, 35), Width = 100, Height = 30, AutoSize = false, Checked = true
    };
    private readonly TextBox _txtCondition = new()
    {
        Location = new Point(16, 106), Width = 748, Height = 118, Multiline = true,
        ScrollBars = ScrollBars.Vertical, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
    };

    private readonly ComboBox _cmbTriggerMode = new()
    {
        Location = new Point(92, 34), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly NumericUpDown _numCooldown = new()
    {
        Location = new Point(352, 34), Width = 92, Height = 30, Minimum = 0, Maximum = 600000,
        Increment = 500, Value = 1000
    };
    private readonly CheckBox _chkActivateCapture = new()
    {
        Text = "识别前激活窗口", Location = new Point(458, 35), Width = 128, Height = 30, AutoSize = false
    };
    private readonly CheckBox _chkActivateAction = new()
    {
        Text = "按键前激活窗口", Location = new Point(598, 35), Width = 168, Height = 30, AutoSize = false,
        Checked = true
    };

    private readonly DataGridView _stepsGrid = new();
    private readonly FlowLayoutPanel _actionsFlow = new();
    private readonly Label _lblActionsHint = new();

    private readonly Button _btnAddText = new() { Text = "添加文字", Width = 96, Height = 34, Margin = new Padding(0, 0, 8, 0) };
    private readonly Button _btnAddKeys = new() { Text = "添加按键", Width = 96, Height = 34, Margin = new Padding(0, 0, 8, 0) };
    private readonly Button _btnAddDelay = new() { Text = "添加延迟", Width = 96, Height = 34, Margin = new Padding(0, 0, 8, 0) };
    private readonly Button _btnDelStep = new() { Text = "删除步骤", Width = 96, Height = 34, Margin = new Padding(0, 0, 8, 0) };
    private readonly Button _btnUpStep = new() { Text = "上移", Width = 76, Height = 34, Margin = new Padding(0, 0, 8, 0) };
    private readonly Button _btnDownStep = new() { Text = "下移", Width = 76, Height = 34, Margin = new Padding(0, 0, 8, 0) };
    private readonly Button _btnTestAction = new() { Text = "测试动作", Width = 100, Height = 34, Margin = new Padding(0, 0, 8, 0) };

    private GroupBox _gTarget = null!;
    private GroupBox _gRegion = null!;
    private GroupBox _gOcr = null!;
    private GroupBox _gCondition = null!;
    private GroupBox _gStrategy = null!;
    private GroupBox _gActions = null!;

    private MonitorService? _monitor;
    private RuleConfig? _currentRule;
    private bool _loading;
    private bool _syncingList;

    public MainForm()
    {
        _config = ConfigStore.Load();
        _ocrLanguages = LoadOcrLanguages();

        if (_config.Rules.Count == 0)
        {
            _config.Rules.Add(CreateDefaultRule());
        }

        foreach (var rule in _config.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.OcrLanguage) && _ocrLanguages.Count > 0)
            {
                rule.OcrLanguage = PickDefaultOcrLanguage(_ocrLanguages);
            }
        }

        InitializeUi();
        LoadRules();
        AppendLog("就绪。请选择目标窗口、框选监控区域、设置条件与动作，然后启动监控。");
        AppendLog("提示：目标窗口需要保持可见；若目标程序以管理员身份运行，本程序也需要以管理员身份运行。");
    }

    private static List<string> LoadOcrLanguages()
    {
        try
        {
            return OcrService.AvailableLanguages.ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static string PickDefaultOcrLanguage(IReadOnlyList<string> languages)
    {
        return languages.FirstOrDefault(l => l.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
               ?? languages.FirstOrDefault(l => l.StartsWith("en", StringComparison.OrdinalIgnoreCase))
               ?? languages.FirstOrDefault()
               ?? "";
    }

    private RuleConfig CreateDefaultRule() => new()
    {
        Name = $"规则 {_config.Rules.Count + 1}",
        OcrLanguage = PickDefaultOcrLanguage(_ocrLanguages),
        OcrScale = 2.0,
        Steps = new List<ActionStep>()
    };

    private void InitializeUi()
    {
        SuspendLayout();

        Text = "窗口区域文本监控助手";
        Size = new Size(1240, 840);
        MinimumSize = new Size(1140, 660);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Microsoft YaHei UI", 9F);

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 58 };
        _btnStart.Click += BtnStart_Click;
        _btnStop.Click += BtnStop_Click;
        _btnSave.Click += BtnSave_Click;

        _statusLabel.Text = "状态：已停止";
        _statusLabel.Location = new Point(392, 11);
        _statusLabel.Size = new Size(260, 36);
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.ForeColor = Color.DimGray;
        _statusLabel.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);

        toolbar.Controls.AddRange(new Control[] { _btnStart, _btnStop, _btnSave, _statusLabel });

        BuildEditor();
        _editorPanel.Resize += (_, _) => LayoutEditor();

        var ruleGroup = new GroupBox { Text = "规则", Dock = DockStyle.Fill, Padding = new Padding(10) };
        _ruleList.Dock = DockStyle.Fill;
        _ruleList.IntegralHeight = false;
        _ruleList.DrawMode = DrawMode.OwnerDrawFixed;
        _ruleList.ItemHeight = 30;
        _ruleList.DrawItem += RuleList_DrawItem;
        _ruleList.SelectedIndexChanged += RuleList_SelectedIndexChanged;

        var ruleButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(0, 10, 0, 8)
        };
        ruleButtons.Controls.Add(MkButton("新建", 72, 34, BtnNewRule_Click));
        ruleButtons.Controls.Add(MkButton("复制", 72, 34, BtnCopyRule_Click));
        ruleButtons.Controls.Add(MkButton("删除", 72, 34, BtnDeleteRule_Click));

        ruleGroup.Controls.Add(_ruleList);
        ruleGroup.Controls.Add(ruleButtons);

        _mainSplit.Dock = DockStyle.Fill;
        _mainSplit.Orientation = Orientation.Vertical;
        _mainSplit.SplitterWidth = 8;
        _mainSplit.Panel1MinSize = 200;
        _mainSplit.FixedPanel = FixedPanel.Panel1;
        _mainSplit.Panel1.Padding = new Padding(6);
        _mainSplit.Panel1.Controls.Add(ruleGroup);

        _rightSplit.Dock = DockStyle.Fill;
        _rightSplit.Orientation = Orientation.Horizontal;
        _rightSplit.SplitterWidth = 8;
        _rightSplit.FixedPanel = FixedPanel.Panel2;
        _rightSplit.Panel1.Controls.Add(_editorPanel);

        var logGroup = new GroupBox { Text = "日志", Dock = DockStyle.Fill, Padding = new Padding(10) };
        _logList.Dock = DockStyle.Fill;
        _logList.IntegralHeight = false;
        _logList.HorizontalScrollbar = true;

        var logButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 46, FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 0, 0, 8)
        };
        logButtons.Controls.Add(MkButton("清空日志", 100, 34, (_, _) => _logList.Items.Clear()));

        logGroup.Controls.Add(_logList);
        logGroup.Controls.Add(logButtons);
        _rightSplit.Panel2.Controls.Add(logGroup);

        _mainSplit.Panel2.Padding = new Padding(0, 6, 6, 6);
        _mainSplit.Panel2.Controls.Add(_rightSplit);

        Controls.Add(_mainSplit);
        Controls.Add(toolbar);

        _cmbWindowMatch.Items.AddRange(new object[] { "包含", "精确", "正则" });
        _cmbConditionMode.Items.AddRange(new object[] { "包含任一词", "包含全部词", "正则匹配", "不包含任一词" });
        _cmbTriggerMode.Items.AddRange(new object[] { "边沿触发", "重复触发" });
        _cmbWindowMatch.SelectedIndex = 0;
        _cmbConditionMode.SelectedIndex = 0;
        _cmbTriggerMode.SelectedIndex = 0;

        foreach (var language in _ocrLanguages)
        {
            _cmbLang.Items.Add(language);
        }

        _txtName.TextChanged += TxtName_TextChanged;

        Shown += MainForm_Shown;
        FormClosing += MainForm_FormClosing;

        ResumeLayout(false);
    }

    private void BuildEditor()
    {
        _gTarget = new GroupBox { Text = "目标窗口", Location = new Point(12, 12), Size = new Size(780, 168) };
        _gRegion = new GroupBox
        {
            Text = "监控区域（相对目标窗口客户区，单位：像素）", Location = new Point(12, 192),
            Size = new Size(780, 112)
        };
        _gOcr = new GroupBox { Text = "OCR 识别", Location = new Point(12, 316), Size = new Size(780, 112) };
        _gCondition = new GroupBox { Text = "触发条件", Location = new Point(12, 440), Size = new Size(780, 240) };
        _gStrategy = new GroupBox { Text = "触发策略", Location = new Point(12, 692), Size = new Size(780, 126) };
        _gActions = new GroupBox
        {
            Text = "执行动作（按顺序执行，发送到当前前台窗口）", Location = new Point(12, 830),
            Size = new Size(780, 382)
        };

        _gTarget.Controls.AddRange(new Control[]
        {
            MkLabel("规则名称", 16, 32, 66), _txtName, _chkEnabled,
            MkLabel("检测间隔(ms)", 470, 32, 104), _numInterval,
            MkLabel("匹配文本", 16, 80, 66), _txtWindowMatch, _btnPickWindow,
            MkLabel("匹配方式", 468, 80, 66), _cmbWindowMatch,
            MkLabel("标题匹配方式", 656, 80, 116),
            MkLabel("进程名", 16, 128, 66), _txtProcess,
            MkLabel("留空表示不限制进程", 350, 128, 220)
        });

        ConfigureNumeric(_numX, 38, 34, 74, -10000, 20000);
        ConfigureNumeric(_numY, 144, 34, 74, -10000, 20000);
        ConfigureNumeric(_numW, 256, 34, 74, 1, 20000, 400);
        ConfigureNumeric(_numH, 368, 34, 74, 1, 20000, 120);

        _gRegion.Controls.AddRange(new Control[]
        {
            MkLabel("X", 16, 36, 20), _numX, MkLabel("Y", 122, 36, 20), _numY,
            MkLabel("宽", 228, 36, 26), _numW, MkLabel("高", 340, 36, 26), _numH,
            MkLabel("相对客户区", 446, 36, 94), _btnSelectRegion, _btnTestOcr
        });

        ConfigureNumeric(_numScale, 376, 34, 78, 1, 4, 2, 1, 0.5m);

        _gOcr.Controls.AddRange(new Control[]
        {
            MkLabel("识别语言", 16, 36, 70), _cmbLang,
            MkLabel("放大倍数", 300, 36, 70), _numScale,
            _chkIgnoreWhitespace, _chkLogOcr
        });

        _gCondition.Controls.AddRange(new Control[]
        {
            MkLabel("条件模式", 16, 36, 70), _cmbConditionMode, _chkIgnoreCase,
            MkLabel("每行一个词；正则模式使用整个文本", 400, 36, 370),
            MkLabel("匹配文本", 16, 80, 70),
            MkLabel("提示：OCR 可能带入空格，可勾选上方“忽略空白”", 92, 80, 520, Color.Gray),
            _txtCondition
        });

        _gStrategy.Controls.AddRange(new Control[]
        {
            MkLabel("触发模式", 16, 36, 70), _cmbTriggerMode,
            MkLabel("冷却(ms)", 278, 36, 70), _numCooldown,
            _chkActivateCapture, _chkActivateAction,
            MkLabel("边沿触发：条件开始满足时执行一次；重复触发：满足期间每隔冷却时间执行一次", 16, 86, 740, Color.Gray)
        });

        _stepsGrid.Location = new Point(16, 34);
        _stepsGrid.Size = new Size(748, 240);
        _stepsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _stepsGrid.AllowUserToAddRows = false;
        _stepsGrid.AllowUserToDeleteRows = false;
        _stepsGrid.RowHeadersVisible = false;
        _stepsGrid.MultiSelect = true;
        _stepsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _stepsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _stepsGrid.BackgroundColor = SystemColors.Window;
        _stepsGrid.EditMode = DataGridViewEditMode.EditOnEnter;
        _stepsGrid.RowTemplate.Height = 32;
        _stepsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _stepsGrid.ColumnHeadersHeight = 38;

        var typeColumn = new DataGridViewComboBoxColumn
        {
            HeaderText = "类型", Name = "colType", Width = 130,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None, FlatStyle = FlatStyle.Flat
        };
        typeColumn.Items.AddRange("文字输入", "按键", "延迟");

        var valueColumn = new DataGridViewTextBoxColumn
        {
            HeaderText = "内容", Name = "colValue", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };
        var delayColumn = new DataGridViewTextBoxColumn
        {
            HeaderText = "之后延迟(ms)", Name = "colDelay", Width = 140,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };

        _stepsGrid.Columns.AddRange(typeColumn, valueColumn, delayColumn);
        _stepsGrid.DataError += (_, e) => e.ThrowException = false;

        _actionsFlow.Location = new Point(16, 290);
        _actionsFlow.Size = new Size(748, 40);
        _actionsFlow.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _actionsFlow.WrapContents = true;

        _btnAddText.Click += (_, _) => AddStepRow("文字输入", "");
        _btnAddKeys.Click += (_, _) => AddStepRow("按键", "Enter");
        _btnAddDelay.Click += (_, _) => AddStepRow("延迟", "");
        _btnDelStep.Click += BtnDeleteStep_Click;
        _btnUpStep.Click += (_, _) => MoveStep(-1);
        _btnDownStep.Click += (_, _) => MoveStep(1);
        _btnTestAction.Click += BtnTestAction_Click;

        _actionsFlow.Controls.AddRange(new Control[]
        {
            _btnAddText, _btnAddKeys, _btnAddDelay, _btnDelStep,
            _btnUpStep, _btnDownStep, _btnTestAction
        });

        _lblActionsHint.Text =
            "按键写法：Up / Down / Left / Right / Enter / Tab / Esc / Ctrl+A；多个用逗号分隔。文字支持 \\n \\t。";
        _lblActionsHint.Location = new Point(16, 340);
        _lblActionsHint.Size = new Size(748, 22);
        _lblActionsHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblActionsHint.ForeColor = Color.Gray;

        _gActions.Controls.AddRange(new Control[] { _stepsGrid, _actionsFlow, _lblActionsHint });

        _editorPanel.Controls.Add(_gTarget);
        _editorPanel.Controls.Add(_gRegion);
        _editorPanel.Controls.Add(_gOcr);
        _editorPanel.Controls.Add(_gCondition);
        _editorPanel.Controls.Add(_gStrategy);
        _editorPanel.Controls.Add(_gActions);

        _btnPickWindow.Click += BtnPickWindow_Click;
        _btnSelectRegion.Click += BtnSelectRegion_Click;
        _btnTestOcr.Click += BtnTestOcr_Click;
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        FitToWorkingArea();

        try
        {
            _mainSplit.SplitterDistance = 280;
        }
        catch
        {
        }

        try
        {
            _rightSplit.SplitterDistance = Math.Max(340, _rightSplit.Height - 210);
        }
        catch
        {
        }

        LayoutEditor();
    }

    private void FitToWorkingArea()
    {
        var area = Screen.FromControl(this).WorkingArea;
        var width = Math.Min(Width, Math.Max(MinimumSize.Width, area.Width - 24));
        var height = Math.Min(Height, Math.Max(MinimumSize.Height, area.Height - 24));
        if (width == Width && height == Height)
        {
            return;
        }

        Size = new Size(width, height);
        Location = new Point(
            area.Left + Math.Max(0, (area.Width - Width) / 2),
            area.Top + Math.Max(0, (area.Height - Height) / 2));
    }

    private void LayoutEditor()
    {
        if (_gTarget is null)
        {
            return;
        }

        var width = Math.Max(780, _editorPanel.ClientSize.Width - 28);
        foreach (var group in new[] { _gTarget, _gRegion, _gOcr, _gCondition, _gStrategy, _gActions })
        {
            group.Width = width;
        }

        _txtCondition.Width = Math.Max(300, width - 32);
        _stepsGrid.Width = Math.Max(300, width - 32);
        _actionsFlow.Width = Math.Max(300, width - 32);
        _lblActionsHint.Width = Math.Max(300, width - 32);
        _editorPanel.AutoScrollMinSize = new Size(0, _gActions.Bottom + 16);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveCurrentEditorToIfAny();
        try
        {
            ConfigStore.Save(_config);
        }
        catch
        {
        }

        try
        {
            _monitor?.StopAsync().Wait(2000);
        }
        catch
        {
        }
    }

    private void LoadRules()
    {
        _loading = true;
        try
        {
            _ruleList.BeginUpdate();
            _ruleList.Items.Clear();
            foreach (var rule in _config.Rules)
            {
                _ruleList.Items.Add(rule);
            }

            _ruleList.EndUpdate();

            if (_ruleList.Items.Count > 0)
            {
                _ruleList.SelectedIndex = 0;
            }
        }
        finally
        {
            _loading = false;
        }

        if (_ruleList.SelectedItem is RuleConfig selected)
        {
            LoadEditorFrom(selected);
        }
    }

    private void RuleList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_loading || _syncingList)
        {
            return;
        }

        var previous = _currentRule;
        if (previous != null)
        {
            SaveCurrentEditorTo(previous);
        }

        if (_ruleList.SelectedItem is RuleConfig selected && !ReferenceEquals(selected, previous))
        {
            LoadEditorFrom(selected);
        }
    }

    private void RuleList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _ruleList.Items.Count)
        {
            return;
        }

        e.DrawBackground();
        var selected = (e.State & DrawItemState.Selected) != 0;
        var color = selected ? SystemColors.HighlightText : SystemColors.ControlText;
        var bounds = new Rectangle(
            e.Bounds.X + 10,
            e.Bounds.Y,
            Math.Max(0, e.Bounds.Width - 12),
            e.Bounds.Height);

        TextRenderer.DrawText(
            e.Graphics,
            _ruleList.Items[e.Index]?.ToString() ?? "",
            _ruleList.Font,
            bounds,
            color,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        if ((e.State & DrawItemState.Focus) != 0)
        {
            e.DrawFocusRectangle();
        }
    }

    private void TxtName_TextChanged(object? sender, EventArgs e)
    {
        if (_loading || _currentRule == null)
        {
            return;
        }

        var name = _txtName.Text.Trim();
        if (name.Length == 0)
        {
            return;
        }

        _currentRule.Name = name;
        RefreshListItem(_currentRule);
    }

    private void LoadEditorFrom(RuleConfig rule)
    {
        _loading = true;
        try
        {
            _currentRule = rule;
            _txtName.Text = rule.Name;
            _chkEnabled.Checked = rule.Enabled;
            _txtWindowMatch.Text = rule.WindowMatch;
            _txtProcess.Text = rule.ProcessName;
            _cmbWindowMatch.SelectedIndex = Math.Clamp((int)rule.WindowMatchMode, 0, _cmbWindowMatch.Items.Count - 1);
            _numInterval.Value = ClampToRange(rule.CheckIntervalMs, _numInterval);

            _numX.Value = ClampToRange(rule.RegionX, _numX);
            _numY.Value = ClampToRange(rule.RegionY, _numY);
            _numW.Value = ClampToRange(rule.RegionWidth, _numW);
            _numH.Value = ClampToRange(rule.RegionHeight, _numH);

            _cmbLang.SelectedItem = rule.OcrLanguage;
            if (_cmbLang.SelectedIndex < 0 && _cmbLang.Items.Count > 0)
            {
                _cmbLang.SelectedIndex = 0;
            }

            _numScale.Value = ClampToRange((decimal)rule.OcrScale, _numScale);
            _chkIgnoreWhitespace.Checked = rule.IgnoreWhitespace;
            _chkLogOcr.Checked = rule.LogRecognizedText;

            _cmbConditionMode.SelectedIndex =
                Math.Clamp((int)rule.ConditionMode, 0, _cmbConditionMode.Items.Count - 1);
            _txtCondition.Text = rule.ConditionText;
            _chkIgnoreCase.Checked = rule.IgnoreCase;

            _cmbTriggerMode.SelectedIndex =
                Math.Clamp((int)rule.TriggerMode, 0, _cmbTriggerMode.Items.Count - 1);
            _numCooldown.Value = ClampToRange(rule.CooldownMs, _numCooldown);
            _chkActivateCapture.Checked = rule.ActivateBeforeCapture;
            _chkActivateAction.Checked = rule.ActivateBeforeAction;

            LoadStepsToGrid(rule.Steps);
        }
        finally
        {
            _loading = false;
        }
    }

    private void SaveCurrentEditorToIfAny()
    {
        if (_currentRule != null)
        {
            SaveCurrentEditorTo(_currentRule);
        }
    }

    private void SaveCurrentEditorTo(RuleConfig rule)
    {
        var name = _txtName.Text.Trim();
        if (name.Length > 0)
        {
            rule.Name = name;
        }

        rule.Enabled = _chkEnabled.Checked;
        rule.WindowMatch = _txtWindowMatch.Text.Trim();
        rule.ProcessName = _txtProcess.Text.Trim();
        rule.WindowMatchMode = (WindowMatchMode)Math.Max(0, _cmbWindowMatch.SelectedIndex);
        rule.CheckIntervalMs = (int)_numInterval.Value;

        rule.RegionX = (int)_numX.Value;
        rule.RegionY = (int)_numY.Value;
        rule.RegionWidth = (int)_numW.Value;
        rule.RegionHeight = (int)_numH.Value;

        if (_cmbLang.SelectedItem != null)
        {
            rule.OcrLanguage = _cmbLang.SelectedItem.ToString() ?? rule.OcrLanguage;
        }

        rule.OcrScale = (double)_numScale.Value;
        rule.IgnoreWhitespace = _chkIgnoreWhitespace.Checked;
        rule.LogRecognizedText = _chkLogOcr.Checked;

        rule.ConditionMode = (ConditionMode)Math.Max(0, _cmbConditionMode.SelectedIndex);
        rule.ConditionText = _txtCondition.Text;
        rule.IgnoreCase = _chkIgnoreCase.Checked;

        rule.TriggerMode = (TriggerMode)Math.Max(0, _cmbTriggerMode.SelectedIndex);
        rule.CooldownMs = (int)_numCooldown.Value;
        rule.ActivateBeforeCapture = _chkActivateCapture.Checked;
        rule.ActivateBeforeAction = _chkActivateAction.Checked;

        rule.Steps = ReadStepsFromGrid();
        RefreshListItem(rule);
    }

    private void RefreshListItem(RuleConfig rule)
    {
        var index = _config.Rules.IndexOf(rule);
        if (index < 0 || index >= _ruleList.Items.Count)
        {
            return;
        }

        var oldValue = _syncingList;
        _syncingList = true;
        try
        {
            _ruleList.Items[index] = rule;
        }
        finally
        {
            _syncingList = oldValue;
        }
    }

    private void LoadStepsToGrid(IReadOnlyList<ActionStep> steps)
    {
        _stepsGrid.Rows.Clear();
        foreach (var step in steps)
        {
            var typeText = step.Type switch
            {
                StepType.Keys => "按键",
                StepType.Delay => "延迟",
                _ => "文字输入"
            };

            _stepsGrid.Rows.Add(typeText, step.Value, step.DelayAfterMs.ToString());
        }
    }

    private List<ActionStep> ReadStepsFromGrid()
    {
        var steps = new List<ActionStep>();
        foreach (DataGridViewRow row in _stepsGrid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var typeText = Convert.ToString(row.Cells["colType"].Value) ?? "文字输入";
            var type = typeText switch
            {
                "按键" => StepType.Keys,
                "延迟" => StepType.Delay,
                _ => StepType.Text
            };

            var value = Convert.ToString(row.Cells["colValue"].Value) ?? "";
            var delayText = Convert.ToString(row.Cells["colDelay"].Value);
            var delay = 0;
            if (!string.IsNullOrWhiteSpace(delayText))
            {
                int.TryParse(delayText, out delay);
            }

            steps.Add(new ActionStep
            {
                Type = type,
                Value = value,
                DelayAfterMs = Math.Max(0, delay)
            });
        }

        return steps;
    }

    private void AddStepRow(string typeText, string value)
    {
        var index = _stepsGrid.Rows.Add(typeText, value, typeText == "延迟" ? "500" : "50");
        _stepsGrid.CurrentCell = _stepsGrid.Rows[index].Cells["colValue"];
        _stepsGrid.BeginEdit(true);
    }

    private void BtnDeleteStep_Click(object? sender, EventArgs e)
    {
        _stepsGrid.EndEdit();
        if (_stepsGrid.SelectedRows.Count == 0)
        {
            return;
        }

        foreach (var row in _stepsGrid.SelectedRows.Cast<DataGridViewRow>()
                     .OrderByDescending(r => r.Index)
                     .ToList())
        {
            if (!row.IsNewRow)
            {
                _stepsGrid.Rows.Remove(row);
            }
        }
    }

    private void MoveStep(int delta)
    {
        _stepsGrid.EndEdit();
        if (_stepsGrid.CurrentRow == null)
        {
            return;
        }

        var index = _stepsGrid.CurrentRow.Index;
        var target = index + delta;
        if (target < 0 || target >= _stepsGrid.Rows.Count)
        {
            return;
        }

        var rows = _stepsGrid.Rows.Cast<DataGridViewRow>()
            .Where(r => !r.IsNewRow)
            .Select(r => (
                Type: Convert.ToString(r.Cells["colType"].Value) ?? "文字输入",
                Value: Convert.ToString(r.Cells["colValue"].Value) ?? "",
                Delay: Convert.ToString(r.Cells["colDelay"].Value) ?? "0"))
            .ToList();

        (rows[index], rows[target]) = (rows[target], rows[index]);

        _stepsGrid.Rows.Clear();
        foreach (var row in rows)
        {
            _stepsGrid.Rows.Add(row.Type, row.Value, row.Delay);
        }

        _stepsGrid.CurrentCell = _stepsGrid.Rows[target].Cells["colValue"];
    }

    private void BtnPickWindow_Click(object? sender, EventArgs e)
    {
        var window = WindowPickerForm.Pick(this);
        if (window == null)
        {
            return;
        }

        _txtWindowMatch.Text = window.Title;
        _txtProcess.Text = window.ProcessName;
        _cmbWindowMatch.SelectedIndex = (int)WindowMatchMode.Contains;
        AppendLog($"已选择窗口：{window.Title}（{window.ProcessName}）");
    }

    private void BtnSelectRegion_Click(object? sender, EventArgs e)
    {
        if (_currentRule == null)
        {
            return;
        }

        SaveCurrentEditorTo(_currentRule);
        var hWnd = WindowService.FindWindow(_currentRule);
        if (hWnd == IntPtr.Zero)
        {
            MessageBox.Show(
                this, "未找到目标窗口。请先选择窗口或填写匹配文本。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (WindowService.IsIconic(hWnd))
        {
            WindowService.Activate(hWnd);
        }

        var windowRectangle = WindowService.GetWindowRectangle(hWnd);
        if (windowRectangle.Width <= 0 || windowRectangle.Height <= 0)
        {
            MessageBox.Show(this, "无法获取目标窗口的位置。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var selector = new RegionSelectorForm(windowRectangle);
        if (selector.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var client = WindowService.GetClientScreenRectangle(hWnd);
        var intersection = Rectangle.Intersect(selector.SelectedScreenRect, client);
        if (intersection.Width < 2 || intersection.Height < 2)
        {
            MessageBox.Show(
                this, "选区不在目标窗口的客户区内，请重新框选。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _numX.Value = ClampToRange(intersection.X - client.X, _numX);
        _numY.Value = ClampToRange(intersection.Y - client.Y, _numY);
        _numW.Value = ClampToRange(intersection.Width, _numW);
        _numH.Value = ClampToRange(intersection.Height, _numH);

        SaveCurrentEditorTo(_currentRule);
        AppendLog($"已设置区域：X={intersection.X - client.X}, Y={intersection.Y - client.Y}, " +
                  $"{intersection.Width}×{intersection.Height}");
    }

    private async void BtnTestOcr_Click(object? sender, EventArgs e)
    {
        if (_currentRule == null)
        {
            return;
        }

        SaveCurrentEditorTo(_currentRule);

        try
        {
            var hWnd = WindowService.FindWindow(_currentRule);
            if (hWnd == IntPtr.Zero)
            {
                MessageBox.Show(
                    this, "未找到目标窗口。请先选择窗口或填写匹配文本。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (WindowService.IsIconic(hWnd))
            {
                WindowService.Activate(hWnd);
            }

            var client = WindowService.GetClientScreenRectangle(hWnd);
            var region = Rectangle.Intersect(
                new Rectangle(
                    client.X + _currentRule.RegionX,
                    client.Y + _currentRule.RegionY,
                    _currentRule.RegionWidth,
                    _currentRule.RegionHeight),
                client);

            if (region.Width < 2 || region.Height < 2)
            {
                MessageBox.Show(this, "监控区域无效。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var bitmap = new ScreenCaptureService().Capture(region);
            var ocr = new OcrService();
            ocr.EnsureEngine(_currentRule.OcrLanguage);
            var text = await ocr.RecognizeAsync(bitmap, _currentRule.OcrScale);

            using var preview = new PreviewForm(bitmap, text);
            preview.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "识别失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnTestAction_Click(object? sender, EventArgs e)
    {
        if (_currentRule == null)
        {
            return;
        }

        SaveCurrentEditorTo(_currentRule);
        var steps = ReadStepsFromGrid();
        if (steps.Count == 0)
        {
            MessageBox.Show(this, "请先添加动作步骤。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var hWnd = WindowService.FindWindow(_currentRule);
            if (hWnd != IntPtr.Zero && _currentRule.ActivateBeforeAction)
            {
                WindowService.Activate(hWnd);
                await Task.Delay(200);
            }

            await InputService.ExecuteAsync(steps, CancellationToken.None);
            AppendLog("已执行测试动作");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "执行失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnNewRule_Click(object? sender, EventArgs e)
    {
        SaveCurrentEditorToIfAny();
        var rule = CreateDefaultRule();
        _config.Rules.Add(rule);
        RebuildListSelect(_config.Rules.Count - 1);
    }

    private void BtnCopyRule_Click(object? sender, EventArgs e)
    {
        SaveCurrentEditorToIfAny();
        if (_currentRule == null)
        {
            return;
        }

        var copy = ConfigStore.Clone(_currentRule);
        copy.Name = _currentRule.Name + " 副本";
        var index = _config.Rules.IndexOf(_currentRule);
        _config.Rules.Insert(index + 1, copy);
        RebuildListSelect(index + 1);
    }

    private void BtnDeleteRule_Click(object? sender, EventArgs e)
    {
        SaveCurrentEditorToIfAny();
        var index = _ruleList.SelectedIndex;
        if (index < 0)
        {
            return;
        }

        if (_config.Rules.Count <= 1)
        {
            MessageBox.Show(this, "至少需要保留一个规则。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _config.Rules.RemoveAt(index);
        _currentRule = null;
        RebuildListSelect(Math.Min(index, _config.Rules.Count - 1));
    }

    private void RebuildListSelect(int index)
    {
        _loading = true;
        try
        {
            _ruleList.BeginUpdate();
            _ruleList.Items.Clear();
            foreach (var rule in _config.Rules)
            {
                _ruleList.Items.Add(rule);
            }

            _ruleList.EndUpdate();

            if (_config.Rules.Count > 0)
            {
                _ruleList.SelectedIndex = Math.Clamp(index, 0, _config.Rules.Count - 1);
            }
        }
        finally
        {
            _loading = false;
        }

        if (_ruleList.SelectedItem is RuleConfig selected)
        {
            LoadEditorFrom(selected);
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        SaveCurrentEditorToIfAny();
        try
        {
            ConfigStore.Save(_config);
            AppendLog($"配置已保存：{ConfigStore.ConfigPath}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_monitor != null)
        {
            return;
        }

        SaveCurrentEditorToIfAny();
        var errors = ValidateRules();
        if (errors.Count > 0)
        {
            MessageBox.Show(
                this, string.Join(Environment.NewLine, errors), "配置检查",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            ConfigStore.Save(_config);
        }
        catch
        {
        }

        var clone = ConfigStore.Clone(_config);
        _monitor = new MonitorService();
        _monitor.Log += AppendLog;
        _monitor.Start(clone);
        SetRunning(true);
        AppendLog("监控已启动。运行中修改配置后，需要停止并重新启动才会生效。");
    }

    private async void BtnStop_Click(object? sender, EventArgs e)
    {
        await StopMonitoringAsync();
    }

    private async Task StopMonitoringAsync()
    {
        if (_monitor == null)
        {
            return;
        }

        var monitor = _monitor;
        _monitor = null;
        monitor.Log -= AppendLog;
        await monitor.StopAsync();
        monitor.Dispose();
        SetRunning(false);
        AppendLog("监控已停止。");
    }

    private void SetRunning(bool running)
    {
        _btnStart.Enabled = !running;
        _btnStop.Enabled = running;
        _statusLabel.Text = running ? "状态：运行中" : "状态：已停止";
        _statusLabel.ForeColor = running ? Color.ForestGreen : Color.DimGray;
    }

    private List<string> ValidateRules()
    {
        var errors = new List<string>();
        var enabledRules = _config.Rules.Where(r => r.Enabled).ToList();
        if (enabledRules.Count == 0)
        {
            errors.Add("没有启用任何规则。");
        }

        foreach (var rule in enabledRules)
        {
            var prefix = $"规则「{rule.Name}」：";

            if (string.IsNullOrWhiteSpace(rule.WindowMatch) && string.IsNullOrWhiteSpace(rule.ProcessName))
            {
                errors.Add(prefix + "未设置目标窗口（匹配文本或进程名）。");
            }

            if (rule.RegionWidth <= 0 || rule.RegionHeight <= 0)
            {
                errors.Add(prefix + "监控区域尺寸无效。");
            }

            if (string.IsNullOrWhiteSpace(rule.ConditionText))
            {
                errors.Add(prefix + "未设置匹配条件。");
            }

            if (!_ocrLanguages.Contains(rule.OcrLanguage))
            {
                errors.Add(prefix + $"OCR 语言不可用：{rule.OcrLanguage}。");
            }

            if (rule.WindowMatchMode == WindowMatchMode.Regex && rule.WindowMatch.Length > 0)
            {
                try
                {
                    _ = new Regex(rule.WindowMatch);
                }
                catch (Exception ex)
                {
                    errors.Add(prefix + "窗口匹配正则无效：" + ex.Message);
                }
            }

            if (rule.ConditionMode == ConditionMode.Regex && rule.ConditionText.Trim().Length > 0)
            {
                try
                {
                    _ = new Regex(rule.ConditionText);
                }
                catch (Exception ex)
                {
                    errors.Add(prefix + "条件正则无效：" + ex.Message);
                }
            }

            if (rule.Steps.Any(s => s.Type == StepType.Keys && string.IsNullOrWhiteSpace(s.Value)))
            {
                errors.Add(prefix + "存在空的按键步骤。");
            }
        }

        return errors;
    }

    private void AppendLog(string message)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action<string>(AppendLog), message);
            }
            catch (InvalidOperationException)
            {
            }

            return;
        }

        _logList.Items.Add($"{DateTime.Now:HH:mm:ss}  {message}");
        while (_logList.Items.Count > 2000)
        {
            _logList.Items.RemoveAt(0);
        }

        _logList.TopIndex = _logList.Items.Count - 1;
    }

    private static decimal ClampToRange(decimal value, NumericUpDown numeric)
    {
        return Math.Min(numeric.Maximum, Math.Max(numeric.Minimum, value));
    }

    private static void ConfigureNumeric(
        NumericUpDown numeric,
        int x,
        int y,
        int width,
        decimal minimum,
        decimal maximum,
        decimal value = 0,
        int decimalPlaces = 0,
        decimal increment = 1)
    {
        numeric.Location = new Point(x, y);
        numeric.Width = width;
        numeric.Height = 30;
        numeric.Minimum = minimum;
        numeric.Maximum = maximum;
        numeric.DecimalPlaces = decimalPlaces;
        numeric.Increment = increment;
        numeric.Value = Math.Clamp(value, minimum, maximum);
    }

    private static Label MkLabel(string text, int x, int y, int width, Color? color = null) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(width, 26),
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = color ?? SystemColors.ControlText
    };

    private static Button MkButton(string text, int width, int height, EventHandler onClick)
    {
        var button = new Button { Text = text, Width = width, Height = height, Margin = new Padding(0, 0, 8, 0) };
        button.Click += onClick;
        return button;
    }
}
