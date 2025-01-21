using System;
using System.Collections.Concurrent;  // 引入线程安全的集合
using System.Threading;  // 引入多线程相关功能

namespace Doorstop
{
    // 用于防抖处理的类，避免频繁调用某个操作
    internal class Debouncer
    {
        // 存储每个文件路径对应的定时器
        private readonly ConcurrentDictionary<string, Timer> changes = new();

        // 防抖延迟周期
        private readonly TimeSpan debouncePeriod;

        // 防抖时触发的操作
        private readonly Action<string> action;

        // 构造函数，初始化防抖周期和操作
        internal Debouncer(TimeSpan debouncePeriod, Action<string> action)
        {
            this.debouncePeriod = debouncePeriod;
            this.action = action;
        }

        // 向防抖器添加文件路径，如果文件路径已存在则重置定时器
        internal void Add(string filePath)
        {
            // 检查文件路径是否已有定时器，如果有则更新定时器的触发时间
            if (changes.TryGetValue(filePath, out var existingTimer))
            {
                // 更新定时器，延迟至新的防抖周期
                existingTimer.Change(debouncePeriod, Timeout.InfiniteTimeSpan);
                return;
            }

            // 如果文件路径没有对应的定时器，则创建一个新的定时器
            changes[filePath] = new Timer(_ => TimerCallback(filePath), null, debouncePeriod, Timeout.InfiniteTimeSpan);
        }

        // 定时器回调方法，执行防抖时指定的操作
        private void TimerCallback(string filePath)
        {
            // 移除文件路径对应的定时器
            changes.TryRemove(filePath, out var timer);

            // 执行防抖操作
            action(filePath);
        }
    }
}