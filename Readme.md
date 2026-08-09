# Tibber Query CLI, tqtool
Basic cli tool to query current electric power price and cost from Tibber

# Debug in Visual Studio 2022
Create a appsettings.Development.json file and add necessary settings based on the requirements, see the [template file](./TqTool/template.appsettings.json). Afther that you should be good to go.
To install this tool as a global tool you will alson need an appsetting.json file with necessary settings.

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
There are a few requirements to this project

## .net sdk
.net 7 sdk must be installed on the client computer

## Tibber api information
Development login at tibber, https://developer.tibber.com/
Private developer key and api endpoint which can be obtained from the developer portal at Tibber

Once the tool is installed, store them with the config command instead of editing any file:

    tqtool config -token <your token> -endpoint https://api.tibber.com/v1-beta/gql
    tqtool config -show

This writes to %APPDATA%\tqtool\appsettings.json, which survives tool updates. The token is never printed
back by -show. The environment variables apiToken and apiEndpoint override it, and an appsettings.json next
to the executable acts as a default beneath it.

Avoid putting a real token in the appsettings.json beside the executable: that file is included when the
tool is packed, so the token would travel inside the package.
