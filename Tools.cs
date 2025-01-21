using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static HarmonyLib.AccessTools;

namespace Doorstop
{
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
	public class ReloadableAttribute : Attribute
	{
	}

	internal static class Tools
	{
        // 存储 ReloadableAttribute 类型的名称，用于识别可重载的方法
        static readonly string reloadableTypeName = typeof(ReloadableAttribute).Name;

        // 定义一个委托，用于替换方法的劫持
        internal delegate void DetourMethodDelegate(MethodBase method, MethodBase replacement);

        // 使用反射获取方法并将其赋值给委托，初始化 DetourMethod 委托
        internal static readonly DetourMethodDelegate DetourMethod = MethodDelegate<DetourMethodDelegate>(Method("HarmonyLib.PatchTools:DetourMethod"));

        // 扩展方法：将日志消息添加到消息队列中
        internal static void LogMessage(this string log) => UIRoot_UIRootUpdate_Patch.messages.Enqueue(log);

        // 扩展方法：将警告消息添加到警告队列中
        internal static void LogWarning(this string log) => UIRoot_UIRootUpdate_Patch.warnings.Enqueue(log);

        // 扩展方法：将错误消息添加到错误队列中
        internal static void LogError(this string log) => UIRoot_UIRootUpdate_Patch.errors.Enqueue(log);

        // 扩展方法：检查方法是否具有 ReloadableAttribute 特性
        internal static bool ReflectIsReloadable(this MethodBase method) =>
            method.GetCustomAttributesData().Any(a => a.AttributeType.Name == reloadableTypeName);

        // 扩展方法：检查方法是否有 ReloadableAttribute 特性
        internal static bool IsReloadable(this MethodBase method) =>
            method.CustomAttributes.Any(a => a.AttributeType.Name == reloadableTypeName);


        // 扩展方法：返回文件路径，不包括扩展名
        internal static string WithoutFileExtension(this string filePath)
        {
            // 获取文件所在的目录
            var directory = Path.GetDirectoryName(filePath);

            // 获取文件名，不包括扩展名
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

            // 返回完整路径，不包括扩展名
            return Path.Combine(directory, fileNameWithoutExtension);
        }

        // 获取方法成员的唯一标识符
        internal static string Id(this MethodBase member)
        {
            var sb = new StringBuilder(128);

            // 拼接类全名、方法名、方法参数类型等信息
            sb.Append(member.DeclaringType.FullName);
            sb.Append('.');
            sb.Append(member.Name);
            sb.Append('(');
            sb.Append(string.Join(", ", member.GetParameters().Select(p => p.ParameterType.FullName)));
            sb.Append(')');

            // 返回方法的唯一标识符
            return sb.ToString();
        }

        // 获取类型中所有可重载的方法
        internal static IEnumerable<MethodBase> AllReloadableMembers(this Type type, bool reflectionOnly)
        {
            // 根据 reflectionOnly 参数选择不同的检查方法
            Func<MethodBase, bool> isReloadable = reflectionOnly ? ReflectIsReloadable : IsReloadable;

            // 获取类型中所有符合条件的可重载方法
            foreach (var member in type.GetMethods(all).Where(isReloadable))
            {
                yield return member;
            }
        }

        internal static void Copy(string source, string target, int n)
        {
            // 读取源程序集文件
            var assembly = AssemblyDefinition.ReadAssembly(source);

            // 修改程序集的名称，添加一个计数器（n）作为后缀
            assembly.Name.Name = $"{assembly.Name.Name}-{n}";

            // 清除程序集的公钥信息，避免复制时带有原始公钥
            assembly.Name.PublicKey = null;
            assembly.Name.HasPublicKey = false;

            // 将修改后的程序集保存到目标路径
            assembly.Write(target);
        }
    }
}