namespace SdltmWriter;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private Panel pnlDrop;
    private Label lblDropIcon;
    private Label lblDropText;
    private Label lblDropHint;
    private TextBox txtFile;
    private Button btnBrowse;
    private ComboBox cmbSource;
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
        ("zh-CN",  "\u7b80\u4f53\u4e2d\u6587\uff08\u4e2d\u56fd\u5927\u9646\uff09"),
        ("zh-TW",  "\u7e41\u4f53\u4e2d\u6587\uff08\u4e2d\u56fd\u53f0\u6e7e\uff09"),
        ("zh-HK",  "\u7e41\u4f53\u4e2d\u6587\uff08\u4e2d\u56fd\u9999\u6e2f\uff09"),
        ("en-US",  "\u82f1\u8bed\uff08\u7f8e\u56fd\uff09"),
        ("en-GB",  "\u82f1\u8bed\uff08\u82f1\u56fd\uff09"),
        ("ja-JP",  "\u65e5\u8bed\uff08\u65e5\u672c\uff09"),
        ("ko-KR",  "\u97e9\u8bed\uff08\u97e9\u56fd\uff09"),
        ("de-DE",  "\u5fb7\u8bed\uff08\u5fb7\u56fd\uff09"),
        ("fr-FR",  "\u6cd5\u8bed\uff08\u6cd5\u56fd\uff09"),
        ("es-ES",  "\u897f\u73ed\u7259\u8bed\uff08\u897f\u73ed\u7259\uff09"),
        ("ru-RU",  "\u4fc4\u8bed\uff08\u4fc4\u7f57\u65af\uff09"),
        ("pt-BR",  "\u8461\u8404\u7259\u8bed\uff08\u5df4\u897f\uff09"),
        ("it-IT",  "\u610f\u5927\u5229\u8bed\uff08\u610f\u5927\u5229\uff09"),
    };

    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(448, 436);
        Text = "SDLTM Writer";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(22, 22, 30);
        Font = new Font("Segoe UI", 9.5F);

        int MX = 32; // left/right margin
        int CW = 448 - MX * 2; // content width = 384

        // Title
        var lblTitle = new Label { Text = "SDLTM Writer", Font = new Font("Segoe UI Semibold", 15F), ForeColor = Color.FromArgb(240, 240, 248), Location = new Point(MX, 18), AutoSize = true };
        var lblSub = new Label { Text = "TMX \u2192 Trados Studio \u7ffb\u8bd1\u8bb0\u5fc6\u8f6c\u6362", Font = new Font("Microsoft YaHei UI", 8.5F), ForeColor = Color.FromArgb(110, 110, 130), Location = new Point(MX, 44), AutoSize = true };

        // Drop zone
        pnlDrop = new Panel { Location = new Point(MX, 72), Size = new Size(CW, 80), BackColor = Color.FromArgb(30, 30, 42), Cursor = Cursors.Hand };
        pnlDrop.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 48, 64), 1.5f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlDrop.Width - 1, pnlDrop.Height - 1);
        };
        lblDropIcon = new Label { Text = "\u21ea", Font = new Font("Segoe UI Symbol", 20F), ForeColor = Color.FromArgb(65, 65, 88), Location = new Point(20, 18), AutoSize = true };
        lblDropText = new Label { Text = "\u62d6\u5165 .tmx \u6587\u4ef6", Font = new Font("Microsoft YaHei UI", 10F), ForeColor = Color.FromArgb(150, 150, 172), Location = new Point(68, 22), AutoSize = true };
        lblDropHint = new Label { Text = "\u6216\u70b9\u51fb\u9009\u62e9\u6587\u4ef6", Font = new Font("Microsoft YaHei UI", 8F), ForeColor = Color.FromArgb(75, 75, 95), Location = new Point(68, 44), AutoSize = true };
        pnlDrop.Controls.AddRange(new Control[] { lblDropIcon, lblDropText, lblDropHint });
        pnlDrop.Click += (s, e) => btnBrowse.PerformClick();
        pnlDrop.MouseEnter += (s, e) => { pnlDrop.BackColor = Color.FromArgb(36, 36, 50); lblDropIcon.ForeColor = Color.FromArgb(233, 80, 100); };
        pnlDrop.MouseLeave += (s, e) => { pnlDrop.BackColor = Color.FromArgb(30, 30, 42); lblDropIcon.ForeColor = Color.FromArgb(65, 65, 88); };
        pnlDrop.DragEnter += (s, e) => { if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) { e.Effect = DragDropEffects.Copy; pnlDrop.BackColor = Color.FromArgb(42, 38, 56); } };
        pnlDrop.DragLeave += (s, e) => pnlDrop.BackColor = Color.FromArgb(30, 30, 42);
        pnlDrop.DragDrop += OnDropPanel;

        // File path row
        txtFile = new TextBox { Location = new Point(MX, 160), Size = new Size(CW - 76, 26), ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(30, 30, 42), ForeColor = Color.FromArgb(170, 170, 192), Font = new Font("Consolas", 8.5F) };
        btnBrowse = new Button { Text = "\u6d4f\u89c8", Location = new Point(MX + CW - 70, 159), Size = new Size(70, 27), BackColor = Color.FromArgb(42, 42, 58), ForeColor = Color.FromArgb(170, 170, 192), FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei UI", 8.5F), Cursor = Cursors.Hand };
        btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 68);
        btnBrowse.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 52, 72);
        btnBrowse.Click += (s, e) =>
        {
            using var dlg = new OpenFileDialog { Filter = "TMX \u7ffb\u8bd1\u8bb0\u5fc6|*.tmx;*.xml|\u6240\u6709\u6587\u4ef6|*.*", Title = "\u9009\u62e9 TMX \u6587\u4ef6" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _tmxPath = dlg.FileName;
                txtFile.Text = Path.GetFileName(_tmxPath);
                btnExport.Enabled = true;
                DetectLanguages();
            }
        };

        // Language row — labels
        var lblSrc = new Label { Text = "\u6e90\u8bed\u8a00", Font = new Font("Microsoft YaHei UI", 8F), ForeColor = Color.FromArgb(120, 120, 140), Location = new Point(MX, 196), AutoSize = true };
        var lblArrow = new Label { Text = "", Font = new Font("Segoe UI", 11F), ForeColor = Color.FromArgb(233, 80, 100), Location = new Point(MX + CW / 2 - 10, 220), AutoSize = true };
        var lblTrg = new Label { Text = "\u76ee\u6807\u8bed\u8a00", Font = new Font("Microsoft YaHei UI", 8F), ForeColor = Color.FromArgb(120, 120, 140), Location = new Point(MX + CW / 2 + 10, 196), AutoSize = true };

        // Language row — combos
        int comboW = CW / 2 - 16;
        cmbSource = new ComboBox { Location = new Point(MX, 216), Size = new Size(comboW, 26), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 30, 42), ForeColor = Color.FromArgb(210, 210, 228), FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei UI", 9F) };
        foreach (var (code, name) in Languages) cmbSource.Items.Add(new ComboItem(code, name));
        cmbSource.SelectedIndex = 0;

        cmbTarget = new ComboBox { Location = new Point(MX + CW / 2 + 10, 216), Size = new Size(comboW, 26), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 30, 42), ForeColor = Color.FromArgb(210, 210, 228), FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei UI", 9F) };
        foreach (var (code, name) in Languages) cmbTarget.Items.Add(new ComboItem(code, name));
        cmbTarget.SelectedIndex = 3;

        // Export
        btnExport = new Button { Text = "\u5bfc\u51fa SDLTM", Location = new Point(MX, 260), Size = new Size(CW, 42), BackColor = Color.FromArgb(233, 80, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold), Enabled = false, Cursor = Cursors.Hand };
        btnExport.FlatAppearance.BorderSize = 0;
        btnExport.FlatAppearance.MouseOverBackColor = Color.FromArgb(210, 68, 88);
        btnExport.FlatAppearance.MouseDownBackColor = Color.FromArgb(185, 55, 75);
        btnExport.Click += BtnExport_Click;

        // Status
        lblStatus = new Label { Text = "\u62d6\u5165\u6587\u4ef6\u5f00\u59cb", Font = new Font("Microsoft YaHei UI", 8.5F), ForeColor = Color.FromArgb(90, 90, 110), Location = new Point(MX, 312), Size = new Size(CW, 22), TextAlign = ContentAlignment.MiddleCenter };

        // Info
        lblInfo = new Label
        {
            Text = "\u8f93\u51fa\u683c\u5f0f: Trados Studio \u517c\u5bb9 .sdltm \u00b7 \u5305\u542b\u7cbe\u786e\u5339\u914d + \u6a21\u7cca\u7d22\u5f15\n\u9996\u6b21\u5728 Trados \u4e2d\u6253\u5f00\u65f6\u6267\u884c\u4e00\u6b21\u300c\u91cd\u5efa\u7d22\u5f15\u300d\u5373\u53ef",
            Font = new Font("Microsoft YaHei UI", 7.5F),
            ForeColor = Color.FromArgb(55, 55, 72),
            Location = new Point(MX, 360),
            Size = new Size(CW, 40),
            TextAlign = ContentAlignment.TopCenter
        };

        Controls.AddRange(new Control[] { lblTitle, lblSub, pnlDrop, txtFile, btnBrowse, lblSrc, cmbSource, lblArrow, lblTrg, cmbTarget, btnExport, lblStatus, lblInfo });
        AcceptButton = btnExport;
        ResumeLayout(false);
    }

    private void OnDropPanel(object? s, DragEventArgs e)
    {
        pnlDrop.BackColor = Color.FromArgb(30, 30, 42);
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
