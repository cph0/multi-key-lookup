using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MultiKeyLookup;

public class MultiKeyLookup<T> : IEnumerable<T>
{
    private readonly Dictionary<int, T> _Data;
    private readonly Dictionary<string, Func<T, object>> _FieldMap;
    private readonly Dictionary<string, Dictionary<object, HashSet<int>>> _Indexes;
    private readonly Stack<int> _UnusedIds;

    public int Count => _Data.Count;
    public int Indexes => _Indexes.Count;
    public IEnumerable<T> Values => _Data.Values;

    public MultiKeyLookup() : this(Array.Empty<T>()) { }
    public MultiKeyLookup(params (string, Func<T, object>)[] indexes) : this(Array.Empty<T>(), indexes) { }
    public MultiKeyLookup(T[] data, params (string, Func<T, object>)[] indexes)
    {
        _Data = data.Select((s, i) => new { s, i }).ToDictionary(k => k.i, k => k.s);
        _UnusedIds = new(data.Length);

        if (indexes != null)
        {
            _FieldMap = indexes.ToDictionary(k => k.Item1, k => k.Item2);
            _Indexes = indexes.ToDictionary(k => k.Item1, k => new Dictionary<object, HashSet<int>>());
        }
        else
        {
            _FieldMap = new();
            _Indexes = new();
        }

        if (data != null && indexes != null)
        {
            foreach ((string, Func<T, object>) Index in indexes)
                SetIndex(Index);
        }
    }

    private void SetIndex((string, Func<T, object>) index)
    {
        Dictionary<object, HashSet<int>> Map = _Data.GroupBy(g => index.Item2(g.Value))
            .ToDictionary(k => k.Key, k => k.Select(s => s.Key).ToHashSet());
        _FieldMap.TryAdd(index.Item1, index.Item2);

        if (!_Indexes.TryAdd(index.Item1, Map))
            _Indexes[index.Item1] = Map;
    }

    public void AddIndex(string field, Func<T, object> fieldSelector)
    {
        if (!_Indexes.ContainsKey(field))
            SetIndex((field, fieldSelector));
    }

    public void RemoveIndex(string field)
    {
        if (_Indexes.ContainsKey(field))
        {
            _FieldMap.Remove(field);
            _Indexes.Remove(field);
        }
    }

    public void Add(params T[] data)
    {
        foreach (T Item in data)
        {
            int DataIndex = _Data.Count;

            if (_UnusedIds.TryPop(out int UnusedId))
                DataIndex = UnusedId;

            _Data.Add(DataIndex, Item);

            foreach (KeyValuePair<string, Dictionary<object, HashSet<int>>> Index in _Indexes)
            {
                if (_FieldMap.TryGetValue(Index.Key, out Func<T, object> key))
                {
                    if (Index.Value.TryGetValue(key(Item), out HashSet<int> set))
                        set.Add(DataIndex);
                    else
                        Index.Value.Add(key(Item), new() { DataIndex });
                }
            }
        }
    }

    public bool ContainsKey(string field, object key, Func<T, object> fieldSelector = null)
    {
        if (_Indexes.TryGetValue(field, out Dictionary<object, HashSet<int>> Map))
            return Map != null && Map.ContainsKey(key);
        else
            return key != null && _Data.Values.Any(a => fieldSelector(a).Equals(key));
    }

    public bool TryGetValue(string field, object key, out T item, Func<T, object> fieldSelector = null)
    {
        item = GetValueOrDefault(field, key, fieldSelector);
        return item != null;
    }

    public T GetValueOrDefault(string field, object key, Func<T, object> fieldSelector = null)
        => Get(field, key, fieldSelector).SingleOrDefault();

    public IEnumerable<T> Get(string field, object key, Func<T, object> fieldSelector = null)
    {
        if (_Indexes.TryGetValue(field, out Dictionary<object, HashSet<int>> Map))
        {
            if (Map != null && Map.TryGetValue(key, out HashSet<int> dataIndexes))
            {
                List<T> Matches = new List<T>(dataIndexes.Count);

                foreach (int DataIndex in dataIndexes)
                {
                    if (_Data.TryGetValue(DataIndex, out T Item))
                        yield return Item;
                }
            }
        }
        else if (fieldSelector != null)
        {
            foreach (T data in _Data.Values.Where(w => fieldSelector(w).Equals(key)))
                yield return data;
        }
    }

    private void DeleteData(IEnumerable<int> dataIndexes)
    {
        foreach (int DataIndex in dataIndexes)
        {
            if (_Data.TryGetValue(DataIndex, out T Item))
            {
                foreach (KeyValuePair<string, Dictionary<object, HashSet<int>>> Index in _Indexes)
                {
                    if (_FieldMap.TryGetValue(Index.Key, out Func<T, object> Map))
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

                _Data.Remove(DataIndex);
                _UnusedIds.Push(DataIndex);
            }
        }
    }

    public void Remove(string field, object key, Func<T, object> fieldSelector = null)
    {
        if (_Indexes.TryGetValue(field, out Dictionary<object, HashSet<int>> index))
        {
            if (index.TryGetValue(key, out HashSet<int> dataIndexes))
                DeleteData(dataIndexes);
        }
        else if (fieldSelector != null)
        {
            IEnumerable<int> dataIndexes = _Data.Where(w => fieldSelector(w.Value) == key).Select(s => s.Key);
            DeleteData(dataIndexes);
        }
    }

    public IEnumerator<T> GetEnumerator() => _Data.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
