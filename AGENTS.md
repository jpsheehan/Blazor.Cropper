# Repository Guidelines

## Project Structure & Module Organization

- `Blazor.Cropper/` contains the .NET 10 Razor component library. `Cropper.razor` and its code-behind manage the UI; `ImageProcessing.cs`, `ImageCroppedResult.cs`, and `ImageFormat.cs` handle SkiaSharp processing and encoding.
- `Blazor.Cropper/wwwroot/` contains JavaScript interop, styles, and component assets.
- `CropperSample/` is a Blazor WebAssembly application referencing the local library. Examples live in `Pages/`.
- `Blazor.Cropper.Tests/` contains C# and JavaScript regression tests. `imgs/` holds README illustrations.
- `Blazor.Cropper.slnx` includes the library, sample, and tests.

## Build, Test, and Development Commands

Run commands from the repository root. Install the .NET 10 SDK and Node.js. Native WebAssembly processing requires `dotnet workload install wasm-tools`.

- `dotnet build Blazor.Cropper/Blazor.Cropper.csproj -c Release` builds the library and generates its NuGet package.
- `dotnet test Blazor.Cropper.Tests/Blazor.Cropper.Tests.csproj -c Release` runs C# tests.
- `node --test Blazor.Cropper.Tests/CropHelper.test.cjs` runs JavaScript tests without npm dependencies.
- `dotnet run --project CropperSample/CropperSample.csproj` starts the interactive sample.
- `dotnet publish CropperSample/CropperSample.csproj -c Release` checks trimming and native WebAssembly linking.

## Coding Style & Naming Conventions

Use four-space indentation and follow adjacent files. C# uses PascalCase for types, methods, and properties, camelCase for locals, and underscore-prefixed private fields. Keep Razor markup and component logic in their existing paired files. Document public APIs with XML comments. No repository-wide formatter or linter configuration is checked in.

## Testing Guidelines

Use xUnit `[Fact]` and `[Theory]` tests and Node's built-in test runner. Name tests after behavior, such as `CropResizesToFinalDimensions`. Add regression coverage for changed crop geometry, orientation, encoding, stream handling, and disposal. No coverage percentage is configured. For UI or interop changes, exercise both canvas and `PureCSharpProcessing` modes in the sample.

## Commit & Pull Request Guidelines

History uses short action-oriented subjects, including plain descriptions and occasional `fix:` or `chore:` prefixes. Keep commits focused. PRs should describe the problem, resulting behavior, and validation; link related issues and include screenshots for visible UI changes. Document API, format, or platform changes in `README.md`. Pushes to `master` trigger package publishing through `.github/workflows/publish.yaml`.

## Dependency Maintenance

Keep SkiaSharp managed and native package versions aligned. Check new dependencies' licenses and run `dotnet list Blazor.Cropper/Blazor.Cropper.csproj package --vulnerable --include-transitive` when changing packages.
