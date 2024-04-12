using DreamNode.Graph;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System.Windows.Media;

namespace DreamNode.Register
{
    public class Engine
    {
        public List<Pool> pools;
        public List<Tuple<Pool, Pool>> views;
        public Pool active;
        public Pool old;

        public event PropertyChangedEventHandler propertyChanged;
         
        public Engine()
        { 
            pools = new List<Pool>(); 
            views = new List<Tuple<Pool, Pool>>(); 
        }
        public void AddEmptyPassage(PassageType passagetype)
        {
            active.passages.Add(new Passage { type = passagetype, link = null});

        }
        public void AddSimplePassage(PassageType passagetype)
        {
            Passage p = new Passage{ type = passagetype, link = NewPool() };

            active.passages.Add(p);

            p.link.passages.Add(new Passage { type = Passage.reverse(passagetype), link = active });

        }

        public bool Save(string filePath)
        {
            JObject rss;

            try
            {
                rss = new JObject(
                        new JProperty("poolNumber", Pool.id_count),
                        new JProperty("views",
                            new JArray(
                                from v in views
                                select new JObject(
                                    new JProperty("1", v.Item1.id),
                                    new JProperty("2", v.Item2.id)
                        ))),
                        new JProperty("pools",
                            new JArray(
                                from p in pools
                                select new JObject(
                                    new JProperty("id", p.id),
                                    new JProperty("desc", p.desc),
                                    new JProperty("size", p.size),
                                    new JProperty("passages", new JArray(
                                        from s in p.passages
                                        select new JObject(
                                            new JProperty("type", s.type),
                                            new JProperty("link", s.linkId),
                                            new JProperty("description", s.description)

                 )))))));

            } 
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            if (rss.Count < 100)
                return false;

            if (File.Exists(filePath))
            {
#if !DEBUG
                File.Copy(filePath, "G:\\Documents\\Code\\DreamNode\\BackupGraph.txt", true);
#endif
                File.Delete(filePath);
            }

            File.WriteAllText(filePath, rss.ToString());
            return true;

        }

        public void Restore(string filePath)
        {
            string c = File.ReadAllText(filePath);

            if (c.Length == 0)
                return;

            JObject j = JObject.Parse(c);

            Pool.id_count = (int)j["poolNumber"]!;



            foreach (var p in j["pools"]!)
            {
                Pool n = NewPool();
                n.id = (string)p["id"]!;
                n.desc = (string)p["desc"]!;
                n.size = (PoolSize) ((int)p["size"]!);
            }

            foreach (var p in j["pools"]!)
            {
                Pool n = pools.Find(x => x.id == (string)p["id"]);
                foreach (var s in p["passages"]!)
                {
                    Passage m = new Passage();
                    m.type = (PassageType) ((int)s["type"]!);
                    m.link = pools.Find(x => x.id == (string)s["link"]!)!;
                    m.description = (string)s["description"]!;

                    n.passages.Add(m);
                }
            }

            foreach (var v in j["views"]!)
            {
                Tuple<Pool, Pool> t = new Tuple<Pool, Pool>(
                    pools.Find(x => x.id == (string)v["1"]),
                    pools.Find(x => x.id == (string)v["2"])
                );

                views.Add(t);
            }

            Pool.id_count = (int)j["poolNumber"]!;
     
        }

        public void GoTo(Pool pool, bool tp = false)
        {
            if(!tp)
                old = active;

            active = pool;
        }

        public Pool NewPool()
        {

            Pool x = new Pool();
            pools.Add(x);
            return x;
        }

        internal void Start()
        {
            if (pools.Count == 0)
                active = NewPool();
            else
                active = pools.First();
        }
    }


}
