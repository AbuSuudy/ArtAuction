# Art Auction

A playground for MassTransit routing slips on Azure. You submit a bid on a piece of art. If it's at least 80% of the asking price, the bid is accepted and the art is acquired; otherwise it's rejected. Everything runs on Azure Container Apps, with Service Bus, Table Storage and Entra ID. You can run  locally with Aspire and opted to use Rabbit MQ locally over the Azure Service Bus Emulator. Due to Mass Transit abstraction outside of configuration in the program.cs  nothing else have to be updated 

## Project Structure

| Path | What it is |
| --- | --- |
| `src/ArtAuction.API` | Minimal API that accepts bids and publishes them to the bus. Secured with Entra ID. |
| `src/ArtAuction.Consumer` | Worker that consumes bids and runs the routing slip activities. |
| `src/ArtAuction.Contract` | Shared message contracts and Table Storage entities. |
| `src/ArtAuction.AppHost` | Aspire AppHost for running everything locally. |
| `src/Infra` | Bicep for all the Azure resources, deployed as a deployment stack. |

## Flow Diagram

`ArtAuction.API` accepts a bid and publishes `ArtWorkBid` onto the bus (Service Bus in Azure, RabbitMQ when running locally). `ArtAuction.Consumer` consumes it, records the bid in Table Storage, and then drives a MassTransit routing slip through Courier activities to accept or reject the bid. If a later activity in the slip faults, MassTransit walks back through the `Compensate` methods of the activities that already ran, updating the entity's state in Table Storage.

```mermaid
%%{init: {
  "theme": "dark",
  "themeVariables": {
    "darkMode": true,
    "background": "transparent",
    "primaryColor": "#1f2937",
    "primaryTextColor": "#e5e7eb",
    "primaryBorderColor": "#4b5563",
    "lineColor": "#9ca3af",
    "secondaryColor": "#111827",
    "tertiaryColor": "#111827",
    "edgeLabelBackground": "transparent",
    "fontSize": "14px"
  }
}}%%
flowchart TB
    IdP[Entra ID<br/>App Registration]
    Client([Client]) -->|Request JWT| IdP
    IdP -->|"Return JWT<br/>role: Bid.Submit"| Client
    Client -->|Submit Bid| API[API]
    API -->|Publish| SB[("Service Bus - Deployed<br/>RabbitMQ - Locally")]

    Contract[["Message Contract"]]
    API -.- Contract
    Contract -.- Consumer[Consumer]

    SB --> Consumer
    Consumer -->|Add Entity| Table[(Table Storage)]
    Consumer --> RS{"≥80% price?"}

    subgraph Activities["Routing Slip"]
        direction LR
        RS -->|Yes| BA[BidAccepted] --> AQ[Aquired]
        RS -->|No| BR[BidRejected]
    end

    subgraph Comp["Compensate · reverse order"]
        direction LR
        CAQ[Aquired] --> CBA[BidAccepted]
        CBR[BidRejected]
    end

    AQ -->|Fault| Comp
    BR -->|Fault| Comp
    linkStyle 13,14 stroke:#f85149,stroke-width:2px,color:#ffffff
    Activities -->|"Update<br/>Entity State"| Table
    Comp -->|"Update<br/>Entity State"| Table

    classDef entry fill:#2d3139,stroke:#8b949e,color:#f0f3f6
    classDef svc fill:#15344d,stroke:#58a6ff,color:#f0f3f6
    classDef broker fill:#3b2461,stroke:#bc8cff,color:#f0f3f6
    classDef decision fill:#4d3a0a,stroke:#f2b84b,color:#fff3d6
    classDef act fill:#113828,stroke:#3fb950,color:#e9fbef
    classDef store fill:#0d3b45,stroke:#39c5cf,color:#e9fcff
    classDef comp fill:#4a1515,stroke:#f85149,color:#ffecec
    classDef shared fill:#2d3139,stroke:#8b949e,stroke-dasharray:4 3,color:#f0f3f6
    classDef idp fill:#4a2f0a,stroke:#f0883e,color:#ffe9d6

    class Client entry
    class API,Consumer svc
    class SB broker
    class RS decision
    class BA,AQ,BR act
    class Table store
    class Comp,CAQ,CBA,CBR comp
    class Contract shared
    class IdP idp
```

## Architecture Choice
I was planning to deploy the resources with private endpoints in a shared virtual network, but private networking on Service Bus needs the Premium SKU, which is too expensive. Instead, I was happy to experiment with Network Security Perimeter, which creates a logical network boundary around platform-as-a-service (PaaS) resources deployed outside your virtual networks. With my current profile settings, it only allows requests between resources in the same subscription.

![Resource Diagram](Imgaes/Resouce-Diagram.svg)


## Generate Token from Identity Provider using PowerShell

```
$body = @{
    grant_type    = "client_credentials"
    client_id     = "345e735e-327c-429b-946b-39d7edaf4c9c"
    client_secret = ""
    scope         = "api://8036976b-35ee-4425-bebf-76ede18b97d0/.default"
}

$response = Invoke-RestMethod -Uri "https://login.microsoftonline.com/3853050a-07fd-4295-9fec-e56ecda64939/oauth2/v2.0/token" -Method Post -Body $body

$response.access_token
```

## Application Registration 

There are three application registrations:
- **Art-Auction-Github-Deployment**: The deployment identity, which uses OIDC federated credentials. It has the `Contributor` role on the subscription because it creates the resource group at subscription scope, and `Role Based Access Control Administrator` so it can assign roles to the managed identities.

- **Art-Auction-API**: Represents the API and defines the roles used for role-based access, such as `Bid.Submit`.

- **Art-Auction-Api-Client**: Used by the client, along with a secret, to generate the JWT. The API permission I granted lets it use the role defined in the Art-Auction-API registration.


## Container Registry
Allow the container app to pull from the container registry. This is now handled in Bicep, but I'm noting it here in case the resources are hand cranked.

```bash
az containerapp registry set --name ca-artauctionapi-neu-001 --resource-group RG-ARTAUCTION-NEU-001 --server crartauctionneu001.azurecr.io --identity <user-assigned-identity-resource-id>
```

## Why Container Apps Use User-Assigned Identities

The Container Apps scale rules need access to Service Bus to work. I had trouble using a system-assigned identity because this check ran before the identity was created. So I created a user-assigned managed identity ahead of time, gave it the permissions it needs on Service Bus, and associated it with the container app. Bicep sets the identity's client ID as an environment variable on the container app, and the app picks it up at runtime.

**Before (system-assigned) — circular deadlock:**
```mermaid
flowchart LR
    CA["containerApps<br/>(creates revision +<br/>KEDA SB scaler)"]
    RA["roleAssignments<br/>(grants SB role<br/>to principalId)"]

    CA -- "needs principalId<br/>(only exists once CA is created)" --> RA
    RA -- "must run before CA's<br/>scaler auth can succeed" --> CA

    style CA fill:#5a1f1f,stroke:#f66,color:#fff
    style RA fill:#5a1f1f,stroke:#f66,color:#fff
```

**After (user-assigned) — no deadlock:**
```mermaid
flowchart LR
    MI["managedIdentities<br/>(own resource,<br/>principalId known<br/>immediately)"]
    RA2["roleAssignments<br/>(grants SB/ACR/storage<br/>roles to that principalId)"]
    CA2["containerApps<br/>(references the already-<br/>permissioned identity)"]

    MI --> RA2 --> CA2

    style MI fill:#1f3d1f,stroke:#6f6,color:#fff
    style RA2 fill:#1f3d1f,stroke:#6f6,color:#fff
    style CA2 fill:#1f3d1f,stroke:#6f6,color:#fff
```
