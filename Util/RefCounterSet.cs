using System;
using System.Collections;
using System.Collections.Generic;

public class RefCounterSet : ICollection<string>
{
    private readonly Dictionary<string, int> _refs = new Dictionary<string, int>();

    /// <summary>
    /// Adds a string or increments the count if it already exists.
    /// </summary>
    public bool Add(string item)
    {
        if (_refs.ContainsKey(item))
        {
            _refs[item]++;
            return false;
        }
        else
        {
            _refs[item] = 1;
            return true;
        }
    }

    void ICollection<string>.Add(string item)
    {
        Add(item);
    }

    /// <summary>
    /// Decreases the count or removes completely if it reaches 0.
    /// </summary>
    public bool Remove(string item)
    {
        int count;
        if (_refs.TryGetValue(item, out count))
        {
            if (count <= 1)
            {
                _refs.Remove(item);
                return true; 
            }
            else
            {
                _refs[item] = count - 1;
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if the element is present (with any count).
    /// </summary>
    public bool Contains(string item)
    {
        return _refs.ContainsKey(item);
    }

    public void Clear()
    {
        _refs.Clear();
    }

    public int Count
    {
        get { return _refs.Count; }
    }

    public bool IsReadOnly
    {
        get { return false; }
    }

    /// <summary>
    /// Copies unique strings into an array.
    /// </summary>
    public void CopyTo(string[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException("array");

        if (arrayIndex < 0 || arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException("arrayIndex");

        if ((array.Length - arrayIndex) < _refs.Count)
            throw new ArgumentException("Not enough space in the target array.");

        foreach (var key in _refs.Keys)
        {
            array[arrayIndex++] = key;
        }
    }

    public IEnumerator<string> GetEnumerator()
    {
        return _refs.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int GetRefCount(string item)
    {
        int count;
        return _refs.TryGetValue(item, out count) ? count : 0;
    }

    public IEnumerable<KeyValuePair<string, int>> AsPairs()
    {
        return _refs;
    }
}