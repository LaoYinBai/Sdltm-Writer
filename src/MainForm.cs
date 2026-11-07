using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace SdltmWriter;

public partial class MainForm : Form
{
    private string? _tmxPath;
    
    public MainForm()
    {
        InitializeComponent();
        AllowDrop = true;
        DragEnter += (s, e) => { if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy; };
        DragDrop += OnDrop;
    }

    private void OnDrop(object? s, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;
        var f = files[0];
        if (!f.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        { lblStatus.Text = "请拖入 .tmx 文件"; return; }
        _tmxPath = f;
        txtFile.Text = Path.GetFileName(f);
        btnExport.Enabled = true;
        lblStatus.Text = $"已选择: {Path.GetFileName(f)} ({new FileInfo(f).Length / 1024.0:F1} KB)";
        DetectLanguages();
    }

    private void DetectLanguages()
    {
        if (_tmxPath == null) return;
        try
        {
            var content = File.ReadAllText(_tmxPath);
            var langMatches = Regex.Matches(content, @"xml:lang=""([^""]+)""");
            var langs = langMatches.Select(m => m.Groups[1].Value).Distinct().Take(2).ToList();
            if (langs.Count >= 2)
            {
                SelectComboByCode(cmbSource, langs[0]);
                SelectComboByCode(cmbTarget, langs[1]);
            }
            else if (langs.Count == 1)
            {
                var header = Regex.Match(content, @"srclang=""([^""]+)""");
                if (header.Success) SelectComboByCode(cmbSource, header.Groups[1].Value);
                SelectComboByCode(cmbTarget, langs[0].ToLower().StartsWith("en") ? "zh-CN" : "en-US");
            }
        }
        catch { }
    }

    private void BtnBrowse_Click(object? s, EventArgs e)
    {
        using var dlg = new OpenFileDialog { Filter = "TMX files|*.tmx;*.xml|All files|*.*" };
        if (dlg.ShowDialog() == DialogResult.OK) { _tmxPath = dlg.FileName; txtFile.Text = Path.GetFileName(_tmxPath); btnExport.Enabled = true; DetectLanguages(); }
    }

    private async void BtnExport_Click(object? s, EventArgs e)
    {
        if (_tmxPath == null) return;
        btnExport.Enabled = false;
        lblStatus.Text = "转换中...";
        var srcLang = GetSelectedCode(cmbSource);
        var trgLang = GetSelectedCode(cmbTarget);
        var outPath = Path.ChangeExtension(_tmxPath, ".sdltm");
        
        try
        {
            await Task.Run(() => Generate(_tmxPath, outPath, srcLang, trgLang,
                p => BeginInvoke(() => { lblStatus.Text = p; })));
            lblStatus.Text = $"✓ 完成: {outPath}";
            lblStatus.ForeColor = Color.LimeGreen;
            Process.Start("explorer.exe", $"/select,\"{outPath}\"");
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"✗ {ex.Message}";
            lblStatus.ForeColor = Color.IndianRed;
        }
        btnExport.Enabled = true;
    }

    private static void Generate(string tmxPath, string outPath, string srcLang, string trgLang, Action<string> progress)
    {
        var content = File.ReadAllText(tmxPath);
        var tus = ParseTmx(content);
        
        if (File.Exists(outPath)) File.Delete(outPath);
        using var db = new SqliteConnection($"Data Source={outPath}");
        db.Open();

        // Schema
        var schema = """
            CREATE TABLE resources(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid BLOB NOT NULL, type INT NOT NULL, language TEXT, data BLOB NOT NULL);
            CREATE INDEX idx_Resources1 ON resources(type, language);
            CREATE INDEX idx_Resources2 ON resources(id);
            CREATE TABLE tm_resources(tm_id INT NOT NULL REFERENCES translation_memories(id) ON DELETE CASCADE, resource_id INT NOT NULL REFERENCES resources(id) ON DELETE CASCADE, CONSTRAINT PK_tr PRIMARY KEY(tm_id, resource_id));
            CREATE TABLE translation_memories(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid BLOB NOT NULL, name TEXT NOT NULL UNIQUE, source_language TEXT NOT NULL, target_language TEXT NOT NULL, copyright TEXT, description TEXT, settings INT NOT NULL, creation_user TEXT NOT NULL, creation_date DATETIME NOT NULL, expiration_date DATETIME, fuzzy_indexes INT NOT NULL, last_recompute_date DATETIME, last_recompute_size INT, flags INT NOT NULL DEFAULT 0, tucount INT NOT NULL DEFAULT 0, fga_support INT NOT NULL, data_version INT NOT NULL, text_context_match_type INT NOT NULL, id_context_match BIT NOT NULL);
            CREATE TABLE parameters(translation_memory_id INT NULL REFERENCES translation_memories ON DELETE CASCADE, name TEXT NOT NULL, value TEXT NOT NULL);
            CREATE INDEX p_main ON parameters(translation_memory_id, name);
            CREATE TABLE translation_units(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid BLOB NOT NULL, translation_memory_id INT NOT NULL REFERENCES translation_memories(id) ON DELETE CASCADE, source_hash INTEGER NOT NULL, source_segment TEXT, target_hash INTEGER NOT NULL, target_segment TEXT, creation_date DATETIME NOT NULL, creation_user TEXT NOT NULL, change_date DATETIME NOT NULL, change_user TEXT NOT NULL, last_used_date DATETIME NOT NULL, last_used_user TEXT NOT NULL, usage_counter INT NOT NULL, flags INT, source_token_data BLOB, target_token_data BLOB, alignment_data BLOB, align_model_date DATETIME, insert_date DATETIME, tokenization_sig_hash INTEGER);
            CREATE INDEX idx_tus_hashes ON translation_units(translation_memory_id, source_hash, target_hash);
            CREATE INDEX idx_tus_insert_dates ON translation_units(insert_date);
            CREATE INDEX idx_tus_toksighash ON translation_units(tokenization_sig_hash);
            CREATE TABLE translation_unit_contexts(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, left_source_context INTEGER NOT NULL, left_target_context INTEGER NOT NULL, CONSTRAINT PK_tuc PRIMARY KEY(translation_unit_id, left_source_context, left_target_context));
            CREATE TABLE translation_unit_idcontexts(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, idcontext TEXT NOT NULL, CONSTRAINT PK_tuidc PRIMARY KEY(translation_unit_id, idcontext));
            CREATE TABLE translation_unit_fragments(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, fragment_hash INTEGER NOT NULL, CONSTRAINT tuf_a UNIQUE(translation_unit_id, fragment_hash));
            CREATE TABLE attributes(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid BLOB NOT NULL, name TEXT NOT NULL, type INT NOT NULL, tm_id INT NOT NULL, CONSTRAINT CK_a UNIQUE(name, tm_id));
            CREATE TABLE string_attributes(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, attribute_id INT NOT NULL REFERENCES attributes(id) ON DELETE CASCADE, value TEXT NOT NULL);
            CREATE TABLE numeric_attributes(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, attribute_id INT NOT NULL REFERENCES attributes(id) ON DELETE CASCADE, value INT NOT NULL);
            CREATE TABLE date_attributes(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, attribute_id INT NOT NULL REFERENCES attributes(id) ON DELETE CASCADE, value DATETIME NOT NULL);
            CREATE TABLE picklist_values(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid BLOB NOT NULL, attribute_id INT NOT NULL REFERENCES attributes(id) ON DELETE CASCADE, value TEXT NOT NULL);
            CREATE TABLE picklist_attributes(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, picklist_value_id INT NOT NULL REFERENCES picklist_values(id) ON DELETE CASCADE);
            CREATE TABLE fuzzy_data(translation_memory_id INT NOT NULL REFERENCES translation_memories(id) ON DELETE CASCADE, translation_unit_id INT NOT NULL, fi1 TEXT, fi2 TEXT, fi4 TEXT, fi8 TEXT, CONSTRAINT PK_fi1 PRIMARY KEY(translation_memory_id, translation_unit_id));
            CREATE TABLE vocab_src(id INT NOT NULL, vocab TEXT NOT NULL, freq INT NOT NULL);
            CREATE TABLE vocab_trg(id INT NOT NULL, vocab TEXT NOT NULL, freq INT NOT NULL);
            CREATE TABLE vocabfilter(token TEXT NOT NULL);
            CREATE TABLE trans_model(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, sourcekey INTEGER NOT NULL, targetkey INTEGER NOT NULL, floatval REAL NOT NULL);
            CREATE TABLE trans_model_rev(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, sourcekey INTEGER NOT NULL, targetkey INTEGER NOT NULL, floatval REAL NOT NULL);
            """;
        using (var cmd = db.CreateCommand()) { cmd.CommandText = schema; cmd.ExecuteNonQuery(); }

        // Parameters
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Exec(db, "INSERT INTO parameters VALUES(NULL,'VERSION','8.06'),(NULL,'VERSION_CREATED','8.10'),(NULL,'FREQUENCYTOP','1000'),(1,'TokenDataVersion','1'),(1,'AlignmentDataVersion','1')");
        
        // TM row
        var tmGuid = Guid.NewGuid().ToByteArray();
        using (var cmd = db.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO translation_memories VALUES(1,@guid,@name,@src,@trg,NULL,'Generated by SDLTM Writer',63,'sdltm-writer',@now,NULL,15,NULL,NULL,0,@count,1,1,1,0)";
            cmd.Parameters.AddWithValue("@guid", tmGuid);
            cmd.Parameters.AddWithValue("@name", Path.GetFileNameWithoutExtension(tmxPath));
            cmd.Parameters.AddWithValue("@src", srcLang);
            cmd.Parameters.AddWithValue("@trg", trgLang);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.Parameters.AddWithValue("@count", tus.Count);
            cmd.ExecuteNonQuery();
        }

        // TUs
        progress?.Invoke($"写入 {tus.Count} 条翻译单元...");
        var srcLocale = srcLang.StartsWith("zh") ? "zh" : "en";
        var trgLocale = trgLang.StartsWith("zh") ? "zh" : "en";
        int idx = 0;
        foreach (var tu in tus)
        {
            idx++;
            if (idx % 50 == 0) progress?.Invoke($"处理 {idx}/{tus.Count}...");
            var sHash = GetStrictHash(tu.Source);
            var tHash = GetStrictHash(tu.Target);
            var srcXml = MakeXml(tu.Source, srcLang);
            var trgXml = MakeXml(tu.Target, trgLang);
            var fi1 = string.Join("|", WordFeatures(Tokenize(tu.Source, srcLocale), false));
            var fi2 = string.Join("|", Char2Features(Tokenize(tu.Source, srcLocale)));
            var fi4 = string.Join("|", Char2Features(Tokenize(tu.Target, trgLocale)));
            var fi8 = string.Join("|", WordFeatures(Tokenize(tu.Target, trgLocale), true));

            using var cmd = db.CreateCommand();
            cmd.CommandText = "INSERT INTO translation_units VALUES(@id,@guid,1,@shash,@sseg,@thash,@tseg,@now,'sdltm-writer',@now,'sdltm-writer',@now,'sdltm-writer',0,393473,NULL,NULL,NULL,NULL,@now,NULL)";
            cmd.Parameters.AddWithValue("@id", idx);
            cmd.Parameters.AddWithValue("@guid", Guid.NewGuid().ToByteArray());
            cmd.Parameters.AddWithValue("@shash", sHash);
            cmd.Parameters.AddWithValue("@sseg", srcXml);
            cmd.Parameters.AddWithValue("@thash", tHash);
            cmd.Parameters.AddWithValue("@tseg", trgXml);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.ExecuteNonQuery();

            Exec(db, $"INSERT INTO fuzzy_data VALUES(1,{idx},'{fi1}','{fi2}','{fi4}','{fi8}')");
            Exec(db, $"INSERT INTO translation_unit_fragments VALUES({idx},{sHash})");
        }
        
        Exec(db, "VACUUM");
        progress?.Invoke("完成!");
    }

    static void Exec(SqliteConnection db, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    // ===== TMX Parser =====
    static List<(string Source, string Target)> ParseTmx(string xml)
    {
        var tus = new List<(string, string)>();
        var tuMatches = Regex.Matches(xml, @"<tu[^>]*>([\s\S]*?)</tu>");
        foreach (Match m in tuMatches)
        {
            var tuvMatches = Regex.Matches(m.Groups[1].Value, @"<tuv[^>]*xml:lang=""([^""]*)""[^>]*>([\s\S]*?)</tuv>");
            var vars = new Dictionary<string, string>();
            foreach (Match tm in tuvMatches)
            {
                var sm = Regex.Match(tm.Groups[2].Value, @"<seg[^>]*>([\s\S]*?)</seg>");
                if (sm.Success)
                {
                    var text = Regex.Replace(sm.Groups[1].Value, @"<[^>]+>", "");
                    text = System.Net.WebUtility.HtmlDecode(text);
                    vars[tm.Groups[1].Value] = text;
                }
            }
            if (vars.Count >= 2)
            {
                var vals = vars.Values.ToList();
                tus.Add((vals[0], vals[1]));
            }
        }
        return tus;
    }

    // ===== Hash (verified against Trados engine) =====
    static uint Jenkins32(byte[] b) { uint h = 0; foreach (var x in b) { unchecked { h += x; h += h << 10; h ^= h >> 6; } } unchecked { h += h << 3; h ^= h >> 11; h += h << 15; } return h; }
    static uint Fnv1a32(byte[] b) { uint h = 2166136261; foreach (var x in b) { unchecked { h = (h ^ x) * 16777619; } } return h; }
    
    static long GetStrictHash(string s)
    {
        var identity = s.Replace("\\", "\\\\");
        var bytes = Encoding.Unicode.GetBytes(identity);
        long r = (long)Fnv1a32(bytes) + (long)(Jenkins32(bytes) & 0xFFFF0000) + (identity.Length & 0xFFFF);
        return r;
    }

    static int GetHashCodeInt(string s)
    {
        int h = 0;
        foreach (char c in s) h = h * 31 + c;
        return h;
    }

    static long GetHashCodeLong(string s)
    {
        long h = 0;
        foreach (char c in s) h = h * 31 + c;
        return h;
    }

    // ===== Tokenizer =====
    static List<string> Tokenize(string text, string locale)
    {
        var tokens = new List<string>();
        if (locale == "zh")
        {
            // Character-level for CJK (matches ICU for isolated chars; dictionary words handled by Trados reindex)
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || c > 0x2E7F) tokens.Add(c.ToString());
                else if (!char.IsWhiteSpace(c) && c > 0x20) { /* punctuation: skip for features */ }
            }
        }
        else
        {
            // Word-level for Latin scripts
            var matches = Regex.Matches(text, @"[\p{L}\p{N}]+");
            foreach (Match m in matches)
            {
                var w = m.Value;
                if (w.Contains('\u2019') || w.Contains('\'')) tokens.AddRange(w.Split(new[] { '\u2019', '\'' }, StringSplitOptions.RemoveEmptyEntries));
                else tokens.Add(w);
            }
        }
        return tokens;
    }

    // ===== Stemmer =====
    static string StemEn(string w)
    {
        if (w.Length <= 2) return w;
        if (w.EndsWith("sses")) w = w[..^2];
        else if (w.EndsWith("ies")) w = w[..^2];
        else if (!w.EndsWith("ss") && w.EndsWith("s") && !w.EndsWith("us")) w = w[..^1];
        if (!w.EndsWith("eed"))
        {
            if (w.EndsWith("ed") && w.Length > 4) w = w[..^2];
            else if (w.EndsWith("ing") && w.Length > 5) w = w[..^3];
        }
        if (w.EndsWith("y") && w.Length > 2 && !"aeiou".Contains(w[^2])) w = w[..^1] + "i";
        if (w.Length > 2 && w[^1] == w[^2] && !"lsz".Contains(w[^1])) w = w[..^1];
        string[] suffixes = { "ation", "vement", "isms", "iveness", "fulness", "ousness", "ization", "ator", "alism", "ment", "ance", "ence", "able", "ible", "ally", "ous", "ize", "ate", "al", "er", "ic", "ed", "ly", "es", "e", "y" };
        foreach (var suf in suffixes) { if (w.EndsWith(suf) && w.Length - suf.Length >= 3) { w = w[..^suf.Length]; break; } }
        return w;
    }

    // ===== Features =====
    static List<int> WordFeatures(List<string> tokens, bool isEn)
    {
        var set = new HashSet<int>();
        foreach (var t in tokens)
        {
            var lower = t.ToLowerInvariant();
            set.Add(GetHashCodeInt(isEn ? StemEn(lower) : lower));
        }
        return set.OrderBy(x => x).ToList();
    }

    static List<long> Char2Features(List<string> tokens)
    {
        var stream = "|" + string.Join("|", tokens.Select(t => t.ToLowerInvariant())) + "|";
        var set = new HashSet<long>();
        for (int i = 0; i + 2 <= stream.Length; i++)
            set.Add(GetHashCodeLong(stream.Substring(i, 2)));
        return set.OrderBy(x => x).ToList();
    }

    // ===== XML =====
    static string MakeXml(string text, string culture)
    {
        var e = System.Security.SecurityElement.Escape(text);
        return $"<Segment xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><Elements><Text><Value>{e}</Value></Text></Elements><CultureName>{culture}</CultureName></Segment>";
    }
}
