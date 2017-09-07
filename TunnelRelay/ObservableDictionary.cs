using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TunnelRelay
{
    /// <summary>
    /// Provides a dictionary for use with data binding.
    /// </summary>
    /// <typeparam name="TKey">Specifies the type of the keys in this collection.</typeparam>
    /// <typeparam name="TValue">Specifies the type of the values in this collection.</typeparam>
    [DebuggerDisplay("Count={Count}")]
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        /// <summary>
        /// Internal storage instance.
        /// </summary>
        private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        public TValue this[TKey key]
        {
            get => this.dictionary[key];
            set
            {
                bool exists = this.dictionary.ContainsKey(key);
                KeyValuePair<TKey, TValue> oldItem = default(KeyValuePair<TKey, TValue>);
                int oldIndex = 0;
                if (exists)
                {
                    oldItem = (this.dictionary as ICollection<KeyValuePair<TKey, TValue>>).First(item => item.Key.Equals(key));

                    oldIndex = this.dictionary.ToList().IndexOf(oldItem);
                }

                this.dictionary[key] = value;

                KeyValuePair<TKey, TValue> newItem = (this.dictionary as ICollection<KeyValuePair<TKey, TValue>>).First(item => item.Key.Equals(key));

                if (exists)
                {
                    var eventData = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, oldIndex);
                    this.CollectionChanged?.Invoke(this, eventData);
                }
                else
                {
                    this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, AsList(newItem)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Keys)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                }

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Values)));
            }
        }

        public ICollection<TKey> Keys => this.dictionary.Keys;

        public ICollection<TValue> Values => this.dictionary.Values;

        public int Count => this.dictionary.Count;

        public bool IsReadOnly => false;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(TKey key, TValue value)
        {
            this.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            (this.dictionary as ICollection<KeyValuePair<TKey, TValue>>).Add(item);
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, AsList(item)));

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Keys)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Values)));
        }

        public void Clear()
        {
            this.dictionary.Clear();
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Keys)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Values)));
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.dictionary.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return this.dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            (this.dictionary as ICollection<KeyValuePair<TKey, TValue>>).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            if (this.dictionary.ContainsKey(key))
            {
                return this.Remove((this.dictionary as ICollection<KeyValuePair<TKey, TValue>>).First(item => item.Key.Equals(key)));
            }

            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool result = (this.dictionary as ICollection<KeyValuePair<TKey, TValue>>).Remove(item);

            if (result)
            {
                this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, AsList(item)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Keys)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Values)));
            }

            return result;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this.dictionary as IEnumerable).GetEnumerator();
        }

        private static List<KeyValuePair<TKey, TValue>> AsList(KeyValuePair<TKey, TValue> item)
        {
            return new List<KeyValuePair<TKey, TValue>>() { item };
        }
    }
}
