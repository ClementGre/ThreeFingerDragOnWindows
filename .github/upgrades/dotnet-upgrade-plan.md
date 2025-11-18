# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade ThreeFingerDragElevator\ThreeFingerDragElevator.csproj
4. Upgrade ThreeFingerDragOnWindows\ThreeFingerDragOnWindows.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

No projects are excluded from this upgrade.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                        | Current Version      | New Version          | Description                                   |
|:------------------------------------|:--------------------:|:--------------------:|:----------------------------------------------|
| H.NotifyIcon.WinUI                  | 2.0.131              |                      | No supported version found for .NET 10.0      |
| Microsoft.WindowsAppSDK             | 1.5.240627000        | 1.8.251106002        | Recommended for .NET 10.0                     |
| WinUICommunity.Components           | 6.9.0                |                      | Keep current version (user requested)         |
| WinUICommunity.Core                 | 6.9.0                |                      | Keep current version (user requested)         |
| WinUICommunity.LandingPages         | 6.9.0                |                      | Keep current version (user requested)         |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### ThreeFingerDragElevator\ThreeFingerDragElevator.csproj modifications

Project properties changes:
  - Target framework should be changed from `net6.0` to `net10.0-windows`

No NuGet package changes for this project.

#### ThreeFingerDragOnWindows\ThreeFingerDragOnWindows.csproj modifications

Project properties changes:
  - Target framework should be changed from `net6.0-windows10.0.19041.0` to `net10.0-windows10.0.22000.0`

NuGet packages changes:
  - H.NotifyIcon.WinUI: No supported version found for .NET 10.0 (may need manual investigation)
  - Microsoft.WindowsAppSDK should be updated from `1.5.240627000` to `1.8.251106002` (*recommended for .NET 10.0*)
  - WinUICommunity.Components: Keep current version 6.9.0 (user requested)
  - WinUICommunity.Core: Keep current version 6.9.0 (user requested)
  - WinUICommunity.LandingPages: Keep current version 6.9.0 (user requested)
