---
topic: sample
products:
- Microsoft Teams
- Office 365
languages:
- C#
extensions:
  contentType: tools
  createdDate: 6/2/2017 5:30:01 PM
---
# Tunnel Relay

Tunnel relay allows you to expose local services to the outside world over HTTPS using [Azure Relay](https://docs.microsoft.com/en-us/azure/service-bus-relay/relay-what-is-it#hybrid-connections).

![Tunnel Relay Logo](Documentation/TunnelRelaylogo-01.png "Tunnel Relay")

## Overview
Since most developers don't have static IP addresses for their development machines visible to the external world, it is incredibly cumbersome for them to develop and test services. Tunnel Relay is a free and open-source tool that provides you a static URL for your local service which can be accessed externally!

![Overview](Documentation/BotDevelopementTR.png "Overview")

## Download
Latest release can be downloaded [here](https://github.com/OfficeDev/microsoft-teams-tunnelrelay/releases/latest) 

## Requirements
We wanted to build a solution which was easy to use and works out of the box. Here are the things you need to run Tunnel Relay.

1. Machine running Windows (.Net Framework 4.6.1 and above), Linux or Mac OSX and above
2. Microsoft Azure subscription (you can sign up for a free trial [here](https://azure.microsoft.com/en-us/free/))

...__**and that is it!**__

## Get Started
Following image explains the basic components of the app. User needs to login before they can start using the app. This is covered in [Configuration](Documenation/Configuration.md).
![Main Window](Documentation/MainWindow.png "Tunnel Relay Main Window")

## FAQs
**Q.** How is TunnelRelayv2 different from TunnelRelayv1? </br>
**A.** TunnelRelayv2 adds support for Linux and Mac Operation systems. To achieve this, it uses [Hybrid Connections](https://docs.microsoft.com/en-us/azure/service-bus-relay/relay-what-is-it#hybrid-connections).
This also means that the Relays of v1 cannot be reused in v2. Additionally v2 adds support for commandline.

**Q.** Do my clients need to change to understand that they are contacting server over Tunnel Relay vs directly? </br>
**A.** Short answer no. All your clients need to see is the url exposed by Tunnel Relay. Rest everything remains the same.

**Q.** Can I share same Service bus namespace across multiple machines? </br>
**A.** Yes, you can share same service bus namespace across multiple machines. Each relay is a unique combination of service bus and machine name.

**Q.** How much will this cost me? </br>
**A.** Tunnel Relay itself is a free, open-source tool, although since Azure Service Bus back it, you will have to pay for the Service Bus itself. Pricing for service bus can be found [here](https://azure.microsoft.com/en-us/pricing/details/service-bus/). Tunnel Relay creates a service bus with __basic tier__. Please look for section WCF Relays to get the pricing information.

**Q.** I have an issue which needs your attention how can I contact you? </br>
**A.** We monitor this Github repo for issues. Please open a new issue or reply to an existing one. We will get back to you soon. 

**Q.** I want to extend Tunnel Relay. How can I do so? </br>
**A.** Tunnel Relay is released under [MIT License](https://opensource.org/licenses/MIT). Please look at the contributing section below if you want to contribute to this project.

## Understand how Tunnel Relay works
Please refer to following articles to learn more about Tunnel Relay and its internal workings

1. [Configuration](Documentation/Configuration.md)
2. [Request Handling](Documentation/RequestHandling.md) 
3. [Internal Design](Documentation/InternalDesign.md)
4. [Extending Tunnel Relay - Plugins](Documentation/PluginManagement.md)
5. [Advanced Functionality](Documentation/AdvancedTopics.md)

## Issues, problems, feedback, questions?
Please report them [here](https://github.com/OfficeDev/microsoft-teams-tunnelrelay/issues)

## Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
