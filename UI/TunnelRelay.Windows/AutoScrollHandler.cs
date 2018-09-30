// <copyright file="AutoScrollHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// Contents copied from https://social.msdn.microsoft.com/Forums/vstudio/en-US/0f524459-b14e-4f9a-8264-267953418a2d/trivial-listboxlistview-autoscroll?forum=wpf
namespace System.Windows.Workarounds
{
    using System.Collections;
    using System.Collections.Specialized;
    using System.Windows.Data;

    /// <summary>
    /// Auto scroll handler.
    /// </summary>
    /// <seealso cref="System.Windows.DependencyObject" />
    /// <seealso cref="System.IDisposable" />
    public class AutoScrollHandler : DependencyObject, IDisposable
    {
        /// <summary>
        /// The items source property.
        /// </summary>
        private static readonly DependencyProperty ItemsSourcePropertyValue =
            DependencyProperty.Register(
                "ItemsSource",
                typeof(IEnumerable),
                typeof(AutoScrollHandler),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.None,
                    new PropertyChangedCallback(ItemsSourcePropertyChanged)));

        /// <summary>
        /// The target.
        /// </summary>
        private System.Windows.Controls.ListBox target;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoScrollHandler"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public AutoScrollHandler(System.Windows.Controls.ListBox target)
        {
            this.target = target;
            Binding b = new Binding("ItemsSource");
            b.Source = this.target;
            BindingOperations.SetBinding(this, ItemsSourcePropertyValue, b);
        }

        /// <summary>
        /// Gets the items source property.
        /// </summary>
        /// <value>
        /// The items source property.
        /// </value>
        public static DependencyProperty ItemsSourceProperty => ItemsSourcePropertyValue;

        /// <summary>
        /// Gets or sets the items source.
        /// </summary>
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)this.GetValue(ItemsSourceProperty); }
            set { this.SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            BindingOperations.ClearBinding(this, ItemsSourceProperty);
        }

        /// <summary>
        /// Itemses the source property changed.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ItemsSourcePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((AutoScrollHandler)o).ItemsSourceChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
        }

        /// <summary>
        /// Itemses the source changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        private void ItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            INotifyCollectionChanged collection = oldValue as INotifyCollectionChanged;
            if (collection != null)
            {
                collection.CollectionChanged -= new NotifyCollectionChangedEventHandler(this.Collection_CollectionChanged);
            }

            collection = newValue as INotifyCollectionChanged;
            if (collection != null)
            {
                collection.CollectionChanged += new NotifyCollectionChangedEventHandler(this.Collection_CollectionChanged);
            }
        }

        /// <summary>
        /// Handles the CollectionChanged event of the Collection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null || e.NewItems.Count < 1)
            {
                return;
            }

            this.target.ScrollIntoView(e.NewItems[e.NewItems.Count - 1]);
        }
    }
}