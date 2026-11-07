namespace SdltmWriter;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private Panel pnlMain;
    private Label lblTitle;
    private Label lblSub;
    private Panel pnlDrop;
    private Label lblDropIcon;
    private Label lblDropText;
    private Label lblDropHint;
    private TextBox txtFile;
    private Button btnBrowse;
    private Label lblSource;
    private ComboBox cmbSource;
    private Label lblArrow;
    private Label lblTarget;
    private ComboBox cmbTarget;
    private Button btnExport;
    private Label lblStatus;
    private Label lblInfo;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    internal static readonly (string Code, string Name)[] Languages = new[]
    {
        ("zh-CN",  "简体中文（中国大陆）"),
        ("zh-TW",  "繁体中文（台湾）"),
        ("zh-HK",  "繁体中文（香港）"),
        ("en-US",  "英语（美国）"),
        ("en-GB",  "英语（英国）"),
        ("ja-JP",  "日语（日本）"),
        ("ko-KR",  "韩语（韩国）"),
        ("de-DE",  "德语（德国）"),
        ("fr-FR",  "法语（法国）"),
        ("es-ES",  "西班牙语（西班牙）"),
        ("ru-RU",  "俄语（俄罗斯）"),
        ("pt-BR",  "葡萄牙语（巴西）"),
        ("it-IT",  "意大利语（意大利）"),
    };

    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(440, 420);
        Text = "SDLTM Writer";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 24, 32);
        Font = new Font("Segoe UI", 9.5F);

        pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 20, 28, 16), BackColor = Color.Transparent };

        lblTitle = new Label { Text = "SDLTM Writer", Font = new Font("Segoe UI Semibold", 16F), ForeColor = Color.FromArgb(240, 240, 245), Location = new Point(0, 0), AutoSize = true };
        lblSub = new Label { Text = "TMX \u2192 Trados Studio \u7ffb\u8bd1\u8bb0\u5fc6\u8f6c\u6362", Font = new Font("Microsoft YaHei UI", 9F), ForeColor = Color.FromArgb(120, 120, 140), Location = new Point(0, 32), AutoSize = true };

        pnlDrop = new Panel { Location = new Point(0, 66), Size = new Size(384, 88), BackColor = Color.FromArgb(32, 32, 44), Cursor = Cursors.Hand };
        pnlDrop.Paint += (s, e) => { using var pen = new Pen(Color.FromArgb(50, 50, 68), 1.5f); e.Graphics.DrawRectangle(pen, 0, 0, pnlDrop.Width - 1, pnlDrop.Height - 1); };
        lblDropIcon = new Label { Text = "\u21ea", Font = new Font("Segoe UI Symbol", 22F), ForeColor = Color.FromArgb(70, 70, 95), Location = new Point(20, 18), AutoSize = true };
        lblDropText = new Label { Text = "\u62d6\u5165 .tmx \u6587\u4ef6", Font = new Font("Microsoft YaHei UI", 10F), ForeColor = Color.FromArgb(160, 160, 180), Location = new Point(72, 24), AutoSize = true };
        lblDropHint = new Label { Text = "\u6216\u70b9\u51fb\u9009\u62e9\u6587\u4ef6", Font = new Font("Microsoft YaHei UI", 8F), ForeColor = Color.FromArgb(80, 80, 100), Location = new Point(72, 46), AutoSize = true };
        pnlDrop.Controls.AddRange(new Control[] { lblDropIcon, lblDropText, lblDropHint });
        pnlDrop.Click += (s, e) => btnBrowse.PerformClick();
        pnlDrop.MouseEnter += (s, e) => { pnlDrop.BackColor = Color.FromArgb(38, 38, 52); lblDropIcon.ForeColor = Color.FromArgb(233, 80, 100); };
        pnlDrop.MouseLeave += (s, e) => { pnlDrop.BackColor = Color.FromArgb(32, 32, 44); lblDropIcon.ForeColor = Color.FromArgb(70, 70, 95); };
        pnlDrop.DragEnter += (s, e) => { if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) { e.Effect = DragDropEffects.Copy; pnlDrop.BackColor = Color.FromArgb(45, 40, 58); } };
        pnlDrop.DragLeave += (s, e) => pnlDrop.BackColor = Color.FromArgb(32, 32, 44);
        pnlDrop.DragDrop += OnDropPanel;

        txtFile = new TextBox { Location = new Point(0, 162), Size = new Size(310, 26), ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(32, 32, 44), ForeColor = Color.FromArgb(180, 180, 200), Font = new Font("Consolas", 8.5F) };
        btnBrowse = new Button { Text = "\u6d4f\u89c8", Location = new Point(316, 161), Size = new Size(68, 27), BackColor = Color.FromArgb(45, 45, 62), ForeColor = Color.FromArgb(180, 180, 200), FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei UI", 8.5F), Cursor = Cursors.Hand };
        btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(55, 55, 75);
        btnBrowse.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 55, 78);
        btnBrowse.Click += (s, e) => { using var dlg = new OpenFileDialog { Filter = "TMX|\u00b7.tmz;\u00b7.xml|\u00b7\u00b7\u00b7|*.*", Title = "\u9009\u62e9 TMX \u6587\u4ef6" }; if (dlg.ShowDialog() == DialogResult.OK) { _tmxPath = dlg.FileName; txtFile.Text = Path.GetFileName(_tmxPath); btnExport.Enabled = true; DetectLanguages(); } };

        lblSource = new Label { Text = "\u6e90\u8bed\u8a00", Font = new Font("Microsoft YaHei UI", 8.5F), ForeColor = Color.FromArgb(130, 130, 150), Location = new Point(0, 198), AutoSize = true };
        cmbSource = new ComboBox { Location = new Point(0, 218), Size = new Size(170, 26), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(32, 32, 44), ForeColor = Color.FromArgb(220, 220, 235), FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei UI", 9F) };
        foreach (var (code, name) in Languages) cmbSource.Items.Add(new ComboItem(code, name));
        cmbSource.SelectedIndex = 0;

        lblArrow = new Label { Text = "\u2192", Font = new Font("Segoe UI", 13F), ForeColor = Color.FromArgb(233, 80, 100), Location = new Point(178, 219), AutoSize = true };

        lblTarget = new Label { Text = "\u76ee\u6807\u8bed\u8a00", Font = new Font("Microsoft YaHei UI", 8.5F), ForeColor = Color.FromArgb(130, 130, 150), Location = new Point(214, 198), AutoSize = true };
        cmbTarget = new ComboBox { Location = new Point(214, 218), Size = new Size(170, 26), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(32, 32, 44), ForeColor = Color.FromArgb(220, 220, 235), FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei UI", 9F) };
        foreach (var (code, name) in Languages) cmbTarget.Items.Add(new ComboItem(code, name));
        cmbTarget.SelectedIndex = 3;

        btnExport = new Button { Text = "\u5bfc\u51fa SDLTM", Location = new Point(0, 262), Size = new Size(384, 42), BackColor = Color.FromArgb(233, 80, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold), Enabled = false, Cursor = Cursors.Hand };
        btnExport.FlatAppearance.BorderSize = 0;
        btnExport.FlatAppearance.MouseOverBackColor = Color.FromArgb(210, 68, 88);
        btnExport.FlatAppearance.MouseDownBackColor = Color.FromArgb(185, 55, 75);
        btnExport.Click += BtnExport_Click;

        lblStatus = new Label { Text = "\u62d6\u5165\u6587\u4ef6\u5f00\u59cb", Font = new Font("Microsoft YaHei UI", 8.5F), ForeColor = Color.FromArgb(100, 100, 120), Location = new Point(0, 314), Size = new Size(384, 22), TextAlign = ContentAlignment.MiddleCenter };

        lblInfo = new Label { Text = "\u8f93\u51fa\u683c\u5f0f: Trados Studio \u517c\u5bb9 .sdltm  \u00b7  \u5305\u542b\u7cbe\u786e\u5339\u914d + \u6a21\u7cca\u7d22\u5f15\n\u9996\u6b21\u5728 Trados \u4e2d\u6253\u5f00\u65f6\u6267\u884c\u4e00\u6b21\u300c\u91cd\u5efa\u7d22\u5f15\u300d\u5373\u53ef", Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = Color.FromArgb(60, 60, 80), Location = new Point(0, 368), Size = new Size(384, 36), TextAlign = ContentAlignment.TopCenter };

        pnlMain.Controls.AddRange(new Control[] { lblTitle, lblSub, pnlDrop, txtFile, btnBrowse, lblSource, cmbSource, lblArrow, lblTarget, cmbTarget, btnExport, lblStatus, lblInfo });
        Controls.Add(pnlMain);
        AcceptButton = btnExport;
        ResumeLayout(false);
    }

    private void OnDropPanel(object? s, DragEventArgs e)
    {
        pnlDrop.BackColor = Color.FromArgb(32, 32, 44);
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;
        var f = files[0];
        if (!f.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        { lblStatus.Text = "\u2717 \u8bf7\u62d6\u5165 .tmx \u6587\u4ef6"; lblStatus.ForeColor = Color.FromArgb(240, 80, 80); return; }
        _tmxPath = f;
        txtFile.Text = Path.GetFileName(f);
        btnExport.Enabled = true;
        lblStatus.Text = $"\u5df2\u9009\u62e9: {Path.GetFileName(f)} ({new FileInfo(f).Length / 1024.0:F1} KB)";
        lblStatus.ForeColor = Color.FromArgb(100, 200, 120);
        DetectLanguages();
    }

    internal static void SelectComboByCode(ComboBox cmb, string code)
    {
        for (int i = 0; i < cmb.Items.Count; i++)
        {
            if (((ComboItem)cmb.Items[i]).Code.Equals(code, StringComparison.OrdinalIgnoreCase))
            { cmb.SelectedIndex = i; return; }
        }
    }

    internal static string GetSelectedCode(ComboBox cmb)
        => ((ComboItem)cmb.SelectedItem!).Code;

    internal class ComboItem
    {
        public string Code { get; }
        public string Name { get; }
        public ComboItem(string code, string name) { Code = code; Name = name; }
        public override string ToString() => Name;
    }
}
