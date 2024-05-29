# SimCube.Aspire

SimCube.Aspire is a project that provides Microsoft Aspire Service Defaults and extensions. This project aims to enhance the functionality and usability of Microsoft Aspire services by providing default configurations and additional extensions.

## Features

- **Service Defaults**: Provides default configurations for Microsoft Aspire services, making it easier to get started and maintain consistency across different services.
- **Extensions**: Offers additional functionality to Microsoft Aspire services, extending their capabilities and making them more versatile.

## CI Script

This script automates various stages of the solution's lifecycle, including cleaning, restoring, building, packing, and pushing to NuGet.

### Usage

```bash
./ci.sh <action>
```

Where <action> is one of the following:

- clean: Cleans the directories by removing the bin, obj, and artifacts directories.
- restore: Restores NuGet packages.
- build: Builds the solution.
- pack: Packs the project into a .nupkg file.
- push: Pushes the .nupkg files to NuGet.
- local_ci: Runs the clean, restore, build, and pack functions in sequence.


The script uses the following environment variables for configuration:

- DOTNET_CONFIGURATION: The configuration to use for building and packing. Can be either Release or Debug. Defaults to Release if not set.
- NUGET_SOURCE: The NuGet source to push to. Defaults to https://api.nuget.org/v3/index.json if not set.
- NUGET_API_KEY: The API key to use when pushing to NuGet. Must be set before running the push action.