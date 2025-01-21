// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;
// 向代码分析器抑制特定的警告消息

[assembly: SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Used to keep a reference around", Scope = "member", Target = "~F:Doorstop.Entrypoint.reloader")]
[assembly: SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Used to keep a reference around", Scope = "member", Target = "~F:Doorstop.Reloader.instance")]

/*
 * 注释解释：
[SuppressMessage] 特性用于抑制代码分析工具（如 Visual Studio 中的静态代码分析）生成的警告。
"CodeQuality"：表示要抑制的警告类型。
"IDE0052:Remove unread private members"：警告 ID，表示移除未使用的私有成员。
Justification = "Used to keep a reference around"：解释抑制该警告的原因，这里表示成员被保留用于保持引用。
Scope = "member"：指定警告适用于成员（字段、方法等）。
Target = "~F:Doorstop.Entrypoint.reloader" 和 Target = "~F:Doorstop.Reloader.instance"：指定警告应用的目标字段（成员）。
 */