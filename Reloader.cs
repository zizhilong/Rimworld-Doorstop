using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Verse;

namespace Doorstop
{
    public class Reloader
    {
        public static void Start()
        {
            var harmony = new Harmony("brrainz.doorstop");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            instance = new Reloader();
        }

        public static Reloader instance;
        const string doorstopPrefix = "doorstop_";
        readonly string modsDir;
        static readonly Dictionary<string, MethodBase> reloadableMembers = new Dictionary<string, MethodBase>();
        static readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        /*
        // 文件更改去抖动器，防止频繁触发文件更改事件
        static readonly Debouncer changedFiles = new(TimeSpan.FromSeconds(3), basePath =>
        {
            // 拼接 DLL 文件路径
            var path = $"{basePath}.dll";

            // 记录文件更改事件并输出路径
            FLog.WriteLog($"File change detected, attempting to reload assembly: {path}");

            try
            {
                // 尝试重载指定路径的程序集
                var assembly = ReloadAssembly(path, true);

                // 记录程序集成功加载
                FLog.WriteLog($"Successfully reloaded assembly: {assembly.FullName}");

                // 更新程序集信息
                UpdateAssembly(assembly);
            }
            catch (Exception ex)
            {
                // 如果发生异常，记录错误信息
                FLog.WriteLog($"Error loading assembly from path: {path}. Exception: {ex.Message}");
            }
        });
        */

        public Reloader()
        {
            // 设置 mods 文件夹的路径，假设该文件夹位于当前工作目录下
            modsDir = Path.Combine(Directory.GetCurrentDirectory(), "Mods");

            // 删除所有文件（可能是为了清理之前的加载或缓存）
            DeleteAllFiles();

            // 为 "dll" 文件和 "pdb" 文件创建文件监视器
            watchers.Add(CreateWatcher("dll"));
        }

        // 创建一个文件系统监视器，用于监视指定后缀的文件
        FileSystemWatcher CreateWatcher(string suffix)
        {
            // 创建文件监视器
            var watcher = new FileSystemWatcher(modsDir)
            {
                // 设置过滤器，监视指定后缀的文件
                Filter = $"*.{suffix}",

                // 包括子目录中的文件
                IncludeSubdirectories = true,

                // 启用事件触发
                EnableRaisingEvents = true
            };

            // 记录文件监视器的添加信息
            FLog.WriteLog($"Started watching directory: {modsDir} for files with suffix: .{suffix}");

            // 错误事件处理：如果发生错误，记录异常信息
            watcher.Error += (sender, e) => FLog.WriteLog($"Error occurred: {e.GetException().Message}");

            // 文件更改事件处理：当监视的文件发生变化时触发
            watcher.Changed += (object _, FileSystemEventArgs e) =>
            {
                var path = e.FullPath;

                // 记录文件更改的信息
                FLog.WriteLog($"File changed: {path}");


                // 判断文件目录是否以 "Assemblies" 结尾
                if (!path.Replace('\\', '/').Contains("/Assemblies/"))
                {
                    FLog.WriteLog($"Ignoring file outside 'Assemblies' directory: {path}");
                    return;
                }

                // 判断文件名是否不包含下划线
                var fileName = Path.GetFileName(path);
                if (fileName.Contains("_") || fileName.Contains(".pdb"))
                {
                    FLog.WriteLog($"Ignoring file with underscore in name: {path}");
                    return;
                }

                // 如果文件符合条件，继续处理文件更改的逻辑
                FLog.WriteLog($"Processing file: {path}");

                var assembly = ReloadAssembly(path, false);

                // 记录程序集成功加载
                FLog.WriteLog($"Successfully reloaded assembly: {assembly.FullName}");

                // 更新程序集信息
                UpdateAssembly(assembly);

                // 这里可以继续处理文件更改的逻辑
            };

            return watcher;
        }

        public static void RewriteAssemblyResolving()
        {
            // 注册当前 AppDomain 的反射程序集解析事件
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (sender, args) =>
            {
                // 获取请求的程序集名称
                var requestedAssemblyName = new AssemblyName(args.Name).Name;

                // 在已加载的程序集列表中查找与请求的程序集名称匹配的程序集
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                     .FirstOrDefault(a => new AssemblyName(a.FullName).Name == requestedAssemblyName);

                // 如果找到了已加载的程序集，则返回该程序集的反射只读版本
                if (loadedAssembly != null)
                    return Assembly.ReflectionOnlyLoadFrom(loadedAssembly.Location);

                // 如果没有找到匹配的程序集，抛出异常
                throw new InvalidOperationException($"Unable to resolve assembly: {args.Name}");
            };
        }


        // 删除所有与 doorstop 前缀匹配的文件（dll 和 pdb）
        public void DeleteAllFiles()
        {
            // 删除所有符合指定模式的 dll 文件
            DeleteFiles(modsDir, $"{doorstopPrefix}??????????????_*.dll");

            // 删除所有符合指定模式的 pdb 文件
            DeleteFiles(modsDir, $"{doorstopPrefix}??????????????_*.pdb");
        }

        // 删除指定目录中匹配搜索模式的所有文件
        static void DeleteFiles(string directory, string searchPattern)
        {
            // 遍历指定目录及其子目录下的所有匹配的文件
            foreach (var file in Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories))
                try
                {
                    // 删除文件
                    File.Delete(file);
                }
                finally { }
        }

        // 加载原始程序集，并注册可重载的成员
        public static Assembly LoadOriginalAssembly(string path)
        {
            try
            {
                // 记录开始加载原始程序集的信息
                FLog.WriteLog($"Starting to load assembly from path: {path}");

                // 重新加载指定路径的程序集
                var originalAssembly = ReloadAssembly(path, false);

                // 记录程序集加载完成的信息
                FLog.WriteLog($"Successfully loaded assembly: {originalAssembly.FullName}");

                // 获取程序集中的所有类型，并查找其中的可重载成员
                originalAssembly.GetTypes().SelectMany(type => Tools.AllReloadableMembers(type, reflectionOnly: false))
                    .Do(member =>
                    {
                        // 输出已注册的成员信息，便于调试
                        FLog.WriteLog($"Registered {member.FullDescription()} for reloading [{member.Id()}] {member.GetHashCode()}");

                        // 将成员注册到重载成员字典中，使用成员 ID 作为键
                        reloadableMembers[member.Id()] = member;
                    });

                // 返回原始程序集
                return originalAssembly;
            }
            catch (Exception ex)
            {
                // 捕获异常并记录错误信息
                FLog.WriteLog($"Error while loading assembly from path: {path}. Exception: {ex.Message}");
                throw; // 重新抛出异常
            }
        }

        static int n = 0;
        // 重新加载指定路径的程序集
        static Assembly ReloadAssembly(string path, bool reflectionOnly)
        {
            try
            {
                // 获取程序集所在目录
                var assembliesDir = Path.GetDirectoryName(path);

                // 获取程序集文件的基本名称（不包含扩展名）
                var baseName = Path.GetFileNameWithoutExtension(path);

                // 创建文件名前缀，包含当前时间戳
                var filenamePrefix = $"{doorstopPrefix}{DateTime.Now:yyyyMMddHHmmss}_";
                var thisid = ++n;

                // 构造原始 DLL 文件路径
                var originalDll = Path.Combine(assembliesDir, $"{baseName}.dll");

                // 构造复制后的 DLL 文件路径
                var copyDllPath = Path.Combine(assembliesDir, $"{filenamePrefix}{baseName}.dll");

                // 日志记录：复制前的操作
                FLog.WriteLog($"Attempting to copy {originalDll} to {copyDllPath}.");

                // 复制原始 DLL 文件到新的路径，并增加计数
                Tools.Copy(originalDll, copyDllPath, thisid);

                // 构造原始 DLL 文件路径
                var originalPdb = Path.Combine(assembliesDir, $"{baseName}.pdb");

                // 构造复制后的 DLL 文件路径
                var copyPdbPath = Path.Combine(assembliesDir, $"{filenamePrefix}{baseName}.pdb");
                File.Copy(originalPdb, copyPdbPath, overwrite: true);
                // 日志记录：复制成功
                FLog.WriteLog($"Successfully copied {originalDll} to {copyDllPath}.");

                Assembly assembly;

                    if (File.Exists(copyPdbPath))
                    {
                        //byte[] dllData = File.ReadAllBytes(copyDllPath);
                        //byte[] pdbData = File.ReadAllBytes(copyPdbPath);
                        //assembly = Assembly.Load(dllData, pdbData);
                    FLog.WriteLog($"Successfully loaded {copyDllPath} FOR PDB");
                    /*
                    // 加载 DLL 文件
                    Assembly assembly = Assembly.LoadFrom(copyDllPath);


                    // 如果 PDB 文件存在，则尝试加载调试符号
                    if (File.Exists(copyPdbPath))
                    {
                        // 通常 PDB 不需要加载到内存中，但可以在调试时与 DLL 文件匹配
                        FLog.WriteLog($"PDB file {copyPdbPath} found for {copyDllPath}, ready for debugging.");
                        // 你可以通过调试工具或其他方式来设置符号文件路径
                    }
                    else
                    {
                        FLog.WriteLog($"No PDB file found for {copyDllPath}. Debugging information might be missing.");
                    }
                    */
                }
                    else
                    {
                        //byte[] dllData = File.ReadAllBytes(copyDllPath);
                        //assembly = Assembly.Load(dllData);
                        FLog.WriteLog($"Successfully loaded {copyDllPath} FOR dll");
                    //FLog.WriteLog($"Error: DLL file {copyDllPath} does not exist.");
                };

                //Assembly.Load()

                // 读取复制后的 DLL 文件并加载它
                assembly= Assembly.LoadFrom(copyDllPath);



                foreach (var module in assembly.GetModules()) {
                    FLog.WriteLog($"GetModules {module.Name}");
                }


                // 日志记录：加载程序集
                FLog.WriteLog($"Assembly {baseName} loaded from {copyDllPath} hash{assembly.GetHashCode()}.");

                return assembly;
            }
            catch (Exception ex)
            {
                // 日志记录：捕获异常并写入日志
                FLog.WriteLog($"Error occurred while reloading assembly: {ex.Message}");
                throw;
            }
        }

        // 更新指定程序集中的方法，进行重载



        // 更新程序集及其引用
        static void UpdateAssembly(Assembly assembly)
        {

            //查询更新程序集所有成员
            assembly.GetTypes().SelectMany(type => Tools.AllReloadableMembers(type, reflectionOnly: false))
                .Do(member =>
                {
                    // 输出已注册的成员信息，便于调试
                    //FLog.WriteLog($"Registered {member.FullDescription()} for reloading [{member.Id()}]");
                    // 将成员注册到重载成员字典中，使用成员 ID 作为键
                    //reloadableMembers[member.Id()] = member;
                    if (reloadableMembers.ContainsKey(member.Id()))
                    {

                        var originalMethod = reloadableMembers[member.Id()];
                        FLog.WriteLog($"Detouring method: {originalMethod.Name}");
                        var patch = new HarmonyMethod((MethodInfo)member);
                        MethodInfo methodInfo = member as MethodInfo;
                        Tools.DetourMethod(originalMethod, member);
                        try
                        {

                            //FLog.WriteLog($"Detouring method Start: {originalMethod.Name} {originalMethod.GetHashCode()}  {member.GetHashCode()}");

                            //Tools.DetourMethod(originalMethod, member);
                            FLog.WriteLog($"Detouring method End: {originalMethod.Name}");
                            /*
                            // 强制将 MethodBase 转换为 MethodInfo
                            if (methodInfo == null)
                                {
                                    throw new InvalidCastException("Original method is not a MethodInfo.");
                                }

                                // 创建一个 HarmonyMethod 实例来处理补丁
                                var prefixPatch = new HarmonyMethod(methodInfo);
                                if (prefixPatch == null)
                                {
                                    throw new InvalidCastException("prefixPatch  is null.");
                                }
                            // 创建 Harmony 实例
                            var harmony = new Harmony("com.example.dynamicpatch1");

                            // 应用补丁
                            FLog.WriteLog($"Patch Method name : {prefixPatch}");
                            harmony.Patch(originalMethod, prefix: prefixPatch);

                                // 成功应用补丁后记录方法名称
                                FLog.WriteLog($"Patch Method name : {methodInfo.Name}");
                            */
                        }

                        catch (InvalidCastException castEx)
                        {
                            // 捕捉类型转换异常
                            FLog.WriteLog($"Error: {castEx.Message}");
                        }
                        catch (Exception ex)
                        {
                            // 捕捉其他异常
                            FLog.WriteLog($"An error occurred: {ex.Message}");
                        }

                    }
                    else
                    {
                        FLog.WriteLog($"Not Conv MethodInfo : {member.Name}");
                    }

                });


        }
    }
}