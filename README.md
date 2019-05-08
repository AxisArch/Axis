# Axis
Axis is Grasshopper plugin for McNeel's Rhinoceros 3D that facilitates the intuitive programming and simulation of industrial robots.
The library is written in C# and compiled against .NET 4.6.1. The library is under constant development, and we are always working on new features and integrations. We're currently busy with:

- [x] Component cleanup and simplification
- [x] Online control for IRC5 controllers
- [x] Creating a Windows installer file
- [ ] Fast program checking
- [ ] Full-feature support for KUKA robots

## Installation
The latest release version of Axis can be installed using the MSI installer. This will copy Axis to your Grasshopper libraries folder along with a library of user objects and all necessry dependency files.

## Building From Source
To build the solution from source, you need to first change the following project variables:
* The build output path (Properties) needs to be changed to reflect the current system
e.g  ..\\..\\..\\..\AppData\Roaming\Grasshopper\Libraries\Axis\
* To debug the solution you need to update the path to your Rhino installation directory (Debug)
e.g C:\Program Files\Rhino 6\System\Rhino.exe

## Dependencies
The core of library is dependent on the following libraries:
* RhinoCommon.dll
* Grasshopper.dll
* GH_IO.dll

Secondary components are dependent on:
* MathNet.Numerics.dll
* MathNet.Spatial.dll
* EPPlus.dll

Online control functionality for ABB robots requires the latest version of the ABB Communication Runtime to be installed, which can be found here:

## Controlling Speed
The target components can be supplied with default speed values (double) corresponding to the TCP speed from the following table:

<img src="https://github.com/rhughes42/Axis/blob/master/Images/StandardSpeeds.PNG" width="400">

Alternatively, custom speeds can be created using the Speed component from the Toolpath tab. The component accepts a non-standard TCP path speed and creates a custom speed object. Additional functionality can be accessed by right-clicking the component, such as specifying the specific speed components or outputting a declaration for the custom speed, which can be passed to the code generation component when creating the program files.

## Controlling Zone
The target components can similarly be supplied with default zone values (double) corresponding to the TCP Path Radius from the following table:

<img src="https://github.com/rhughes42/Axis/blob/master/Images/StandardZones.PNG" width="400">

Alternatively, custom zones can be created using the Zone component from the toolpath tab. The component accepts a non-standard TCP path radius and creates a custom zone object. Additional functionality can be accessed by right-clicking the component, such as specifying the specific zone components or outputting a declaration for the custom zone, which can be passed to the code generation component when creating the program files.
