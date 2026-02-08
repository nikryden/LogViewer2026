# Troubleshooting Guide - DataGrid Empty Issue

## Issue: DataGrid shows empty after loading log file

### Root Causes Fixed

1. **ComboBox Binding Mismatch** ✅
   - **Problem**: The Level filter ComboBox was binding `ComboBoxItem` objects to a `LogLevel?` enum property
   - **Symptom**: Binding errors, filter not working correctly, possible filtering out all entries
   - **Fix**: 
     - Added `AvailableLogLevels` collection to ViewModel
     - Created `LogLevelToStringConverter` for display
     - Updated ComboBox to use `ItemsSource` binding with proper converter

2. **Multi-file Mode Flag** ✅
   - **Problem**: `_isMultiFileMode` flag might persist between operations
   - **Fix**: Explicitly reset `_isMultiFileMode = false` when loading single file

3. **Empty Result Handling** ✅
   - **Problem**: No feedback when parser returns no entries
   - **Fix**: Added check for `TotalLogCount > 0` with appropriate status message

### How to Verify the Fix

1. **Build the solution**:
   ```bash
   dotnet build
   ```

2. **Run the application**:
   ```bash
   dotnet run --project LogViewer2026.UI
   ```

3. **Test with provided test files**:
   - Open `TestData\tiny.log` (50 lines) - should see entries immediately
   - Open `TestData\tiny.json` (50 lines) - should see entries immediately
   - Check status bar shows "Loaded X entries"
   - Check Level filter shows "All" selected

### Debugging Steps if Still Empty

If the DataGrid is still empty after these fixes:

#### 1. Check Status Bar
- Does it show "Loaded N entries"?
- If 0 entries, the file might not be parsing correctly

#### 2. Check Output Window
- Look for any binding errors
- Look for parsing exceptions

#### 3. Verify File Format
The parser expects:
- **JSON**: `{"Timestamp":"2024-01-01T10:00:00.000Z","Level":"Information","MessageTemplate":"..."}`
- **Text**: `2024-01-01 10:00:00.123 [INF] SourceContext Message`

#### 4. Test with Known Good File
Generate fresh test file:
```powershell
.\GenerateTestLogs.ps1
```
Then open `TestData\tiny.log`

#### 5. Check FilterLevel Value
- Open in debugger
- Set breakpoint in `LoadLogEntriesAsync`
- Verify `FilterLevel` is `null` (should show "All")
- If not null, it might be filtering everything out

#### 6. Verify Parser Detection
In `LogService.cs`, the parser is auto-detected:
```csharp
private ILogParser? DetectFormat()
{
    var firstLine = _fileReader.ReadLine(offset);
    foreach (var parser in _parsers)
    {
        if (parser.CanParse(firstLine))
            return parser;
    }
    return null;
}
```

If detection fails, no parser is selected and `GetLogEntries` returns empty.

#### 7. Manual Test
Add this temporary code to `LoadFileAsync` after loading:
```csharp
// DEBUG: Check if service can read entries
var testEntries = _logService.GetLogEntries(0, 10).ToList();
StatusText = $"DEBUG: Got {testEntries.Count} test entries";
```

### Common Issues

| Symptom | Cause | Solution |
|---------|-------|----------|
| Status says "Loaded 0 entries" | Parser not detecting format | Check file format matches Serilog pattern |
| Status says "Loaded N entries" but grid empty | Binding issue | Check for XAML binding errors in Output |
| App crashes on load | File too large for available memory | Start with smaller test files |
| Grid shows some entries missing | Filter is active | Click "Clear Filters" button |
| Grid shows briefly then clears | Search/filter clearing entries | Check SearchText and FilterLevel are null/empty |

### Expected Behavior

After loading `TestData\tiny.log` (50 lines):
- Status bar: "Loaded 50 log entries from tiny.log"
- DataGrid: Shows 50 rows
- Columns: Line, Timestamp, Level, Source, Message populated
- Level filter: Shows "All"
- Details panel: Shows selected entry details when clicking a row

### Files Modified in Fix

1. `LogViewer2026.UI\ViewModels\MainViewModel.cs`
   - Added `AvailableLogLevels` property
   - Fixed `LoadFileAsync` to reset multi-file mode
   - Added empty result handling

2. `LogViewer2026.UI\MainWindow.xaml`
   - Fixed Level filter ComboBox binding
   - Changed from ComboBoxItems to ItemsSource binding

3. `LogViewer2026.UI\Converters\LogLevelToStringConverter.cs` (NEW)
   - Converts LogLevel enum to display string
   - Handles null as "All"

4. `LogViewer2026.UI\App.xaml`
   - Registered `LogLevelToStringConverter` resource

### Next Steps if Issue Persists

1. Enable detailed logging
2. Check if `_logService` is properly initialized
3. Verify dependency injection is working
4. Test parsers individually
5. Open an issue with:
   - Sample log file (first 10 lines)
   - Screenshot of empty grid
   - Output window errors
   - Status bar text
