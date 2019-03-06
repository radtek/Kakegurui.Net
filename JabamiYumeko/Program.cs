using System;
using ZooKeeperNet;

namespace JabamiYumeko
{
    class Watcher : IWatcher
    {
        public void Process(WatchedEvent @event)
        {
            if (@event.Type == EventType.NodeDataChanged)
            {
                Console.WriteLine(@event.Path);
            }
        }
    }

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
