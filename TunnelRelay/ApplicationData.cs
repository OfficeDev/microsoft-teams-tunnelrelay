// <copyright file="ApplicationData.cs" company="Microsoft">
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
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Application data.
    /// </summary>
    public class ApplicationData
    {
        /// <summary>
        /// Initializes static members of the <see cref="ApplicationData"/> class.
        /// </summary>
        static ApplicationData()
        {
            if (File.Exists("appSettings.json"))
            {
                Instance = JsonConvert.DeserializeObject<ApplicationData>(File.ReadAllText("appSettings.json"));
            }
            else
            {
                Instance = new ApplicationData
                {
                    RedirectionUrl = "https://localhost/",
                };
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationData"/> class.
        /// </summary>
        public ApplicationData()
        {
            this.EnabledPlugins = new List<string>();
        }

        /// <summary>
        /// Gets the application data instance.
        /// </summary>
        [JsonIgnore]
        public static ApplicationData Instance { get; internal set; }

        /// <summary>
        /// Gets the redirection URL.
        /// </summary>
        [JsonProperty(PropertyName = "RedirectionUrl")]
        public string RedirectionUrl { get; internal set; }

        /// <summary>
        /// Gets the proxy base URL.
        /// </summary>
        [JsonIgnore]
        public string ProxyBaseUrl
        {
            get
            {
                return this.ServiceBusUrl + Environment.MachineName;
            }
        }

        /// <summary>
        /// Gets the overflow properties. Can be used to store plugin data.
        /// </summary>
        [JsonExtensionData(ReadData = true, WriteData = true)]
        public Dictionary<string, JToken> Properties { get; internal set; }

        /// <summary>
        /// Gets or sets the service bus URL.
        /// </summary>
        [JsonProperty(PropertyName = "ServiceBusUrl")]
        internal string ServiceBusUrl { get; set; }

        /// <summary>
        /// Gets or sets the name of the service bus key.
        /// </summary>
        [JsonProperty(PropertyName = "ServiceBusKeyName")]
        internal string ServiceBusKeyName { get; set; }

        /// <summary>
        /// Gets or sets the service bus shared key.
        /// </summary>
        [JsonProperty(PropertyName = "ServiceBusSharedKey")]
        internal string ServiceBusSharedKey { get; set; }

        /// <summary>
        /// Gets or sets the list of enabled plugins.
        /// </summary>
        [JsonProperty(PropertyName = "EnabledPlugins")]
        internal List<string> EnabledPlugins { get; set; }

        /// <summary>
        /// Stores the application data. Can be used to store plugin data.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        public static void StoreApplicationData(string keyName, string value)
        {
            Instance.Properties[keyName] = value;
        }
    }
}
