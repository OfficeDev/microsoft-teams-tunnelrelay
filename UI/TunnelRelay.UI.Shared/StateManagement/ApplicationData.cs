// <copyright file="ApplicationData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.StateManagement
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Application data.
    /// </summary>
    internal class ApplicationData
    {
        /// <summary>
        /// Current version of the config.
        /// </summary>
        internal const int CurrentVersion = 2;

        /// <summary>
        /// Gets or sets the relay shared key bytes.
        /// These can be encrypted if <see cref="EnableCredentialEncryption"/> is set to true.
        /// </summary>
        [JsonProperty(PropertyName = "relaySharedKey")]
        private byte[] relaySharedKeyBytes;

        /// <summary>
        /// Gets or sets the redirection URL.
        /// </summary>
        [JsonProperty(PropertyName = "redirectionUrl")]
        public string RedirectionUrl { get; set; }

        /// <summary>
        /// Gets or sets the hybrid connection URL.
        /// </summary>
        [JsonProperty(PropertyName = "hybridConnectionUrl")]
        public string HybridConnectionUrl { get; set; }

        /// <summary>
        /// Gets or sets the hybrid connection name.
        /// </summary>
        [JsonProperty(PropertyName = "hybridConnectionName")]
        public string HybridConnectionName { get; set; }

        /// <summary>
        /// Gets or sets the name of the hybrid connection key.
        /// </summary>
        [JsonProperty(PropertyName = "hybridConnectionKeyName")]
        public string HybridConnectionKeyName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether encryption of credentials is enabled or not.
        /// </summary>
        [JsonProperty(PropertyName = "enableCredentialEncryption")]
        public bool EnableCredentialEncryption { get; set; } = true;

        /// <summary>
        /// Gets or sets the hybrid connection shared key.
        /// </summary>
        [JsonIgnore]
        public string HybridConnectionSharedKey
        {
            get
            {
                if (this.EnableCredentialEncryption)
                {
                    return Convert.ToBase64String(DataProtection.Unprotect(this.relaySharedKeyBytes));
                }
                else
                {
                    return Convert.ToBase64String(this.relaySharedKeyBytes);
                }
            }

            set
            {
                if (this.EnableCredentialEncryption)
                {
                    this.relaySharedKeyBytes = DataProtection.Protect(Convert.FromBase64String(value));
                }
                else
                {
                    this.relaySharedKeyBytes = Convert.FromBase64String(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of enabled plugins.
        /// </summary>
        [JsonProperty(PropertyName = "enabledPlugins")]
        public HashSet<string> EnabledPlugins { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the plugin settings map.
        /// </summary>
        [JsonProperty(PropertyName = "pluginSettingsMap")]
        public Dictionary<string, Dictionary<string, string>> PluginSettingsMap { get; set; } = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Gets the version of the config.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int Version { get; private set; }

        /// <summary>
        /// Imports the settings.
        /// </summary>
        /// <param name="serializedSettings">The serialized settings.</param>
        /// <returns>Application data.</returns>
        public static ApplicationData ImportSettings(string serializedSettings)
        {
            ApplicationData applicationData = JsonConvert.DeserializeObject<ApplicationData>(serializedSettings);

            // Encrypt the data with DPAPI.
            if (applicationData.EnableCredentialEncryption)
            {
                applicationData.relaySharedKeyBytes = DataProtection.Protect(applicationData.relaySharedKeyBytes);
            }

            return applicationData;
        }

        /// <summary>
        /// Gets the exported settings.
        /// </summary>
        /// <returns>Settings which can be moved in between machines.</returns>
        public string ExportSettings()
        {
            // Clone the current object into a new one.
            ApplicationData applicationData = JObject.FromObject(this).ToObject<ApplicationData>();

            // Unprotect the data before exporting.
            applicationData.relaySharedKeyBytes = DataProtection.Unprotect(applicationData.relaySharedKeyBytes);

            return JsonConvert.SerializeObject(applicationData);
        }

        /// <summary>
        /// Called after deserialization is done.
        /// </summary>
        /// <param name="context">Deserialization context.</param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (this.Version < CurrentVersion)
            {
                this.HybridConnectionKeyName = null;
                this.relaySharedKeyBytes = null;
                this.EnableCredentialEncryption = true;
                this.HybridConnectionUrl = null;
            }

            // This allows repackaging the logged in TunnelRelay internally to others.
            if (string.IsNullOrEmpty(this.HybridConnectionName))
            {
                this.HybridConnectionName = Environment.MachineName;
            }
        }

        /// <summary>
        /// Called during serialization.
        /// </summary>
        /// <param name="context">Serialzation context.</param>
        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            this.Version = CurrentVersion;
        }
    }
}
