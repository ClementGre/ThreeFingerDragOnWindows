[![Release](https://img.shields.io/github/v/release/clementgre/ThreeFingersDragOnWindows?label=Download%20version)](https://github.com/clementgre/ThreeFingersDragOnWindows/releases/latest)
[![TotalDownloads](https://img.shields.io/github/downloads/clementgre/ThreeFingersDragOnWindows/total)](https://github.com/clementgre/ThreeFingersDragOnWindows/releases/latest)
[![LatestDownloads](https://img.shields.io/github/downloads/clementgre/ThreeFingersDragOnWindows/latest/total)](https://github.com/clementgre/ThreeFingersDragOnWindows/releases/latest)

# ThreeFingersDragOnWindows

A windows app that allows the macos three fingers drag, using the Raw Inputs of precision touchpad.

- [kamektx/TouchpadGestures_Advanced][1]
- [mfakane/rawinput-sharp][2]
- [emoacht/RawInput.Touchpad][3]

## Preview
![screenshot](https://raw.githubusercontent.com/ClementGre/ThreeFingersDragOnWindows/main/Resources/preview.png)

## How to use
Make sure to disable the "Tap twice and drag to multi-select" behaviour and all of the defaults 3-finger swipe behaviour via ``Touchpad settings`` in windows preferences for the drag to work without interferences.

To open the configuration pane, click the ThreeFingersDragOnWindows icon on the Windows task bar.

## Requirements

- .NET 6.0

## Build and Execute
```
cd \path\to\ThreeFingersDragOnWindows\directory\
dotnet build .\ThreeFingersDragOnWindows.csproj
dotnet exec .\bin\Debug\net6.0-windows\win-x64\ThreeFingersDragOnWindows.dll
```

## Release
```
cd \path\to\ThreeFingersDragOnWindows\directory\
dotnet publish --self-contained false
```
App files will be available at ``.\bin\Debug\net6.0-windows\win-x64\publish\``.

You can then use Inno Setup with the ``inno_setup.iss`` file to build an installer executable.

## License

- MIT License

[1]: https://github.com/kamektx/TouchpadGestures_Advanced

[2]: https://github.com/mfakane/rawinput-sharp

[3]: https://github.com/emoacht/RawInput.Touchpad
