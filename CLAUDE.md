# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`tqtool` — a .NET 10 global CLI tool that queries the Tibber GraphQL API for electricity prices, consumption cost, and account/home info. Two projects: `TqTool` (the tool) and `TqTool.Tests` (xUnit).

## Commands

```powershell
dotnet build TibberQuery.sln
dotnet test                                          # all tests
dotnet test --filter FullyQualifiedName~PriceTests   # one class
dotnet test --filter "FullyQualifiedName~GetPriceAsync_ShouldReturnCurrentPrices"   # one test

dotnet run --project TqTool -- price -hrs 6          # run locally (see config below)
dotnet run --project TqTool -- cost -days 5

dotnet pack TqTool -c Release
dotnet tool install --global --add-source TqTool\bin\Release TqTool
dotnet tool uninstall --global TqTool
```

Commands: `price [-hrs n|-max]`, `cost [-days n]`, `owner`, `homes`, `config [-token x] [-endpoint url] [-show]`.

## Configuration

Four sources, each optional, **later ones winning** (`SetupConfiguration.InitConfiguration`):

1. `appsettings.json` beside the executable — a shipped default, and the file `template.appsettings.json` is a template for. Gitignored.
2. `%APPDATA%\tqtool\appsettings.json` (`UserSettings.DefaultFilePath`) — written by `tqtool config`. In the user profile, not the tool directory, because dotnet replaces the latter on every tool update.
3. `appsettings.{ASPNETCORE_ENVIRONMENT}.json` — the launch profile sets `development`, so debugging keeps using `appsettings.development.json` even if user settings exist. Gitignored.
4. Environment variables `apiEndpoint` / `apiToken` — highest, for CI and one-off runs.

Blank values in the user file are skipped rather than applied, so a half-filled file cannot wipe out another source.

`tqtool config` deliberately bypasses `ICommandLineHandler`: resolving that constructs the api client, which refuses to start without credentials, so routing the command that *supplies* credentials through it would deadlock on itself. The client is otherwise built on first resolve, so `--help` works with no configuration at all, and a command needing credentials fails with an actionable message and exit code 1.

Config is loaded from the **assembly directory**, not the current working directory (`SetupConfiguration.InitConfiguration` uses `Assembly.GetAssembly(typeof(Program)).Location`) — this is what makes it work when installed globally. The corollary is that a settings file which isn't copied to the output directory is never read, and since the environment overlay is loaded with `optional: true` that failure is silent: you get whatever `appsettings.json` holds. `appsettings.development.json` is therefore copied in **Debug only** (`Pack=false`), so a real token can't reach a packed release.

## Architecture

Flow: `Program` → `CommandLineBuilderFactory` → `ICommandLineHandler` → feature service → `IGraphClientWrapper` → Tibber GraphQL.

**One container, built by hand.** `Program.Main` builds a `ServiceProvider` from `SetupConfiguration.ConfigureServices` and hands it to `CommandLineBuilderFactory.BuildRootCommand`; command handlers resolve everything from that provider via `GetRequiredService`. There is no generic host — new services go in `SetupConfiguration.ConfigureServices`.

**System.CommandLine is the 2.0 GA API**, not the old beta. Options are built with object initialisers (`Description`, `DefaultValueFactory`), not constructor arguments; handlers are `SetAction((parseResult, ct) => ...)` reading values via `parseResult.GetValue(option)`. `CommandLineBuilder`, `SetHandler`, and `AddCommand` no longer exist. `System.CommandLine.Hosting` is deliberately absent — it still targets the beta5 line and cannot coexist with GA.

**Feature folders.** Each feature is `Features/<Name>/` containing `I<Name>Service` + `<Name>Service`, optionally `I<Name>ViewModelFactory` + factory, and `Models/<Name>Models.cs` (all `record` types, positional). Services hold their GraphQL query strings inline as verbatim strings and shape the response into a view model via the factory; factories are pure and hold the unit conversion / rounding logic.

**`IGraphClientWrapper` is the test seam.** It's a thin pass-through over `IGraphQLClient` whose only purpose is mockability — every service depends on it, and every service test substitutes it. Don't inject `IGraphQLClient` directly into a service.

**`CommandLineHandler` is the only presentation layer.** It's the sole place that writes to `Console` and the sole place that catches exceptions (logging them via `ILogger` and returning normally). Services and factories throw freely.

**Times are `DateTimeOffset` everywhere, deliberately.** Tibber sends `"2026-08-09T00:00:00.000+02:00"` — the home's local time with its offset. Deserialising that into `DateTime` yields `Kind=Local`, silently re-expressing the home's schedule in whatever zone the machine runs in. Keeping the offset means the current hour is found by asking which window contains the instant (`StartsAt <= now < StartsAt + 1h`) rather than matching a truncated local hour, which is what makes a DST fold resolvable — the two 02:00s differ only by offset. `PriceViewModelFactory` takes a `TimeProvider` so this is testable; production gets `TimeProvider.System`.

**Notable behaviours**
- No caching. An `IMemoryCache` was here once, but the process serves one command and exits, so it could never hit.
- `-hrs` is clamped to 1..12 by `CommandLineBuilderFactory.CalculateHours`; out-of-range values silently become 12.
- `PriceViewModelFactory` converts prices to *öre* (× 100, rounded to `int`, `MidpointRounding.ToEven`) and back-fills tomorrow's prices when today's remaining hours don't cover the requested window.
- `ConsumptionViewModelFactory` skips nodes with `Cost` null or `< 1`, so the reported day count can be lower than `-days`.
- Auth is a `Bearer` header set once on the shared `GraphQLHttpClient` at container setup.

## Conventions

- `.editorconfig` is authoritative: **tabs**, CRLF, `var` everywhere, braces on new lines.
- File-scoped namespaces; primary constructors for DI (`public class PriceService(IGraphClientWrapper x, ...) : IPriceService`); `record` for all models and view models; nullable enabled.
- Tests: xUnit + NSubstitute + Shouldly (+ AutoFixture available). `Xunit` and `Shouldly` are global usings in `TqTool.Tests/Usings.cs` — don't re-import them. Arrange/Act/Assert comment blocks, `Method_ShouldDoThing` naming, test files mirror the `Features/<Name>/` layout.
- The published tool version comes from `<VersionPrefix>` in `TqTool/TqTool.csproj`; bump it before packing a release.

## Releasing

`.github/workflows/release-cli.yml` is manual only (`workflow_dispatch`). It builds, tests, packs, and publishes the `.nupkg` as a GitHub Release tagged `v<Version>`, keeping the newest `KEEP_RELEASES` (4) and deleting older releases with their tags.

The tag comes from MSBuild (`-getProperty:Version`, derived from `<VersionPrefix>`) rather than from a grep, so it cannot disagree with the version stamped on the package. A run whose tag already exists fails deliberately — **bump `<VersionPrefix>` and merge that first**, or the release aborts. Install instructions live only in `Readme.md`, which the release notes link to, so the two cannot drift.
