using System;

namespace MemoryHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            // 枚举所有奶块窗口
            MemoryTools.EnumMilkWindows1();

            // 修改奶块窗口标题
            MemoryTools.SetMilkWindowTitle();

            Console.WriteLine("请输入人物名称,不输入代表所有人物：");
            string renwu = Console.ReadLine();
            // 选择人物
            var hwndsNames = MemoryTools.SelectPerson(renwu);

            // 秒矿代码
            Console.WriteLine("请输入秒矿进度，收菜建议0.7,全挖建议10");
            string miaokuangjinduInput = Console.ReadLine();
            float miaokuangjindu = 0;
            if (!string.IsNullOrEmpty(miaokuangjinduInput))
            {
                float.TryParse(miaokuangjinduInput, out miaokuangjindu);
            }
            // 修改秒矿进度
            MemoryTools.Miaokuang(hwndsNames, miaokuangjindu);

            // 秒上坐骑代码
            Console.WriteLine("请输入是否开启秒上坐骑");
            Console.WriteLine("1为开启（0或不输入代表复原）");
            string shifouxiugaiInput = Console.ReadLine();
            int shifouxiugai = 0;
            if (!string.IsNullOrEmpty(shifouxiugaiInput))
            {
                int.TryParse(shifouxiugaiInput, out shifouxiugai);
            }
            // 修改秒上坐骑
            MemoryTools.Miaoshangzuoqi(hwndsNames, shifouxiugai);

            Console.WriteLine("操作完成，按任意键退出...");
            Console.ReadKey();
        }
    }
}
