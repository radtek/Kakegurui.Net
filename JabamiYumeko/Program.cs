using System;

namespace JabamiYumeko
{
    class Program
    {
        static void Main(string[] args)
        {
            MonitorMaid maid = new MonitorMaid();
            maid.Start();
            Console.ReadLine();
            maid.Stop();
        }
    }
}
