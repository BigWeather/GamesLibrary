using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamesLibrary
{
#if !WINDOWS
    public class SortedList<K, V> where K : IComparable
    {
        public List<K> Keys
        {
            get { return _keys; }
        }
        List<K> _keys;

        public List<V> Values
        {
            get { return _values; }
        }
        List<V> _values;

        Dictionary<K, V> _dictValuesByKey;

        public SortedList()
        {
            _keys = new List<K>();
            _values = new List<V>();
            _dictValuesByKey = new Dictionary<K, V>();
        }

        public bool ContainsKey(K key)
        {
            return _dictValuesByKey.ContainsKey(key);            
        }

        public void Add(K key, V value)
        {
            if (ContainsKey(key))
                return;

            int insertIdx = -1;
            for (int i = 0; i < _keys.Count; i++)
            {
                if (_keys[i].CompareTo(key) <= 0)
                    continue;

                insertIdx = i;
                break;
            }

            if (insertIdx >= 0)
            {
                _keys.Insert(insertIdx, key);
                _values.Insert(insertIdx, value);
            }
            else
            {
                _keys.Add(key);
                _values.Add(value);
            }

            _dictValuesByKey.Add(key, value);
        }
    }
#endif
}
