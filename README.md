# LogViewer2026

A high-performance Windows desktop application for viewing, searching, and filtering large log files (2GB+).

## Features

- **High Performance**: Handles 2GB+ log files efficiently using memory-mapped files
- **Virtual Scrolling**: Smooth 60 FPS scrolling through millions of log entries
- **Dual Format Support**: Automatically detects and parses both JSON and text Serilog formats
- **Advanced Filtering**: Filter by log level, timestamp range, and custom criteria
- **Full-Text Search**: Fast search across all log entries
- **Rich UI**: Modern WPF interface with color-coded log levels
- **Copy to Clipboard**: Easy copying of log entries for further analysis

## Technology Stack

- .NET 10
- WPF for UI
- C# 13
- CommunityToolkit.Mvvm for MVVM pattern
- Memory-mapped files for efficient I/O
- LRU caching for parsed entries

## Performance Targets

- Open 2GB file: < 5 seconds
- Search: < 10 seconds
- Scrolling: 60 FPS
- Memory usage: < 500MB for 2GB files

## Building

### Requirements
- Visual Studio 2022
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

## Testing

Run all tests:
```bash
dotnet test
```

## Architecture

The application follows a layered architecture:

- **LogViewer2026.Core**: Domain models and interfaces
- **LogViewer2026.Infrastructure**: File reading and parsing implementations
- **LogViewer2026.Application**: Application services and business logic
- **LogViewer2026.UI**: WPF user interface with MVVM pattern

## Usage

1. Launch the application
2. Click "Open" or use File → Open Log File
3. Select a Serilog log file (JSON or text format)
4. Use the search bar to find specific entries
5. Apply filters to narrow down results
6. Click on any entry to see full details

## License

MIT License
