using System;
using System.IO;
using System.Threading;

namespace Doorstop
{
    public static class FLog
    {
        // 日志文件路径
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doorstop_log.txt");
        // 用于确保写入操作是线程安全的
        private static readonly object lockObj = new object();

        // 写入日志的方法
        public static void WriteLog(string message)
        {
            try
            {
                // 获取当前时间戳
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logMessage = $"[{timestamp}] {message}";

                // 确保线程安全地写入日志
                lock (lockObj)
                {
                    // 如果文件不存在，创建文件
                    if (!File.Exists(logFilePath))
                    {
                        using (StreamWriter writer = new StreamWriter(logFilePath, true))
                        {
                            writer.WriteLine("Log started: " + timestamp);
                        }
                    }

                    // 将日志信息写入文件
                    using (StreamWriter writer = new StreamWriter(logFilePath, true))
                    {
                        writer.WriteLine(logMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果日志写入出错，可以将错误记录到控制台或其他地方
                Console.WriteLine($"Failed to write log: {ex.Message}");
            }
        }
    }
}
