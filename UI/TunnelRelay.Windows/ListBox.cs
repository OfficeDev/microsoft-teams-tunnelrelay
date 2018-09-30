// <copyright file="ListBox.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// Contents copied from https://social.msdn.microsoft.com/Forums/vstudio/en-US/0f524459-b14e-4f9a-8264-267953418a2d/trivial-listboxlistview-autoscroll?forum=wpf
namespace System.Windows.Workarounds
{
    /// <summary>
    /// Extended auto scoll for list box.
    /// </summary>
    public static class ListBox
    {
        /// <summary>
        /// The automatic scroll handler property.
        /// </summary>
        public static readonly DependencyProperty AutoScrollHandlerProperty =
            DependencyProperty.RegisterAttached("AutoScrollHandler", typeof(AutoScrollHandler), typeof(System.Windows.Controls.ListBox));

        /// <summary>
        /// The automatic scroll property.
        /// </summary>
        private static readonly DependencyProperty AutoScrollPropertyValue =
            DependencyProperty.RegisterAttached(
                "AutoScroll",
                typeof(bool),
                typeof(System.Windows.Controls.ListBox),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets the automatic scroll property.
        /// </summary>
        /// <value>
        /// The automatic scroll property.
        /// </value>
        public static DependencyProperty AutoScrollProperty => AutoScrollPropertyValue;

        /// <summary>
        /// Gets the automatic scroll.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>true is auto scoll is enabled false otherwise.</returns>
        public static bool GetAutoScroll(System.Windows.Controls.ListBox instance)
        {
            return (bool)instance.GetValue(AutoScrollProperty);
        }

        /// <summary>
        /// Sets the automatic scroll.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="value">Value to set for Automatic scroll.</param>
        public static void SetAutoScroll(System.Windows.Controls.ListBox instance, bool value)
        {
            AutoScrollHandler oldHandler = (AutoScrollHandler)instance.GetValue(AutoScrollHandlerProperty);
            if (oldHandler != null)
            {
                oldHandler.Dispose();
                instance.SetValue(AutoScrollHandlerProperty, null);
            }

            instance.SetValue(AutoScrollProperty, value);
            if (value)
            {
                instance.SetValue(AutoScrollHandlerProperty, new AutoScrollHandler(instance));
            }
        }
    }
}