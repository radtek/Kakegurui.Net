using System;
using System.Net;
using Kakegurui.Protocol;

namespace JabamiYumeko
{
    class Program
    {
        static void Main(string[] args)
        {
            ProtocolMaid maid1=new ProtocolMaid();
            maid1.Start();
            maid1.AddConnectEndPoint(new IPEndPoint(IPAddress.Loopback, 22000),new ProtocolHandler());
            MonitorTask maid = new MonitorTask();
            maid.Start();
            Console.ReadLine();
            maid.Stop();
        }
    }
}
