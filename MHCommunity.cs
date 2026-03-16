using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MemoryHelper
{
    public class MemoryTools
    {
        // 日志事件定义
        public delegate void LogEventHandler(string message);
        public static event LogEventHandler OnLog;

        // 辅助方法：输出日志
        private static void Log(string message)
        {
            Console.WriteLine(message);
            OnLog?.Invoke(message);
        }

        // 保存原始内存数据的结构
        private struct MemoryBackup
        {
            public IntPtr Address;
            public string OriginalBytes;
            public int Size;
        }

        // 按进程ID保存的内存备份
        private static Dictionary<uint, List<MemoryBackup>> memoryBackups = new Dictionary<uint, List<MemoryBackup>>();
        
        // 保存窗口的原始标题
        private static Dictionary<IntPtr, string> originalWindowTitles = new Dictionary<IntPtr, string>();

        // 进程访问权限常量
        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_VM_WRITE = 0x0020;
        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;

        // Windows API 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EnumProcessModules(IntPtr hProcess, IntPtr[] lphModule, uint cb, out uint lpcbNeeded);

        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern uint GetModuleBaseName(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, uint nSize);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        // 委托类型
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // 获取指定窗口句柄对应的进程ID
        public static uint? GetProcessId(IntPtr hwnd)
        {
            uint processId;
            GetWindowThreadProcessId(hwnd, out processId);
            return processId != 0 ? (uint?)processId : null;
        }

        // 获取指定进程中指定模块的基址
        public static IntPtr? GetModuleBaseAddress(uint processId, string moduleName)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);
            if (hProcess == IntPtr.Zero)
                return null;

            try
            {
                const int MAX_MODULES = 1024;
                IntPtr[] hModules = new IntPtr[MAX_MODULES];
                uint cbNeeded;

                if (EnumProcessModules(hProcess, hModules, (uint)(hModules.Length * IntPtr.Size), out cbNeeded))
                {
                    int modulesCount = (int)(cbNeeded / (uint)IntPtr.Size);
                    StringBuilder moduleNameBuffer = new StringBuilder(260);

                    for (int i = 0; i < modulesCount; i++)
                    {
                        if (GetModuleBaseName(hProcess, hModules[i], moduleNameBuffer, (uint)moduleNameBuffer.Capacity) > 0)
                        {
                            if (moduleNameBuffer.ToString().ToLower() == moduleName.ToLower())
                                return hModules[i];
                        }
                    }
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }

            return null;
        }

        // 从指定进程内存中读取字节数组数据
        public static string ReadProcessMemoryAsString(uint processId, IntPtr address, int size = 4)
        {
            IntPtr hProcess = OpenProcess(PROCESS_VM_READ, false, processId);
            if (hProcess == IntPtr.Zero)
                return null;

            try
            {
                byte[] buffer = new byte[size];
                uint bytesRead;

                if (ReadProcessMemory(hProcess, address, buffer, (uint)size, out bytesRead))
                {
                    return string.Join(" ", buffer.Take((int)bytesRead).Select(b => b.ToString("x2")));
                }
                return null;
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        // 对字节数组切片
        public static string SliceBytes(string bytesArray, int start, int end)
        {
            string[] byteList = bytesArray.Split();
            string[] slicedList = byteList.Skip(start).Take(end - start).ToArray();
            return string.Join(" ", slicedList);
        }

        // 把字节数组转换为整数
        public static uint BytesToInt(string bytesArray)
        {
            string[] byteList = bytesArray.Split();
            Array.Reverse(byteList);
            string hexString = string.Join("", byteList);
            return Convert.ToUInt32(hexString, 16);
        }

        // 把整数转换为字节数组
        public static string HexToBytes(uint integerValue)
        {
            byte[] byteData = BitConverter.GetBytes(integerValue);
            return string.Join(" ", byteData.Select(b => b.ToString("x2")));
        }

        // 把整数先转化为16进制然后输出为字符串
        public static string IntToHexString(uint integerValue)
        {
            string hexString = integerValue.ToString("X8");
            return hexString.Substring(6, 2) + hexString.Substring(4, 2) + hexString.Substring(2, 2) + hexString.Substring(0, 2);
        }

        // 把单浮点数转换为字节数组
        public static string FloatToBytes(float floatValue)
        {
            byte[] byteData = BitConverter.GetBytes(floatValue);
            return string.Join(" ", byteData.Select(b => b.ToString("x2")));
        }

        // 把字节数组转换为单浮点数
        public static float BytesToFloat(string bytesArray)
        {
            string[] byteList = bytesArray.Split();
            byte[] byteData = byteList.Select(b => Convert.ToByte(b, 16)).ToArray();
            return BitConverter.ToSingle(byteData, 0);
        }

        // 把字节数组转换为UTF8字符串
        public static string BytesToUtf8String(string bytesArray)
        {
            string[] byteList = bytesArray.Split();
            byte[] byteData = byteList.Select(b => Convert.ToByte(b, 16)).ToArray();

            // 查找字符串结束符
            int nullPos = Array.IndexOf(byteData, (byte)0);
            if (nullPos != -1)
                Array.Resize(ref byteData, nullPos);

            try
            {
                return Encoding.UTF8.GetString(byteData);
            }
            catch
            {
                try
                {
                    // 尝试使用GBK编码
                    return Encoding.GetEncoding(936).GetString(byteData); // GBK
                }
                catch
                {
                    try
                    {
                        // 尝试使用默认编码
                        return Encoding.Default.GetString(byteData);
                    }
                    catch
                    {
                        // 作为最后的尝试，使用Latin1编码
                        return Encoding.GetEncoding("Latin1").GetString(byteData);
                    }
                }
            }
        }

        // 把UTF8字符串转换为字节数组
        public static string Utf8StringToBytes(string stringValue)
        {
            byte[] byteData = Encoding.UTF8.GetBytes(stringValue);
            return string.Join(" ", byteData.Select(b => b.ToString("x2")));
        }

        // 保存原始内存数据
        private static void SaveOriginalMemory(uint processId, IntPtr address, int size)
        {
            if (!memoryBackups.ContainsKey(processId))
            {
                memoryBackups[processId] = new List<MemoryBackup>();
                Log($"[内存备份] 为进程 {processId} 创建新的备份列表");
            }

            // 检查是否已经备份过这个地址
            if (memoryBackups[processId].Any(b => b.Address == address))
            {
                Log($"[内存备份] 地址 {address.ToString("X8")} 已存在备份，跳过");
                return;
            }

            Log($"[内存备份] 正在读取进程 {processId} 地址 {address.ToString("X8")} 的原始内存 (大小: {size} 字节)");
            string originalBytes = ReadProcessMemoryAsString(processId, address, size);
            if (originalBytes != null)
            {
                memoryBackups[processId].Add(new MemoryBackup
                {
                    Address = address,
                    OriginalBytes = originalBytes,
                    Size = size
                });
                Log($"[内存备份] 成功保存原始内存: 地址 {address.ToString("X8")} = {originalBytes}");
            }
            else
            {
                Log($"[内存备份] 读取原始内存失败: 地址 {address.ToString("X8")}");
            }
        }

        // 恢复原始内存数据
        public static bool RestoreOriginalMemory(uint processId)
        {
            Log($"[内存还原] 开始还原进程 {processId} 的内存");
            if (!memoryBackups.ContainsKey(processId))
            {
                Log($"[内存还原] 进程 {processId} 没有找到备份数据");
                return false;
            }

            Log($"[内存还原] 进程 {processId} 共有 {memoryBackups[processId].Count} 个内存区域需要还原");
            bool success = true;
            int index = 1;
            foreach (var backup in memoryBackups[processId])
            {
                Log($"[内存还原] [{index}/{memoryBackups[processId].Count}] 正在还原地址 {backup.Address.ToString("X8")} 到原始值: {backup.OriginalBytes}");
                if (WriteProcessMemory(processId, backup.Address, backup.OriginalBytes))
                {
                    Log($"[内存还原] [{index}/{memoryBackups[processId].Count}] 成功还原地址 {backup.Address.ToString("X8")}");
                }
                else
                {
                    Log($"[内存还原] [{index}/{memoryBackups[processId].Count}] 还原地址 {backup.Address.ToString("X8")} 失败");
                    success = false;
                }
                index++;
            }

            memoryBackups.Remove(processId);
            Log($"[内存还原] 进程 {processId} 还原完成，结果: {(success ? "成功" : "部分失败")}");
            return success;
        }

        // 还原窗口标题
        public static void RestoreWindowTitles(List<Tuple<IntPtr, string>> hwndsNames)
        {
            Log("[窗口标题还原] 开始还原窗口标题");
            if (hwndsNames == null)
            {
                Log("[窗口标题还原] 没有选中任何窗口");
                return;
            }

            int index = 1;
            foreach (var item in hwndsNames)
            {
                IntPtr hwnd = item.Item1;
                if (originalWindowTitles.ContainsKey(hwnd))
                {
                    string originalTitle = originalWindowTitles[hwnd];
                    Log($"[窗口标题还原] [{index}/{hwndsNames.Count}] 还原窗口 {hwnd.ToString("X8")} 的标题为: {originalTitle}");
                    SetWindowText(hwnd, originalTitle);
                    originalWindowTitles.Remove(hwnd);
                }
                else
                {
                    Log($"[窗口标题还原] [{index}/{hwndsNames.Count}] 窗口 {hwnd.ToString("X8")} 没有保存的原始标题");
                }
                index++;
            }
            Log("[窗口标题还原] 窗口标题还原完成");
        }

        // 恢复所有进程的原始内存数据
        public static void RestoreAllMemory(List<Tuple<IntPtr, string>> hwndsNames)
        {
            Log("[内存还原] 开始还原所有选中窗口的内存");
            if (hwndsNames == null)
            {
                Log("[内存还原] 没有选中任何窗口");
                return;
            }

            Log($"[内存还原] 共有 {hwndsNames.Count} 个窗口需要还原");
            int index = 1;
            foreach (var item in hwndsNames)
            {
                IntPtr hwnd = item.Item1;
                string name = item.Item2;
                Log($"[内存还原] [{index}/{hwndsNames.Count}] 处理窗口: {name} (句柄: {hwnd.ToString("X8")})");
                uint? processId = GetProcessId(hwnd);
                if (processId.HasValue)
                {
                    Log($"[内存还原] [{index}/{hwndsNames.Count}] 获取到进程ID: {processId.Value}");
                    RestoreOriginalMemory(processId.Value);
                }
                else
                {
                    Log($"[内存还原] [{index}/{hwndsNames.Count}] 无法获取进程ID");
                }
                index++;
            }
            
            // 还原窗口标题
            RestoreWindowTitles(hwndsNames);
            
            Log("[内存还原] 所有窗口还原操作完成");
        }

        // 向指定进程的指定地址写入字节数组（带备份功能）
        public static bool WriteProcessMemoryWithBackup(uint processId, IntPtr address, string bytesArray, int size)
        {
            SaveOriginalMemory(processId, address, size);
            return WriteProcessMemory(processId, address, bytesArray);
        }

        // 向指定进程的指定地址写入字节数组
        public static bool WriteProcessMemory(uint processId, IntPtr address, string bytesArray)
        {
            IntPtr hProcess = OpenProcess(PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, processId);
            if (hProcess == IntPtr.Zero)
                return false;

            try
            {
                string[] byteList = bytesArray.Split();
                byte[] byteData = byteList.Select(b => Convert.ToByte(b, 16)).ToArray();
                uint bytesWritten;

                if (WriteProcessMemory(hProcess, address, byteData, (uint)byteData.Length, out bytesWritten))
                    return true;
                return false;
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        // 定义函数 获取窗口的人物偏转角地址
        public static IntPtr GetPersonRotationAddress(IntPtr hwnd)
        {
            uint? processId = GetProcessId(hwnd);
            if (!processId.HasValue)
                return IntPtr.Zero;

            IntPtr? moduleBase = GetModuleBaseAddress(processId.Value, "netcraft.exe");
            if (!moduleBase.HasValue)
                return IntPtr.Zero;

            IntPtr address = moduleBase.Value + 0x1ACB4D0;
            string bytesArray = ReadProcessMemoryAsString(processId.Value, address, 4);
            if (bytesArray == null)
                return IntPtr.Zero;

            uint intValue = BytesToInt(bytesArray);
            IntPtr dihzi = (IntPtr)(intValue + 0x12A4);
            bytesArray = ReadProcessMemoryAsString(processId.Value, dihzi, 4);
            if (bytesArray == null)
                return IntPtr.Zero;

            float floatValue = BytesToFloat(bytesArray);
            // 这个方法在Windows Forms应用程序中不再直接输出到控制台
            // 而是通过Form1中的AddOutput方法显示在UI中
            return dihzi;
        }

        // 定义函数 获取人物名称
        public static string GetPersonName(IntPtr hwnd)
        {
            uint? processId = GetProcessId(hwnd);
            if (!processId.HasValue)
                return "";

            IntPtr? moduleBase = GetModuleBaseAddress(processId.Value, "netcraft.exe");
            if (!moduleBase.HasValue)
                return "";

            IntPtr address = moduleBase.Value + 0x0191D564;
            string bytesArray = ReadProcessMemoryAsString(processId.Value, address, 4);
            if (bytesArray == null)
                return "";

            uint intValue = BytesToInt(bytesArray);
            address = (IntPtr)(intValue + 0x1DC);
            bytesArray = ReadProcessMemoryAsString(processId.Value, address, 4);
            if (bytesArray == null)
                return "";

            intValue = BytesToInt(bytesArray);
            address = (IntPtr)(intValue + 0xC);
            bytesArray = ReadProcessMemoryAsString(processId.Value, address, 4);
            if (bytesArray == null)
                return "";

            intValue = BytesToInt(bytesArray);
            address = (IntPtr)(intValue + 0x160);
            bytesArray = ReadProcessMemoryAsString(processId.Value, address, 16);
            if (bytesArray == null)
                return "";

            return BytesToUtf8String(bytesArray);
        }

        // 窗口信息结构
        public struct WindowInfo
        {
            public IntPtr Hwnd;
            public string Name;
            public string Title;
            public uint ProcessId;
        }

        // 枚举所有奶块窗口
        public static List<WindowInfo> EnumMilkWindows()
        {
            List<WindowInfo> windows = new List<WindowInfo>();
            GCHandle listHandle = GCHandle.Alloc(windows);

            try
            {
                EnumWindows((hWnd, lParam) =>
                {
                    if (IsWindowVisible(hWnd))
                    {
                        StringBuilder className = new StringBuilder(256);
                        GetClassName(hWnd, className, className.Capacity);

                        if (className.ToString() == "CIrrDeviceWin32")
                        {
                            StringBuilder title = new StringBuilder(256);
                            GetWindowText(hWnd, title, title.Capacity);

                            if (!string.IsNullOrEmpty(title.ToString()))
                            {
                                uint processId;
                                GetWindowThreadProcessId(hWnd, out processId);
                                string name = GetPersonName(hWnd);
                                windows.Add(new WindowInfo { Hwnd = hWnd, Name = name, Title = title.ToString(), ProcessId = processId });
                            }
                        }
                    }
                    return true;
                }, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }

            return windows;
        }

        // 枚举所有奶块窗口输出
        public static void EnumMilkWindows1()
        {
            List<WindowInfo> windows = EnumMilkWindows();
            // 这个方法在Windows Forms应用程序中不再直接输出到控制台
            // 而是通过Form1中的AddOutput方法显示在UI中
        }

        // 选中人物
        public static List<Tuple<IntPtr, string>> SelectPerson(string name = "")
        {
            List<WindowInfo> all = EnumMilkWindows();
            List<IntPtr> hwnds = all.Select(w => w.Hwnd).ToList();
            List<string> names = all.Select(w => w.Name).ToList();

            if (!string.IsNullOrEmpty(name))
            {
                if (!names.Contains(name))
                {
                    Console.WriteLine("查无此人");
                    return null;
                }
                else
                {
                    int index = names.IndexOf(name);
                    hwnds = new List<IntPtr> { hwnds[index] };
                    names = new List<string> { names[index] };
                }
            }

            return hwnds.Zip(names, (h, n) => Tuple.Create(h, n)).ToList();
        }

        // 修改窗口标题
        public static bool SetWindowTitle(IntPtr hwnd, string newTitle)
        {
            try
            {
                return SetWindowText(hwnd, newTitle);
            }
            catch
            {
                return false;
            }
        }

        // 修改奶块窗口标题
        public static void SetMilkWindowTitle(List<Tuple<IntPtr, string>> hwndsNames = null)
        {
            Log("[窗口标题] 开始修改窗口标题");
            
            if (hwndsNames == null)
            {
                // 如果没有传入窗口列表，就枚举所有窗口
                List<WindowInfo> allWindows = EnumMilkWindows();
                if (allWindows == null)
                    return;

                int bianhao = 0;
                foreach (var window in allWindows)
                {
                    IntPtr hwnd = window.Hwnd;
                    string name = window.Name;
                    string originalTitle = window.Title;

                    // 保存原始标题
                    if (!originalWindowTitles.ContainsKey(hwnd))
                    {
                        originalWindowTitles[hwnd] = originalTitle;
                        Log($"[窗口标题] 保存窗口 {hwnd.ToString("X8")} 的原始标题: {originalTitle}");
                    }

                    string newTitle;
                    if (string.IsNullOrEmpty(name))
                    {
                        bianhao++;
                        newTitle = $"未知人物{bianhao}";
                    }
                    else
                    {
                        newTitle = name;
                    }
                    
                    Log($"[窗口标题] 将窗口 {hwnd.ToString("X8")} 的标题从 '{originalTitle}' 修改为 '{newTitle}'");
                    SetWindowText(hwnd, newTitle);
                }
            }
            else
            {
                // 使用传入的窗口列表（包含用户输入的名称）
                foreach (var item in hwndsNames)
                {
                    IntPtr hwnd = item.Item1;
                    string name = item.Item2;
                    
                    // 获取原始标题
                    string originalTitle = "";
                    StringBuilder titleBuilder = new StringBuilder(256);
                    GetWindowText(hwnd, titleBuilder, titleBuilder.Capacity);
                    originalTitle = titleBuilder.ToString();

                    // 保存原始标题
                    if (!originalWindowTitles.ContainsKey(hwnd))
                    {
                        originalWindowTitles[hwnd] = originalTitle;
                        Log($"[窗口标题] 保存窗口 {hwnd.ToString("X8")} 的原始标题: {originalTitle}");
                    }

                    string newTitle = name;
                    if (string.IsNullOrEmpty(newTitle))
                    {
                        newTitle = originalTitle; // 如果名称为空，保持原样
                    }
                    
                    Log($"[窗口标题] 将窗口 {hwnd.ToString("X8")} 的标题从 '{originalTitle}' 修改为 '{newTitle}'");
                    SetWindowText(hwnd, newTitle);
                }
            }
        }

        // 修改秒矿进度
        public static void MiaokuangJindu(List<Tuple<IntPtr, string>> hwndsNames, float miaokuangjindu = 0)
        {
            Log($"[秒矿进度] 开始应用秒矿进度设置，进度值: {miaokuangjindu}");
            if (hwndsNames == null)
            {
                Log("[秒矿进度] 参数无效，跳过");
                return;
            }

            Log($"[秒矿进度] 共有 {hwndsNames.Count} 个窗口需要应用秒矿进度设置");
            int windowIndex = 1;
            foreach (var item in hwndsNames)
            {
                IntPtr hwnd = item.Item1;
                string name = item.Item2;
                Log($"[秒矿进度] [{windowIndex}/{hwndsNames.Count}] 处理窗口: {name} (句柄: {hwnd.ToString("X8")})");

                uint? processId = GetProcessId(hwnd);
                if (!processId.HasValue)
                {
                    Log($"[秒矿进度] [{windowIndex}/{hwndsNames.Count}] 无法获取进程ID，跳过");
                    continue;
                }
                Log($"[秒矿进度] [{windowIndex}/{hwndsNames.Count}] 进程ID: {processId.Value}");

                IntPtr? moduleBase = GetModuleBaseAddress(processId.Value, "netcraft.exe");
                if (!moduleBase.HasValue)
                {
                    Log($"[秒矿进度] [{windowIndex}/{hwndsNames.Count}] 无法获取模块基址，跳过");
                    continue;
                }
                Log($"[秒矿进度] [{windowIndex}/{hwndsNames.Count}] 模块基址: {moduleBase.Value.ToString("X8")}");

                // 修改初始进度
                IntPtr address = moduleBase.Value + 0x4CF4B1 + 3;
                string bytesArrayMiaokuangjindu = FloatToBytes(miaokuangjindu);
                Log($"[秒矿进度] [{windowIndex}/{hwndsNames.Count}] 修改秒矿进度: 地址 {address.ToString("X8")} = {bytesArrayMiaokuangjindu}");
                WriteProcessMemoryWithBackup(processId.Value, address, bytesArrayMiaokuangjindu, 4);

                Log($"[秒矿进度] [{windowIndex}/{hwndsNames.Count}] 窗口 {name} 秒矿进度设置应用完成");
                windowIndex++;
            }
            Log("[秒矿进度] 所有窗口秒矿进度设置应用完成");
        }

        // 修改秒矿间隔
        public static void MiaokuangInterval(List<Tuple<IntPtr, string>> hwndsNames, bool enable = false)
        {
            Log($"[秒矿间隔] 开始应用秒矿间隔设置，状态: {(enable ? "开启" : "关闭")}");
            if (hwndsNames == null)
            {
                Log("[秒矿间隔] 参数无效，跳过");
                return;
            }

            Log($"[秒矿间隔] 共有 {hwndsNames.Count} 个窗口需要应用秒矿间隔设置");
            int windowIndex = 1;
            foreach (var item in hwndsNames)
            {
                IntPtr hwnd = item.Item1;
                string name = item.Item2;
                Log($"[秒矿间隔] [{windowIndex}/{hwndsNames.Count}] 处理窗口: {name} (句柄: {hwnd.ToString("X8")})");

                uint? processId = GetProcessId(hwnd);
                if (!processId.HasValue)
                {
                    Log($"[秒矿间隔] [{windowIndex}/{hwndsNames.Count}] 无法获取进程ID，跳过");
                    continue;
                }
                Log($"[秒矿间隔] [{windowIndex}/{hwndsNames.Count}] 进程ID: {processId.Value}");

                IntPtr? moduleBase = GetModuleBaseAddress(processId.Value, "netcraft.exe");
                if (!moduleBase.HasValue)
                {
                    Log($"[秒矿间隔] [{windowIndex}/{hwndsNames.Count}] 无法获取模块基址，跳过");
                    continue;
                }
                Log($"[秒矿间隔] [{windowIndex}/{hwndsNames.Count}] 模块基址: {moduleBase.Value.ToString("X8")}");

                // 修改秒矿间隔（禁止修改进度）
                IntPtr address = moduleBase.Value + 0x3921D8;
                if (enable)
                {
                    Log($"[秒矿间隔] [{windowIndex}/{hwndsNames.Count}] 开启秒矿间隔: 地址 {address.ToString("X8")} = 90 90 90 90");
                    WriteProcessMemoryWithBackup(processId.Value, address, "90 90 90 90", 4);
                }
                else
                {
                    Log($"[秒矿间隔] [{windowIndex}/{hwndsNames.Count}] 关闭秒矿间隔: 地址 {address.ToString("X8")} = F3 0F 11 00");
                    WriteProcessMemoryWithBackup(processId.Value, address, "F3 0F 11 00", 4);
                }

                // 修改秒矿间隔检查
                address = moduleBase.Value + 0x4CCB39;
                if (enable)
                {
                    Log($"[秒矿间隔] [{windowIndex}/{hwndsNames.Count}] 删除秒矿间隔检查: 地址 {address.ToString("X8")} = 90 90");
                    WriteProcessMemoryWithBackup(processId.Value, address, "90 90", 2);
                }
                else
                {
                    Log($"[秒矿间隔] [{windowIndex}/{hwndsNames.Count}] 恢复秒矿间隔检查: 地址 {address.ToString("X8")} = 72 54");
                    WriteProcessMemoryWithBackup(processId.Value, address, "72 54", 2);
                }

                Log($"[秒矿间隔] [{windowIndex}/{hwndsNames.Count}] 窗口 {name} 秒矿间隔设置应用完成");
                windowIndex++;
            }
            Log("[秒矿间隔] 所有窗口秒矿间隔设置应用完成");
        }

        // 修改快刀
        public static void Kuaidao(List<Tuple<IntPtr, string>> hwndsNames, bool enable = false)
        {
            Log($"[快刀设置] 开始应用快刀设置，状态: {(enable ? "开启" : "关闭")}");
            if (hwndsNames == null)
            {
                Log("[快刀设置] 参数无效，跳过");
                return;
            }

            Log($"[快刀设置] 共有 {hwndsNames.Count} 个窗口需要应用快刀设置");
            int windowIndex = 1;
            foreach (var item in hwndsNames)
            {
                IntPtr hwnd = item.Item1;
                string name = item.Item2;
                Log($"[快刀设置] [{windowIndex}/{hwndsNames.Count}] 处理窗口: {name} (句柄: {hwnd.ToString("X8")})");

                uint? processId = GetProcessId(hwnd);
                if (!processId.HasValue)
                {
                    Log($"[快刀设置] [{windowIndex}/{hwndsNames.Count}] 无法获取进程ID，跳过");
                    continue;
                }
                Log($"[快刀设置] [{windowIndex}/{hwndsNames.Count}] 进程ID: {processId.Value}");

                IntPtr? moduleBase = GetModuleBaseAddress(processId.Value, "netcraft.exe");
                if (!moduleBase.HasValue)
                {
                    Log($"[快刀设置] [{windowIndex}/{hwndsNames.Count}] 无法获取模块基址，跳过");
                    continue;
                }
                Log($"[快刀设置] [{windowIndex}/{hwndsNames.Count}] 模块基址: {moduleBase.Value.ToString("X8")}");

                // 修改快刀
                IntPtr address = moduleBase.Value + 0x4CF4B1 + 3; // 这里需要确认正确的偏移量
                if (enable)
                {
                    Log($"[快刀设置] [{windowIndex}/{hwndsNames.Count}] 开启快刀: 地址 {address.ToString("X8")} = 90 90 90");
                    WriteProcessMemoryWithBackup(processId.Value, address, "90 90 90", 3);
                }
                else
                {
                    Log($"[快刀设置] [{windowIndex}/{hwndsNames.Count}] 关闭快刀: 地址 {address.ToString("X8")} = D9 5E 08");
                    WriteProcessMemoryWithBackup(processId.Value, address, "D9 5E 08", 3);
                }

                Log($"[快刀设置] [{windowIndex}/{hwndsNames.Count}] 窗口 {name} 快刀设置应用完成");
                windowIndex++;
            }
            Log("[快刀设置] 所有窗口快刀设置应用完成");
        }

        // 修改秒上坐骑
        public static void Miaoshangzuoqi(List<Tuple<IntPtr, string>> hwndsNames, int shifouxiugai = 0)
        {
            Log($"[秒上坐骑设置] 开始应用秒上坐骑设置，状态: {(shifouxiugai == 1 ? "开启" : "关闭")}");
            if (hwndsNames == null || shifouxiugai == 0)
            {
                Log("[秒上坐骑设置] 参数无效，跳过");
                return;
            }

            Log($"[秒上坐骑设置] 共有 {hwndsNames.Count} 个窗口需要应用秒上坐骑设置");
            int windowIndex = 1;
            foreach (var item in hwndsNames)
            {
                IntPtr hwnd = item.Item1;
                string name = item.Item2;
                Log($"[秒上坐骑设置] [{windowIndex}/{hwndsNames.Count}] 处理窗口: {name} (句柄: {hwnd.ToString("X8")})");

                uint? processId = GetProcessId(hwnd);
                if (!processId.HasValue)
                {
                    Log($"[秒上坐骑设置] [{windowIndex}/{hwndsNames.Count}] 无法获取进程ID，跳过");
                    continue;
                }
                Log($"[秒上坐骑设置] [{windowIndex}/{hwndsNames.Count}] 进程ID: {processId.Value}");

                IntPtr? moduleBase = GetModuleBaseAddress(processId.Value, "netcraft.exe");
                if (!moduleBase.HasValue)
                {
                    Log($"[秒上坐骑设置] [{windowIndex}/{hwndsNames.Count}] 无法获取模块基址，跳过");
                    continue;
                }
                Log($"[秒上坐骑设置] [{windowIndex}/{hwndsNames.Count}] 模块基址: {moduleBase.Value.ToString("X8")}");

                // 修改初始进度
                IntPtr address = moduleBase.Value + 0x701299;
                Log($"[秒上坐骑设置] [{windowIndex}/{hwndsNames.Count}] 修改秒上坐骑: 地址 {address.ToString("X8")} = D0 07");
                WriteProcessMemoryWithBackup(processId.Value, address, "D0 07", 2);

                Log($"[秒上坐骑设置] [{windowIndex}/{hwndsNames.Count}] 窗口 {name} 秒上坐骑设置应用完成");
                windowIndex++;
            }
            Log("[秒上坐骑设置] 所有窗口秒上坐骑设置应用完成");
        }
    }

}

