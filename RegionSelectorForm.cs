namespace WindowWatcher;

public sealed class RegionSelectorForm : Form
{
    private Point _start;
    private Point _current;
    private bool _dragging;

    public Rectangle SelectedScreenRect { get; private set; }

    public RegionSelectorForm(Rectangle windowRectangle)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = windowRectangle;
        TopMost = true;
        ShowInTaskbar = false;
        Opacity = 0.4;
        BackColor = Color.Black;
        Cursor = Cursors.Cross;
        KeyPreview = true;
        DoubleBuffered = true;

        var hint = new Label
        {
            Text = "拖动鼠标框选监控区域，按 Esc 取消",
            AutoSize = false,
            Size = new Size(340, 34),
            ForeColor = Color.Lime,
            BackColor = Color.Transparent,
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(20, 18)
        };

        Controls.Add(hint);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (!_dragging)
        {
            return;
        }

        var rectangle = GetSelection();
        using var fill = new SolidBrush(Color.FromArgb(60, 255, 255, 255));
        using var pen = new Pen(Color.Lime, 2);
        e.Graphics.FillRectangle(fill, rectangle);
        e.Graphics.DrawRectangle(pen, rectangle);
        TextRenderer.DrawText(
            e.Graphics,
            $"{rectangle.Width} × {rectangle.Height}",
            Font,
            new Point(rectangle.X + 4, Math.Max(4, rectangle.Y - 22)),
            Color.Lime);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _start = _current = e.Location;
        _dragging = true;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_dragging)
        {
            return;
        }

        _current = e.Location;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        var rectangle = GetSelection();

        if (rectangle.Width >= 5 && rectangle.Height >= 5)
        {
            SelectedScreenRect = new Rectangle(
                Left + rectangle.X,
                Top + rectangle.Y,
                rectangle.Width,
                rectangle.Height);
            DialogResult = DialogResult.OK;
        }
        else
        {
            DialogResult = DialogResult.Cancel;
        }

        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private Rectangle GetSelection()
    {
        var x = Math.Min(_start.X, _current.X);
        var y = Math.Min(_start.Y, _current.Y);
        return new Rectangle(x, y, Math.Abs(_current.X - _start.X), Math.Abs(_current.Y - _start.Y));
    }
}
