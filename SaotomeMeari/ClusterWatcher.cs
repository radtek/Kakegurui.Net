using System;
using Org.Apache.Zookeeper.Data;
using ZooKeeperNet;

namespace SaotomeMeari
{
    public class ClusterNodeChangedEventArgs : EventArgs
    {
        public int Index { get; set; }
        public int Count { get; set; }
    }

    class ClusterWatcher : IWatcher
    {
        private const string RootPath = "/kakegurui";

        private readonly ZooKeeper _zk;

        private int _index;

        public event EventHandler<ClusterNodeChangedEventArgs> ClusterNodeChanged;

        public ClusterWatcher(string address)
        {
            _zk = new ZooKeeper(address, new TimeSpan(0, 0, 0, 5), this);
        }

        public void Process(WatchedEvent e)
        {
            if (e.State == KeeperState.SyncConnected)
            {
                if (e.Path == null || e.Type == EventType.None)
                {
                    Stat stat = _zk.Exists(RootPath, true);
                    if (stat == null)
                    {
                        try
                        {
                            _zk.Create(RootPath, string.Empty.GetBytes(), Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
                        }
                        catch (KeeperException.NodeExistsException)
                        {

                        }
                    }
                    _zk.GetChildren(RootPath, true);
                    string result = _zk.Create(string.Format("{0}/", RootPath), string.Empty.GetBytes(), Ids.OPEN_ACL_UNSAFE, CreateMode.EphemeralSequential);
                    _index = Convert.ToInt32(result.Split("/", StringSplitOptions.RemoveEmptyEntries)[1]);
                }
                else if (e.Path == RootPath && e.Type == EventType.NodeChildrenChanged)
                {
                    var nodes = _zk.GetChildren(RootPath, true);
                    int count = 0;
                    int index = 0;
                    foreach (string node in nodes)
                    {
                        if (Convert.ToInt32(node) == _index)
                        {
                            index = count;
                        }
                        count++;
                    }
                    ClusterNodeChanged?.Invoke(this, new ClusterNodeChangedEventArgs
                    {
                        Count = count,
                        Index = index
                    });
                }
            }
        }
    }
}
