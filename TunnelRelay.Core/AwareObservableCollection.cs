// <copyright file="AwareObservableCollection.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
// Licensed under the MIT license.
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace TunnelRelay
{
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;

    /// <summary>
    /// Highly aware observable collection that also reacts to changes in its components' properties
    /// </summary>
    /// <typeparam name="T">Type of param.</typeparam>
    /// <seealso cref="System.Collections.ObjectModel.ObservableCollection{T}" />
    public class AwareObservableCollection<T> : ObservableCollection<T>
        where T : INotifyPropertyChanged
    {
        /// <summary>
        /// The locker object.
        /// </summary>
        private object lockerObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="AwareObservableCollection{T}"/> class.
        /// </summary>
        public AwareObservableCollection()
            : base()
        {
            this.CollectionChanged += this.ObservableCollectionEx_CollectionChanged;
        }

        /// <summary>
        /// Inserts an element into the <see cref="T:System.Collections.ObjectModel.Collection`1" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert. The value can be null for reference types.</param>
        public new void Insert(int index, T item)
        {
            lock (this.lockerObject)
            {
                base.Insert(index, item);
            }
        }

        /// <summary>
        /// Handles the CollectionChanged event of the ObservableCollectionEx control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void ObservableCollectionEx_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (T item in e.OldItems)
                {
                    // Removed items
                    item.PropertyChanged -= this.EntityViewModelPropertyChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (T item in e.NewItems)
                {
                    // Added items
                    item.PropertyChanged += this.EntityViewModelPropertyChanged;
                }
            }
        }

        /// <summary>
        /// Fires when view model property is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void EntityViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            lock (this.lockerObject)
            {
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}
