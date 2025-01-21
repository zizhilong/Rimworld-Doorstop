using System;
using System.IO;
using System.Reflection;  // 引入反射相关功能

namespace Doorstop
{
    // 入口点类，用于加载程序集和启动 Reloader
    public class Entrypoint
    {
        // 定义一个字符串数组，包含要加载的程序集名称
        static readonly string[] assemblies = new string[]
        {
            "0Harmony.dll",            // Harmony 库
            "Mono.Cecil.dll",          // Mono.Cecil 库，用于程序集的读取和修改
            "Mono.Cecil.Mdb.dll",      // Mono.Cecil 库的调试符号支持
            "Mono.Cecil.Pdb.dll",      // Mono.Cecil 库的 PDB 支持
            "Mono.Cecil.Rocks.dll"     // Mono.Cecil 库的拓展支持
        };
        private static readonly string logFilePath = "Doorstop_load.log"; // 设置日志文件路径

        // 写入日志到文件
        public static void WriteLog(string message)
        {
            try
            {
                // 如果文件不存在，创建文件并写入头部信息
                if (!File.Exists(logFilePath))
                {
                    using (StreamWriter writer = new StreamWriter(logFilePath, true))
                    {
                        writer.WriteLine("Log started: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                }

                // 写入日志内容
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to log file: " + ex.Message);
            }
        }

        // 启动方法：加载所需的程序集并启动 Reloader
        public static void Start()
        {

            Guanggao();
            // 遍历所有程序集名称，并加载对应的程序集
            foreach (var assemblyName in assemblies)
            {
                if (string.IsNullOrEmpty(assemblyName))
                {
                    WriteLog("Assembly name is null or empty, skipping...");
                    continue;
                }

                var assemblyBytes = LoadResourceBytes(assemblyName);
                if (assemblyBytes == null || assemblyBytes.Length == 0)
                {
                    WriteLog($"Failed to load resource bytes for assembly: {assemblyName}");
                    continue;
                }

                try
                {
                    Assembly.Load(assemblyBytes);  // 从嵌入资源中加载程序集
                    WriteLog($"Successfully loaded assembly: {assemblyName}");
                }
                catch (Exception ex)
                {
                    WriteLog($"Error loading assembly {assemblyName}: {ex.Message}");
                }
            }

            // 获取 Reloader 类型并调用 Start 方法启动
            try
            {
                var reloaderType = Type.GetType("Doorstop.Reloader");
                if (reloaderType == null)
                {
                    WriteLog("Failed to get Reloader type.");
                    return;
                }

                var startMethod = reloaderType.GetMethod("Start");
                if (startMethod == null)
                {
                    WriteLog("Failed to find Start method on Reloader.");
                    return;
                }

                startMethod.Invoke(null, null);
                WriteLog("Reloader started successfully.");
            }
            catch (Exception ex)
            {
                WriteLog($"Error invoking Start method: {ex.Message}");
            }

        }

        static void Guanggao() {
            if (!File.Exists("关于热更新.txt"))
            {
                using (StreamWriter writer = new StreamWriter("关于热更新.txt", true))
                {
                    writer.WriteLine("本插件由QQ 104978 紫之龙开发");
                    writer.WriteLine("插播广告");
                    writer.WriteLine("临沂呆马区块链,承接各类政府企事业单位软件项目,信创系统升级改造,物流大数据.物联网应用开发");
                    writer.WriteLine("官方网站  https://daima.mobi/   https://9885.com/");

                }
            }
        }


        // 从嵌入的资源文件中加载字节数据
        static byte[] LoadResourceBytes(string resourceName)
        {
            // 获取嵌入资源流
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            // 创建字节数组并读取资源内容
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            return data;
        }
    }
}