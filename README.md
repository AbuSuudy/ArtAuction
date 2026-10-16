## Architecture

 `ArtAuction.API` accepts a bid and publishes `ArtWorkBid` onto Service Bus. `ArtAuction.Consumer` consumes it, records the bid in Table Storage, then drives a MassTransit routing slip through courier activities to accept or reject the bid. If a later activity in the slip faults, MassTransit walks back through the already-executed activities' `Compensate` methods and update the state of the entity on Table Storage.

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
    IdP -->|Return JWT| Client
    Client -->|Submit Bid| API[API]
    API -->|Publish| SB[(Service Bus)]

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

    AQ -. Fault .-> Comp
    BR -. Fault .-> Comp
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

There are three application registration
- **Art-Auction-Github-Deployment** :  Which acts the deployment identity which has OIDC federated credentials. The identity has `contributor` role on the subscription since it's create RG on the subscription scope level. Also `
Role Based Access Control Administrator` for role assignments of the managed identities.

- **Art-Auction-API**: which represent the API and contains roles needed for role based access.

- **Art-Auction-Api-Client** - Which is used by client along with a secret generate the JWT token and via API permison that I grant can use the role created in Art-Auction-API application registration.


## Container Registry
Allow the container app to pull from the container registry. This now done with bicep, but just to noted if resources are hand cranked.

```bash
az containerapp registry set --name art-auction-api --resource-group MassTransit-Playground --server masstransitcontainerregistry.azurecr.io --identity system
```

## Why Container Apps Use User-Assigned Identities

The container apps use a user-assigned managed identity (`Infra/Modules/managedIdentities.bicep`) instead of `systemAssigned: true`. This isn't a style choice — system-assigned identity caused a real deployment deadlock.

The consumer app's KEDA scale rule authenticates to Service Bus using the container app's identity:
```bicep
custom: {
  type: 'azure-servicebus'
  identity: 'system'
  metadata: { queueName: ..., namespace: serviceBusName, ... }
}
```

When Container Apps provisions a revision, it has to validate that scale rule's auth against Service Bus *as part of* bringing the revision online — not as a later step. With a system-assigned identity, the principal ID doesn't exist until the container app itself is created, so the role assignment (`Modules/roleAssignments.bicep`) can only run *after* the container app module finishes. But the container app can't finish until the scaler successfully authenticates, which needs the role assignment to already exist. That's a true circular dependency — no `dependsOn` ordering can resolve it, because the missing piece (the principal ID) is only produced by the exact step that's blocked waiting on something derived from it.

In practice this showed up as both container apps failing with `Operation expired` after ~20 minutes, with zero revisions ever created and zero RBAC role assignments ever granted on the ACR/Service Bus/Storage Account.

A user-assigned identity breaks the cycle: it's its own resource, so its principal ID is known immediately on creation, with no dependency on the container app. That makes the order a straight line — create identity, grant roles, then create the container app referencing the already-permissioned identity — instead of a cycle.

**Before (system-assigned) — circular, deadlocks forever:**
```mermaid
flowchart LR
    CA["containerApps<br/>(creates revision +<br/>KEDA SB scaler)"]
    RA["roleAssignments<br/>(grants SB role<br/>to principalId)"]

    CA -- "needs principalId<br/>(only exists once CA is created)" --> RA
    RA -- "must run before CA's<br/>scaler auth can succeed" --> CA

    style CA fill:#5a1f1f,stroke:#f66,color:#fff
    style RA fill:#5a1f1f,stroke:#f66,color:#fff
```

**After (user-assigned) — straight line, no cycle:**
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


