using System;

namespace JabamiYumeko
{
    class Program
    {
        static void Main(string[] args)
        {
            MonitorTask maid = new MonitorTask();
            maid.Start();
            Console.ReadLine();
            maid.Stop();
        }
    }
}
