using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamNode.Graph
{

    public enum PassageType
    {
        North,
        South,
        East,
        West,
        Down,
        Up,
        Plus
    }

    public class Passage
    {
        public Pool link { get; set; }

        public PassageType type { get; set; }

        [JsonIgnore]
        public string linkId { get => link?.id ?? "null"; }

        public string description { get; set; }

        public Passage() { }
         
        public static PassageType reverse(PassageType pt)
        {
            if (pt == PassageType.North ||
                pt == PassageType.East ||
                pt == PassageType.Down)
                return pt + 1;
            else if (pt == PassageType.South ||
                pt == PassageType.West ||
                pt == PassageType.Up)
                return pt - 1;
            else
                return PassageType.Plus;

        }
    }
}
