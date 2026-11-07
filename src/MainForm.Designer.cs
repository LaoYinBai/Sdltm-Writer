namespace SdltmWriter;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private Label lblTitle;
    private Label lblSub;
    private TextBox txtFile;
    private Button btnBrowse;
    private Label lblSource;
    private ComboBox cmbSource;
    private Label lblTarget;
    private ComboBox cmbTarget;
    private Button btnExport;
    private Label lblStatus;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        // Form
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(480, 380);
        Text = "SDLTM Writer — TMX → Trados 翻译记忆";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 46);
        Font = new Font("Segoe UI", 10F);

        // Title
        lblTitle = new Label { Text = "SDLTM Writer", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(233, 69, 96), Location = new Point(24, 20), AutoSize = true };
        lblSub = new Label { Text = "TMX → Trados Studio 翻译记忆转换", ForeColor = Color.FromArgb(136, 136, 136), Location = new Point(24, 56), AutoSize = true };

        // File
        txtFile = new TextBox { Location = new Point(24, 96), Size = new Size(340, 28), ReadOnly = true, BackColor = Color.FromArgb(40, 40, 60), ForeColor = Color.White, PlaceholderText = "拖拽 .tmx 文件到窗口，或点击浏览..." };
        btnBrowse = new Button { Text = "浏览...", Location = new Point(372, 94), Size = new Size(84, 30), BackColor = Color.FromArgb(15, 52, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnBrowse.Click += BtnBrowse_Click;

        // Language selectors
        lblSource = new Label { Text = "源语言", ForeColor = Color.White, Location = new Point(24, 142), AutoSize = true };
        cmbSource = new ComboBox { Location = new Point(24, 166), Size = new Size(200, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(40, 40, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        cmbSource.Items.AddRange(new object[] { "zh-CN", "zh-TW", "en-US", "en-GB", "ja-JP", "ko-KR", "de-DE", "fr-FR", "es-ES", "ru-RU" });
        cmbSource.SelectedIndex = 0;
        
        var lblArrow = new Label { Text = "→", ForeColor = Color.FromArgb(233, 69, 96), Font = new Font("Segoe UI", 14F, FontStyle.Bold), Location = new Point(232, 164), AutoSize = true };
        
        lblTarget = new Label { Text = "目标语言", ForeColor = Color.White, Location = new Point(256, 142), AutoSize = true };
        cmbTarget = new ComboBox { Location = new Point(256, 166), Size = new Size(200, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(40, 40, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        cmbTarget.Items.AddRange(new object[] { "en-US", "en-GB", "zh-CN", "zh-TW", "ja-JP", "ko-KR", "de-DE", "fr-FR", "es-ES", "ru-RU" });
        cmbTarget.SelectedIndex = 2;

        // Export button
        btnExport = new Button { Text = "导出 SDLTM", Location = new Point(24, 220), Size = new Size(432, 44), BackColor = Color.FromArgb(233, 69, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12F, FontStyle.Bold), Enabled = false, Cursor = Cursors.Hand };
        btnExport.FlatAppearance.BorderSize = 0;
        btnExport.Click += BtnExport_Click;

        // Status
        lblStatus = new Label { Text = "拖拽 .tmx 文件到窗口开始", ForeColor = Color.FromArgb(136, 136, 136), Location = new Point(24, 280), Size = new Size(432, 40), AutoSize = false };

        // Info
        var lblInfo = new Label { 
            Text = "输出: Trados Studio 兼容 .sdltm\n包含: 精确哈希 + 模糊索引(fi1-fi8) + Token 数据\n首次打开请执行一次「重建索引」", 
            ForeColor = Color.FromArgb(80, 80, 80), 
            Location = new Point(24, 320), 
            Size = new Size(432, 50), 
            AutoSize = false,
            Font = new Font("Segoe UI", 8F) 
        };

        Controls.AddRange(new Control[] { lblTitle, lblSub, txtFile, btnBrowse, lblSource, cmbSource, lblArrow, lblTarget, cmbTarget, btnExport, lblStatus, lblInfo });
        ResumeLayout(false);
    }
}
