// <copyright file="RequestDetails.cs" company="Microsoft">
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
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Net;

    /// <summary>
    /// Request Details for UI.
    /// </summary>
    public class RequestDetails : INotifyPropertyChanged
    {
        private string statusCode;

        private bool exceptionHit;

        private string requestData;

        private string responseData;

        private string duration;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDetails"/> class.
        /// </summary>
        public RequestDetails()
        {
            this.ResponseHeaders = new ObservableCollection<HeaderDetails>();
            this.ResponseHeaders.CollectionChanged += this.ResponseHeaders_CollectionChanged;
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the HTTP method.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        public string StatusCode
        {
            get
            {
                return this.statusCode;
            }

            internal set
            {
                this.statusCode = value;
                this.OnPropertyChanged(nameof(this.StatusCode));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether exception was hit during exeution.
        /// </summary>
        public bool ExceptionHit
        {
            get
            {
                return this.exceptionHit;
            }

            internal set
            {
                this.exceptionHit = value;
                this.OnPropertyChanged(nameof(this.ExceptionHit));
            }
        }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the request receive time.
        /// </summary>
        public DateTime RequestReceiveTime { get; set; }

        /// <summary>
        /// Gets or sets the request headers.
        /// </summary>
        public ObservableCollection<HeaderDetails> RequestHeaders { get; set; }

        /// <summary>
        /// Gets or sets the request data.
        /// </summary>
        public string RequestData
        {
            get
            {
                return this.requestData;
            }

            set
            {
                this.requestData = value;
                this.OnPropertyChanged(nameof(this.RequestData));
            }
        }

        /// <summary>
        /// Gets or sets the request sender.
        /// </summary>
        public string RequestSender { get; set; }

        /// <summary>
        /// Gets or sets the response headers.
        /// </summary>
        public ObservableCollection<HeaderDetails> ResponseHeaders { get; set; }

        /// <summary>
        /// Gets or sets the response data.
        /// </summary>
        public string ResponseData
        {
            get
            {
                return this.responseData;
            }

            set
            {
                this.responseData = value;
                this.OnPropertyChanged(nameof(this.ResponseData));
            }
        }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        public string Duration
        {
            get
            {
                return this.duration;
            }

            set
            {
                this.duration = value;
                this.OnPropertyChanged(this.Duration);
            }
        }

        /// <summary>
        /// Called when property changed.
        /// </summary>
        /// <param name="name">The name.</param>
        protected void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Handles the CollectionChanged event of the ResponseHeaders control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void ResponseHeaders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(this.ResponseHeaders));
        }
    }
}
