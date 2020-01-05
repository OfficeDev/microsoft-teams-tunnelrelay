# Tunnel Relay Internals

This document explains how Tunnel Relay processes the request internally. This does not cover any operations done by external components. Refer to following documents for detailed description for respective topics

- [Request Handling](RequestHandling.md)
- [Understanding Plugins](PluginManagement.md)

## High level design
On a high level Tunnel Relay can be split into 3 components

- Tunnel Relay Relay management
- Tunnel Relay Request management engine
- Tunnel Relay UI

## Understanding components

### Tunnel Relay Relay management engine

Tunnel Relay, Azure Relay management engine, maintains communication to and from Azure Relay. Requests are forwarded to request management engine and responses and sent back to Azure Relay.

### Tunnel Relay request management engine

Tunnel relay core engine is the request processor component of Tunnel Relay. It processes requests received from WCF relay and calls other components where needed to perform required operations before sending response back to WCF relay.
Main functionality performed by Core engine includes

1. Create request to be sent to Hosted service
2. Send updates to UI as and when the request progressed through the pipeline
3. Call plugins
4. Create requests and responses while acting as proxy

### Tunnel Relay UI
Tunnel relay is used to show to user how the requests are progressing and information about headers and content as received and send back to caller. UI is a loosely coupled component and primarily works on events raised by Core engine.

### Request lifetime

Following diagram describes the lifetime of a request. Individual requests are handled in parallel fashion and don't impact each other.

![Request Lifetime](RequestLifetime.png "Request lifetime")
