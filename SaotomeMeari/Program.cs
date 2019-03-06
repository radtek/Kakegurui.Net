using System;

namespace SaotomeMeari
{
    class Program
    {
        static void Main(string[] args)
        {
            PingMaid maid = new PingMaid();
            maid.Start();
            Console.ReadLine();
            maid.Stop();
        }
    }
}
