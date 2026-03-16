using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MemoryHelper
{
    public class MainScreen : Form
    {
        private List<Tuple<IntPtr, string>> selectedWindows = new List<Tuple<IntPtr, string>>();
        private List<MemoryTools.WindowInfo> allWindows = new List<MemoryTools.WindowInfo>();

        public MainScreen()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "梦幻社区";
            this.Size = new System.Drawing.Size(800, 550); // 进一步增加表单大小以适应更大的间距
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

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
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15));
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
            rightTablePanel.Controls.Add(outputTextBox, 0, 2);

            // 清空输出按钮
            Button clearOutputButton = new Button();
            clearOutputButton.Text = "清空输出";
            clearOutputButton.Dock = DockStyle.Fill;
            clearOutputButton.Click += ClearOutputButton_Click;
            rightTablePanel.Controls.Add(clearOutputButton, 0, 3);

            // 底部面板 - 秒矿和坐骑设置
            Panel bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Fill;
            bottomPanel.Padding = new Padding(20);
            mainPanel.Controls.Add(bottomPanel, 0, 1);
            mainPanel.SetColumnSpan(bottomPanel, 2);

            // 秒矿设置标签
            Label miaokuangLabel = new Label();
            miaokuangLabel.Text = "秒矿设置:";
            miaokuangLabel.Location = new Point(20, 20);
            miaokuangLabel.AutoSize = true;
            bottomPanel.Controls.Add(miaokuangLabel);

            // 秒矿进度输入
            TextBox miaokuangInput = new TextBox();
            miaokuangInput.Name = "miaokuangInput";
            miaokuangInput.Location = new Point(100, 16);
            miaokuangInput.Size = new Size(100, 20);
            miaokuangInput.Text = "0.7";
            bottomPanel.Controls.Add(miaokuangInput);

            // 秒矿说明
            Label miaokuangHint = new Label();
            miaokuangHint.Text = "收菜建议0.7,全挖建议10";
            miaokuangHint.Location = new Point(210, 20);
            miaokuangHint.AutoSize = true;
            bottomPanel.Controls.Add(miaokuangHint);

            // 秒矿按钮
            Button miaokuangButton = new Button();
            miaokuangButton.Text = "应用秒矿设置";
            miaokuangButton.Location = new Point(370, 10);
            miaokuangButton.Size = new Size(120, 30);
            miaokuangButton.Click += MiaokuangButton_Click;
            bottomPanel.Controls.Add(miaokuangButton);

            // 秒上坐骑设置
            Label zuoqiLabel = new Label();
            zuoqiLabel.Text = "秒上坐骑:";
            zuoqiLabel.Location = new Point(20, 60);
            zuoqiLabel.AutoSize = true;
            bottomPanel.Controls.Add(zuoqiLabel);

            // 秒上坐骑复选框
            CheckBox zuoqiCheckBox = new CheckBox();
            zuoqiCheckBox.Name = "zuoqiCheckBox";
            zuoqiCheckBox.Text = "开启秒上坐骑";
            zuoqiCheckBox.Location = new Point(100, 58);
            bottomPanel.Controls.Add(zuoqiCheckBox);

            // 秒上坐骑按钮
            Button zuoqiButton = new Button();
            zuoqiButton.Text = "应用坐骑设置";
            zuoqiButton.Location = new Point(370, 55);
            zuoqiButton.Size = new Size(120, 30);
            zuoqiButton.Click += ZuoqiButton_Click;
            bottomPanel.Controls.Add(zuoqiButton);

            // 还原按钮
            Button restoreButton = new Button();
            restoreButton.Text = "还原所有设置";
            restoreButton.Location = new Point(510, 10);
            restoreButton.Size = new Size(120, 75);
            restoreButton.Click += RestoreButton_Click;
            bottomPanel.Controls.Add(restoreButton);
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
            outputTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}\r\n");
            outputTextBox.ScrollToCaret();
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

            if (selectedIndex >= 0 && selectedIndex < allWindows.Count)
            {
                var selectedWindow = allWindows[selectedIndex];
                selectedWindows = new List<Tuple<IntPtr, string>>();
                selectedWindows.Add(Tuple.Create(selectedWindow.Hwnd, selectedWindow.Name));
                AddOutput($"已选择窗口: {selectedWindow.Name} - {selectedWindow.Title}");
            }
            else
            {
                // 选择所有窗口
                selectedWindows = allWindows.Select(w => Tuple.Create(w.Hwnd, w.Name)).ToList();
                AddOutput("已选择所有窗口");
            }

            // 修改窗口标题
            MemoryTools.SetMilkWindowTitle();
            AddOutput("已更新窗口标题");
        }

        private void MiaokuangButton_Click(object sender, EventArgs e)
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
                AddOutput($"正在应用秒矿设置: {miaokuangjindu}");
                MemoryTools.Miaokuang(selectedWindows, miaokuangjindu);
                AddOutput("秒矿设置已应用");
            }
            else
            {
                AddOutput("请输入有效的秒矿进度值");
            }
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

            CheckBox zuoqiCheckBox = (CheckBox)this.Controls.Find("zuoqiCheckBox", true)[0];
            zuoqiCheckBox.Checked = false;
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
