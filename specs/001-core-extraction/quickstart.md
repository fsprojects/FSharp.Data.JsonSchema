# Quickstart: Core Extraction and Multi-Target Architecture

## Verify Current State (before any changes)

```bash
# All existing tests must pass before starting
dotnet build -c Release
dotnet test -c Release --no-build --framework net8.0
```

Expected: All 22 snapshot tests + serialization/validation tests pass.

## Step 1: Create Core Project

```bash
# Create project
mkdir -p src/FSharp.Data.JsonSchema.Core
dotnet new classlib -lang F# -o src/FSharp.Data.JsonSchema.Core
dotnet sln add src/FSharp.Data.JsonSchema.Core/FSharp.Data.JsonSchema.Core.fsproj --solution-folder src
```

Edit `src/FSharp.Data.JsonSchema.Core/FSharp.Data.JsonSchema.Core.fsproj`:
- Set TargetFrameworks to `netstandard2.0;netstandard2.1;netcoreapp3.1;net6.0;net8.0;net9.0;net10.0`
- Remove all PackageReferences except FSharp.Core
- Add files: SchemaNode.fs, SchemaGeneratorConfig.fs, SchemaAnalyzer.fs

Verify: `dotnet build src/FSharp.Data.JsonSchema.Core -c Release`

## Step 2: Implement IR Types

Create `src/FSharp.Data.JsonSchema.Core/SchemaNode.fs` with:
- `SchemaNode` DU (all 11 variants)
- `ObjectSchema`, `PropertySchema`, `Discriminator` records
- `PrimitiveType` DU
- `SchemaDocument` record

Verify: Project compiles with no warnings.

## Step 3: Implement SchemaAnalyzer

Create `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs` with:
- `SchemaAnalyzer.analyze` function
- Recursive type traversal with visited-set cycle detection
- Support all "extraction scope" types

Verify: Core test suite passes for all extraction-scope type categories.

## Step 4: Add Core Dependency to Existing Package

Edit `src/FSharp.Data.JsonSchema/FSharp.Data.JsonSchema.fsproj`:
- Add `<ProjectReference Include="../FSharp.Data.JsonSchema.Core/FSharp.Data.JsonSchema.Core.fsproj" />`

Verify: `dotnet build src/FSharp.Data.JsonSchema -c Release`

## Step 5: Implement NJsonSchema Translator

Create `src/FSharp.Data.JsonSchema/NJsonSchemaTranslator.fs`:
- Pattern match over `SchemaNode` → `NJsonSchema.JsonSchema`
- Handle all node types including Ref resolution

Modify `src/FSharp.Data.JsonSchema/JsonSchema.fs`:
- Change `Generator.CreateInternal` to call `SchemaAnalyzer.analyze >> NJsonSchemaTranslator.translate`
- Preserve all existing processors and public API

**CRITICAL CHECKPOINT**: Run existing snapshot tests:
```bash
dotnet test test/FSharp.Data.JsonSchema.Tests -c Release --framework net8.0
```
All 22 snapshot tests MUST pass byte-identical.

## Step 6: Create OpenApi Project

```bash
mkdir -p src/FSharp.Data.JsonSchema.OpenApi
dotnet new classlib -lang F# -o src/FSharp.Data.JsonSchema.OpenApi
dotnet sln add src/FSharp.Data.JsonSchema.OpenApi/FSharp.Data.JsonSchema.OpenApi.fsproj --solution-folder src
```

Edit fsproj:
- TargetFrameworks: `net9.0;net10.0`
- Add Core project reference
- Add Microsoft.OpenApi and Microsoft.AspNetCore.OpenApi package references
  (version-conditional for net9.0 vs net10.0)

## Step 7: Implement OpenApi Translator + Transformer

Create translator and `FSharpSchemaTransformer` with conditional compilation
for net9.0 vs net10.0 differences.

Verify with integration test using ASP.NET Core test host.

## Validation Checklist

- [ ] `dotnet build -c Release` succeeds for all projects
- [ ] Existing snapshot tests pass byte-identical
- [ ] Core tests cover all extraction-scope types
- [ ] OpenApi translator tests cover all IR node types
- [ ] FSharpSchemaTransformer integration test verifies endpoint schemas
- [ ] `dotnet pack -c Release` produces 3 NuGet packages
- [ ] Core package has no transitive NJsonSchema/OpenApi/Namotion dependencies
