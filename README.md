[![Microsoft Store Badge](https://img.shields.io/badge/Microsoft%20Store-005FB8?logo=microsoftstore&logoColor=fff&style=flat)](https://apps.microsoft.com/detail/9MSX91WQCM2V?)
[![Release](https://img.shields.io/github/v/release/clementgre/ThreeFingerDragOnWindows?label=Download%20version)](https://github.com/clementgre/ThreeFingerDragOnWindows/releases/latest)
[![TotalDownloads](https://img.shields.io/github/downloads/clementgre/ThreeFingerDragOnWindows/total)](https://github.com/clementgre/ThreeFingerDragOnWindows/releases/latest)
[![LatestDownloads](https://img.shields.io/github/downloads/clementgre/ThreeFingerDragOnWindows/latest/total)](https://github.com/clementgre/ThreeFingerDragOnWindows/releases/latest)

## Overview

The goal of ThreeFingerDragOnWindows is to bring the macOS-style three-finger dragging functionality to Windows Precision touchpads.

This allows you to drag windows and select text (by emulating a cursor drag holding down the left mouse button) with a simple touchpad gesture.

## Preview
<p align="center">
  <img src='https://raw.githubusercontent.com/ClementGre/ThreeFingerDragOnWindows/main/ThreeFingerDragOnWindows/Assets/Screenshot-1.png' alt="App screenshot: Touchpad tab" width='700'>
  <img src='https://raw.githubusercontent.com/ClementGre/ThreeFingerDragOnWindows/main/ThreeFingerDragOnWindows/Assets/Screenshot-2.png' alt="App screenshot: Three Fingers Drag tab" width='700'>
  <img src='https://raw.githubusercontent.com/ClementGre/ThreeFingerDragOnWindows/main/ThreeFingerDragOnWindows/Assets/Screenshot-3.png' alt="App screenshot: Other Settings tab" width='700'>
</p>

## Installation

If the installation fails, your computer might need to have the Windows App SDK redistributable installed. You can download it from this page: [https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads).

## How to use

Make sure to disable the "Tap twice and drag to multi-select" behaviour and all of the defaults 3-finger swipe behaviour
via ``Touchpad settings`` in windows preferences for the drag to work without interferences.

To open the configuration pane, click the ThreeFingerDragOnWindows tray icon on the Windows task bar.

## Build and Execute

The app can be built and run in Microsoft Visual Studio or Jetbrains Rider.

## Libraries used

The app is WinUI 3 app, that uses the [Microsoft.UI.Xaml](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/) library.

Other libraries used:
- [emoacht/RawInput.Touchpad](https://github.com/emoacht/RawInput.Touchpad) Allows to get the raw input of the touchpad (included in the source code as TouchpadHelper.cs).
- [HavenDV/H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) API for Windows taskbar tray icon in a WinUI app.
- [dahall/TaskScheduler](https://github.com/dahall/TaskScheduler) API for Windows TaskScheduler (used for the skipUAC).
