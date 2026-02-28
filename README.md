# LogViewer2026

A high-performance Windows desktop application for viewing, searching, and filtering large log files (2GB+).

## Features

### Core Functionality
- **High Performance**: Handles 2GB+ log files efficiently using memory-mapped files
- **Virtual Scrolling**: Smooth 60 FPS scrolling through millions of log entries
- **Dual Format Support**: Automatically detects and parses both JSON and text Serilog formats
- **Advanced Filtering**: Filter by log level, timestamp range, and custom criteria
- **Rich UI**: Modern WPF interface with color-coded log levels and dark/light themes
- **Copy to Clipboard**: Easy copying of log entries for further analysis
- **Multi-File Support**: Load and navigate multiple log files or entire folders
- **File Monitoring**: Automatic detection of file changes with reload notifications

### Search Capabilities ✨ NEW
- **Full-Text Search**: Fast search across all log entries with instant highlighting
- **Regex Search**: Advanced pattern matching with regular expressions support
  - Optimized with .NET 10 `EnumerateMatches()` for zero-allocation performance
  - Pattern caching for 10-100x faster repeated searches
  - Manual search button prevents errors from incomplete patterns
  - 1-2 second timeout protection for complex patterns
- **Search Navigation**: Navigate through results with F3/Shift+F3 or toolbar buttons
- **Filter to Results**: Show only lines matching search criteria
- **Performance**: 40% faster first search, 90% faster repeated searches

### Reload Preferences ✨ NEW
- **Smart Reload**: Choose reload behavior in Settings
  - Stay at selected row (default) - maintains your current position
  - Jump to last row - automatically scroll to newest entries
- **Keyboard Shortcuts**: Ctrl+Shift+R for quick reload

### Looking Glass Panel
- **Context Viewer**: See surrounding lines around selected log entry
- **Auto-Update**: Optional automatic refresh when navigating
- **Configurable Context**: Adjust number of context lines in Settings
- **Undockable**: Open in separate window for multi-monitor setups

### User Interface
- **Dual Theme Support**: Light and Dark themes
- **Undockable Panels**: Log Editor and Looking Glass can be undocked
- **Custom Title Bar**: Modern borderless window design
- **Filter Management**: Save, load, import, and export filter configurations
- **Status Indicators**: Real-time file change notifications

## Technology Stack

- **.NET 10**: Latest .NET framework with modern C# 14 features
- **WPF**: Windows Presentation Foundation for rich UI
- **C# 14**: Using latest language features including:
  - Primary constructors
  - Collection expressions `[]`
  - Span-based optimizations for performance
- **CommunityToolkit.Mvvm**: Modern MVVM pattern implementation
- **AvalonEdit**: High-performance text editor control
- **Memory-Mapped Files**: Efficient I/O for large files
- **LRU Caching**: Smart caching for parsed entries
- **.NET 10 Regex**: Optimized with `EnumerateMatches()` and pattern compilation

## Recent Updates

### Version 0.0.6 (Latest)
- ✨ **Regex Search**: Full regular expression support with optimization
  - .NET 10 `EnumerateMatches()` for zero-allocation enumeration
  - Pattern caching for 10-100x faster repeated searches
  - Manual search button prevents incomplete pattern errors
  - Timeout protection (2 seconds) for complex patterns
- ✨ **Reload Preferences**: Choose scroll behavior on file reload
  - Stay at selected row (default)
  - Jump to last row option
- 🚀 **Performance**: 40-90% faster searches with reduced memory usage
- 🎨 **UI**: Search button with Enter key support
- 🐛 **Stability**: Better error handling for invalid regex patterns

### Previous Updates
- Filter management with save/load/import/export
- Multi-file and folder loading
- File monitoring with auto-reload notifications
- Looking Glass panel with context viewing
- Undockable panels for multi-monitor setups
- Dark/Light theme support

## Performance Targets

- **File Loading**: Open 2GB file in < 5 seconds
- **Search Performance**: 
  - First search: < 3 seconds (40% improvement)
  - Repeated search: < 0.5 seconds (90% improvement with caching)
  - Regex search: < 2 seconds with timeout protection
- **Scrolling**: Consistent 60 FPS
- **Memory Usage**: < 500MB for 2GB files
- **Search Memory**: Reduced GC pressure with span-based operations

## Building

### Requirements
- Visual Studio 2022/2026
- .NET 10 SDK
- Windows 10/11

### Build Steps
```bash
git clone https://github.com/nikryden/LogViewer2026.git
cd LogViewer2026
dotnet restore
dotnet build
```

### Run
```bash
dotnet run --project LogViewer2026.UI
```

### Build for Release
```bash
dotnet publish LogViewer2026.UI -c Release -r win-x64 --self-contained
```

## Testing

Run all tests:
```bash
dotnet test
```

Run specific test project:
```bash
dotnet test LogViewer2026.Core.Tests
dotnet test LogViewer2026.UI.Tests
dotnet test LogViewer2026.Infrastructure.Tests
```

Run with code coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture

The application follows a clean, layered architecture:

- **LogViewer2026.Core**: Domain models, interfaces, and configuration
  - Models for log entries and filters
  - Service interfaces
  - Configuration classes (AppSettings)

- **LogViewer2026.Infrastructure**: File I/O and parsing implementations
  - Memory-mapped file reader
  - JSON and text format parsers
  - File system monitoring

- **LogViewer2026.Application**: Application services and business logic
  - Multi-file log service
  - Filter configuration service
  - Settings service

- **LogViewer2026.UI**: WPF user interface with MVVM pattern
  - ViewModels (MainViewModel, SettingsViewModel)
  - Views (MainWindow, SettingsWindow, FilterWindow)
  - Custom controls (SearchResultHighlighter)
  - Themes (Light/Dark)

### Key Design Patterns
- **MVVM**: Separation of UI and business logic
- **Dependency Injection**: Constructor injection for testability
- **Repository Pattern**: Abstraction of data access
- **Observer Pattern**: Property change notifications
- **Strategy Pattern**: Different log format parsers

## Usage

### Opening Files
1. Launch the application
2. Click "Open" toolbar button or use File menu:
   - **Open Log File**: Load a single log file
   - **Open Multiple Files**: Select and load multiple files
   - **Open Folder**: Load all log files from a directory

### Searching
3. Use the search bar in the toolbar:
   - **Text Search**: Type and search automatically updates (default)
   - **Regex Search**: Check "Regex" checkbox, type pattern, press Enter or click 🔍
     - Example patterns: `^ERROR.*timeout$`, `\d{4}-\d{2}-\d{2}`, `(ERROR|WARN|FATAL)`
   - **Filter to Results**: Check to show only matching lines
   - **Navigation**: Use ⬆️⬇️ buttons or F3/Shift+F3 to navigate results

### Filtering
4. Click "Filters" button or View → Filter Settings:
   - Filter by log level (DEBUG, INFO, WARN, ERROR, FATAL)
   - Filter by timestamp range
   - Save and reuse filter configurations
   - Export/Import filters for team sharing

### Viewing Details
5. Select any log entry:
   - Looking Glass panel shows surrounding context
   - Right-click for context menu (Copy, Copy to Search, etc.)
   - Use Ctrl+L to select whole line
   - Undock panels for multi-monitor setups

### Settings
6. File → Settings or ⚙️ button:
   - **Theme**: Switch between Light and Dark themes
   - **Search**: Enable regex search by default
   - **Reload**: Choose reload behavior (last row or selected row)
   - **Looking Glass**: Configure context lines and auto-update
   - **Performance**: Adjust cache size and indexing options

### Keyboard Shortcuts
- **Ctrl+R**: Clear all filters
- **Ctrl+Shift+R**: Reload files
- **Ctrl+C**: Copy selected text
- **Ctrl+L**: Select whole line
- **Ctrl+A**: Select all
- **F3**: Next search result
- **Shift+F3**: Previous search result
- **Enter** (in search box): Execute search

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Guidelines
1. Follow the existing code style and architecture
2. Write unit tests for new features
3. Update documentation (README, code comments)
4. Ensure all tests pass before submitting PR
5. Use conventional commit messages (feat:, fix:, docs:, etc.)

### Reporting Issues
- Use GitHub Issues for bug reports and feature requests
- Include steps to reproduce for bugs
- Provide sample log files if applicable (sanitized)

## Roadmap

- [ ] Log export functionality
- [ ] Custom log format support
- [ ] Bookmark/favorite log entries
- [ ] Log comparison tool
- [ ] Performance profiling dashboard
- [ ] Plugin system for custom parsers

## License

MIT License

Copyright (c) 2026 Niklas Rydén

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Acknowledgments

- Built with ❤️ using .NET 10 and WPF
- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) for text editing
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) for MVVM
- Inspired by various log viewer tools in the .NET ecosystem
