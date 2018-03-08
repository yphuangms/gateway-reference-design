using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublisherDesignerApp
{
    public class NodeDataType
    {
        [JsonProperty(PropertyName = "session")]
        public string session { get; set; }

        [JsonProperty(PropertyName = "address")]
        public string nodeid { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string value { get; set; }
        public NodeDataType(string name, string id, string val)
        {
            session = name;
            nodeid = id;
            value = val;
        }
    }

    public class NodeDataQueue<T> : Queue<T>
    {
        private object m_lock;
        public NodeDataQueue(int size) : base(size)
        {
            m_lock = new object();
        }
        public NodeDataQueue() : base()
        {
            m_lock = new object();
        }

        public void ClearWithLock()
        {
            lock (m_lock)
            {
                this.Clear();
            }
        }

        public T DequeueWithLock()
        {
            T result;
            lock (m_lock)
            {
                result = this.Dequeue();
            }
            return result;
        }
        public void EnqueueWithLock(T item)
        {
            lock (m_lock)
            {
                this.Enqueue(item);
            }
        }
    }
}
