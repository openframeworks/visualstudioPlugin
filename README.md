
# Visual Studio 2017 openFrameworks plugin

Allows to easily create new openFrameworks projects from inside Visual Studio 2017 and configure the addons in them.

- [Getting started](#getting-started)
- [Contributing](#contributor-guidelines)
- [Previous versions](#previous-versions)
- [Release notes](#release-notes)

## Getting started

For more info on how to use openFrameworks with Visual Studio, check the setup 
guide at http://openframeworks.cc/setup/vs

Note that the guide might refer to Visual Studio 2015, but the instructions also apply to Visual Studio 2017.

### Note
This project is an upgraded version from the original [Visual Studio 2015 extension](https://github.com/openframeworks/visualstudioPlugin), 
developed by by [@arturo](https://github.com/arturoc).

## Contributor guidelines

```
The information below is for developers looking to contribute to the openFrameworks project creator for Visual Studio.
To learn about how to get started with openFrameworks using Visual Studio check http://openframeworks.cc/setup/vs.
```

To develop this solution further, clone the repo and open /src/VSIXopenFrameworks.sln in Visual Studio.

Running the **VSIXopenFrameworks** project (right-click, Debug, or F5) will start the experimental version of Visual Studio, 
which will run having the Visual Studio Extension (vsix) already loaded.

If you are new to Visual Studio Extension, check the documentation at 
https://docs.microsoft.com/en-us/visualstudio/extensibility/starting-to-develop-visual-studio-extensions?view=vs-2017.

## Previous versions

The project creator for Visual Studio 2015 is not from the Market Place, meaning it cannot be installed from Visual Studio 
Tools menu, Extensions and Updates, Online. If you are still using using VS 2015, you can manually installed the extension
from https://github.com/openframeworks/visualstudioPlugin/tree/vs2015 and installing the VSIX file.


## Release notes

2019-02-17 v0.6
- Fixes issue with AdditionalDependencies not working when using add-ons with dependencies to other libraries.
  See https://github.com/openframeworks/visualstudioPlugin/issues/1
 
2019-02-15 v0.5
- Upgraded project file to use Visual Studio 2017 (ToolsVersion="15.0").
- Included Contributor Guidelines and Release Notes.

2017-07-10 v0.4
- First working version.Initial publication to Visual Studio Marketplace at 
	https://marketplace.visualstudio.com/items?itemName=HalfA.openFrameworkspluginforVisualStudio2017
