Composa 1.4.0 简体中文界面

基准提交：86529d3b5355d28ddc28ec3db2e4de1d4d00f63b。

译文资源位于 src/Composa.App/Localization/，分为术语、提示与错误信息三个 TSV 文件。每行以制表符分隔英文与中文。动态内容使用 {0} 等占位符；中文版中的 {0:t} 表示该内容也需要按显示名称翻译。文件名和其他用户内容应使用不带 :t 的占位符。

src/Composa.App/Localization.cs 负责显示层翻译；原有编辑命令、快捷键 ID、面板设置键、序列化标识及历史图标判断仍保留英文内部名称。Ui.UserLabel 用于用户输入和文件名称，避免误翻译。Ui.Combo 的 localize:false 用于字体名称。

src/Composa.Core/Editing/DefaultNames.cs 提供默认名称翻译委托，由中文版应用初始化。仅新建内容使用中文默认名称；用户重命名和导入内容不改写。

中文版使用独立设置与恢复目录 Composa.zh-CN，默认关闭自动更新。官方更新下载按钮明确指向英文官方版本。

使用 .NET 10 SDK 及正常的 NuGet 连接，按 README 构建。版本号由 MinVer 与 Git 标签决定；不要手工改动版本属性。中文版标签采用 v1.4.0-zh.N，区别于上游版本。

L10n.Enabled 默认为 true。上游界面回归测试在 TestApp 中关闭翻译，以保留原有菜单查找和功能断言；ChineseLocalizationTests 单独启用中文，验证真实菜单操作、内部历史标识和用户数据保存。此开关不暴露为用户设置。

遵守根目录 LICENSE 及第三方许可证。
