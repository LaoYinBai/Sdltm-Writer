# SDLTM Writer

> 手里有一份 TMX 翻译记忆，却没有装 Trados？这个小工具帮你把它变成 Trados Studio 能打开的 `.sdltm` 文件。

**不用装 Trados，也不用装任何环境。** 双击打开，拖入 TMX，选好语言方向，点一下导出就完成了。

**在线体验（实验版）**：翻译站 **[translate.modiconnect.cn](https://translate.modiconnect.cn)** 的「中英语料工具」里，也能把 TMX 导出为 SDLTM。全程在浏览器中完成，目前仍是实验功能。

---

## 怎么用

1. 到 [Releases](../../releases) 页面下载 `SdltmWriter.exe`（目前提供 Windows 版）。
2. 双击打开。
3. 把 `.tmx` 文件拖进窗口。
4. 选择源语言和目标语言。
5. 点 **「导出 SDLTM」**，选一个保存位置。

生成的 `.sdltm` 在 Trados Studio 里第一次打开时，会提示「需要重建索引」。点一次就好，之后它就是正常的翻译记忆了。

> 第一次使用，建议先拿一份不重要的 TMX 试试，确认在你的 Trados 里能正常检索，再用到正式项目上。

---

## 它能做什么

- ✅ 精确匹配
- ✅ 模糊匹配
- ✅ Concordance 搜索
- ✅ 中英双向
- ✅ 完整的索引数据

---

## 它是怎么工作的

`.sdltm` 文件本质上是一个 SQLite 数据库。这个工具按 Trados 的结构写入数据：

- 用与 Trados 同源的分词方式（浏览器 `Intl.Segmenter` / Windows ICU）处理文本；
- 计算精确匹配需要的哈希值；
- 生成模糊搜索需要的字符二元组和词特征索引；
- 按 Trados 引擎内嵌的表结构写入标准 SQLite schema。

想了解格式细节的话，见 [docs/FORMAT.md](docs/FORMAT.md)。

---

## 从源码构建

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

需要 .NET 8 SDK。

---

## 声明

本项目与 RWS Holdings plc / SDL Trados Technologies 无关。"Trados" 和 "SDL" 是 RWS Holdings plc 的商标。

所有代码均为原创实现。

---

## 许可证

[MIT](LICENSE)
