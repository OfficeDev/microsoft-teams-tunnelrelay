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

namespace TunnelRelay.Core
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
        /// Gets or sets the service bus shared key encrypted.
        /// </summary>
        [JsonProperty(PropertyName = "ServiceBusSharedKey")]
        private byte[] serviceBusSharedKeyBytes;

        /// <summary>
        /// Initializes static members of the <see cref="ApplicationData"/> class.
        /// </summary>
        static ApplicationData()
        {
            if (File.Exists("appSettings.json"))
            {
                Logger.LogInfo(CallInfo.Site(), "Loading existing settings");
                Instance = JsonConvert.DeserializeObject<ApplicationData>(File.ReadAllText("appSettings.json"));
            }
            else
            {
                Logger.LogInfo(CallInfo.Site(), "Appsettings don't exist. Creating new one.");
                Instance = new ApplicationData
                {
                    RedirectionUrl = "http://localhost:3979/",
                };
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationData"/> class.
        /// </summary>
        public ApplicationData()
        {
            this.EnabledPlugins = new HashSet<string>();
            this.PluginSettingsMap = new Dictionary<string, Dictionary<string, string>>();
        }

        /// <summary>
        /// Gets the application data instance.
        /// </summary>
        [JsonIgnore]
        public static ApplicationData Instance { get; internal set; }

        /// <summary>
        /// Gets or sets the redirection URL.
        /// </summary>
        [JsonProperty(PropertyName = "RedirectionUrl")]
        public string RedirectionUrl { get; set; }

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
        /// Gets or sets the service bus URL.
        /// </summary>
        [JsonProperty(PropertyName = "ServiceBusUrl")]
        public string ServiceBusUrl { get; set; }

        /// <summary>
        /// Sets the name of the service bus key.
        /// </summary>
        [JsonProperty(PropertyName = "ServiceBusKeyName")]
        public string ServiceBusKeyName { internal get; set; }

        /// <summary>
        /// Sets the service bus shared key.
        /// </summary>
        [JsonIgnore]
        public string ServiceBusSharedKey
        {
            internal get
            {
                return Convert.ToBase64String(DataProtection.Unprotect(this.serviceBusSharedKeyBytes));
            }

            set
            {
                this.serviceBusSharedKeyBytes = DataProtection.Protect(Convert.FromBase64String(value));
            }
        }

        /// <summary>
        /// Gets or sets the list of enabled plugins.
        /// </summary>
        [JsonProperty(PropertyName = "EnabledPlugins")]
        internal HashSet<string> EnabledPlugins { get; set; }

        /// <summary>
        /// Gets or sets the plugin settings map.
        /// </summary>
        [JsonProperty(PropertyName = "PluginSettingsMap")]
        internal Dictionary<string, Dictionary<string, string>> PluginSettingsMap { get; set; }

        /// <summary>
        /// Logouts this instance.
        /// </summary>
        public static void Logout()
        {
            Logger.LogInfo(CallInfo.Site(), "Logging out");
            Instance = new ApplicationData();
        }

        /// <summary>
        /// Gets the exported settings.
        /// </summary>
        /// <returns>Settings which can be moved in between machines.</returns>
        public static string GetExportedSettings()
        {
            // Clone the current object into a new one.
            ApplicationData applicationData = JObject.FromObject(ApplicationData.Instance).ToObject<ApplicationData>();

            // Unprotect the data before exporting.
            applicationData.serviceBusSharedKeyBytes = DataProtection.Unprotect(applicationData.serviceBusSharedKeyBytes);

            return JsonConvert.SerializeObject(applicationData);
        }

        /// <summary>
        /// Imports the settings.
        /// </summary>
        /// <param name="serializedSettings">The serialized settings.</param>
        public static void ImportSettings(string serializedSettings)
        {
            ApplicationData applicationData = JsonConvert.DeserializeObject<ApplicationData>(serializedSettings);

            // Encrypt the data with DPAPI.
            applicationData.serviceBusSharedKeyBytes = DataProtection.Protect(applicationData.serviceBusSharedKeyBytes);

            ApplicationData.Instance = applicationData;
        }
    }
}
