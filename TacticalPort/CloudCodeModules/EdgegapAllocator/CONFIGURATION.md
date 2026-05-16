# Edgegap Allocator Configuration

## Required Secret

Create this secret in Unity Dashboard under `Administration > Secrets`:

- `EDGEGAP_API_TOKEN`

The token comes from the EdgeGap console user settings.

## Project Constants

The module is currently configured in `Project/EdgegapAllocator.cs` with:

```csharp
private const string ApplicationName = "tortugarena";
private const string VersionName = "0-1-2_26.05.15-10.25.29-UTC";
private const string PortName = "gameport";
```

Update `VersionName` every time the EdgeGap application version used for matchmaking changes.

## Matchmaker Pool

Configure the Matchmaker pool hosting provider as Cloud Code:

- Module Name: `EdgegapAllocator`
- Allocate Function Name: `Matchmaker_AllocateServer`
- Poll Function Name: `Matchmaker_PollAllocation`

