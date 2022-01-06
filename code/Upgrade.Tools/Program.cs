using System;
using System.IO;

namespace Upgrade.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine("- Upgrade Tools");
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine("- 1. 生成更新源");
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine("输入选择：");
            string index = Console.ReadLine();
            Console.WriteLine("--------------------------------------------------------------");
            if (index.Equals("1"))
            {
                Console.WriteLine("输入目标目录：");
                string target_dir = Console.ReadLine();
                Update update = new Update();
                update.GenerateRepository(target_dir);
            }
        }
    }
}
