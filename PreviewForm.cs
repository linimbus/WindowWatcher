namespace WindowWatcher;

public sealed class PreviewForm : Form
{
    public PreviewForm(Bitmap image, string recognizedText)
    {
        Text = "区域预览 / OCR 结果";
        Size = new Size(880, 640);
        MinimumSize = new Size(640, 460);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        Font = new Font("Microsoft YaHei UI", 9F);

        var picture = new PictureBox
        {
            Dock = DockStyle.Top,
            Height = 340,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = image,
            BackColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle
        };

        var textPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 8, 14, 8) };
        var resultLabel = new Label
        {
            Text = "OCR 识别结果：",
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = true,
            BackColor = SystemColors.Window,
            Text = string.IsNullOrWhiteSpace(recognizedText) ? "（未识别到文字）" : recognizedText
        };

        textPanel.Controls.Add(textBox);
        textPanel.Controls.Add(resultLabel);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12, 12, 12, 12)
        };
        var buttonClose = new Button
        {
            Text = "关闭", Width = 104, Height = 36, DialogResult = DialogResult.Cancel
        };
        buttons.Controls.Add(buttonClose);
        CancelButton = buttonClose;

        Controls.Add(textPanel);
        Controls.Add(buttons);
        Controls.Add(picture);
    }
}
