using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using QUT.Gppg;

namespace Microsoft.Automata.MSO.Mona
{
    /// <summary>
    /// Stack of dictionaries (or maps).
    /// </summary>
    /// <typeparam name="K">key type</typeparam>
    /// <typeparam name="V">value type</typeparam>
    public class MapStack<K, V>
    {
        Cons<Dictionary<K, V>> maps = Cons<Dictionary<K, V>>.Empty;
        MapStack<K, V> rest = null;

        /// <summary>
        /// Tye empty mapstack
        /// </summary>
        public static MapStack<K, V> Empty = new MapStack<K, V>();

        /// <summary>
        /// True iff the mapstack is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return maps.IsEmpty;
            }
        }

        MapStack()
        {
        }

        MapStack(Cons<Dictionary<K, V>> maps, MapStack<K, V> rest)
        {
            this.maps = maps;
            this.rest = rest;
        }

        /// <summary>
        /// Pushes a new map at the top. Returns the new mapstack.
        /// </summary>
        /// <param name="map">map to be pushed</param>
        public MapStack<K, V> Push(Dictionary<K, V> map)
        {
            var maps1 = new Cons<Dictionary<K, V>>(map, maps);
            return new MapStack<K, V>(maps1, this);
        }

        /// <summary>
        /// Pushes a new maplet at the top. Returns the new mapstack.
        /// </summary>
        /// <param name="key">key of the maplet</param>
        /// <param name="val">value of the maplet</param>
        public MapStack<K, V> Push(K key, V val)
        {
            var map = new Dictionary<K, V>();
            map[key] = val;
            return Push(map);
        }

        /// <summary>
        /// Returns the mapstack without the topmost map.
        /// Throws InvalidOperationException if the mapstack is empty.
        /// </summary>
        public MapStack<K, V> Pop()
        {
            if (maps.IsEmpty)
                throw new InvalidOperationException();
            return rest;
        }

        /// <summary>
        /// Tries to get a value for a given key. Returns true iff a value is found.
        /// Looks first in the topmost map and then in the rest of the 
        /// mapstack.
        /// </summary>
        /// <param name="key">given key</param>
        /// <param name="val">resulting value or default(V) if no value was found</param>
        /// <returns></returns>
        public bool TryGetValue(K key, out V val)
        {
            var maps_ = maps;
            while (!maps_.IsEmpty)
            {
                if (maps_.First.TryGetValue(key, out val))
                    return true;
                maps_ = maps_.Rest;
            }
            val = default(V);
            return false;
        }

        /// <summary>
        /// Flatten the mapstack to a map.
        /// </summary>
        public Dictionary<K, V> Flatten()
        {
            if (IsEmpty)
                return new Dictionary<K, V>();
            else 
            {
                var res = rest.Flatten();
                foreach (var kv in maps.First)
                    res[kv.Key] = kv.Value;
                return res;
            }
        }

        public Dictionary<K, V> AsDictionary
        {
            get
            {
                return Flatten();
            }
        }

        /// <summary>
        /// Former maplets override latter maplets.
        /// </summary>
        public override string ToString()
        {
            return maps.ToString(", ", MapToStr, "[", "]");
        }

        static string MapToStr(Dictionary<K, V> map)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            bool addcomma = false;
            foreach (var kv in map)
            {
                if (addcomma)
                    sb.Append(", ");
                sb.AppendFormat("{0} -> {1}", kv.Key, kv.Value);
                addcomma = true;
            }
            sb.Append("}");
            return sb.ToString();
        }
    }
}
