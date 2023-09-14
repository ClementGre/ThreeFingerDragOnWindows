[![Release](https://img.shields.io/github/v/release/clementgre/ThreeFingersDragOnWindows?label=Download%20version)](https://github.com/clementgre/ThreeFingersDragOnWindows/releases/latest)
[![TotalDownloads](https://img.shields.io/github/downloads/clementgre/ThreeFingersDragOnWindows/total)](https://github.com/clementgre/ThreeFingersDragOnWindows/releases/latest)
[![LatestDownloads](https://img.shields.io/github/downloads/clementgre/ThreeFingersDragOnWindows/latest/total)](https://github.com/clementgre/ThreeFingersDragOnWindows/releases/latest)

## Overview

A windows app that enables macOS-style three-finger dragging functionality on Windows Precision touchpads.

## Preview

![screenshot](https://raw.githubusercontent.com/ClementGre/ThreeFingersDragOnWindows/main/Resources/preview.png)

## How to use

Make sure to disable the "Tap twice and drag to multi-select" behaviour and all of the defaults 3-finger swipe behaviour
via ``Touchpad settings`` in windows preferences for the drag to work without interferences.

To open the configuration pane, click the ThreeFingersDragOnWindows icon on the Windows task bar.

## Build and Execute

## Libraries used

The app is WinUI 3 app, using the [Microsoft.UI.Xaml](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/)
library.

- [emoacht/RawInput.Touchpad][3] that allow to get the raw input of the touchpad.

