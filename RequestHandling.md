#Tunnel Relay Request Handling

## Understand data flow
Azure Service Bus relay backs Tunnel relay hence the request flow is like WCF Relay as explained [here](https://docs.microsoft.com/en-us/azure/service-bus-relay/relay-wcf-dotnet-get-started). Following diagram explains how the requests and responses flows through various layers to the system.
![Request Handling](TunnelRelayWorking.png "Tunnel Relay Request handling.")

## Step-by-step description
1. Actor (user or service) sends the request to hosted service using the url shown on the Tunnel Relay. Azure Service Bus relay receives this request.
2. Azure Service Bus then checks for the listeners to the specific relay. The listener in this case is Tunnel Relay application running on developer machine.
3. Request is sent to Tunnel Relay.
4. Tunnel Relay receives the request and does the necessary processing (you can read up more about this in [Plugin Management](PluginManagement.md)) and forwards the request to hosted service.
5. Hosted service then does the necessary processing and returns a response this is then forward to the caller down the same pipeline.

## FAQs
Q. Are requests to Tunnel Relay secure? </br>
A. Endpoint exposed by Tunnel Relay is always an HTTPs url and hence secured by end to end encryption. Requests between Azure Service Bus and Tunnel Relay are also secure. Tunnel Relay to Hosted service the calls can be made over HTTP or HTTPs. Tunnel Relay does not check for certificate validity for the hosted services as most developers don't usually have SSL certificates for local development.

Q. What protocols does Tunnel Relay support for hosted services? </br>
A. Tunnel Relay supports both HTTP and HTTPs for hosted services. Endpoint exposed by Tunnel relay is always HTTPs though. 
