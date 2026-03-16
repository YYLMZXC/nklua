using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace MemoryHelper
{
    public class MainScreen : Form
    {
        private List<Tuple<IntPtr, string>> selectedWindows = new List<Tuple<IntPtr, string>>();
        private List<MemoryTools.WindowInfo> allWindows = new List<MemoryTools.WindowInfo>();

        public MainScreen()
        {
            // 订阅日志事件
            MemoryTools.OnLog += MemoryTools_OnLog;
            // 订阅窗口关闭事件
            this.FormClosing += MainScreen_FormClosing;
            InitializeUI();
        }

        private void MainScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            AddOutput("程序正在退出，正在还原所有修改...");
            
            // 还原所有内存修改
            if (selectedWindows != null && selectedWindows.Count > 0)
            {
                MemoryTools.RestoreAllMemory(selectedWindows);
                AddOutput("所有修改已还原，程序即将退出");
            }
            else
            {
                AddOutput("没有选中窗口，无需还原，程序即将退出");
            }
        }

        private void MemoryTools_OnLog(string message)
        {
            // 确保在UI线程上更新
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AddOutput(message)));
            }
            else
            {
                AddOutput(message);
            }
        }

        private void InitializeUI()
        {
            this.Text = "梦幻社区";
            this.Size = new System.Drawing.Size(1200, 800); // 大幅增加窗体大小
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable; // 允许调整大小
            this.MaximizeBox = true; // 允许最大化
            this.MinimumSize = new System.Drawing.Size(800, 550); // 设置最小尺寸

            // 创建UI元素
            CreateControls();

            // 枚举窗口并填充列表
            EnumerateWindows();
        }

        private void CreateControls()
        {
            // 创建主布局面板
            TableLayoutPanel mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 2;
            mainPanel.RowCount = 3;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35)); // 窗口列表占35%
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65)); // 日志界面占65%
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70)); // 主界面区域70%
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20)); // 设置区域20%
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10)); // 底部预留10%
            this.Controls.Add(mainPanel);

            // 左侧面板 - 使用嵌套的TableLayoutPanel
            TableLayoutPanel leftTablePanel = new TableLayoutPanel();
            leftTablePanel.Dock = DockStyle.Fill;
            leftTablePanel.ColumnCount = 1;
            leftTablePanel.RowCount = 2;
            leftTablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            leftTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftTablePanel.Padding = new Padding(20);
            mainPanel.Controls.Add(leftTablePanel, 0, 0);

            // 窗口列表标签
            Label windowListLabel = new Label();
            windowListLabel.Text = "奶块窗口列表:";
            windowListLabel.Dock = DockStyle.Fill;
            windowListLabel.TextAlign = ContentAlignment.BottomLeft;
            leftTablePanel.Controls.Add(windowListLabel, 0, 0);

            // 窗口列表
            ListBox windowListBox = new ListBox();
            windowListBox.Name = "windowListBox";
            windowListBox.Dock = DockStyle.Fill;
            windowListBox.Margin = new Padding(0, 5, 0, 0);
            leftTablePanel.Controls.Add(windowListBox, 0, 1);

            // 右侧面板 - 使用嵌套的TableLayoutPanel
            TableLayoutPanel rightTablePanel = new TableLayoutPanel();
            rightTablePanel.Dock = DockStyle.Fill;
            rightTablePanel.ColumnCount = 1;
            rightTablePanel.RowCount = 4;
            rightTablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            rightTablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            rightTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rightTablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            rightTablePanel.Padding = new Padding(20);
            mainPanel.Controls.Add(rightTablePanel, 1, 0);

            // 刷新按钮
            Button refreshButton = new Button();
            refreshButton.Text = "刷新窗口";
            refreshButton.Dock = DockStyle.Fill;
            refreshButton.Margin = new Padding(0, 0, 0, 5);
            refreshButton.Click += RefreshButton_Click;
            rightTablePanel.Controls.Add(refreshButton, 0, 0);

            // 选择按钮
            Button selectButton = new Button();
            selectButton.Text = "选择窗口";
            selectButton.Dock = DockStyle.Fill;
            selectButton.Margin = new Padding(0, 0, 0, 5);
            selectButton.Click += SelectButton_Click;
            rightTablePanel.Controls.Add(selectButton, 0, 1);

            // 输出文本框
            TextBox outputTextBox = new TextBox();
            outputTextBox.Name = "outputTextBox";
            outputTextBox.Dock = DockStyle.Fill;
            outputTextBox.Multiline = true;
            outputTextBox.ReadOnly = true;
            outputTextBox.ScrollBars = ScrollBars.Vertical;
            outputTextBox.Margin = new Padding(0, 0, 0, 5);
            outputTextBox.MaxLength = 0; // 0表示无限制
            outputTextBox.WordWrap = false; // 不自动换行
            rightTablePanel.Controls.Add(outputTextBox, 0, 2);

            // 清空输出按钮
            Button clearOutputButton = new Button();
            clearOutputButton.Text = "清空输出";
            clearOutputButton.Dock = DockStyle.Fill;
            clearOutputButton.Click += ClearOutputButton_Click;
            rightTablePanel.Controls.Add(clearOutputButton, 0, 3);

            // 底部面板 - 使用嵌套的TableLayoutPanel
            TableLayoutPanel bottomTablePanel = new TableLayoutPanel();
            bottomTablePanel.Dock = DockStyle.Fill;
            bottomTablePanel.ColumnCount = 5;
            bottomTablePanel.RowCount = 3;
            bottomTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            bottomTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            bottomTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            bottomTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            bottomTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottomTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            bottomTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            bottomTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
            bottomTablePanel.Padding = new Padding(20);
            mainPanel.Controls.Add(bottomTablePanel, 0, 1);
            mainPanel.SetColumnSpan(bottomTablePanel, 2);

            // 第一行：秒矿设置
            // 秒矿进度标签
            Label miaokuangJinduLabel = new Label();
            miaokuangJinduLabel.Text = "秒矿进度:";
            miaokuangJinduLabel.Dock = DockStyle.Fill;
            miaokuangJinduLabel.TextAlign = ContentAlignment.MiddleLeft;
            bottomTablePanel.Controls.Add(miaokuangJinduLabel, 0, 0);

            // 秒矿进度输入
            TextBox miaokuangInput = new TextBox();
            miaokuangInput.Name = "miaokuangInput";
            miaokuangInput.Dock = DockStyle.Fill;
            miaokuangInput.Text = "0.7";
            miaokuangInput.Margin = new Padding(5, 5, 5, 5);
            bottomTablePanel.Controls.Add(miaokuangInput, 1, 0);

            // 应用秒矿进度按钮
            Button miaokuangJinduButton = new Button();
            miaokuangJinduButton.Text = "应用秒矿进度";
            miaokuangJinduButton.Dock = DockStyle.Fill;
            miaokuangJinduButton.Margin = new Padding(5, 5, 5, 5);
            miaokuangJinduButton.Click += MiaokuangJinduButton_Click;
            bottomTablePanel.Controls.Add(miaokuangJinduButton, 2, 0);

            // 秒矿间隔复选框
            CheckBox miaokuangIntervalCheckBox = new CheckBox();
            miaokuangIntervalCheckBox.Name = "miaokuangIntervalCheckBox";
            miaokuangIntervalCheckBox.Text = "开启秒矿间隔";
            miaokuangIntervalCheckBox.Dock = DockStyle.Fill;
            miaokuangIntervalCheckBox.TextAlign = ContentAlignment.MiddleLeft;
            miaokuangIntervalCheckBox.Margin = new Padding(5, 5, 5, 5);
            bottomTablePanel.Controls.Add(miaokuangIntervalCheckBox, 3, 0);

            // 应用秒矿间隔按钮
            Button miaokuangIntervalButton = new Button();
            miaokuangIntervalButton.Text = "应用秒矿间隔";
            miaokuangIntervalButton.Dock = DockStyle.Fill;
            miaokuangIntervalButton.Margin = new Padding(5, 5, 5, 5);
            miaokuangIntervalButton.Click += MiaokuangIntervalButton_Click;
            bottomTablePanel.Controls.Add(miaokuangIntervalButton, 4, 0);

            // 第二行：快刀和坐骑设置
            // 快刀复选框
            CheckBox kuaidaoCheckBox = new CheckBox();
            kuaidaoCheckBox.Name = "kuaidaoCheckBox";
            kuaidaoCheckBox.Text = "开启快刀";
            kuaidaoCheckBox.Dock = DockStyle.Fill;
            kuaidaoCheckBox.TextAlign = ContentAlignment.MiddleLeft;
            kuaidaoCheckBox.Margin = new Padding(0, 5, 5, 5);
            bottomTablePanel.Controls.Add(kuaidaoCheckBox, 0, 1);

            // 应用快刀按钮
            Button kuaidaoButton = new Button();
            kuaidaoButton.Text = "应用快刀";
            kuaidaoButton.Dock = DockStyle.Fill;
            kuaidaoButton.Margin = new Padding(5, 5, 5, 5);
            kuaidaoButton.Click += KuaidaoButton_Click;
            bottomTablePanel.Controls.Add(kuaidaoButton, 1, 1);

            // 秒上坐骑复选框
            CheckBox zuoqiCheckBox = new CheckBox();
            zuoqiCheckBox.Name = "zuoqiCheckBox";
            zuoqiCheckBox.Text = "开启秒上坐骑";
            zuoqiCheckBox.Dock = DockStyle.Fill;
            zuoqiCheckBox.TextAlign = ContentAlignment.MiddleLeft;
            zuoqiCheckBox.Margin = new Padding(5, 5, 5, 5);
            bottomTablePanel.Controls.Add(zuoqiCheckBox, 2, 1);

            // 应用坐骑按钮
            Button zuoqiButton = new Button();
            zuoqiButton.Text = "应用坐骑设置";
            zuoqiButton.Dock = DockStyle.Fill;
            zuoqiButton.Margin = new Padding(5, 5, 5, 5);
            zuoqiButton.Click += ZuoqiButton_Click;
            bottomTablePanel.Controls.Add(zuoqiButton, 3, 1);

            // 还原按钮
            Button restoreButton = new Button();
            restoreButton.Text = "还原所有设置";
            restoreButton.Dock = DockStyle.Fill;
            restoreButton.Margin = new Padding(5, 5, 0, 5);
            restoreButton.Click += RestoreButton_Click;
            bottomTablePanel.Controls.Add(restoreButton, 4, 1);

            // 第三行：视角锁定设置
            // 视角锁定复选框
            CheckBox shijiaoCheckBox = new CheckBox();
            shijiaoCheckBox.Name = "shijiaoCheckBox";
            shijiaoCheckBox.Text = "开启视角锁定";
            shijiaoCheckBox.Dock = DockStyle.Fill;
            shijiaoCheckBox.TextAlign = ContentAlignment.MiddleLeft;
            shijiaoCheckBox.Margin = new Padding(0, 5, 5, 5);
            bottomTablePanel.Controls.Add(shijiaoCheckBox, 0, 2);

            // 视角锁定值输入
            TextBox shijiaoInput = new TextBox();
            shijiaoInput.Name = "shijiaoInput";
            shijiaoInput.Dock = DockStyle.Fill;
            shijiaoInput.Text = "89.5";
            shijiaoInput.Margin = new Padding(5, 5, 5, 5);
            bottomTablePanel.Controls.Add(shijiaoInput, 1, 2);

            // 应用视角锁定按钮
            Button shijiaoButton = new Button();
            shijiaoButton.Text = "应用视角锁定";
            shijiaoButton.Dock = DockStyle.Fill;
            shijiaoButton.Margin = new Padding(5, 5, 5, 5);
            shijiaoButton.Click += ShijiaoButton_Click;
            bottomTablePanel.Controls.Add(shijiaoButton, 2, 2);
            bottomTablePanel.SetColumnSpan(shijiaoButton, 3);
        }

        private void EnumerateWindows()
        {
            allWindows = MemoryTools.EnumMilkWindows();
            ListBox windowListBox = (ListBox)this.Controls.Find("windowListBox", true)[0];
            windowListBox.Items.Clear();

            foreach (var window in allWindows)
            {
                windowListBox.Items.Add($"{window.Name} - {window.Title} (PID: {window.ProcessId})");
            }

            AddOutput($"已枚举到 {allWindows.Count} 个奶块窗口");
        }

        private void AddOutput(string message)
        {
            TextBox outputTextBox = (TextBox)this.Controls.Find("outputTextBox", true)[0];
            // 如果消息已经包含时间戳，就不再添加
            if (!message.StartsWith("["))
            {
                outputTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}\r\n");
            }
            else
            {
                outputTextBox.AppendText(message + "\r\n");
            }
            // 滚动到底部
            outputTextBox.SelectionStart = outputTextBox.Text.Length;
            outputTextBox.ScrollToCaret();
        }

        private void AddDetailedLog(string prefix, string message)
        {
            AddOutput($"[{prefix}] {message}");
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            AddOutput("正在刷新窗口列表...");
            EnumerateWindows();
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            ListBox windowListBox = (ListBox)this.Controls.Find("windowListBox", true)[0];
            int selectedIndex = windowListBox.SelectedIndex;

            // 计算输入框的居中位置
            int inputBoxWidth = 300;
            int inputBoxHeight = 150;
            int xPos = this.Left + (this.Width - inputBoxWidth) / 2;
            int yPos = this.Top + (this.Height - inputBoxHeight) / 2;

            if (selectedIndex >= 0 && selectedIndex < allWindows.Count)
            {
                var selectedWindow = allWindows[selectedIndex];
                string playerName = selectedWindow.Name;
                
                // 如果名称为空，让用户输入
                if (string.IsNullOrEmpty(playerName))
                {
                    string inputName = Microsoft.VisualBasic.Interaction.InputBox(
                        "请输入人物名称:", 
                        "输入人物名称", 
                        "", 
                        xPos, 
                        yPos);
                    
                    if (!string.IsNullOrEmpty(inputName))
                    {
                        playerName = inputName;
                        AddOutput($"已为窗口 {selectedWindow.Title} 设置名称: {playerName}");
                    }
                    else
                    {
                        AddOutput("取消输入，使用默认名称");
                    }
                }
                
                selectedWindows = new List<Tuple<IntPtr, string>>();
                selectedWindows.Add(Tuple.Create(selectedWindow.Hwnd, playerName));
                AddOutput($"已选择窗口: {playerName} - {selectedWindow.Title}");
            }
            else
            {
                // 选择所有窗口
                selectedWindows = new List<Tuple<IntPtr, string>>();
                foreach (var window in allWindows)
                {
                    string playerName = window.Name;
                    
                    // 如果名称为空，让用户输入
                    if (string.IsNullOrEmpty(playerName))
                    {
                        string inputName = Microsoft.VisualBasic.Interaction.InputBox(
                            $"请输入窗口 {window.Title} 的人物名称:", 
                            "输入人物名称", 
                            "", 
                            xPos, 
                            yPos);
                        
                        if (!string.IsNullOrEmpty(inputName))
                        {
                            playerName = inputName;
                            AddOutput($"已为窗口 {window.Title} 设置名称: {playerName}");
                        }
                        else
                        {
                            AddOutput($"取消输入，窗口 {window.Title} 使用默认名称");
                        }
                    }
                    
                    selectedWindows.Add(Tuple.Create(window.Hwnd, playerName));
                }
                AddOutput("已选择所有窗口");
            }

            // 修改窗口标题，传入包含用户输入名称的窗口列表
            MemoryTools.SetMilkWindowTitle(selectedWindows);
            AddOutput("已更新窗口标题");
        }

        private void MiaokuangJinduButton_Click(object sender, EventArgs e)
        {
            if (selectedWindows == null || selectedWindows.Count == 0)
            {
                AddOutput("请先选择窗口");
                return;
            }

            TextBox miaokuangInput = (TextBox)this.Controls.Find("miaokuangInput", true)[0];
            float miaokuangjindu = 0;

            if (float.TryParse(miaokuangInput.Text, out miaokuangjindu))
            {
                AddOutput($"正在应用秒矿进度设置: {miaokuangjindu}");
                MemoryTools.MiaokuangJindu(selectedWindows, miaokuangjindu);
                AddOutput("秒矿进度设置已应用");
            }
            else
            {
                AddOutput("请输入有效的秒矿进度值");
            }
        }

        private void MiaokuangIntervalButton_Click(object sender, EventArgs e)
        {
            if (selectedWindows == null || selectedWindows.Count == 0)
            {
                AddOutput("请先选择窗口");
                return;
            }

            CheckBox miaokuangIntervalCheckBox = (CheckBox)this.Controls.Find("miaokuangIntervalCheckBox", true)[0];
            bool enable = miaokuangIntervalCheckBox.Checked;

            AddOutput($"正在应用秒矿间隔设置: {(enable ? "开启" : "关闭")}");
            MemoryTools.MiaokuangInterval(selectedWindows, enable);
            AddOutput("秒矿间隔设置已应用");
        }

        private void KuaidaoButton_Click(object sender, EventArgs e)
        {
            if (selectedWindows == null || selectedWindows.Count == 0)
            {
                AddOutput("请先选择窗口");
                return;
            }

            CheckBox kuaidaoCheckBox = (CheckBox)this.Controls.Find("kuaidaoCheckBox", true)[0];
            bool enable = kuaidaoCheckBox.Checked;

            AddOutput($"正在应用快刀设置: {(enable ? "开启" : "关闭")}");
            MemoryTools.Kuaidao(selectedWindows, enable);
            AddOutput("快刀设置已应用");
        }

        private void ZuoqiButton_Click(object sender, EventArgs e)
        {
            if (selectedWindows == null || selectedWindows.Count == 0)
            {
                AddOutput("请先选择窗口");
                return;
            }

            CheckBox zuoqiCheckBox = (CheckBox)this.Controls.Find("zuoqiCheckBox", true)[0];
            int shifouxiugai = zuoqiCheckBox.Checked ? 1 : 0;

            AddOutput($"正在应用秒上坐骑设置: {(shifouxiugai == 1 ? "开启" : "关闭")}");
            MemoryTools.Miaoshangzuoqi(selectedWindows, shifouxiugai);
            AddOutput("秒上坐骑设置已应用");
        }

        private void ShijiaoButton_Click(object sender, EventArgs e)
        {
            if (selectedWindows == null || selectedWindows.Count == 0)
            {
                AddOutput("请先选择窗口");
                return;
            }

            CheckBox shijiaoCheckBox = (CheckBox)this.Controls.Find("shijiaoCheckBox", true)[0];
            TextBox shijiaoInput = (TextBox)this.Controls.Find("shijiaoInput", true)[0];
            bool enable = shijiaoCheckBox.Checked;
            float shijiaoValue = 89.5f;

            if (float.TryParse(shijiaoInput.Text, out shijiaoValue))
            {
                AddOutput($"正在应用视角锁定设置: {(enable ? "开启" : "关闭")}, 值: {shijiaoValue}");
                MemoryTools.ShijiaoLock(selectedWindows, shijiaoValue, enable);
                AddOutput("视角锁定设置已应用");
            }
            else
            {
                AddOutput("请输入有效的视角锁定值");
            }
        }

        private void ClearOutputButton_Click(object sender, EventArgs e)
        {
            TextBox outputTextBox = (TextBox)this.Controls.Find("outputTextBox", true)[0];
            outputTextBox.Clear();
        }

       

        private void RestoreButton_Click(object sender, EventArgs e)
        {
            if (selectedWindows == null || selectedWindows.Count == 0)
            {
                AddOutput("请先选择窗口");
                return;
            }

            AddOutput("正在还原所有内存修改...");

            // 还原所有内存修改
            MemoryTools.RestoreAllMemory(selectedWindows);

            AddOutput("所有内存修改已还原");

            // 重置UI控件
            TextBox miaokuangInput = (TextBox)this.Controls.Find("miaokuangInput", true)[0];
            miaokuangInput.Text = "0.7";

            CheckBox miaokuangIntervalCheckBox = (CheckBox)this.Controls.Find("miaokuangIntervalCheckBox", true)[0];
            miaokuangIntervalCheckBox.Checked = false;

            CheckBox kuaidaoCheckBox = (CheckBox)this.Controls.Find("kuaidaoCheckBox", true)[0];
            kuaidaoCheckBox.Checked = false;

            CheckBox zuoqiCheckBox = (CheckBox)this.Controls.Find("zuoqiCheckBox", true)[0];
            zuoqiCheckBox.Checked = false;

            CheckBox shijiaoCheckBox = (CheckBox)this.Controls.Find("shijiaoCheckBox", true)[0];
            shijiaoCheckBox.Checked = false;

            TextBox shijiaoInput = (TextBox)this.Controls.Find("shijiaoInput", true)[0];
            shijiaoInput.Text = "89.5";
        }


    }

    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainScreen());
        }
    }
}
