using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NLog.Raygun
{
  class UserCustomDictionary : IDictionary, IDictionary<string, object>
  {
    private readonly IDictionary<string, object> _wrapped;

    public UserCustomDictionary(IDictionary<string, object> dictionary)
    {
      _wrapped = dictionary;
    }

    object IDictionary.this[object key] { get => ValueSafeConverter(_wrapped[key.ToString()]); set => _wrapped[key.ToString()] = value; }
    public object this[string key] { get => ValueSafeConverter(_wrapped[key]); set => _wrapped[key] = value; }

    bool IDictionary.IsFixedSize => false;

    public bool IsReadOnly => _wrapped.IsReadOnly;

    ICollection IDictionary.Keys => (_wrapped.Keys as ICollection) ?? _wrapped.Keys.ToList();

    ICollection IDictionary.Values => (_wrapped.Values as ICollection) ?? _wrapped.Values.ToList();

    public ICollection<string> Keys => _wrapped.Keys;

    public ICollection<object> Values => _wrapped.Values;

    public int Count => _wrapped.Count;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    void IDictionary.Add(object key, object value)
    {
      _wrapped.Add(key.ToString(), value);
    }

    public void Add(string key, object value)
    {
      _wrapped.Add(key, value);
    }

    public void Add(KeyValuePair<string, object> item)
    {
      _wrapped.Add(item);
    }

    public void Clear()
    {
      _wrapped.Clear();
    }

    bool IDictionary.Contains(object key)
    {
      return _wrapped.ContainsKey(key.ToString());
    }

    public bool Contains(KeyValuePair<string, object> item)
    {
      return _wrapped.Contains(item);
    }

    public bool ContainsKey(string key)
    {
      return _wrapped.ContainsKey(key);
    }

    public void CopyTo(Array array, int index)
    {
      if (array is KeyValuePair<string, object>[] validArray)
        _wrapped.CopyTo(validArray, index);
      else
        (_wrapped as ICollection)?.CopyTo(array, index);
    }

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
      _wrapped.CopyTo(array, arrayIndex);
    }

    public void Remove(object key)
    {
      _wrapped.Remove(key.ToString());
    }

    public bool Remove(string key)
    {
      return _wrapped.Remove(key);
    }

    public bool Remove(KeyValuePair<string, object> item)
    {
      return _wrapped.Remove(item);
    }

    public bool TryGetValue(string key, out object value)
    {
      if (_wrapped.TryGetValue(key, out value))
      {
        value = ValueSafeConverter(value);
        return true;
      }
      return false;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
      return new SafeDictionaryEnumerator(_wrapped.GetEnumerator());
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
      return new SafeDictionaryEntryEnumerator(_wrapped.GetEnumerator());
    }

    class SafeDictionaryEnumerator : IEnumerator<KeyValuePair<string, object>>
    {
      readonly IEnumerator<KeyValuePair<string, object>> _wrapped;

      public KeyValuePair<string, object> Current => new KeyValuePair<string, object>(_wrapped.Current.Key, ValueSafeConverter(_wrapped.Current.Value));

      object IEnumerator.Current => Current;

      public SafeDictionaryEnumerator(IEnumerator<KeyValuePair<string, object>> wrapped)
      {
        _wrapped = wrapped;
      }

      public void Dispose()
      {
        _wrapped.Dispose();
      }

      public bool MoveNext()
      {
        return _wrapped.MoveNext();
      }

      public void Reset()
      {
        _wrapped.Reset();
      }
    }

    class SafeDictionaryEntryEnumerator : IDictionaryEnumerator
    {
      readonly IEnumerator<KeyValuePair<string, object>> _wrapped;

      public DictionaryEntry Entry => new DictionaryEntry(_wrapped.Current.Key, ValueSafeConverter(_wrapped.Current.Value));

      public object Key => _wrapped.Current.Key;

      public object Value => ValueSafeConverter(_wrapped.Current.Value);

      public object Current => Entry;

      public SafeDictionaryEntryEnumerator(IEnumerator<KeyValuePair<string, object>> wrapped)
      {
        _wrapped = wrapped;
      }

      public bool MoveNext()
      {
        return _wrapped.MoveNext();
      }

      public void Reset()
      {
        _wrapped.Reset();
      }
    }

    private static object ValueSafeConverter(object value)
    {
      try
      {
        if (Convert.GetTypeCode(value) != TypeCode.Object)
          return value;
        else
          return value.ToString();
      }
      catch
      {
        return null;
      }
    }
  }
}
