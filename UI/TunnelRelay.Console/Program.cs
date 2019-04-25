// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Console
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.Relay.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
    using Microsoft.Extensions.CommandLineUtils;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using TunnelRelay.Core;
    using TunnelRelay.PluginEngine;
    using TunnelRelay.UI.Logger;
    using TunnelRelay.UI.PluginManagement;
    using TunnelRelay.UI.ResourceManagement;
    using TunnelRelay.UI.StateManagement;

    /// <summary>
    /// Program execution class.
    /// </summary>
    public sealed class Program
    {
        /// <summary>
        /// File name where settings are stored.
        /// </summary>
        private const string SettingsFileName = "appSettings.json";

        /// <summary>
        /// Path to the configuration file.
        /// </summary>
        private static readonly string ConfigurationFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Program.SettingsFileName);

        /// <summary>
        /// Program invocation point.
        /// </summary>
        /// <param name="args">Commandline arguments.</param>
        /// <returns>Process return code.</returns>
        public static int Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication(false);
            CommandOption serviceBusUrlOption = commandLineApplication.Option(
                "-BusUrl | --ServiceBusUrl",
                "Url for the Service bus you want to use. This should be in format sbname.servicebus.windows.net",
                CommandOptionType.SingleValue);

            CommandOption serviceBusSharedKeyNameOption = commandLineApplication.Option(
                "-KeyName | --ServiceBusKeyName",
                "Name of the shared key. For example RootManageSharedAccessKey",
                CommandOptionType.SingleValue);

            CommandOption serviceBusSharedKeyOption = commandLineApplication.Option(
                "-Key | --ServiceBusKey",
                "Shared access key. This key should have Manage, Send and Listen permissions",
                CommandOptionType.SingleValue);

            CommandOption connectionNameOption = commandLineApplication.Option(
                "-Name | --ConnectionName",
                "Unique hybrid connection name, This connection should already be created.",
                CommandOptionType.SingleValue);

            CommandOption serviceAddressOption = commandLineApplication.Option(
                "-Address | --ServiceAddress",
                "Endpoint to route requests to. Example http://localhost:4200",
                CommandOptionType.SingleValue);

            CommandOption deleteConfigurationOption = commandLineApplication.Option(
                "-DeleteConfig | --DeleteConfiguration",
                "Deletes the configuration and exits the app. You can relaunch to go through configuration again.",
                CommandOptionType.NoValue);

            CommandOption disableLoggingOption = commandLineApplication.Option(
                "-NoLog | --DisableLogging",
                "Disables logging for the application. If you going to run this for a very long time, it might be better to disable logging to not overrun disk space.",
                CommandOptionType.NoValue);

            commandLineApplication.HelpOption("-h|--help|-?");

            commandLineApplication.ExtendedHelpText = "Commandline arguments takes preference over saved config. If no command line argument is specified" +
                "Config file will be loaded. If no config file is found, application will go through interactive configuration process.";

            commandLineApplication.OnExecute(async () =>
            {
                if (deleteConfigurationOption.HasValue())
                {
                    if (File.Exists(Program.ConfigurationFilePath))
                    {
                        File.Delete(Program.ConfigurationFilePath);
                        return 0;
                    }
                }

                ServiceCollection serviceDescriptors = new ServiceCollection();

                serviceDescriptors.AddLogging(loggingBuilder =>
                {
                    if (!disableLoggingOption.HasValue())
                    {
                        loggingBuilder.Services.Configure<FileLoggerProviderOptions>((fileLoggerOptions) =>
                        {
                            fileLoggerOptions.FileName = "TunnelRelayConsole.log";
                        });

                        loggingBuilder.AddFileLogger();
                    }
                });

                serviceDescriptors.AddSingleton<UserAuthenticator>();
                serviceDescriptors.AddSingleton<ServiceBusResourceManager>();

                ApplicationData applicationData = null;

                // If command line arguments were specified.
                if (!string.IsNullOrEmpty(serviceBusUrlOption.Value()))
                {
                    bool paramsPresent = true;

                    if (string.IsNullOrEmpty(serviceBusSharedKeyNameOption.Value()))
                    {
                        Console.Error.WriteLine("Missing required Service Bus shared key name");
                        paramsPresent = false;
                    }

                    if (string.IsNullOrEmpty(serviceBusSharedKeyOption.Value()))
                    {
                        Console.Error.WriteLine("Missing required Service Bus shared key");
                        paramsPresent = false;
                    }

                    if (string.IsNullOrEmpty(connectionNameOption.Value()))
                    {
                        Console.Error.WriteLine("Missing required hybrid connection name");
                        paramsPresent = false;
                    }

                    if (string.IsNullOrEmpty(serviceAddressOption.Value()))
                    {
                        Console.Error.WriteLine("Missing required service url");
                        paramsPresent = false;
                    }

                    if (!paramsPresent)
                    {
                        return -1;
                    }

                    applicationData = new ApplicationData
                    {
                        HybridConnectionKeyName = serviceBusSharedKeyNameOption.Value(),
                        HybridConnectionName = connectionNameOption.Value(),
                        HybridConnectionSharedKey = serviceBusSharedKeyOption.Value(),
                        HybridConnectionUrl = serviceBusUrlOption.Value(),
                        RedirectionUrl = serviceAddressOption.Value(),
                    };
                }
                else if (File.Exists(Program.ConfigurationFilePath))
                {
                    applicationData = JsonConvert.DeserializeObject<ApplicationData>(File.ReadAllText(Program.ConfigurationFilePath));

                    // Discard old settings and run interactive flow again.
                    if (applicationData.Version < ApplicationData.CurrentVersion)
                    {
                        Console.WriteLine("Stored settings are old and cannot be used. Discarding them. Running interactive configuration.");

                        applicationData = await Program.PerformInteractiveConfigurationAsync(
                            serviceDescriptors.BuildServiceProvider(),
                            serviceAddressOption.Value()).ConfigureAwait(false);
                    }
                }
                else
                {
                    applicationData = await Program.PerformInteractiveConfigurationAsync(
                        serviceDescriptors.BuildServiceProvider(),
                        serviceAddressOption.Value()).ConfigureAwait(false);
                }

                serviceDescriptors.AddSingleton(applicationData);

                File.WriteAllText(Program.ConfigurationFilePath, JsonConvert.SerializeObject(applicationData, Formatting.Indented));

                serviceDescriptors.Configure<HybridConnectionManagerOptions>((hybridConnectionOptions) =>
                {
                    hybridConnectionOptions.ConnectionPath = applicationData.HybridConnectionName;
                    hybridConnectionOptions.ServiceBusKeyName = applicationData.HybridConnectionKeyName;
                    hybridConnectionOptions.ServiceBusSharedKey = applicationData.HybridConnectionSharedKey;
                    hybridConnectionOptions.ServiceBusUrlHost = applicationData.HybridConnectionUrl;
                });

                serviceDescriptors.Configure<RelayRequestManagerOptions>((relayRequestManagerOptions) =>
                {
                    relayRequestManagerOptions.InternalServiceUrl = new Uri(applicationData.RedirectionUrl);
                });

                serviceDescriptors.AddSingleton<IRelayRequestEventListener, RelayRequestEventListener>();

                serviceDescriptors.AddSingleton<PluginManager>();

                serviceDescriptors.AddSingleton<IEnumerable<ITunnelRelayPlugin>>((provider) =>
                {
                    PluginManager pluginManager = provider.GetRequiredService<PluginManager>();

                    ObservableCollection<PluginDetails> plugins = pluginManager.InitializePlugins(provider.GetRequiredService<ApplicationData>());

                    return plugins.Where(details => details.IsEnabled).Select(details => details.PluginInstance);
                });

                serviceDescriptors.AddSingleton<IRelayRequestManager, RelayRequestManager>();

                serviceDescriptors.AddSingleton<IHybridConnectionManager, HybridConnectionManager>();

                IServiceProvider serviceProvider = serviceDescriptors.BuildServiceProvider();

                IHybridConnectionManager hybridConnectionManager = serviceProvider.GetRequiredService<IHybridConnectionManager>();

                hybridConnectionManager.InitializeAsync(CancellationToken.None).Wait();

                Console.WriteLine($"Relay is now redirecting requests from {applicationData.HybridConnectionUrl}{applicationData.HybridConnectionName} to {applicationData.RedirectionUrl}");

                Console.CancelKeyPress += (sender, cancelledEvent) =>
                {
                    Console.WriteLine("Closing relay..");
                    hybridConnectionManager.CloseAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                    Console.WriteLine("Relay closed. Exiting.");
                    Environment.Exit(0);
                };

                // Prevents this host process from terminating so relay keeps running.
                Thread.Sleep(Timeout.Infinite);
                return 0;
            });

            return commandLineApplication.Execute(args);
        }

        private static async Task<ApplicationData> PerformInteractiveConfigurationAsync(
            IServiceProvider serviceProvider,
            string redirectionUrl = null)
        {
            Console.WriteLine("Please wait while we log you in...");

            UserAuthenticator userAuthenticator = serviceProvider.GetRequiredService<UserAuthenticator>();

            ServiceBusResourceManager serviceBusResourceManager = serviceProvider.GetRequiredService<ServiceBusResourceManager>();

            await userAuthenticator.AuthenticateUserAsync().ConfigureAwait(false);

            Console.WriteLine("Please wait while we gather subscription information...");

            List<SubscriptionInner> userSubscriptions = await userAuthenticator.GetUserSubscriptionsAsync().ConfigureAwait(false);

            if (userSubscriptions.Count == 0)
            {
                Console.Error.WriteLine("No Azure subscriptions found");
                throw new InvalidOperationException("User has no associated subscriptions");
            }

            Console.WriteLine("Select the subscription you want to use");

            for (int i = 0; i < userSubscriptions.Count; i++)
            {
                Console.WriteLine($"{i + 1} - {userSubscriptions[i].DisplayName}({userSubscriptions[i].SubscriptionId})");
            }

            int selectedSubscriptionIndex = 0;
            while (true)
            {
                if (!int.TryParse(Console.ReadLine(), out selectedSubscriptionIndex))
                {
                    Console.Error.WriteLine("Invalid input. Please select the index.");
                    continue;
                }

                if (selectedSubscriptionIndex > userSubscriptions.Count || selectedSubscriptionIndex == 0)
                {
                    Console.Error.WriteLine("Invalid input. Select index out of allowed values");
                    continue;
                }

                break;
            }

            SubscriptionInner selectedSubscription = userSubscriptions[selectedSubscriptionIndex - 1];

            List<RelayNamespaceInner> relayNamespaces = await serviceBusResourceManager.GetRelayNamespacesAsync(selectedSubscription).ConfigureAwait(false);

            int selectedRelayIndex = 0;
            if (relayNamespaces.Count != 0)
            {
                Console.WriteLine("Select the Service Bus you want to use.");

                Console.WriteLine("0 - Create a new service bus");
                for (int i = 0; i < relayNamespaces.Count; i++)
                {
                    Console.WriteLine($"{i + 1} - {relayNamespaces[i].Name}");
                }

                while (true)
                {
                    if (!int.TryParse(Console.ReadLine(), out selectedRelayIndex))
                    {
                        Console.Error.WriteLine("Invalid input. Please select the index.");
                        continue;
                    }

                    if (selectedRelayIndex > relayNamespaces.Count)
                    {
                        Console.Error.WriteLine("Invalid input. Select index out of allowed values");
                        continue;
                    }

                    break;
                }
            }

            HybridConnectionDetails hybridConnectionDetails = null;
            if (selectedRelayIndex == 0)
            {
                Console.Write("Enter the name for the new Service Bus. This must be atleast 6 character long. ");
                string serviceBusName = Console.ReadLine();

                Console.WriteLine("Select the location for the new service bus from the list below");

                List<Location> subscriptionLocations = userAuthenticator.GetSubscriptionLocations(selectedSubscription).ToList();

                for (int i = 0; i < subscriptionLocations.Count; i++)
                {
                    Console.WriteLine($"{i + 1} - {subscriptionLocations[i].DisplayName}");
                }

                int selectedLocationIndex = 0;
                while (true)
                {
                    if (!int.TryParse(Console.ReadLine(), out selectedLocationIndex))
                    {
                        Console.Error.WriteLine("Invalid input. Please select the index.");
                        continue;
                    }

                    if (selectedRelayIndex > subscriptionLocations.Count || selectedLocationIndex == 0)
                    {
                        Console.Error.WriteLine("Invalid input. Select index out of allowed values");
                        continue;
                    }

                    break;
                }

                hybridConnectionDetails = await serviceBusResourceManager.CreateHybridConnectionAsync(
                    selectedSubscription,
                    serviceBusName,
                    Environment.MachineName,
                    subscriptionLocations[selectedLocationIndex - 1].DisplayName).ConfigureAwait(false);
            }
            else
            {
                hybridConnectionDetails = await serviceBusResourceManager.GetHybridConnectionAsync(
                    selectedSubscription,
                    relayNamespaces[selectedRelayIndex - 1],
                    Environment.MachineName).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(redirectionUrl))
            {
                Console.Write("Enter the endpoint to route requests to. Example http://localhost:4200 ");
                redirectionUrl = Console.ReadLine();
            }

            return new ApplicationData
            {
#if NET461
                EnableCredentialEncryption = true,
#else
                EnableCredentialEncryption = false,
#endif
                EnabledPlugins = new HashSet<string>(),
                HybridConnectionKeyName = hybridConnectionDetails.HybridConnectionKeyName,
                HybridConnectionName = hybridConnectionDetails.HybridConnectionName,
                HybridConnectionSharedKey = hybridConnectionDetails.HybridConnectionSharedKey,
                HybridConnectionUrl = hybridConnectionDetails.ServiceBusUrl,
                PluginSettingsMap = new Dictionary<string, Dictionary<string, string>>(),
                RedirectionUrl = redirectionUrl,
            };
        }
    }
}
