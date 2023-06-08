# Tibber Query CLI, tq
Basic cli tool to query current electric power price and cost from Tibber

# Debug in Visual Studio 2022
Create a appsettings.Development.json file and add necessary settings based on the requirements. Afther that you should be good to go.

# Install
Verify if the tool is installed: dotnet tool list --global

## Install the tool from source
dotnet tool install --global --add-source <project_root_path>\bin\debug TqTool

### Install the tool from nuget package
dotnet tool install --global --add-source C:\Temp\Example\ TqTool

## Uninstall the tool
dotnet tool uninstall --global TqTool

## Update the tool
dotnet tool update --global --add-source C:\TempExample\ TqTool

The tool installs to folder %userprofile%\.dotnet\tools

# Requirements
Development login at tibber, https://developer.tibber.com/
Private developer key and api endpoint which can be obtained from the developer portal at Tibber
Key and endpoint needs to be set in appsettings file(s)