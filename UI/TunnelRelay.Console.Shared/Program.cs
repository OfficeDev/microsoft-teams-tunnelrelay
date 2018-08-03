using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TunnelRelay.Core;
using TunnelRelay.PluginEngine;

namespace TunnelRelay.Console.Shared
{
    class Program
    {
        static int Main(string[] args)
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

            commandLineApplication.HelpOption("-h|--help|-?");
            commandLineApplication.OnExecute(() =>
            {
                string serviceBusUrl = serviceBusUrlOption.Value();
                string sharedKeyName = serviceBusSharedKeyNameOption.Value();
                string sharedKey = serviceBusSharedKeyOption.Value();
                string connectionName = connectionNameOption.Value();
                string serviceAddress = serviceAddressOption.Value();

                bool paramsPresent = true;
                if (string.IsNullOrEmpty(serviceBusUrl))
                {
                    System.Console.Error.WriteLine("Missing required Service Bus url");
                    paramsPresent = false;
                }

                if (string.IsNullOrEmpty(sharedKeyName))
                {
                    System.Console.Error.WriteLine("Missing required Service Bus shared key name");
                    paramsPresent = false;
                }

                if (string.IsNullOrEmpty(sharedKey))
                {
                    System.Console.Error.WriteLine("Missing required Service Bus shared key");
                    paramsPresent = false;
                }

                if (string.IsNullOrEmpty(connectionName))
                {
                    System.Console.Error.WriteLine("Missing required hybrid connection name");
                    paramsPresent = false;
                }

                if (string.IsNullOrEmpty(serviceAddress))
                {
                    System.Console.Error.WriteLine("Missing required service url");
                    paramsPresent = false;
                }

                if (!paramsPresent)
                {
                    return -1;
                }

                HybridConnectionManagerOptions hybridConnectionManagerOptions = new HybridConnectionManagerOptions
                {
                    ConnectionPath = connectionName,
                    ServiceBusKeyName = sharedKeyName,
                    ServiceBusSharedKey = sharedKey,
                    ServiceBusUrl = serviceBusUrl,
                };

                RelayRequestManager relayManager = new RelayRequestManager(
                    Options.Create(new RelayRequestManagerOptions
                    {
                        InternalServiceUrl = new Uri(serviceAddress),
                    }),
                    new List<ITunnelRelayPlugin>(),
                    new RelayRequestEventListener());

                HybridConnectionManager hybridConnectionManager = new HybridConnectionManager(
                    Options.Create(hybridConnectionManagerOptions),
                    relayManager);

                hybridConnectionManager.InitializeAsync(CancellationToken.None).Wait();

                System.Console.CancelKeyPress += (sender, cancelledEvent) =>
                {
                    hybridConnectionManager.CloseAsync(CancellationToken.None).Wait();
                    System.Environment.Exit(0);
                };

                // Prevents this host process from terminating so relay keeps running.
                Thread.Sleep(Timeout.Infinite);
                return 0;
            });

            return commandLineApplication.Execute(args);
        }
    }
}
