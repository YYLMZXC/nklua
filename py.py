
import win32gui           # Windows GUI操作库，用于枚举窗口
import win32process       # Windows进程操作库，用于获取窗口对应的进程ID
import ctypes             # Python的C语言外部函数库，用于调用Windows API
from ctypes import wintypes  # Windows数据类型定义
import struct             # Python结构体库，用于二进制数据的打包和解包

# 加载Windows核心动态链接库kernel32.dll
# 该库包含进程和内存操作的核心API
kernel32 = ctypes.WinDLL('kernel32', use_last_error=True)

# 定义进程访问权限常量
# 这些常量用于指定打开进程时需要的权限级别
PROCESS_ALL_ACCESS = 0x1F0FFF          # 所有访问权限（最高权限）
PROCESS_VM_READ = 0x0010               # 虚拟内存读取权限
PROCESS_VM_WRITE = 0x0020              # 虚拟内存写入权限
PROCESS_VM_OPERATION = 0x0008         # 虚拟内存操作权限
PROCESS_QUERY_INFORMATION = 0x0400     # 查询进程信息权限

# 定义OpenProcess函数原型
# 功能：打开一个现有的本地进程对象，并返回进程的句柄
# 参数：访问权限、是否继承句柄、进程ID
OpenProcess = kernel32.OpenProcess
OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
OpenProcess.restype = wintypes.HANDLE

# 定义ReadProcessMemory函数原型
# 功能：从指定进程的内存区域读取数据
# 参数：进程句柄、要读取的内存地址、接收数据的缓冲区、要读取的字节数、实际读取的字节数
ReadProcessMemory = kernel32.ReadProcessMemory
ReadProcessMemory.argtypes = [wintypes.HANDLE, wintypes.LPCVOID, wintypes.LPVOID, ctypes.c_size_t, ctypes.POINTER(ctypes.c_size_t)]
ReadProcessMemory.restype = wintypes.BOOL

# 定义WriteProcessMemory函数原型
# 功能：向指定进程的内存区域写入数据
# 参数：进程句柄、要写入的内存地址、要写入的数据缓冲区、要写入的字节数、实际写入的字节数
WriteProcessMemory = kernel32.WriteProcessMemory
WriteProcessMemory.argtypes = [wintypes.HANDLE, wintypes.LPVOID, wintypes.LPCVOID, ctypes.c_size_t, ctypes.POINTER(ctypes.c_size_t)]
WriteProcessMemory.restype = wintypes.BOOL

# 定义CloseHandle函数原型
# 功能：关闭一个打开的对象句柄，释放系统资源
# 参数：要关闭的句柄
CloseHandle = kernel32.CloseHandle
CloseHandle.argtypes = [wintypes.HANDLE]
CloseHandle.restype = wintypes.BOOL

#获取指定窗口句柄对应的进程ID
def  get_process_id(hwnd):
    """
    获取指定窗口句柄对应的进程ID
    
    参数:
        hwnd: 窗口句柄
    
    返回:
        进程ID（整数），如果失败返回None
    """
    # 步骤1: 通过窗口句柄获取线程ID和进程ID
    thread_id, process_id = win32process.GetWindowThreadProcessId(hwnd)
    if not process_id:
        return None
    return process_id

#获取指定进程中指定模块的基址
def get_module_base_address(process_id, module_name):
    """
    获取指定进程中指定模块的基址
    
    参数:
        process_id: 进程ID
        module_name: 模块名称（如"netcraft.exe"）
    
    返回:
        模块基址（整数），如果失败返回None
    """
    # 步骤1: 使用OpenProcess打开目标进程
    # 需要PROCESS_QUERY_INFORMATION权限来查询进程信息
    # 需要PROCESS_VM_READ权限来读取进程内存
    h_process = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, False, process_id)
    if not h_process:
        return None
    
    try:
        # 步骤2: 加载psapi.dll库
        # 该库提供进程和模块操作的API
        psapi = ctypes.WinDLL('psapi', use_last_error=True)
        
        # 定义EnumProcessModules函数
        # 功能：枚举指定进程中加载的所有模块
        # 参数：进程句柄、接收模块句柄的数组、数组大小、实际需要的字节数
        EnumProcessModules = psapi.EnumProcessModules
        EnumProcessModules.argtypes = [wintypes.HANDLE, ctypes.POINTER(wintypes.HMODULE), ctypes.c_ulong, ctypes.POINTER(ctypes.c_ulong)]
        EnumProcessModules.restype = wintypes.BOOL
        
        # 定义GetModuleBaseNameA函数
        # 功能：获取指定模块的基址名称
        # 参数：进程句柄、模块句柄、接收名称的缓冲区、缓冲区大小
        GetModuleBaseNameA = psapi.GetModuleBaseNameA
        GetModuleBaseNameA.argtypes = [wintypes.HANDLE, wintypes.HMODULE, ctypes.c_char_p, ctypes.c_ulong]
        GetModuleBaseNameA.restype = ctypes.c_ulong
        
        # 步骤3: 创建模块句柄数组，最多存储1024个模块
        h_modules = (wintypes.HMODULE * 1024)()
        cb_needed = ctypes.c_ulong()
        
        # 步骤4: 枚举进程中的所有模块
        if EnumProcessModules(h_process, h_modules, ctypes.sizeof(h_modules), ctypes.byref(cb_needed)):
            # 计算实际枚举到的模块数量
            modules_count = cb_needed.value // ctypes.sizeof(wintypes.HMODULE)
            
            # 步骤5: 遍历所有模块，查找目标模块
            for i in range(modules_count):
                # 创建缓冲区来接收模块名称（最大260个字符）
                module_name_buffer = ctypes.create_string_buffer(260)
                
                # 获取当前模块的名称
                if GetModuleBaseNameA(h_process, h_modules[i], module_name_buffer, 260):
                    # 将模块名称转换为字符串并比较（不区分大小写）
                    if module_name_buffer.value.decode('utf-8').lower() == module_name.lower():
                        # 找到目标模块，返回其基址
                        return h_modules[i]
    finally:
        # 步骤6: 关闭进程句柄，释放系统资源
        CloseHandle(h_process)
    
    # 未找到目标模块，返回None
    return None

#从指定窗口的进程内存中读取字节数组数据
def huoquneicun(process_id, dizhi, zijieshuzuchangdu = 4):
    """
    从指定窗口的进程内存中读取字节数组数据
    
    参数:
        process_id: 进程ID
        dizhi: 要获取数据的内存地址（整数）
        zijieshuzuchangdu: 要获取的字节数组的长度
    
    返回:
        用空格分隔的16进制字符串（例如："00 00 33 3f"），如果失败返回None
    """
    try:
        # 打开进程，获取读取内存的权限
        h_process = OpenProcess(PROCESS_VM_READ, False, process_id)
        if not h_process:
            return None
        
        try:
            # 创建缓冲区来接收读取的数据
            buffer = ctypes.create_string_buffer(zijieshuzuchangdu)
            
            # 创建变量来接收实际读取的字节数
            bytes_read = ctypes.c_size_t()
            
            # 调用ReadProcessMemory读取内存
            if ReadProcessMemory(h_process, dizhi, buffer, zijieshuzuchangdu, ctypes.byref(bytes_read)):
                # 读取成功，将字节数据转换为空格分隔的16进制字符串
                byte_data = buffer.raw[:bytes_read.value]
                hex_string = " ".join(f"{byte:02x}" for byte in byte_data)
                return hex_string
            
            # 读取失败，返回None
            return None
        finally:
            # 关闭进程句柄，释放系统资源
            CloseHandle(h_process)
    except:
        # 发生异常，返回None
        return None

#对字节数组切片 
def slice_bytes(bytes_array, start, end):
    """
    对字节数组进行切片操作
    
    参数:
        bytes_array: 字节数组（例如：b"00 00 00 33"）
        start: 切片开始索引（包含）
        end: 切片结束索引（不包含）
    
    返回:
        切片后的字节数组（例如：b"00 00 33"）
    """
    # 步骤1: 用空格分隔字符串，得到字节列表
    byte_list = bytes_array.split()
    
    # 步骤2: 切片操作
    sliced_list = byte_list[start:end]
    
    # 步骤3: 用空格拼接起来
    return " ".join(sliced_list)


#把字节数组转换为整数
def bytes_to_int(bytes_array):
    """
    将用空格分隔的字节字符串转换为16进制整数（倒序拼接）
    
    参数:
        bytes_array: 用空格分隔的字节字符串（例如："00 00 00 33"）
    
    返回:
        16进制整数（例如：0x33000000）
    """
    # 步骤1: 用空格分隔字符串，得到字节列表
    byte_list = bytes_array.split()
    
    # 步骤2: 倒序列表
    byte_list_reversed = byte_list[::-1]
    
    # 步骤3: 拼接起来
    hex_string = "".join(byte_list_reversed)
    
    # 步骤4: 转换为整数
    return int(hex_string, 16)

#把整数转换为字节数组
def hex_to_bytes(integer_value):
    """
    将整数转换为空格分隔的字节数组字符串（小端序）
    
    参数:
        integer_value: 整数值（例如：0x33000000）
    
    返回:
        用空格分隔的字节字符串（例如："00 00 00 33"）
    """
    # 步骤1: 将整数打包为4字节的小端序字节数组
    byte_data = struct.pack('<I', integer_value)
    
    # 步骤2: 将每个字节转换为2位16进制字符串
    hex_list = [f"{byte:02x}" for byte in byte_data]
    
    # 步骤3: 用空格拼接起来
    return " ".join(hex_list)

#把整数先转化为16进制 然后输出为字符串
def int_to_hex_string(integer_value):
    """
    将整数转换为8位16进制字符串（小端序）
    
    参数:
        integer_value: 整数值（例如：0x33000000）
    
    返回:
        8位16进制字符串（例如："33000000"）
    """
    # 步骤1: 将整数转换为16进制字符串
    hex_string = f"{integer_value:08X}"
    
    # 步骤2: 转换为小端序（倒序）
    return hex_string[6:8] + hex_string[4:6] + hex_string[2:4] + hex_string[0:2]



#把单浮点数转换为字节数组
def float_to_bytes(float_value):
    """
    将单浮点数转换为空格分隔的字节数组字符串（小端序）
    
    参数:
        float_value: 单浮点数值（例如：3.1415926）
    
    返回:
        用空格分隔的字节字符串（例如："db 0f 49 40"）
    """
    # 步骤1: 将浮点数打包为4字节的小端序字节数组
    byte_data = struct.pack('<f', float_value)
    
    # 步骤2: 将每个字节转换为2位16进制字符串
    hex_list = [f"{byte:02x}" for byte in byte_data]
    
    # 步骤3: 用空格拼接起来
    return " ".join(hex_list)

#把字节数组转换为单浮点数
def bytes_to_float(bytes_array):
    """
    将用空格分隔的字节字符串转换为单浮点数（小端序）
    
    参数:
        bytes_array: 用空格分隔的字节字符串（例如："db 0f 49 40"）
    
    返回:
        单浮点数值（例如：3.1415926）
    """
    # 步骤1: 用空格分隔字符串，得到字节列表
    byte_list = bytes_array.split()
    
    # 步骤2: 将每个字节字符串转换为整数
    byte_values = [int(byte, 16) for byte in byte_list]
    
    # 步骤3: 将整数列表转换为字节数据
    byte_data = bytes(byte_values)
    
    # 步骤4: 解包为单浮点数（小端序）
    return struct.unpack('<f', byte_data)[0]

#把字节数组转换为UTF8字符串
def bytes_to_utf8_string(bytes_array):
    """
    将用空格分隔的字节字符串转换为UTF8字符串
    
    参数:
        bytes_array: 用空格分隔的字节字符串（例如："e4 bd a0 e5 a5 bd"）
    
    返回:
        UTF8字符串（例如："你好"）
    """
    # 步骤1: 用空格分隔字符串，得到字节列表
    byte_list = bytes_array.split()
    
    # 步骤2: 将每个字节字符串转换为整数
    byte_values = [int(byte, 16) for byte in byte_list]
    
    # 步骤3: 将整数列表转换为字节数据
    byte_data = bytes(byte_values)
    
    # 步骤4: 查找字符串结束符（null终止符\x00）
    null_pos = byte_data.find(b'\x00')
    if null_pos != -1:
        # 截取字符串结束符之前的数据
        byte_data = byte_data[:null_pos]
    
    # 步骤5: 尝试使用UTF-8编码解码字符串
    try:
        return byte_data.decode('utf-8')
    except UnicodeDecodeError:
        # UTF-8解码失败，尝试使用GBK编码（中文常用编码）
        try:
            return byte_data.decode('gbk')
        except UnicodeDecodeError:
            # GBK也失败，使用latin-1编码并忽略错误
            return byte_data.decode('latin-1', errors='ignore')

#把UTF8字符串转换为字节数组
def utf8_string_to_bytes(string_value):
    """
    将UTF8字符串转换为空格分隔的字节数组字符串
    
    参数:
        string_value: UTF8字符串（例如："你好"）
    
    返回:
        用空格分隔的字节字符串（例如："e4 bd a0 e5 a5 bd"）
    """
    # 步骤1: 将字符串编码为UTF-8字节数据
    byte_data = string_value.encode('utf-8')
    
    # 步骤2: 将每个字节转换为2位16进制字符串
    hex_list = [f"{byte:02x}" for byte in byte_data]
    
    # 步骤3: 用空格拼接起来
    return " ".join(hex_list)

#向指定进程的指定地址写入字节数组
def xieruneicun(process_id, dizhi, bytes_array):
    """
    向指定进程的指定地址写入字节数组
    
    参数:
        process_id: 进程ID
        dizhi: 要写入数据的内存地址（整数）
        bytes_array: 用空格分隔的字节字符串（例如："00 00 00 33"）
    
    返回:
        写入成功返回True，失败返回False
    """
    try:
        # 步骤1: 打开进程，获取写入内存的权限
        h_process = OpenProcess(PROCESS_VM_WRITE | PROCESS_VM_OPERATION, False, process_id)
        if not h_process:
            return False
        
        try:
            # 步骤2: 将空格分隔的字节字符串转换为字节数据
            byte_list = bytes_array.split()
            byte_data = bytes(int(byte, 16) for byte in byte_list)
            
            # 步骤3: 创建变量来接收实际写入的字节数
            bytes_written = ctypes.c_size_t()
            
            # 步骤4: 调用WriteProcessMemory写入内存
            if WriteProcessMemory(h_process, dizhi, byte_data, len(byte_data), ctypes.byref(bytes_written)):
                # 写入成功，返回True
                return True
            
            # 写入失败，返回False
            return False
        finally:
            # 步骤5: 关闭进程句柄，释放系统资源
            CloseHandle(h_process)
    except:
        # 发生异常，返回False
        return False

#定义函数 获取窗口的人物偏转角地址
def get_person_rotation_address(hwnd):
    """
    获取窗口的人物偏转角地址
    
    参数:
        hwnd: 窗口句柄
    
    返回:
        人物偏转角地址（整数）
    """
    #获取进程ID
    process_id = get_process_id(hwnd)

    #获取模块基址
    dizhi = get_module_base_address(process_id, "netcraft.exe")


    #计算指针值
    dizhi = dizhi + 0x1ACB4D0
    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, dizhi,4)
    #将字节数组转换为整数
    int_value = bytes_to_int(bytes_array)

    #计算指针
    dihzi = int_value + 0x12A4
    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, dihzi,4)
    #将字节数组转换为单精度浮点数
    float_value = bytes_to_float(bytes_array)
    #打印字符串
    print(f"人物偏转角地址: {dihzi}")
    print(f"人物偏转角值: {float_value}")
    return dihzi

#定义函数 获取人物名称
def get_person_name(hwnd):
    """
    获取窗口的人物名称
    
    参数:
        hwnd: 窗口句柄
    
    返回:
        人物名称（字符串）
    """
    #获取进程ID
    process_id = get_process_id(hwnd)
    #获取模块基址
    int_value = get_module_base_address(process_id, "netcraft.exe")

    #计算指针值
    int_value = int_value + 0x0191D564
    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, int_value,4)
    #将字节数组转换为整数
    int_value = bytes_to_int(bytes_array)
    
    #计算指针值
    int_value = int_value + 0x1DC
    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, int_value,4)
    #将字节数组转换为整数
    int_value = bytes_to_int(bytes_array)

    #计算指针值
    int_value = int_value + 0xC
    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, int_value,4)
    #将字节数组转换为整数
    int_value = bytes_to_int(bytes_array)

    #计算指针值
    int_value = int_value + 0x160

    #读取指针值指向的内存 16字节
    bytes_array = huoquneicun(process_id, int_value,16) 
    #将字节数组转换为UTF-8字符串
    utf8_string = bytes_to_utf8_string(bytes_array)

    return utf8_string

#枚举所有奶块窗口
def enum_milk_windows():
    def enum_windows_callback(hwnd, results):
            
        if win32gui.IsWindowVisible(hwnd):# 检查窗口是否可见

            cls = win32gui.GetClassName(hwnd)#获取类名

            if cls=="CIrrDeviceWin32":#如果类名和奶块的类名相同
                
                title = win32gui.GetWindowText(hwnd)# 获取窗口标题
                
                if title:# 只添加有标题的窗口（排除一些系统窗口）
                    # 获取窗口的进程ID
                    process_id = get_process_id(hwnd)
                    #获取人物名称
                    name = get_person_name(hwnd)
                    results.append((hwnd,name, title, process_id))#把窗口句柄、标题、进程ID添加到列表中
        
    # 枚举所有奶块窗口
    windows = []
    win32gui.EnumWindows(enum_windows_callback, windows)
    return windows

#枚举所有奶块窗口输出
def enum_milk_windows1():
    def enum_windows_callback(hwnd, results):
            
        if win32gui.IsWindowVisible(hwnd):# 检查窗口是否可见

            cls = win32gui.GetClassName(hwnd)#获取类名

            if cls=="CIrrDeviceWin32":#如果类名和奶块的类名相同
                
                title = win32gui.GetWindowText(hwnd)# 获取窗口标题
                
                if title:# 只添加有标题的窗口（排除一些系统窗口）
                    # 获取窗口的进程ID
                    process_id = get_process_id(hwnd)
                    #获取人物名称
                    name = get_person_name(hwnd)
                    results.append((hwnd,name, title, process_id))#把窗口句柄、标题、进程ID添加到列表中
        
    # 枚举所有奶块窗口
    windows = []
    win32gui.EnumWindows(enum_windows_callback, windows)
    print("="*60)
    print(f"{'窗口句柄':<8}\t{'人物名称':<10}\t{'窗口标题':<15}\t{'进程ID'}")
    for hwnd, name, title, process_id in windows:
        print(f"{hwnd:<8}\t{name:<10}\t{title:<15}\t{process_id}")
    print("="*60)
    return 

#选中人物
def xuanzhongrenwu(name = "" ):
    """
    name: 字符串，输入的人物名称 空字符代表修改所有窗口
    
    """
    all = enum_milk_windows()
    hwnds = [i[0] for i in all]
    names = [i[1] for i in all]
    #判断name是否为空
    if name != "":
        #如果不为空，先判断是否在人物列表中
        if name not in names:
            print('查无此人')
            return None
        else:
            #如果在人物列表中，再排除其他人物名字
            #获取人物名称在names列表中的索引
            index = names.index(name)
            #根据索引排除其他人物句柄和名称
            hwnds = [hwnds[index]]
            names = [names[index]]
    #把hwnds列表和names列表合并为一个列表
    hwnds_names = list(zip(hwnds, names))
    return hwnds_names

#修改窗口标题
def set_window_title(hwnd, new_title):
    """
    修改指定窗口的标题
    
    参数:
        hwnd: 窗口句柄
        new_title: 新的窗口标题（字符串）
    
    返回:
        修改成功返回True，失败返回False
    """
    try:
        # 调用win32gui.SetWindowText设置窗口标题
        result = win32gui.SetWindowText(hwnd, new_title)
        return result != 0
    except:
        # 发生异常，返回False
        return False

#修改奶块窗口标题
def set_milk_window_title():
    hwnds_names = xuanzhongrenwu("")

    if hwnds_names == None:
        return
    
    bianhao = 0
    #遍历hwnds_names列表
    for hwnd,name in hwnds_names:
        
        #第一步 判断人物名称是否为空字符
        if name == "":#如果是空字符 命名为未知人物加编号
            bianhao += 1
            new_title = f"未知人物{bianhao}"
        else:
            new_title = name
        #修改窗口标题
        win32gui.SetWindowText(hwnd, new_title)

#修改秒矿
def miaokuang(hwnds_names, miaokuangjindu = 0):
    """
    hwnds_names: 列表，每个元素是一个元组，包含窗口句柄和人物名称
    miaokuangjindu: 浮点数，输入的修改进度 如果是0代表复原
    """
    if hwnds_names == None:
        return
    #遍历hwnds_names列表
    for hwnd,name in hwnds_names:

        #获取进程ID
        process_id = get_process_id(hwnd)
        #获取模块基址
        int_value_jizhi = get_module_base_address(process_id, "netcraft.exe")

        #修改初始进度======================================
        #计算指针值
        int_value = int_value_jizhi + 0x4CF4B1 + 3

        #修改内容为 miaokuangjindu
        bytes_array_miaokuangjindu = float_to_bytes(miaokuangjindu)
        xieruneicun(process_id,int_value, bytes_array_miaokuangjindu)

        #修改秒矿间隔=======================================
        #计算指针值
        int_value = int_value_jizhi + 0x3921D8

        if miaokuangjindu == 0:
            #复原改"F3 0F 11 00"
            xieruneicun(process_id,int_value,"F3 0F 11 00")
        else:
            #修改内容为 "90 90 90 90" 
            xieruneicun(process_id,int_value,"90 90 90 90")

        #删除秒矿间隔
        int_value = int_value_jizhi + 0x4CCB39

        if miaokuangjindu == 0:
            #复原改"72 4B"
            xieruneicun(process_id,int_value, "72 54")
        else:
            #修改内容为 "90 90" 
            xieruneicun(process_id,int_value, "90 90")
        if miaokuangjindu == 0:
            print(f"《{name}》秒矿已关闭")
        else:
            print(f"《{name}》秒矿已开启：{miaokuangjindu}")


#修改秒上坐骑
def miaoshangzuoqi(hwnds_names, shifouxiugai = 0):
    """
    hwnds_names: 列表，每个元素是一个元组，包含窗口句柄和人物名称
    shiofuxiugai: 整数 1代表修改 0代表复原
    """
    if hwnds_names == None:
        return
    #遍历hwnds_names列表
    for hwnd,name in hwnds_names:

        #获取进程ID
        process_id = get_process_id(hwnd)
        #获取模块基址
        int_value_jizhi = get_module_base_address(process_id, "netcraft.exe")

        #修改初始进度======================================
        #计算指针值
        int_value = int_value_jizhi + 0x701299
        if shifouxiugai:
            #修改内容为 "D0 07"
            xieruneicun(process_id,int_value, "D0 07")
            print(f"《{name}》秒上坐骑已开启")
        else:
            #复原改"E8 03"
            xieruneicun(process_id,int_value, "E8 03")
            print(f"《{name}》秒上坐骑已关闭")
        
        

#计算视角地址
def sjdizhi():
    #获取模块基址
    int_value_jizhi = get_module_base_address(process_id, "node.dll")
    #计算指针值
    int_value = int_value_jizhi + 0x020C1FA0



    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, int_value,4)
    #将字节数组转换为整数
    int_value = bytes_to_int(bytes_array)  

    #计算指针值
    int_value = int_value + 0x350 -4

    return int_value

# pianzhuanjiaodizhi = sjdizhi()
# fuyangjiaodizhi = pianzhuanjiaodizhi + 4

#修改视角
def sj(pianzhuanjiao = 0.0, fuyangjiao = 0.0):
    """
    pianzhuanjiao,fuyangjiao:浮点数，输入的修改视角 
    """
    #修改视角
    bytes_array_pianzhuanjiao = float_to_bytes(pianzhuanjiao)
    xieruneicun(process_id,pianzhuanjiaodizhi, bytes_array_pianzhuanjiao)
    bytes_array_fuyangjiao = float_to_bytes(fuyangjiao)
    xieruneicun(process_id,fuyangjiaodizhi, bytes_array_fuyangjiao)

#计算坐标地址
def zuobiaodizhi():
    #获取模块基址
    int_value_jizhi = get_module_base_address(process_id, "netcraft.exe")
    #计算指针值
    int_value = int_value_jizhi + 0x01922AD8


    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, int_value,4)
    #将字节数组转换为整数
    int_value = bytes_to_int(bytes_array)  
    #计算指针值
    int_value = int_value + 0x0

    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, int_value,4)
    #将字节数组转换为整数
    int_value = bytes_to_int(bytes_array) 
    #计算指针值
    int_value = int_value + 0x300

    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, int_value,4)
    #将字节数组转换为整数
    int_value = bytes_to_int(bytes_array)
    #计算指针值
    int_value = int_value + 0x2B8

    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, int_value,4)
    #将字节数组转换为整数
    int_value = bytes_to_int(bytes_array)  
    #计算指针值
    int_value = int_value + 0x0

    return int_value

# #计算坐标地址
# xdizhi = zuobiaodizhi()
# zdizhi = xdizhi + 4
# ydizhi = zdizhi + 4

#获取坐标
def zuobiao():

    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, xdizhi,4)
    #将字节数组转换为浮点
    x = bytes_to_float(bytes_array)  

    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, zdizhi,4)
    #将字节数组转换为浮点数
    z = bytes_to_float(bytes_array)  

    #读取指针值指向的内存 4字节
    bytes_array = huoquneicun(process_id, ydizhi,4)
    y = bytes_to_float(bytes_array)  

    return x,z,y

#关闭物品描述
def guanbiwupinzhishibiao():
    #获取模块基址
    int_value_jizhi = get_module_base_address(process_id, "netcraft.exe")
    #计算指针值
    int_value = int_value_jizhi + 0x6EA48F

    #修改内容为 "90 90"
    xieruneicun(process_id,int_value, "90 90 90 90 90")

#枚举所有奶块窗口
enum_milk_windows1()

#修改奶块窗口标题
set_milk_window_title()

print("请输入人物名称,不输入代表所有人物：")
renwu = input("请输入：")
#选择人物
hwnds_names = xuanzhongrenwu(renwu)




#秒矿代码
print("请输入秒矿进度，收菜建议0.7,全挖建议10")
miaokuangjindu = input("请输入：")
if miaokuangjindu == "":
    miaokuangjindu = 0
else:
    miaokuangjindu = float(miaokuangjindu)
#修改秒矿进度
miaokuang(hwnds_names,miaokuangjindu)   

#秒上坐骑代码
print("请输入是否开启秒上坐骑")
print("1为开启（0或不输入代表复原）")
shifouxiugai = input("请输入：")
if shifouxiugai == "":
    shifouxiugai = 0
else:
    shifouxiugai = int(shifouxiugai)
#修改秒上坐骑
miaoshangzuoqi(hwnds_names,shifouxiugai)





