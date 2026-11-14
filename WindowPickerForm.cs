namespace WindowWatcher;

public sealed class WindowPickerForm : Form
{
    private readonly ListView _listView;

    public WindowInfo? SelectedWindow =>
        _listView.SelectedItems.Count > 0 ? _listView.SelectedItems[0].Tag as WindowInfo : null;

    private WindowPickerForm()
    {
        Text = "选择目标窗口";
        Size = new Size(880, 580);
        MinimumSize = new Size(660, 420);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9F);

        _listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        _listView.Columns.Add("窗口标题", 560);
        _listView.Columns.Add("进程", 200);
        _listView.DoubleClick += (_, _) => Accept();
        _listView.Resize += (_, _) =>
        {
            if (_listView.Columns.Count >= 2)
            {
                _listView.Columns[0].Width = Math.Max(
                    260, _listView.ClientSize.Width - _listView.Columns[1].Width - 30);
            }
        };

        foreach (var window in WindowService.GetWindows()
                     .OrderBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(w => w.Title, StringComparer.OrdinalIgnoreCase))
        {
            var item = new ListViewItem(window.Title) { Tag = window };
            item.SubItems.Add(window.ProcessName);
            _listView.Items.Add(item);
        }

        var hint = new Label
        {
            Text = "选择要监控的窗口（窗口标题可在主界面继续编辑）：",
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(14, 12, 14, 0),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12, 12, 12, 12)
        };

        var buttonOk = new Button
        {
            Text = "确定", Width = 104, Height = 36, DialogResult = DialogResult.OK,
            Margin = new Padding(10, 0, 0, 0)
        };
        buttonOk.Click += (_, _) => Accept();

        var buttonCancel = new Button
        {
            Text = "取消", Width = 104, Height = 36, DialogResult = DialogResult.Cancel
        };

        buttons.Controls.Add(buttonOk);
        buttons.Controls.Add(buttonCancel);
        AcceptButton = buttonOk;
        CancelButton = buttonCancel;

        var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 0, 14, 0) };
        listPanel.Controls.Add(_listView);

        Controls.Add(listPanel);
        Controls.Add(buttons);
        Controls.Add(hint);

        if (_listView.Items.Count > 0)
        {
            _listView.Items[0].Selected = true;
            _listView.Items[0].Focused = true;
        }
    }

    public static WindowInfo? Pick(IWin32Window owner)
    {
        using var form = new WindowPickerForm();
        return form.ShowDialog(owner) == DialogResult.OK ? form.SelectedWindow : null;
    }

    private void Accept()
    {
        if (SelectedWindow == null)
        {
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
