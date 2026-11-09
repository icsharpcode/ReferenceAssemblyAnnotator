# Reference Assembly Annotator

IL weaver for adding nullability annotations to .NET Framework, .NET Standard, and .NET Core reference assemblies.

[![Build](https://github.com/icsharpcode/ReferenceAssemblyAnnotator/actions/workflows/build.yml/badge.svg)](https://github.com/icsharpcode/ReferenceAssemblyAnnotator/actions/workflows/build.yml)

This is the continuation of [TunnelVisionLabs.ReferenceAssemblyAnnotator](https://github.com/tunnelvisionlabs/ReferenceAssemblyAnnotator),
published as `ICSharpCode.ReferenceAssemblyAnnotator`. To migrate, replace the package ID; the MSBuild properties and items are unchanged.

## Requirements

* Build: .NET SDK 8 or newer, or Visual Studio 2022 version 17.8 or newer.

## Usage

### Assemblies included by default

* .NET Framework targets
    * mscorlib
    * System
    * System.Core
    * System.Data
    * System.Drawing
    * System.IO.Compression.FileSystem
    * System.Numerics
    * System.Runtime.Serialization
    * System.Xml
    * System.Xml.Linq
* .NET Standard targets
    * All assemblies which are defined by the .NET Standard
* .NET Core targets before 3.0
    * All reference assemblies defined by .NET Core

.NET Core 3.0 and newer are not annotated: their reference assemblies already carry nullable annotations. Don't add the
`Microsoft.NETCore.App.Ref` `PackageDownload` for those target frameworks. The SDK may download that package itself, and
a second download fails restore with NU1505. When multi-targeting, condition it, for example:

```xml
<!-- <TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks> -->
<PackageDownload Include="Microsoft.NETCore.App.Ref" Version="[10.0.0]" Condition="'$(TargetFramework)' != 'net10.0'" />
```

### Example configuration

```xml
<PropertyGroup>
  <!-- By default, the nullable attributes from dotnet/coreclr are included as source code with 'internal'
       accessibility. Uncomment to suppress this if the attributes are included from another source and/or
       are not needed. -->
  
  <!-- <GenerateNullableAttributes>false</GenerateNullableAttributes> -->
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="ICSharpCode.ReferenceAssemblyAnnotator" Version="2.0.0-preview.*" PrivateAssets="all" />

  <!-- Specifies the version of Microsoft.NETCore.App.Ref to obtain nullability information from. -->
  <PackageDownload Include="Microsoft.NETCore.App.Ref" Version="[10.0.0]" />
</ItemGroup>
```

Minimal:

```xml
<ItemGroup>
  <PackageReference Include="ICSharpCode.ReferenceAssemblyAnnotator" Version="2.0.0-preview.*" PrivateAssets="all" />
  <PackageDownload Include="Microsoft.NETCore.App.Ref" Version="[10.0.0]" />
</ItemGroup>
```

### Configuration reference

* MSBuild properties
    * `<AnnotatedReferenceAssemblyVersion>`: Specifies the version of Microsoft.NETCore.App.Ref to obtain nullability information from. This is required if there are multiple PackageDownload versions of Microsoft.NETCore.App.Ref.
    * `<GenerateNullableAttributes>`: Set to `True` to include definitions of nullability attributes in the build; otherwise, `False` to exclude the definitions. The default value is `True`.
* MSBuild items
    * `<UnannotatedReferenceAssembly>`: Specifies reference assemblies to annotate. This is only required for assemblies that are not automatically annotated by this package.

## Releases

[![NuGet](https://img.shields.io/nuget/vpre/ICSharpCode.ReferenceAssemblyAnnotator.svg)](https://www.nuget.org/packages/ICSharpCode.ReferenceAssemblyAnnotator)

* [Binaries (NuGet)](https://www.nuget.org/packages/ICSharpCode.ReferenceAssemblyAnnotator)
* [Release Notes](https://github.com/icsharpcode/ReferenceAssemblyAnnotator/releases)
* [License](https://github.com/icsharpcode/ReferenceAssemblyAnnotator/blob/master/LICENSE)
