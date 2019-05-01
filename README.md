# Axis
Axis is Grasshopper plugin for McNeel's Rhinoceros 3D that facilitates the intuitive programming and simulation of industrial robots.
The library is written in C# and compiled against .NET 4.6.1. The library is under constant development, and we are always working on new features and integrations. We're currently busy with:

- [x] Component cleanup and simplification
- [x] Online control for IRC5 controllers
- [ ] Fast program checking
- [ ] Full-feature support for KUKA robots

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
