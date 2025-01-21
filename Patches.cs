using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Doorstop
{
    // 处理应用程序退出时清理的类
    public class ShutdownHandler : MonoBehaviour
    {
        // 当应用程序退出时调用该方法
        public void OnApplicationQuit()
        {
            // 删除所有文件
            Reloader.instance.DeleteAllFiles();
        }
    }

    // 为 UIRoot_Entry 类的 Init 方法应用 Harmony 补丁
    [HarmonyPatch(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init))]
    static class UIRoot_Entry_Init_Patch
    {
        // Init 方法执行完毕后执行此 Postfix 方法
        static void Postfix()
        {
            // 创建一个新的 GameObject，命名为 RimWorldDoorstopObject
            var obj = new GameObject("RimWorldDoorstopObject");

            // 让该对象在加载场景时不被销毁
            Object.DontDestroyOnLoad(obj);

            // 为该对象添加 ShutdownHandler 组件，用于处理退出时的清理
            obj.AddComponent<ShutdownHandler>();
        }
    }

	[HarmonyPatch(typeof(ModAssemblyHandler), nameof(ModAssemblyHandler.ReloadAll))]
    // 为 ModAssemblyHandler 类的 ReloadAll 方法应用 Harmony 补丁
    static class ModAssemblyHandler_ReloadAll_Patch
    {
        // 通过 Transpiler 修改字节码，替换指定方法
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // 替换原始的 Assembly.LoadFile 方法为自定义的 Reloader.LoadOriginalAssembly 方法
            return instructions.MethodReplacer(
                SymbolExtensions.GetMethodInfo(() => Assembly.LoadFile("")),
                SymbolExtensions.GetMethodInfo(() => Reloader.LoadOriginalAssembly(""))
            );
        }

        // 在 ReloadAll 方法执行后调用
        static void Postfix()
        {
            // 重写程序集解析逻辑
            Reloader.RewriteAssemblyResolving();
        }
    }

    [HarmonyPatch(typeof(UIRoot), nameof(UIRoot.UIRootUpdate))]
    // 为 UIRoot 类的 UIRootUpdate 方法应用 Harmony 补丁
    [HarmonyPatch(typeof(UIRoot), nameof(UIRoot.UIRootUpdate))]
    static class UIRoot_UIRootUpdate_Patch
    {
        // 定义队列，用于存储不同级别的日志消息
        internal static readonly Queue<string> messages = new();
        internal static readonly Queue<string> warnings = new();
        internal static readonly Queue<string> errors = new();

        // 在 UIRootUpdate 方法执行后调用此方法
        static void Postfix()
        {
            // 如果消息队列中有消息，尝试取出并记录日志
            if (messages.TryDequeue(out var message))
                Log.Message(message);

            // 如果警告队列中有警告，尝试取出并记录警告日志
            if (warnings.TryDequeue(out var warning))
                Log.Warning(warning);

            // 如果错误队列中有错误，尝试取出并记录错误日志
            if (errors.TryDequeue(out var error))
                Log.Error(error);
        }
    }
}
