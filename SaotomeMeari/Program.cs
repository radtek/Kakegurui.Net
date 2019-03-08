using System;
using Kakegurui.Core;

namespace SaotomeMeari
{
    class Program
    {
        static void Main(string[] args)
        {
            DBTask task = new DBTask();
            task.Start();
            Console.ReadLine();
            task.Stop();
        }
    }
}
