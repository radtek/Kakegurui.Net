using System;
using System.Net;
using Kakegurui.Protocol;

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
