# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

An IL weaver shipped as a NuGet package (`ICSharpCode.ReferenceAssemblyAnnotator`, the continuation of `TunnelVisionLabs.ReferenceAssemblyAnnotator`). At build time in a consuming project it takes un-annotated reference assemblies (.NET Framework, .NET Standard, .NET Core < 3.0; .NET Core 3.0+ is skipped because it is already annotated) and copies nullability attributes onto them from the annotated `Microsoft.NETCore.App.Ref` package the consumer pulls via `<PackageDownload>`. The project then compiles against the annotated copies.

## Build

`global.json` selects the newest installed SDK 11 (prerelease allowed). Windows is required for the full test matrix (net35/net48, WPF, msbuild.exe).

```
dotnet build ReferenceAssemblyAnnotator.sln -c Release
```

- `GeneratePackageOnBuild` is on: the build writes the `.nupkg` to `ICSharpCode.ReferenceAssemblyAnnotator/bin/<Configuration>/`. The version comes from Nerdbank.GitVersioning (`version.json`), so it only reflects committed changes.
- `TreatWarningsAsErrors` is on outside Visual Studio; StyleCop.Analyzers (`stylecop.json`) and `ICSharpCode.ReferenceAssemblyAnnotator.ruleset` apply. Assemblies are strong-name signed (`build/Key.snk`).
- The repo annotates its own references with the *published* `ICSharpCode.ReferenceAssemblyAnnotator` (pinned in `Directory.Build.props`), not the local build; bump the pin when a release fixes something that matters for the repo's own TFMs. Because the project has the same ID, its `PackageId` is `ICSharpCode.ReferenceAssemblyAnnotator.NewIdRequiredDueToNuGetBug` during restore (otherwise NU1108 cycle) and is set back by the `RestorePackageId` target before the nuspec is generated.
- The task targets netstandard2.0 (dotnet MSBuild) and net472 (VS MSBuild) and compiles against MSBuild 17.8 (compile-only). Don't raise that to MSBuild 18: it drags in System.Collections.Immutable 10, which older hosts can't load.

## Tests

There is no unit test project. The tests are sample projects under `tests/` that must build cleanly with warnings as errors against the locally built package. Build the solution first, then:

```
./build/test.ps1 -Configuration Release -Engine dotnet    # dotnet msbuild
./build/test.ps1 -Configuration Release -Engine msbuild   # VS msbuild.exe (found via vswhere)
```

The script sets `NUGET_PACKAGES` to the isolated cache `tests/.isolatednugetcache` (an inherited machine-wide `NUGET_PACKAGES` would override `globalPackagesFolder` and serve a stale package with the same version), runs `git clean -dxf tests` before every case, and sets `CONFIGURATION` for the local source in `tests/NuGet.Config`. Source mapping forces `ICSharpCode.ReferenceAssemblyAnnotator` to come from `ICSharpCode.ReferenceAssemblyAnnotator/bin/<Configuration>`. To run a single case by hand:

```
$env:CONFIGURATION='Release'; $env:NUGET_PACKAGES="$PWD\tests\.isolatednugetcache"; git clean -dxf tests
dotnet msbuild -restore tests/SingleTFM -p:TestFramework=netstandard2.0 -warnaserror -nr:false -v:m
```

- `tests/SingleTFM` is parameterized by `-p:TestFramework=` (net35 … net10.0); MultiTFM, RefVersion3.1.0, NoDisabledWarnings (uses `-p:TreatWarningsAsErrors=true`, since `-warnaserror` ignores `WarningsNotAsErrors`) and WPF cover the rest.
- The test projects leave out the `Microsoft.NETCore.App.Ref` `PackageDownload` for .NET Core 3.0+ (netcoreapp3.x, net10.0). Those aren't annotated, and the SDK adds its own download whenever the targeting pack isn't installed, so a second one fails with NU1505. The `GetHashCode` line in the test sources is `#if !NETCOREAPP3_0_OR_GREATER` because only annotated TFMs get `[DisallowNull]` removed (`Program.IsExcludedAnnotation`).
- Keep the local source path in `tests/NuGet.Config` with forward slashes: with backslashes, source mapping doesn't match it on Linux/macOS (NU1100).

CI: `.github/workflows/build.yml` runs the same build and test script with both engines (Release only) and uploads the nupkg. On the runner, paths in the isolated cache exceed MAX_PATH under `msbuild.exe`. So the workflow passes `-PackagesPath` with a short path outside the checkout; that cache isn't wiped between cases, which is safe because a run only ever contains one annotator version. `test.ps1` also passes `core.longpaths=true` to `git clean`.

## Architecture

**MSBuild integration (most of the logic lives here)**: `ICSharpCode.ReferenceAssemblyAnnotator.props`/`.targets`, packed into `build/`. The file names must match the package ID.
- `.props` picks the task DLL: `tools/net472/` for full MSBuild, `tools/netstandard2.0/` for `dotnet` (Core) MSBuild. The Mono.Cecil DLLs are packed next to it via `_GetFilesToPackage`.
- `.targets` hooks `ResolveAssemblyReferencesDependsOn` with `AnnotateReferenceAssemblies` → `UpdateReferences`, except for .NETCoreApp >= 3.0. It resolves the annotated source directory from the `Microsoft.NETCore.App.Ref` `PackageDownload` (errors RA0000–RA0002) and decides which references to annotate per target framework family: a hard-coded list for .NETFramework, framework-package references for .NETStandard/.NETCoreApp, and `ResolvedCompileFileDefinitionsToAdd` below 2.0. It writes the outputs to `$(IntermediateOutputPath)annotated\`, then swaps the `Reference` items and prepends that folder to `TargetFrameworkDirectory`.
- It also adds `NullableAttributes.cs`/`.vb` (internal copies of the nullable attributes) as `Compile` items unless the TFM already has them or `GenerateNullableAttributes=false`. The WPF `_GeneratedCodeFiles` line works around dotnet/wpf#810.

**Weaver (C#)** in `ICSharpCode.ReferenceAssemblyAnnotator/`:
- `AnnotatorBuildTask`: the MSBuild task, invoked once per unannotated assembly; it calls `Program.Main`.
- `Program`: loads both assemblies with Mono.Cecil, walks types, methods, properties and fields, finds the matching member in the annotated assembly, and copies the nullability-related custom attributes. The `EquivalenceComparers+*` classes compare signatures structurally across different assembly identities; note that Cecil's `GetElementType()` returns the *innermost* type, so compare `TypeSpecification.ElementType` instead. It also makes sure the output carries `ReferenceAssemblyAttribute`.
- `WellKnownTypes` + `WellKnownTypes+*ProvidedType`: when the target assembly doesn't define an attribute type, it is synthesized into the output assembly as an internal embedded type via `CustomAttributeFactory`/`MethodFactory`.
- `SuppressibleLoggingHelper`: RA1xxx warnings that honor the consumer's `NoWarn`, which is passed in as `DisabledWarnings`.

**`CecilBasedAnnotator`**: a small net472 console wrapper around `Program.Main` for debugging the weaver outside MSBuild. Arguments: `<unannotated.dll> <tfm-dirs;-separated> <annotated.dll> <output.dll>`; sample profiles are in `Properties/launchSettings.json`.
