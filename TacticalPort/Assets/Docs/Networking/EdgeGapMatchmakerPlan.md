# EdgeGap Matchmaker Plan

## Decision

QuickMatch and future Ranked should use the official Unity Matchmaker Cloud Code allocator path, not a client-triggered allocation after `Found`.

The target chain is:

1. Client creates a ticket in `quickmatch1v1unranked`.
2. UGS Matchmaker finds two players.
3. Matchmaker calls a Cloud Code module once for the match:
   - `Matchmaker_AllocateServer`;
   - `Matchmaker_PollAllocation`.
4. The Cloud Code module asks EdgeGap to start a deployment.
5. Matchmaker returns an `IpPortAssignment` to both tickets.
6. `UgsQuickMatchService` reads the IP/port and the existing runtime connects with PurrNet `UDPTransport`.

This avoids duplicate server allocation from two clients.

## Unity / EdgeGap Setup Later

- Use Unity's official `matchmaker-hosting-providers` repository.
- Use the `EdgegapAllocator` module.
- Configure:
  - Unity secret `EDGEGAP_API_TOKEN`;
  - EdgeGap application name;
  - EdgeGap version name;
  - EdgeGap port name, expected `gameport`.
- In Matchmaker, create or duplicate a pool for Cloud Code hosting:
  - Module Name: `EdgegapAllocator`;
  - Allocate Function Name: `Matchmaker_AllocateServer`;
  - Poll Function Name: `Matchmaker_PollAllocation`.

## Client Status

The Unity client is already prepared for the assignment returned by this path:

- `UgsQuickMatchService` supports `IpPortAssignment`.
- `QuickMatchFlowController` marks endpoint-backed matches as `DedicatedServer`.
- `PurrNetMatchConnector` uses `UDPTransport` for `DedicatedServer`.
- Dedicated server builds read `ARBITRIUM_PORT_GAMEPORT_INTERNAL` and listen on `0.0.0.0:<internal port>` when running on EdgeGap.
- Combat handshake, team compositions, placement, ready, turns and skills stay unchanged.

## Local Validation Before Paying EdgeGap

Keep using:

- Custom Relay for friend tests without port opening.
- Custom Direct IP for local debug.
- QuickMatch local server validation with `LocalGameServerAllocator` if needed.

Do not assign `UgsCloudCodeGameServerAllocator` in `S_Bootstrap` for the preferred production path. It exists as a manual/debug fallback only.

## Sources

- Unity Matchmaker hosting providers: https://docs.unity.com/en-us/matchmaker/multiplay-hosting-migration
- Unity official provider modules: https://github.com/Unity-Technologies/matchmaker-hosting-providers
- EdgeGap deployments: https://docs.edgegap.com/learn/orchestration/deployments
