// Route B: Complete SDLTM generator — pure Node.js, zero Trados dependency.
import { readFileSync, writeFileSync, unlinkSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

// ===== Core hash functions (verified against engine oracle + native data) =====
function jenkins32(b) { let h = 0 >>> 0; for (const x of b) { h = (h + x) >>> 0; h = (h + (h << 10)) >>> 0; h = (h ^ (h >>> 6)) >>> 0; } h = (h + (h << 3)) >>> 0; h = (h ^ (h >>> 11)) >>> 0; h = (h + (h << 15)) >>> 0; return h >>> 0; }
function fnv1a32(b) { let h = 2166136261 >>> 0; for (const x of b) { h = (h ^ x) >>> 0; h = Math.imul(h, 16777619) >>> 0; } return h >>> 0; }
function getStrictHash(s) { const b = Buffer.from(s, "utf16le"); let r = BigInt(fnv1a32(b)) + BigInt((jenkins32(b) & 0xffff0000) >>> 0) + BigInt(s.length & 0xffff); if (r >= 2n ** 63n) r -= 2n ** 64n; return r; }
function getHashCodeInt(s) { let h = 0 | 0; for (const ch of s) h = (Math.imul(h, 31) + (ch.codePointAt(0) & 0xffff)) | 0; return h; }
function getHashCodeLong(s) { let h = 0n; for (const ch of s) h = (h * 31n + BigInt(ch.codePointAt(0) & 0xffff)) & 0xffffffffffffffffn; return BigInt.asIntN(64, h); }

// ===== Snowball English stemmer =====
function stemEn(w) {
  if (w.length <= 2) return w;
  if (w.endsWith("sses")) w = w.slice(0, -2);
  else if (w.endsWith("ies")) w = w.slice(0, -2);
  else if (w.endsWith("ss")) { }
  else if (w.endsWith("s") && !w.endsWith("us")) w = w.slice(0, -1);
  if (w.endsWith("eed")) { }
  else if (w.endsWith("ed") && w.length > 4) w = w.slice(0, -2);
  else if (w.endsWith("ing") && w.length > 5) w = w.slice(0, -3);
  if (w.endsWith("y") && w.length > 2 && !"aeiou".includes(w[w.length - 2])) w = w.slice(0, -1) + "i";
  if (/(.)\1$/.test(w) && !"lsz".includes(w[w.length - 1])) w = w.slice(0, -1);
  const suffixes = ["ation", "vement", "isms", "iveness", "fulness", "ousness", "ization", "ator", "alism", "ful", "ness", "ive", "ion", "ment", "ance", "ence", "able", "ible", "ally", "ous", "ize", "ate", "al", "er", "ic", "ed", "ly", "es", "e", "y"];
  for (const suf of suffixes) { if (w.endsWith(suf) && w.length - suf.length >= 3) { w = w.slice(0, w.length - suf.length); break; } }
  return w;
}

// ===== Tokenization =====
function tokenize(text, lang) {
  const locale = lang.toLowerCase().startsWith("zh") ? "zh-CN" : "en-US";
  const segmenter = new Intl.Segmenter(locale, { granularity: "word" });
  let tokens = [...segmenter.segment(text)].filter(s => s.isWordLike).map(s => s.segment);
  if (lang.startsWith("en")) {
    tokens = tokens.flatMap(w => {
      if (/['\u2019]/.test(w)) return w.split(/['\u2019]/).filter(Boolean);
      if (/[\u2010\u2011\u2012\u2013\u2014\uFF0D-]/.test(w)) return w.split(/[\u2010\u2011\u2012\u2013\u2014\uFF0D-]/).filter(Boolean);
      return [w];
    });
  }
  return tokens;
}

// ===== Features =====
function wordFeatures(tokens, lang) {
  const isEn = lang.startsWith("en");
  return [...new Set(tokens.map(t => getHashCodeInt(isEn ? stemEn(t.text.toLowerCase()) : t.text.toLowerCase())))].sort((a, b) => a - b).join("|");
}
function char2Features(tokens) {
  const stream = "|" + tokens.map(t => t.text.toLowerCase()).join("|") + "|";
  const out = new Set();
  for (let i = 0; i + 2 <= stream.length; i++) out.add(getHashCodeLong(stream.slice(i, i + 2)).toString());
  return [...out].map(Number).sort((a, b) => a - b).join("|");
}

// ===== Token BLOB =====
function serializeTokens(tokens, text) {
  const buf = [0x02, tokens.length & 0xff, 0x00];
  let prevEnd = 0;
  for (let i = 0; i < tokens.length; i++) {
    const tok = tokens[i];
    const start = text.indexOf(tok.text, prevEnd);
    const len = tok.text.length;
    const isSingle = len === 1;
    const isStandard = i > 0 && start === prevEnd;
    const flags = i === 0 ? (isSingle ? 3 : 0) : (isStandard ? (isSingle ? 5 : 4) : (isSingle ? 3 : 0));
    buf.push((flags << 5) | (tok.type & 0x1f));
    if (!(flags & 4)) { buf.push(i & 0xff); buf.push(start & 0xff, (start >> 8) & 0xff); }
    if (!isSingle) buf.push(len & 0xff);
    prevEnd = start + len;
  }
  return Buffer.from(buf).toString("hex");
}

// ===== Helpers =====
const makeXml = (text, culture) => {
  const e = text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&apos;");
  return `<Segment xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"><Elements><Text><Value>${e}</Value></Text></Elements><CultureName>${culture}</CultureName></Segment>`;
};
const netGuid = () => { const id = randomUUID().replace(/-/g, ""); const b = Buffer.alloc(16); b.writeUInt32LE(parseInt(id.slice(0, 8), 16), 0); b.writeUInt16LE(parseInt(id.slice(8, 12), 16), 4); b.writeUInt16LE(parseInt(id.slice(12, 16), 16), 6); Buffer.from(id.slice(16), "hex").copy(b, 8); return b.toString("hex"); };
const esc = (s) => s.replace(/'/g, "''");

// ===== TMX parser =====
function parseTmx(path) {
  const xml = readFileSync(path, "utf8");
  const srcLangM = xml.match(/srclang="([^"]+)"/);
  const srcLang = srcLangM ? srcLangM[1].replace("_", "-") : "zh-CN";
  const tus = [];
  const tuRe = /<tu[^>]*>([\s\S]*?)<\/tu>/g;
  let m;
  while ((m = tuRe.exec(xml)) !== null) {
    const tuvRe = /<tuv[^>]*xml:lang="([^"]*)"[^>]*>([\s\S]*?)<\/tuv>/g;
    const vars = {};
    let tm;
    while ((tm = tuvRe.exec(m[1])) !== null) {
      const sm = tm[2].match(/<seg[^>]*>([\s\S]*?)<\/seg>/);
      if (sm) {
        const text = sm[1].replace(/<[^>]+>/g, "").replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&quot;/g, '"').replace(/&apos;/g, "'").replace(/&#(\d+);/g, (_, d) => String.fromCodePoint(+d)).replace(/&amp;/g, "&");
        vars[tm[1]] = text;
      }
    }
    const keys = Object.keys(vars);
    if (keys.length >= 2) tus.push({ src: vars[keys[0]], trg: vars[keys[1]], srcLang: keys[0], trgLang: keys[1] });
  }
  return { srcLang, tus };
}

// ===== Main =====
const [,, tmxPath, outPath] = process.argv;
if (!tmxPath || !outPath) { console.error("Usage: node gen.mjs <input.tmx> <output.sdltm>"); process.exit(1); }

const tmx = parseTmx(tmxPath);
const srcLang = tmx.tus[0]?.srcLang || tmx.srcLang;
const trgLang = tmx.tus[0]?.trgLang.replace("_", "-") || "en-US";
const tmName = tmxPath.split(/[/\\]/).pop().replace(/\.tmx$/i, "");
const now = new Date().toISOString().replace("T", " ").slice(0, 19);

console.log(`Input: ${tmx.tus.length} TUs, ${srcLang} → ${trgLang}`);

const tuRows = [];
for (const tu of tmx.tus) {
  const s = tu.src.trim(), t = tu.trg.trim();
  if (!s && !t) continue;
  const sToks = tokenize(s, srcLang).map(x => ({ text: x, type: 1 }));
  const tToks = tokenize(t, trgLang).map(x => ({ text: x, type: 1 }));
  tuRows.push({
    srcText: s, trgText: t,
    srcHash: getStrictHash(s), trgHash: getStrictHash(t),
    fi1: wordFeatures(sToks, srcLang), fi2: char2Features(sToks),
    fi4: char2Features(tToks), fi8: wordFeatures(tToks, trgLang),
    srcBlob: serializeTokens(sToks, s), trgBlob: serializeTokens(tToks, t),
  });
}
console.log(`Generated ${tuRows.length} TU rows`);

let sql = `PRAGMA page_size=4096;\nBEGIN;\n`;
sql += `CREATE TABLE resources(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid BLOB NOT NULL, type INT NOT NULL, language TEXT, data BLOB NOT NULL);\nCREATE INDEX idx_Resources1 ON resources(type, language);\nCREATE INDEX idx_Resources2 ON resources(id);\n`;
sql += `CREATE TABLE tm_resources(tm_id INT NOT NULL REFERENCES translation_memories(id) ON DELETE CASCADE, resource_id INT NOT NULL REFERENCES resources(id) ON DELETE CASCADE, CONSTRAINT PK_tr PRIMARY KEY(tm_id, resource_id));\n`;
sql += `CREATE TABLE translation_memories(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid BLOB NOT NULL, name TEXT NOT NULL UNIQUE, source_language TEXT NOT NULL, target_language TEXT NOT NULL, copyright TEXT, description TEXT, settings INT NOT NULL, creation_user TEXT NOT NULL, creation_date DATETIME NOT NULL, expiration_date DATETIME, fuzzy_indexes INT NOT NULL, last_recompute_date DATETIME, last_recompute_size INT, flags INT NOT NULL DEFAULT 0, tucount INT NOT NULL DEFAULT 0, fga_support INT NOT NULL, data_version INT NOT NULL, text_context_match_type INT NOT NULL, id_context_match BIT NOT NULL);\n`;
sql += `CREATE TABLE parameters(translation_memory_id INT NULL REFERENCES translation_memories ON DELETE CASCADE, name TEXT NOT NULL, value TEXT NOT NULL);\nCREATE INDEX p_main ON parameters(translation_memory_id, name);\n`;
sql += `CREATE TABLE translation_units(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid BLOB NOT NULL, translation_memory_id INT NOT NULL REFERENCES translation_memories(id) ON DELETE CASCADE, source_hash INTEGER NOT NULL, source_segment TEXT, target_hash INTEGER NOT NULL, target_segment TEXT, creation_date DATETIME NOT NULL, creation_user TEXT NOT NULL, change_date DATETIME NOT NULL, change_user TEXT NOT NULL, last_used_date DATETIME NOT NULL, last_used_user TEXT NOT NULL, usage_counter INT NOT NULL, flags INT, source_token_data BLOB, target_token_data BLOB, alignment_data BLOB, align_model_date DATETIME, insert_date DATETIME, tokenization_sig_hash INTEGER);\n`;
sql += `CREATE INDEX idx_tus_hashes ON translation_units(translation_memory_id, source_hash, target_hash);\nCREATE INDEX idx_tus_insert_dates ON translation_units(insert_date);\nCREATE INDEX idx_tus_toksighash ON translation_units(tokenization_sig_hash);\n`;
sql += `CREATE TABLE translation_unit_contexts(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, left_source_context INTEGER NOT NULL, left_target_context INTEGER NOT NULL, CONSTRAINT PK_tuc PRIMARY KEY(translation_unit_id, left_source_context, left_target_context));\nCREATE INDEX idx_ctx1 ON translation_unit_contexts(left_source_context);\nCREATE INDEX idx_ctx2 ON translation_unit_contexts(left_target_context);\n`;
sql += `CREATE TABLE translation_unit_idcontexts(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, idcontext TEXT NOT NULL, CONSTRAINT PK_tuidc PRIMARY KEY(translation_unit_id, idcontext));\nCREATE INDEX idx_tus_idcontexts ON translation_unit_idcontexts(translation_unit_id, idcontext);\n`;
sql += `CREATE TABLE translation_unit_fragments(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, fragment_hash INTEGER NOT NULL, CONSTRAINT tuf_a UNIQUE(translation_unit_id, fragment_hash));\nCREATE INDEX idx_tufragments_ids ON translation_unit_fragments(translation_unit_id);\nCREATE INDEX idx_tufragments_hashes ON translation_unit_fragments(fragment_hash);\n`;
sql += `CREATE TABLE attributes(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid BLOB NOT NULL, name TEXT NOT NULL, type INT NOT NULL, tm_id INT NOT NULL, CONSTRAINT CK_a UNIQUE(name, tm_id));\nCREATE INDEX idx_attributes1 ON attributes(tm_id);\nCREATE INDEX idx_attributes2 ON attributes(name, type);\n`;
sql += `CREATE TABLE string_attributes(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, attribute_id INT NOT NULL REFERENCES attributes(id) ON DELETE CASCADE, value TEXT NOT NULL);\nCREATE INDEX idx_string_attributes ON string_attributes(translation_unit_id, attribute_id);\n`;
sql += `CREATE TABLE numeric_attributes(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, attribute_id INT NOT NULL REFERENCES attributes(id) ON DELETE CASCADE, value INT NOT NULL);\nCREATE INDEX idx_numeric_attributes ON numeric_attributes(translation_unit_id, attribute_id);\n`;
sql += `CREATE TABLE date_attributes(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, attribute_id INT NOT NULL REFERENCES attributes(id) ON DELETE CASCADE, value DATETIME NOT NULL);\nCREATE INDEX idx_date_attributes ON date_attributes(translation_unit_id, attribute_id);\n`;
sql += `CREATE TABLE picklist_values(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid BLOB NOT NULL, attribute_id INT NOT NULL REFERENCES attributes(id) ON DELETE CASCADE, value TEXT NOT NULL);\n`;
sql += `CREATE TABLE picklist_attributes(translation_unit_id INT NOT NULL REFERENCES translation_units(id) ON DELETE CASCADE, picklist_value_id INT NOT NULL REFERENCES picklist_values(id) ON DELETE CASCADE);\nCREATE INDEX idx_picklist_attributes ON picklist_attributes(translation_unit_id);\n`;
sql += `CREATE TABLE fuzzy_data(translation_memory_id INT NOT NULL REFERENCES translation_memories(id) ON DELETE CASCADE, translation_unit_id INT NOT NULL, fi1 TEXT, fi2 TEXT, fi4 TEXT, fi8 TEXT, CONSTRAINT PK_fi1 PRIMARY KEY(translation_memory_id, translation_unit_id));\n`;
sql += `CREATE TABLE vocab_src(id INT NOT NULL, vocab TEXT NOT NULL, freq INT NOT NULL);\nCREATE INDEX idx_vocab_src ON vocab_src(vocab);\n`;
sql += `CREATE TABLE vocab_trg(id INT NOT NULL, vocab TEXT NOT NULL, freq INT NOT NULL);\nCREATE INDEX idx_vocab_trg ON vocab_trg(vocab);\n`;
sql += `CREATE TABLE vocabfilter(token TEXT NOT NULL);\nCREATE INDEX idx_vocabfilter ON vocabfilter(token);\n`;
sql += `CREATE TABLE trans_model(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, sourcekey INTEGER NOT NULL, targetkey INTEGER NOT NULL, floatval REAL NOT NULL);\nCREATE TABLE trans_model_rev(id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, sourcekey INTEGER NOT NULL, targetkey INTEGER NOT NULL, floatval REAL NOT NULL);\nCREATE INDEX idx_trans_model_sourcekey ON trans_model(sourcekey);\nCREATE INDEX idx_trans_model_targetkey ON trans_model(targetkey);\nCREATE INDEX idx_trans_model_rev_sourcekey ON trans_model_rev(sourcekey);\nCREATE INDEX idx_trans_model_rev_targetkey ON trans_model_rev(targetkey);\n`;

const tmGuid = netGuid();
sql += `INSERT INTO translation_memories VALUES(1, x'${tmGuid}', '${esc(tmName)}', '${srcLang}', '${trgLang}', NULL, 'Generated by Translate Lab Route B', 63, 'translate-lab', '${now}', NULL, 15, NULL, NULL, 0, ${tuRows.length}, 1, 1, 1, 0);\n`;
sql += `INSERT INTO parameters VALUES(NULL,'VERSION','8.06'),(NULL,'VERSION_CREATED','8.10'),(NULL,'FREQUENCYTOP','1000'),(1,'TokenDataVersion','1'),(1,'AlignmentDataVersion','1');\n`;

for (let i = 0; i < tuRows.length; i++) {
  const tu = tuRows[i];
  sql += `INSERT INTO translation_units VALUES(${i + 1}, x'${netGuid()}', 1, ${tu.srcHash}, '${esc(makeXml(tu.srcText, srcLang))}', ${tu.trgHash}, '${esc(makeXml(tu.trgText, trgLang))}', '${now}', 'translate-lab', '${now}', 'translate-lab', '${now}', 'translate-lab', 0, 393473, x'${tu.srcBlob}', x'${tu.trgBlob}', NULL, NULL, '${now}', NULL);\n`;
  sql += `INSERT INTO fuzzy_data VALUES(1, ${i + 1}, '${tu.fi1}', '${tu.fi2}', '${tu.fi4}', '${tu.fi8}');\n`;
  sql += `INSERT INTO translation_unit_fragments VALUES(${i + 1}, ${tu.srcHash});\n`;
}
sql += `COMMIT;\n`;

try { unlinkSync(outPath); } catch {}
execFileSync("sqlite3", [outPath], { input: sql, encoding: "utf8", maxBuffer: 1 << 28 });
const check = execFileSync("sqlite3", [outPath, "PRAGMA integrity_check; SELECT 'TUs: ' || count(*) FROM translation_units; SELECT 'fi: ' || count(*) FROM fuzzy_data; SELECT 'frag: ' || count(*) FROM translation_unit_fragments;"], { encoding: "utf8" });
console.log(`\nVerification:\n${check}`);
console.log(`Output: ${outPath}`);
console.log(`✓ Route B complete — full fuzzy index, zero Trados dependency`);
