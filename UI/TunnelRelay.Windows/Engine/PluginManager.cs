// <copyright file="PluginManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Extensions.Logging;
    using TunnelRelay.PluginEngine;
    using TunnelRelay.Plugins;

    /// <summary>
    /// Plugin manager.
    /// </summary>
    internal class PluginManager
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private ILogger<PluginManager> logger = LoggingHelper.GetLogger<PluginManager>();

        /// <summary>
        /// Initializes the plugins.
        /// </summary>
        /// <returns>Initialized plugin list.</returns>
        public ObservableCollection<PluginDetails> InitializePlugins()
        {
            List<ITunnelRelayPlugin> pluginInstances = new List<ITunnelRelayPlugin>();
            ObservableCollection<PluginDetails> plugins = new ObservableCollection<PluginDetails>();
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
                        foreach (Type pluginType in assembly.GetExportedTypes().Where(type => type.GetInterfaces().Contains(typeof(ITunnelRelayPlugin))))
                        {
                            this.logger.LogInformation("Loading plugin '{0}'", pluginType.FullName);
                            pluginInstances.Add(Activator.CreateInstance(pluginType) as ITunnelRelayPlugin);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "Plugin discovery hit an error!");
                    }
                });
            }

            pluginInstances.ForEach(plugin =>
            {
                PluginDetails pluginDetails = new PluginDetails
                {
                    PluginInstance = plugin,
                    PluginSettings = new ObservableCollection<PluginSettingDetails>(),
                    IsEnabled = TunnelRelayStateManager.ApplicationData.EnabledPlugins.Contains(plugin.GetType().FullName),
                };

                try
                {
                    var settingProperties = plugin.GetType().GetProperties().Where(memberInfo => memberInfo.GetCustomAttribute(typeof(PluginSettingAttribute)) != null);

                    foreach (var setting in settingProperties)
                    {
                        if (!setting.PropertyType.IsEquivalentTo(typeof(string)))
                        {
                            throw new InvalidDataException("Plugin settings can only be of string datatype");
                        }

                        var pluginSetting = new PluginSettingDetails
                        {
                            AttributeData = setting.GetCustomAttribute<PluginSettingAttribute>(),
                            PluginInstance = plugin,
                            PropertyDetails = setting,
                        };

                        if (TunnelRelayStateManager.ApplicationData.PluginSettingsMap.TryGetValue(pluginDetails.PluginInstance.GetType().FullName, out Dictionary<string, string> pluginSettingsVal))
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
                    this.logger.LogError(ex, "Plugin init failed");
                }

                plugins.Add(pluginDetails);
            });

            return plugins;
        }
    }
}
