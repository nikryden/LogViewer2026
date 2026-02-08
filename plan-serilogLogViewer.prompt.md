# Plan: LogViewer2026 - High-Performance Serilog Log Viewer

Build a Windows desktop application using WPF + .NET 10 to view, search, and filter large Serilog log files (2GB+). The application uses memory-mapped files for efficient I/O, virtual scrolling for UI performance, and follows TDD practices with a layered architecture (Core, Infrastructure, Application, UI).

## Steps

1. **Set up project structure and CI/CD** - Create the LogViewer2026 .NET solution with 4 projects (LogViewer2026.Core [net10.0], LogViewer2026.Infrastructure [net10.0], LogViewer2026.Application [net10.0], LogViewer2026.UI [net10.0-windows]) + corresponding test projects using xUnit, configure GitHub Actions for automated testing, initialize repository at https://github.com/nikryden, use Visual Studio 2022 as primary IDE

2. **Implement core file reading infrastructure (TDD)** - Build `MemoryMappedFileReader` class with line offset indexing, `LRUCache` for parsed entries, write tests for reading large files and measuring memory usage under 500MB for 2GB files

3. **Create Serilog parsers** - Implement `SerilogJsonParser` and `SerilogTextParser` following outputTemplate configuration, auto-detect format, use System.Text.Json for performance, write parser tests with malformed data handling

4. **Build WPF UI with MVVM** - Create MainWindow with virtualized WPF DataGrid (`VirtualizingStackPanel.IsVirtualizing="True"`), implement ViewModels using CommunityToolkit.Mvvm for log display, filtering, and search, add copy-to-clipboard functionality, use VS 2022 XAML designer, write UI tests with xUnit

5. **Implement filtering and search features** - Add filter service for log level/timestamp/message filtering with debounced UI updates, build inverted index for fast full-text search, create import/export functionality for filter configurations (JSON format)

6. **Add configuration and rolling file support** - Implement a configuration page for pathFormat and rollingInterval (following Serilog formats) and other settings, support multi-file sessions for rolled logs, enable loading multiple files or an entire folder (use pathFormat to filter which files are loaded when a folder is selected), add settings UI for outputTemplate configuration

## Further Considerations

1. **Technology alternatives?** - WPF+.NET 10 chosen for best native Windows performance and Visual Studio 2022 integration. Cross-platform support dropped for optimal performance with 2GB+ files.

2. **Test data strategy** - Generate test log files at various sizes (100 lines, 10K, 1M, 2GB). Include test file generation scripts in repository for local and CI use.

3. **Performance targets** - Targeting: Open 2GB file <5s, search <10s, 60 FPS scrolling, <500MB memory. **Performance benchmarks added to CI pipeline** using BenchmarkDotNet with automated regression detection. CI will fail if performance degrades beyond defined thresholds (e.g., >10% slower).

4. **Additional features priority** - Core features clear. For later phases: Tail mode (live file watching), statistics dashboard, regex search, bookmarks? What's the MVP scope?

## Technology Stack

- **Framework**: .NET 10, WPF, C# 13, Windows 10/11
- **MVVM**: CommunityToolkit.Mvvm (source generators)
- **DI**: Microsoft.Extensions.DependencyInjection
- **Testing**: xUnit + FluentAssertions + Moq
- **Performance**: BenchmarkDotNet (integrated in CI pipeline)
- **Logging**: Serilog
- **JSON**: System.Text.Json
- **IDE**: Visual Studio 2022
- **Source Control**: Git + GitHub (https://github.com/nikryden)

## Roles

- **Product Owner**: Define features, prioritize backlog, accept deliverables
- **Planner**: Break down stories, estimate effort, track progress
- **Backend Developer**: Implement Core, Infrastructure, Application layers
- **UI Frontend Developer**: Build WPF views, XAML, user interactions
- **UI Expert**: Design UX/UI, ensure usability for large datasets
- **Tester**: Write unit/integration tests, verify performance targets, TDD practices

## Development Guidelines

- **Documentation**: Keep markdown files minimal - only final deliverables (README, this plan). Do NOT create markdown files documenting each step, change, or intermediate progress.
- **Token efficiency**: Minimize token usage while maintaining code quality. Focus on implementation over documentation.
- **Code over comments**: Write clear, self-documenting code. Use comments sparingly for complex logic only.

## Settings Storage

- **Default location**: Store user settings in the application root folder as settings.json (alongside the .exe) by default.
- **Configurable path**: Allow users to change the settings location in the configuration page; persist the chosen path.
- **Machine-specific**: Use %LocalAppData%\<Company>\<Product> for large caches and indexes.
- **Behavior**: Load on startup, save on explicit user action (Apply/Save) and on app close.
