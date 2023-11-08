[![Release](https://img.shields.io/github/v/release/clementgre/ThreeFingersDragOnWindows?label=Download%20version)](https://github.com/clementgre/ThreeFingersDragOnWindows/releases/latest)
[![TotalDownloads](https://img.shields.io/github/downloads/clementgre/ThreeFingersDragOnWindows/total)](https://github.com/clementgre/ThreeFingersDragOnWindows/releases/latest)
[![LatestDownloads](https://img.shields.io/github/downloads/clementgre/ThreeFingersDragOnWindows/latest/total)](https://github.com/clementgre/ThreeFingersDragOnWindows/releases/latest)

## Overview

The goal of ThreeFingersDragOnWindows is to bring the macOS-style three-finger dragging functionality to Windows Precision touchpads.

This allow to drag windows and select text (drag the cursor with left click pressed) with a simple.

## Preview

TODO

## How to use

Make sure to disable the "Tap twice and drag to multi-select" behaviour and all of the defaults 3-finger swipe behaviour
via ``Touchpad settings`` in windows preferences for the drag to work without interferences.

To open the configuration pane, click the ThreeFingersDragOnWindows tray icon on the Windows task bar.

## Build and Execute

The app can be built and run in Microsoft Visual Studio or Jetbrains Rider.

## Libraries used

The app is WinUI 3 app, that uses the [Microsoft.UI.Xaml](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/) library.

Other libraries used:
- [emoacht/RawInput.Touchpad](https://github.com/emoacht/RawInput.Touchpad) Allows to get the raw input of the touchpad (included in the source code as TouchpadHelper.cs).
- [HavenDV/H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) API for Windows taskbar tray icon in a WinUI app.
- [dahall/TaskScheduler](https://github.com/dahall/TaskScheduler) API for Windows TaskScheduler (used for the skipUAC).
