using System;
using System.Collections.Generic;
using System.Linq;

namespace DataStructure
{
    public class MultiKeyLookup<T>
    {
        private readonly Dictionary<int, T> Data;
        private readonly Dictionary<string, Func<T, object>> FieldMap;
        private readonly Dictionary<string, Dictionary<object, HashSet<int>>> Indexes;
        private readonly Stack<int> UnusedIds;

        public MultiKeyLookup() : this(Array.Empty<T>()) { }
        public MultiKeyLookup(params (string, Func<T, object>)[] indexes) : this(Array.Empty<T>(), indexes) { }
        public MultiKeyLookup(T[] data, params (string, Func<T, object>)[] indexes)
        {
            Data = data.Select((s, i) => new { s, i }).ToDictionary(k => k.i, k => k.s);
            UnusedIds = new(data.Length);

            if (indexes != null)
            {
                FieldMap = indexes.ToDictionary(k => k.Item1, k => k.Item2);
                Indexes = indexes.ToDictionary(k => k.Item1, k => new Dictionary<object, HashSet<int>>());
            }
            else 
            {
                FieldMap = new();
                Indexes = new();
            }

            if (data != null && indexes != null) 
            {
                foreach ((string, Func<T, object>) Index in indexes)
                    SetIndex(Index);            
            }            
        }

        private void SetIndex((string, Func<T, object>) index)
        {
            Dictionary<object, HashSet<int>> Map = Data.GroupBy(g => index.Item2(g.Value))
                .ToDictionary(k => k.Key, k => k.Select(s => s.Key).ToHashSet());
            FieldMap.TryAdd(index.Item1, index.Item2);

            if (!Indexes.TryAdd(index.Item1, Map))
                Indexes[index.Item1] = Map;
        }

        public void AddIndex(string field, Func<T, object> key) 
        {
            if (!Indexes.ContainsKey(field))
                SetIndex((field, key));
        }

        public void RemoveIndex(string field) 
        {
            if (Indexes.ContainsKey(field)) 
            {
                FieldMap.Remove(field);
                Indexes.Remove(field);
            }
        }

        public void Add(params T[] data)
        {
            foreach (T Item in data)
            {
                int DataIndex = Data.Count;

                if (UnusedIds.TryPop(out int UnusedId))
                    DataIndex = UnusedId;

                Data.Add(DataIndex, Item);

                foreach (KeyValuePair<string, Dictionary<object, HashSet<int>>> Index in Indexes)
                {
                    if (FieldMap.TryGetValue(Index.Key, out Func<T, object> key) 
                        && Index.Value.TryGetValue(key(Item), out HashSet<int> set))
                    {
                        if (set != null)
                            set.Add(DataIndex);
                        else
                            Index.Value[key(Item)] = new() { DataIndex };
                    }
                }
            }
        }

        private void DeleteData(IEnumerable<int> dataIndexes) 
        {
            foreach (int DataIndex in dataIndexes)
            {
                if (Data.TryGetValue(DataIndex, out T Item))
                {
                    foreach (KeyValuePair<string, Dictionary<object, HashSet<int>>> Index in Indexes)
                    {
                        if (FieldMap.TryGetValue(Index.Key, out Func<T, object> Map))
                        {
                            object DataAtIndex = Map(Item);

                            if (Index.Value.TryGetValue(DataAtIndex, out HashSet<int> Matches))
                            {
                                if (Matches.Contains(DataIndex))
                                    Matches.Remove(DataIndex);

                                if (Matches.Count == 0)
                                    Index.Value.Remove(DataAtIndex);
                            }
                        }
                    }

                    Data.Remove(DataIndex);
                    UnusedIds.Push(DataIndex);
                }
            }
        }

        public void Delete(string field, object value, Func<T, object> key = null)
        {
            if (Indexes.TryGetValue(field, out Dictionary<object, HashSet<int>> index))
            {
                if (index.TryGetValue(value, out HashSet<int> dataIndexes))
                    DeleteData(dataIndexes);
            }
            else if (key != null)
            {
                IEnumerable<int> dataIndexes = Data.Where(w => key(w.Value) == value).Select(s => s.Key);
                DeleteData(dataIndexes);
            }
        }

        public IEnumerable<T> Get(string field, object value, Func<T, object> key = null)
        {
            if (Indexes.TryGetValue(field, out Dictionary<object, HashSet<int>> Map))
            {
                if (Map != null && Map.TryGetValue(value, out HashSet<int> dataIndexes))
                {
                    List<T> Matches = new List<T>(dataIndexes.Count);

                    foreach (int DataIndex in dataIndexes)
                    {
                        if (Data.TryGetValue(DataIndex, out T Item))
                            yield return Item;
                    }
                }
            }
            else if (key != null)
            {
                foreach (T data in Data.Values.Where(w => key(w).Equals(value)))
                    yield return data;            
            }
        }
    }
}
