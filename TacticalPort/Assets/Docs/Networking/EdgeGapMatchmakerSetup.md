# EdgeGap Matchmaker Setup

## Goal

Use the production-shaped public match flow:

1. Clients create tickets in UGS Matchmaker.
2. Matchmaker forms a match.
3. Matchmaker calls the Cloud Code `EdgegapAllocator` module.
4. EdgeGap starts one dedicated server deployment.
5. Matchmaker returns an IP/port assignment to both clients.
6. Clients connect through PurrNet `UDPTransport`.

The Unity combat runtime already supports step 5 and 6 through `IpPortAssignment`, `MatchRuntimeContext`, and `PurrNetMatchConnector`.

## Project State

- `UgsQuickMatchService` supports `IpPortAssignment`.
- `QuickMatchFlowController` marks endpoint-backed matches as `DedicatedServer`.
- `PurrNetMatchConnector` uses `UDPTransport` for dedicated sessions.
- Server builds read `ARBITRIUM_PORT_GAMEPORT_INTERNAL` and listen on `0.0.0.0:<port>`.
- Custom Relay and Custom Direct IP stay separate and should not be changed for this pass.

## EdgeGap Setup

In EdgeGap:

- Application name: use the exact application name from the EdgeGap application list, for example `tortugarena`.
- Version name: use the exact deployed version name, for example `0-1-2_26.05.15-10.25.29-UTC`.
- Port name: `gameport`.
- Protocol: UDP.
- Internal port: `5000`, unless the server build config changes.

Keep doing one manual deployment test after each new server image. If two clients can connect with the deployment IP and external UDP port, the server image is valid.

## Cloud Code Module

The Cloud Code module is already copied into this repository:

```text
TacticalPort/CloudCodeModules/EdgegapAllocator
```

Unity keeps a module reference asset here:

```text
TacticalPort/Assets/EdgegapAllocator.ccmr
```

The reference points to:

```json
{
  "modulePath": "..\\CloudCodeModules\\EdgegapAllocator\\EdgegapAllocator.sln"
}
```

Edit:

```text
TacticalPort/CloudCodeModules/EdgegapAllocator/Project/EdgegapAllocator.cs
```

Set:

```csharp
private const string ApplicationName = "tortugarena";
private const string VersionName = "<exact EdgeGap version name>";
private const string PortName = "gameport";
```

Do not put the EdgeGap API token in the game client or in this repo.

## Unity Dashboard Setup

In Unity Dashboard:

1. Go to `Administration > Secrets`.
2. Add secret `EDGEGAP_API_TOKEN`.
3. Value: the EdgeGap API token from EdgeGap account settings.
4. Deploy the `EdgegapAllocator` Cloud Code module from Unity's Deployment window or from the UGS CLI.
5. In Matchmaker, duplicate the current queue/pool first if you want a safe test path.
6. Set the test pool hosting type to `Cloud Code`.
7. Set:
   - Module Name: `EdgegapAllocator`
   - Allocate Function Name: `Matchmaker_AllocateServer`
   - Poll Function Name: `Matchmaker_PollAllocation`

For the first test, prefer a duplicate queue such as `quickmatch1v1edgegap`. After validation, either switch `quickmatch1v1unranked` to the Cloud Code pool or point the runtime queue back to the production queue.

The module also exposes `ReleaseEdgeGapServer`. This endpoint is not used by Matchmaker directly. The Unity runtime calls it when a dedicated match session is cleaned up, so EdgeGap deployments can be stopped before their one-hour timeout.

## UGS CLI Deploy

The preferred local deployment path is the UGS CLI. The Cloud Code project must keep a publish profile at:

```text
TacticalPort/CloudCodeModules/EdgegapAllocator/Project/Properties/PublishProfiles/FolderProfile.pubxml
```

Deploy the module with:

```powershell
ugs login
ugs config set project-id <unity-cloud-project-id>
ugs config set environment-name production
dotnet build TacticalPort/CloudCodeModules/EdgegapAllocator/EdgegapAllocator.sln
ugs deploy TacticalPort/CloudCodeModules/EdgegapAllocator/EdgegapAllocator.sln --dry-run
ugs deploy TacticalPort/CloudCodeModules/EdgegapAllocator/EdgegapAllocator.sln
```

If using a non-production Unity environment, replace `production` with the active environment name.

## Unity Editor Deploy

If you deploy through Unity:

1. Open the Deployment window.
2. Select the `EdgegapAllocator` Cloud Code module reference.
3. Deploy it to the same UGS environment used by the Matchmaker queue.
4. After deployment, confirm in Unity Dashboard > Cloud Code that module `EdgegapAllocator` exposes:
   - `Matchmaker_AllocateServer`;
   - `Matchmaker_PollAllocation`;
   - `ReleaseEdgeGapServer`.

## Runtime Inspector Setup

For the preferred Matchmaker Cloud Code path:

- `GO_RuntimeServices > QuickMatchFlowController`
  - `Queue Name`: the queue being tested, for example `quickmatch1v1edgegap`.
  - `Game Server Allocator Source`: leave empty.
- `GO_RuntimeServices > RuntimeServicesBootstrap`
  - `Game Server Allocator Source`: leave empty.
- `GO_RuntimeServices > UgsCloudCodeGameServerReleaser`
  - `Module Name`: `EdgegapAllocator`.
  - `Release Endpoint Name`: `ReleaseEdgeGapServer`.
- `GO_RuntimeServices > MatchRuntimeSessionLifecycle`
  - `Game Server Allocator Source`: assign `UgsCloudCodeGameServerReleaser`.
  - `Release Server Allocation On Session End`: enabled.
- `GO_RuntimeServices > PurrNetMatchConnector`
  - `UDPTransport`: assigned.
  - `UTPTransport`: still assigned for Custom Relay.
  - `Use Match Endpoint When Available`: enabled.
  - `Use Edge Gap Injected Server Port`: enabled.
  - `Edge Gap Port Name`: `gameport`.

`UgsCloudCodeGameServerAllocator` is only a debug fallback. Do not use it for the preferred public Matchmaker path, otherwise both clients may try to allocate.
`UgsCloudCodeGameServerReleaser` is release-only and can stay assigned in the lifecycle for the preferred Matchmaker path.

## First Validation

Use one Unity editor plus two ParrelSync clients.

1. Do not run a local server editor.
2. Start clone 0 and clone 1 as clients.
3. Make sure both clones clear UGS session before sign-in.
4. Start Quick Match on both clients.
5. Expected:
   - both clients reach `Found`;
   - both clients receive the same MatchId;
   - logs show a dedicated endpoint IP and external UDP port;
   - EdgeGap dashboard shows one deployment for the match;
   - both clients load `S_Poutch`;
   - TeamA/TeamB assignments are correct;
   - placement, ready, movement, and skills replicate.
6. Return to the main menu from both clients.
7. Expected:
   - local match context is cleared;
   - PurrNet connection stops;
   - Unity logs `Server allocation released`;
   - EdgeGap deployment moves to terminating/terminated instead of waiting for the one-hour expiry.

If the clients stay in searching, check Matchmaker pool hosting and Cloud Code logs.
If they reach found but cannot connect, check EdgeGap deployment status, port name, external UDP port, and server build logs.
EdgeGap deployment status values can be returned as either `ready` or `Status.READY`; the allocator normalizes both formats before returning the Matchmaker assignment.

## Known Limitation

The official EdgeGap allocator can use a `player_ip` value in Matchmaker player custom data for better location selection. The current client does not send public IP data. EdgeGap will still deploy, but location selection may be less accurate until we add a proper QoS/IP signal.
