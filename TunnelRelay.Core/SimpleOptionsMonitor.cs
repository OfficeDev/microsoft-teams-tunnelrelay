// <copyright file="SimpleOptionsMonitor.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Simple implementation of <see cref="IOptions{TOptions}"/> which works without dependency injection.
    /// </summary>
    /// <typeparam name="T">Type of options.</typeparam>
    public class SimpleOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly List<Action<T, string>> listeners = new List<Action<T, string>>();

        private T currentValue;

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public T CurrentValue
        {
            get
            {
                return this.currentValue;
            }

            set
            {
                this.listeners.ForEach(listener =>
                {
                    try
                    {
                        listener.Invoke(value, string.Empty);
                    }
                    catch (Exception)
                    {
                    }
                });

                this.currentValue = value;
            }
        }

        /// <summary>
        /// Gets the current instance.
        /// </summary>
        /// <param name="name">Default name. Is not used.</param>
        /// <returns>Current instance.</returns>
        public T Get(string name)
        {
            return this.currentValue;
        }

        /// <summary>
        /// Register a callback to be executed when instance is changed.
        /// </summary>
        /// <param name="listener">Callback to execute when instance is changed.</param>
        /// <returns>IDisposable instance with nothing to dispose.</returns>
        public IDisposable OnChange(Action<T, string> listener)
        {
            this.listeners.Add(listener);
            return new NullDisposable();
        }

        private class NullDisposable : IDisposable
        {
            /// <summary>
            /// Empty implementations of dispose.
            /// </summary>
            public void Dispose()
            {
            }
        }
    }
}
