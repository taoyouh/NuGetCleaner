# NuGet Cache Cleaner
A UWP app for auto cleaning NuGet global packages folder.

The app scans the NuGet global packages folder (usually at %USERPROFILE%/.nuget), and deletes the packages that are not accessed within N days, where N is set by the user.

## Install from Microsoft Store

<a href='//www.microsoft.com/store/apps/9P7MQ2FFX51F?cid=storebadge&ocid=badge'><img src='https://assets.windowsphone.com/13484911-a6ab-4170-8b7e-795c1e8b4165/English_get_L_InvariantCulture_Default.png' alt='English badge' style='width: 127px; height: 52px;' width='127px'/></a>

## Build

- Install Visual Studio 2017 with "Universal Windows Platform development" and "Windows 10 SDK (10.0.17134.0)" components.
- Open NuGetCleaner.sln in Visual Studio 2017 and hit F5.
