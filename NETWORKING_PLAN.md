# Networking Plan

## Target

The networking stack stays split by match intent, while combat remains a single runtime path.

| Mode | Discovery | Hosting | Transport | Trust |
| --- | --- | --- | --- | --- |
| QuickMatch / Ranked | UGS Matchmaker | EdgeGap dedicated server | PurrNet UDPTransport | Trusted |
| Custom | UGS Lobby join code | Player host through UGS Relay | PurrNet UTPTransport | Untrusted |
| Local Debug | Manual IP and port | Local editor/build | PurrNet UDPTransport | Untrusted |

Combat code must not depend on UGS, Relay, EdgeGap, Lobby, or transport details. The runtime handoff is still `MatchRuntimeContext -> PurrNetMatchConnector -> PurrNetCombatBridge`.

## Principles

- QuickMatch and future Ranked use server authority and are the only modes allowed to report ranked results.
- Custom games are private, unranked, and player-hosted to avoid EdgeGap cost during friend tests.
- Local Direct IP remains available only as a development/debug path.
- The PurrNet Lobby System is not imported for V1; custom lobbies use direct UGS Lobby and Relay APIs.
- PurrTransport public relay and SteamTransport are not part of this plan.
- Team assignment, team composition manifest, placement, ready, turns, skills, and gameplay replication remain shared across every network mode.

## Passes

### 1. Documentation And Cleanup

Status: done.

- Replace the previous roadmap with this file.
- Document target modes, package expectations, manual setup, and validation.
- Keep the existing local-server and Direct IP flows working while adding Relay support.

### 2. Custom Relay Runtime

Status: done.

- Add `CustomRelayHost` and `CustomRelayJoin` session modes.
- Add relay/lobby fields to runtime snapshots:
  - lobby id;
  - lobby join code;
  - relay join code;
  - host relay allocation for the host process only.
- Add `UgsCustomRelayLobbyService`:
  - anonymous sign-in;
  - create private lobby;
  - create Relay allocation;
  - store relay join code in lobby member data;
  - join lobby by code;
  - leave/delete lobby on cleanup.

### 3. PurrNet Transport Switching

Status: done.

- `PurrNetMatchConnector` selects the transport while `NetworkManager` is offline:
  - `UDPTransport` for QuickMatch local server, future EdgeGap, and Direct IP debug;
  - `UTPTransport` for Custom Relay.
- Relay host initializes `UTPTransport` with the host allocation before starting the PurrNet server listener.
- Relay join initializes `UTPTransport` with the relay join code before `StartClient`.
- Existing combat handshake and manifest synchronization remain unchanged.

### 4. Custom Match UI

Status: done.

- Reuse the existing `S_MainMenu` custom panel.
- Default buttons become:
  - Create Custom Lobby;
  - Join by Code;
  - Leave / Cancel.
- Display lobby code, relay state, status, and errors.
- Keep IP/port fields as a debug Direct IP path controlled from the Inspector.

### 5. EdgeGap Public Path

Status: ready for dashboard/module setup.

- Keep UGS Matchmaker as public matchmaking entrypoint.
- Add the real EdgeGap allocation path behind `IGameServerAllocator`.
- Return public IP/port to the same `MatchRuntimeContext`.
- Do not modify combat for EdgeGap integration.
- Preferred production path: configure the Matchmaker pool hosting type as Cloud Code, using Unity's EdgegapAllocator module:
  - module: `EdgegapAllocator`;
  - allocate function: `Matchmaker_AllocateServer`;
  - poll function: `Matchmaker_PollAllocation`.
- Matchmaker then invokes Cloud Code once per match, polls allocation, and returns an `IpPortAssignment` to both player tickets.
- `UgsQuickMatchService` already supports `IpPortAssignment`; when one is returned, `QuickMatchFlowController` treats the session as `DedicatedServer` and `PurrNetMatchConnector` connects through `UDPTransport`.
- Server builds read EdgeGap's injected `ARBITRIUM_PORT_GAMEPORT_INTERNAL` value and listen on `0.0.0.0:<internal port>`.
- `UgsCloudCodeGameServerAllocator` remains a manual/debug fallback for direct client-triggered allocation experiments, not the preferred production route.
- Required EdgeGap module configuration:
  - Unity secret: `EDGEGAP_API_TOKEN`;
  - EdgeGap application name;
  - EdgeGap version name;
  - EdgeGap port name, expected default `gameport`;
  - server build listens on the same internal UDP port configured in the EdgeGap app version.
- Detailed setup and validation steps are tracked in `TacticalPort/Assets/Docs/Networking/EdgeGapMatchmakerSetup.md`.

## Packages And Plugins

Already present in `TacticalPort/Packages/manifest.json`:

- `dev.purrnet.purrnet`
- `com.unity.services.multiplayer`
- `com.unity.transport`
- `com.unity.services.authentication`
- `com.unity.services.cloudcode`
- `com.unity.services.deployment`
- `com.edgegap.unity-servers-plugin`

To verify in Unity/PurrNet:

- `PurrNet/Transport/UTP Transport` must be available.
- If it does not appear, open `Tools > PurrNet > Addon Library` and verify/import the UTP / Unity Transport addon.

Do not install for this pass:

- `Steamworks.Net` / `SteamTransport`;
- the full PurrNet Lobby System;
- PurrTransport public relay.

## Manual Unity / UGS Setup

Unity Dashboard:

- Confirm the project is linked to the correct Cloud Project ID.
- Enable Authentication anonymous sign-in.
- Enable Lobby.
- Enable Relay.
- Keep Matchmaker configured for `quickmatch1v1unranked` while testing local/client-hosted quick match.
- For the EdgeGap pool later, switch hosting to Cloud Code and set module/function names listed above.
- Keep Cloud Code and Deployment available for the EdgeGap allocator module.
- Add the `EDGEGAP_API_TOKEN` secret in Unity Dashboard > Administration > Secrets, with Cloud Code access.

`S_Bootstrap`, `GO_RuntimeServices`:

- Keep `NetworkManager`.
- Keep `UDPTransport`.
- Add `UTPTransport`.
- Add `UgsCustomRelayLobbyService`.
- Do not assign `UgsCloudCodeGameServerAllocator` for the preferred Matchmaker Cloud Code pool path; Matchmaker will return the server endpoint directly.
- Keep `LocalGameServerAllocator` only for local dedicated-server validation when needed.
- Assign both transports on `PurrNetMatchConnector`.
- Assign the lobby service on `RuntimeServicesBootstrap` and `MatchRuntimeSessionLifecycle` when the fields are visible.

`S_MainMenu`:

- No new scene is required.
- Reuse the existing custom match panel.
- Add or assign a TMP input named `Input_CustomJoinCode` for lobby codes.
- Keep status/error text assigned.
- Keep IP/port fields for debug Direct IP only.

## Validation

Compile:

```powershell
dotnet build TacticalPort\Assembly-CSharp.csproj --no-restore
```

Regression:

- Custom Direct IP host/join still works.
- QuickMatch with local server still works.
- QuickMatch with the current client-hosted pool still reaches `Found`.

Custom Relay:

- Client A creates a lobby and receives a lobby code.
- Client B joins with that code.
- Both clients load `S_Poutch`.
- TeamA/TeamB compositions are correct.
- Placement, Ready, movement, and skills still replicate.
- Returning to the menu stops PurrNet, clears match context, and leaves/deletes the lobby.

EdgeGap later:

- Deploy a Linux server build image to EdgeGap.
- Configure the EdgeGap app version port as UDP `gameport`.
- Deploy Unity's `EdgegapAllocator` Cloud Code module.
- Switch or duplicate the Matchmaker pool to Cloud Code hosting.
- Two clients quickmatch and receive the same match id plus the EdgeGap IP/port.
- Both clients connect through `UDPTransport` and run the same combat flow.

Cross-platform later:

- Windows host + Windows client.
- Windows host + Android client.
- Android host + Windows client only if Android-hosted custom games remain allowed.
