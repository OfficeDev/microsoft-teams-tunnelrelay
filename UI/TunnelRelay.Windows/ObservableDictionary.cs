// <copyright file="ObservableDictionary.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;

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

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets an <see cref="ICollection" /> containing the keys of the <see cref="IDictionary" />.
        /// </summary>
        public ICollection<TKey> Keys => this.dictionary.Keys;

        /// <summary>
        /// Gets an <see cref="ICollection" /> containing the values in the <see cref="IDictionary" />.
        /// </summary>
        public ICollection<TValue> Values => this.dictionary.Values;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ICollection" />.
        /// </summary>
        public int Count => this.dictionary.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="ICollection" /> is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the value with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Value for the specified key.</returns>
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

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(TKey key, TValue value)
        {
            this.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        /// <summary>
        /// Adds an item to the <see cref="ICollection" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="ICollection" />.</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            (this.dictionary as ICollection<KeyValuePair<TKey, TValue>>).Add(item);
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, AsList(item)));

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Keys)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Values)));
        }

        /// <summary>
        /// Removes all items from the <see cref="ICollection" />.
        /// </summary>
        public void Clear()
        {
            this.dictionary.Clear();
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Keys)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Values)));
        }

        /// <summary>
        /// Determines whether the <see cref="ICollection" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="ICollection" />.</param>
        /// <returns>
        /// true if <paramref name="item" /> is found in the <see cref="ICollection" />; otherwise, false.
        /// </returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.dictionary.Contains(item);
        }

        /// <summary>
        /// Determines whether the <see cref="IDictionary" /> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="IDictionary" />.</param>
        /// <returns>
        /// true if the <see cref="IDictionary" /> contains an element with the key; otherwise, false.
        /// </returns>
        public bool ContainsKey(TKey key)
        {
            return this.dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Copies the elements of the <see cref="ICollection" /> to an <see cref="Array" />, starting at a particular <see cref="Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array" /> that is the destination of the elements copied from <see cref="ICollection" />. The <see cref="Array" /> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            (this.dictionary as ICollection<KeyValuePair<TKey, TValue>>).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="IDictionary" />.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key" /> was not found in the original <see cref="IDictionary" />.
        /// </returns>
        public bool Remove(TKey key)
        {
            if (this.dictionary.ContainsKey(key))
            {
                return this.Remove((this.dictionary as ICollection<KeyValuePair<TKey, TValue>>).First(item => item.Key.Equals(key)));
            }

            return false;
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="ICollection" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="ICollection" />.</param>
        /// <returns>
        /// true if <paramref name="item" /> was successfully removed from the <see cref="ICollection" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="ICollection" />.
        /// </returns>
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

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        /// <returns>
        /// true if the object that implements <see cref="IDictionary" /> contains an element with the specified key; otherwise, false.
        /// </returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this.dictionary as IEnumerable).GetEnumerator();
        }

        /// <summary>
        /// Returns the item as a list.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>List containing item.</returns>
        private static List<KeyValuePair<TKey, TValue>> AsList(KeyValuePair<TKey, TValue> item)
        {
            return new List<KeyValuePair<TKey, TValue>>() { item };
        }
    }
}
