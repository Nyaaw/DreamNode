using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DreamNode.Graph
{
    public enum PoolSize
    {
        Small,
        Big,
        Huge
    }
    public class Pool
    {
        public static int id_count;

        public string id { get; set; }

        public List<Passage> passages { get; set; }

        public string desc { get; set; }
        public PoolSize size { get; set;}

        public int totalPassageCount { get => passages.Count; }

        public Pool() : this($"Room {id_count++}")
        { 

        }

        public Pool(string id)
        {
            this.id = id;
            passages = new List<Passage>();
        }

        public Dictionary<PassageType, int> PassageCount()
        {
            Dictionary<PassageType, int> count = new Dictionary<PassageType, int>()
            {
                { PassageType.North, 0 },
                { PassageType.South, 0 },
                { PassageType.East, 0 },
                { PassageType.West, 0 },
                { PassageType.Down, 0 },
                { PassageType.Up, 0 },
                { PassageType.Plus, 0 },

            };
            passages.ForEach(p => count[p.type]++);
            return count;
        }

        public Dictionary<PassageType, List<Passage>> GetPassages()
        {
            Dictionary<PassageType, List<Passage>> count = new Dictionary<PassageType, List<Passage>>();

            
            foreach(PassageType pt in Enum.GetValues(typeof(PassageType)))
            {
                count.Add(pt, passages.Where(p => p.type == pt).ToList());
            }
            
            return count;
        }
    }
}
