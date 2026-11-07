# SDLTM Writer

把 TMX 翻译记忆转换成 Trados Studio 能打开的 `.sdltm` 文件。

**不需要安装 Trados。** 拖入 TMX，选语言方向，点导出。

## 使用

1. 下载 `SdltmWriter.exe`（[Releases](../../releases)）
2. 双击打开
3. 拖入 `.tmx` 文件
4. 选源语言 → 目标语言
5. 点「导出 SDLTM」

生成的 `.sdltm` 在 Trados Studio 中打开时，会提示「需要重建索引」——点一次即可，之后就是正常的 Trados 翻译记忆。

## 能做什么

| 功能 | 支持 |
|------|:----:|
| 精确匹配 | ✅ |
| 模糊匹配 | ✅ |
| Concordance 搜索 | ✅ |
| 中英双向 | ✅ |
| 完整索引数据（fi1-fi8） | ✅ |

## 技术原理

`.sdltm` 文件本质是 SQLite 数据库。本工具：
- 使用与 Trados 同源的 ICU 分词（浏览器 `Intl.Segmenter` / Windows ICU）
- 计算精确匹配哈希（FNV-1a + Jenkins 组合）
- 生成模糊搜索所需的字符二元组和词特征索引
- 写入标准 SQLite schema（与 Trados 引擎内嵌 DDL 一致）

详细格式规格见 [docs/FORMAT.md](docs/FORMAT.md)。

## 从源码构建

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

需要 .NET 8 SDK。

## 声明

本项目与 RWS Holdings plc / SDL Trados Technologies 无关。
"Trados" 和 "SDL" 是 RWS Holdings plc 的商标。
本项目不分发任何专有软件或二进制资产，所有代码为原创实现。

## 许可证

[MIT](LICENSE)
