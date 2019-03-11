using System;
using System.Net;
using Kakegurui.Core;
using Kakegurui.Protocol;

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
