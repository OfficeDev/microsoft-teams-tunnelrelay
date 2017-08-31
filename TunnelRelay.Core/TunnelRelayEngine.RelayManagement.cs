// <copyright file="TunnelRelayEngine.RelayManagement.cs" company="Microsoft">
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
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Security;
    using System.Reflection;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus;
    using Newtonsoft.Json;
    using TunnelRelay.PluginEngine;
    using TunnelRelay.Plugins;

    /// <summary>
    /// Application engine or serving all critical operation.
    /// </summary>
    public partial class TunnelRelayEngine
    {
        /// <summary>
        /// Initializes static members of the <see cref="TunnelRelayEngine"/> class.
        /// </summary>
        static TunnelRelayEngine()
        {
            ////Requests = new AwareObservableCollection<RequestDetails>();
            Plugins = new ObservableCollection<PluginDetails>();
            var pluginInstances = new List<IRedirectionPlugin>();
            pluginInstances.Add(new HeaderAdditionPlugin());
            pluginInstances.Add(new HeaderRemovalPlugin());
            string pluginDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Plugins");

            if (Directory.Exists(pluginDirectory))
            {
                Directory.EnumerateFiles(pluginDirectory, "*.dll").ToList().ForEach(dll =>
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(dll);
                        foreach (Type pluginType in assembly.GetExportedTypes().Where(type => type.GetInterfaces().Contains(typeof(IRedirectionPlugin))))
                        {
                            Logger.LogInfo(CallInfo.Site(), "Loading plugin '{0}'", pluginType.FullName);
                            pluginInstances.Add(Activator.CreateInstance(pluginType) as IRedirectionPlugin);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(CallInfo.Site(), ex);
                    }
                });
            }

            pluginInstances.ForEach(plugin =>
            {
                PluginDetails pluginDetails = new PluginDetails
                {
                    PluginInstance = plugin,
                    PluginSettings = new ObservableCollection<PluginSettingDetails>(),
                    IsEnabled = ApplicationData.Instance.EnabledPlugins.Contains(plugin.GetType().FullName),
                };

                try
                {
                    var settingProperties = plugin.GetType().GetProperties().Where(memberInfo => memberInfo.GetCustomAttribute(typeof(PluginSetting)) != null);

                    foreach (var setting in settingProperties)
                    {
                        if (!setting.PropertyType.IsEquivalentTo(typeof(string)))
                        {
                            throw new InvalidDataException("Plugin settings can only be of string datatype");
                        }

                        var pluginSetting = new PluginSettingDetails
                        {
                            AttributeData = setting.GetCustomAttribute<PluginSetting>(),
                            PluginInstance = plugin,
                            PropertyDetails = setting,
                        };

                        if (ApplicationData.Instance.PluginSettingsMap.TryGetValue(pluginDetails.PluginInstance.GetType().FullName, out Dictionary<string, string> pluginSettingsVal))
                        {
                            if (pluginSettingsVal.TryGetValue(pluginSetting.PropertyDetails.Name, out string propertyValue))
                            {
                                pluginSetting.Value = propertyValue;
                            }
                        }

                        pluginDetails.PluginSettings.Add(pluginSetting);
                    }

                    if (pluginDetails.IsEnabled)
                    {
                        pluginDetails.InitializePlugin();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(CallInfo.Site(), ex);
                }

                Plugins.Add(pluginDetails);
            });
        }

        /// <summary>
        /// Gets the plugins.
        /// </summary>
        public static ObservableCollection<PluginDetails> Plugins { get; internal set; }

        /// <summary>
        /// Gets or sets the service host.
        /// </summary>
        internal static WebServiceHost ServiceHost { get; set; }

        /// <summary>
        /// Stops the azure relay engine.
        /// </summary>
        public static void StopTunnelRelayEngine()
        {
            Logger.LogInfo(CallInfo.Site(), "Shutting down tunnel relay engine");

            if (TunnelRelayEngine.ServiceHost != null)
            {
                // Serialize settings back to json.
                File.WriteAllText("appSettings.json", JsonConvert.SerializeObject(ApplicationData.Instance, Formatting.Indented));

                // Shutdown the Relay.
                TunnelRelayEngine.ServiceHost.Close();
            }
        }

        /// <summary>
        /// Starts the azure relay engine.
        /// </summary>
        public static void StartTunnelRelayEngine()
        {
            try
            {
                ServiceHost = new WebServiceHost(typeof(WCFContract));
                ServiceHost.AddServiceEndpoint(
                    typeof(WCFContract),
                    new WebHttpRelayBinding(
                        EndToEndWebHttpSecurityMode.Transport,
                        RelayClientAuthenticationType.None),
                    ApplicationData.Instance.ProxyBaseUrl)
                .EndpointBehaviors.Add(
                    new TransportClientEndpointBehavior(
                        TokenProvider.CreateSharedAccessSignatureTokenProvider(
                            ApplicationData.Instance.ServiceBusKeyName,
                            ApplicationData.Instance.ServiceBusSharedKey)));

                ServiceHost.Open();

                // Ignore all HTTPs cert errors. We wanna do this after the call to Azure is made so that if Azure call presents wrong cert we bail out.
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((sender, cert, chain, errs) => true);
            }
            catch (Exception ex)
            {
                Logger.LogError(CallInfo.Site(), ex, "Failed to start Service host");
                throw;
            }
        }
    }
}
