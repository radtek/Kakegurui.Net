using System;
using Org.Apache.Zookeeper.Data;
using ZooKeeperNet;

namespace SaotomeMeari
{
    /// <summary>
    /// 集群节点改变事件参数
    /// </summary>
    public class ClusterNodeChangedEventArgs : EventArgs
    {
        public int Index { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// 集群检测
    /// </summary>
    internal class ClusterWatcher : IWatcher
    {
        /// <summary>
        /// 根节点
        /// </summary>
        private const string RootPath = "/kakegurui";

        /// <summary>
        /// zk实例
        /// </summary>
        private readonly ZooKeeper _zk;

        /// <summary>
        /// 当前在集群中的序号
        /// </summary>
        private int _index;

        /// <summary>
        /// 集群节点改变事件
        /// </summary>
        public event EventHandler<ClusterNodeChangedEventArgs> ClusterNodeChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="address">zk地址</param>
        public ClusterWatcher(string address)
        {
            _zk = new ZooKeeper(address, new TimeSpan(0, 0, 0, 5), this);
        }

        /// <summary>
        /// zk监控实现函数
        /// </summary>
        /// <param name="e"></param>
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
                    string result = _zk.Create(string.Format("{0}/", RootPath), string.Empty.GetBytes(), Ids.OPEN_ACL_UNSAFE, CreateMode.EphemeralSequential);
                    _index = Convert.ToInt32(result.Split("/", StringSplitOptions.RemoveEmptyEntries)[1]);
                    GetClusterIndex();
                }
                else if (e.Path == RootPath && e.Type == EventType.NodeChildrenChanged)
                {
                    GetClusterIndex();
                }
            }
        }

        /// <summary>
        /// 获取当前在集群中的顺序
        /// </summary>
        private void GetClusterIndex()
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
