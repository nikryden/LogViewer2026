# Quick Test Guide - Verify the Fixes

## 🚀 5-Minute Test

### Step 1: Run the Application (30 seconds)
```bash
cd E:\Projects\Play\unovfr\LogViewer2026
dotnet run --project LogViewer2026.UI
```

OR press **F5** in Visual Studio (Debug mode recommended)

### Step 2: Open Output Window (if using Visual Studio)
- **View → Output** 
- Dropdown: Select **"Debug"**
- Keep this visible!

### Step 3: Test Level ComboBox (15 seconds)
✅ Should show **"All"** selected  
✅ Click dropdown - should see all levels  
✅ Select "Information" - should work  
✅ Select "All" again - **should work now!**

### Step 4: Load Test File (30 seconds)
1. Click **📂 Open** button
2. Navigate to `TestData` folder
3. Select **`test-manual.log`**
4. Click Open

### Step 5: Verify Success ✅

**In DataGrid, you should see:**
```
Line | Timestamp           | Level | Source        | Message
-----|---------------------|-------|---------------|------------------------
0    | 2024-01-01 10:00:00 | Info  | Test.Manual.. | This is test entry 1
1    | 2024-01-01 10:00:01 | Error | Test.Manual.. | This is test entry 2...
2    | 2024-01-01 10:00:02 | Warn  | Test.Manual.. | This is test entry 3...
3    | 2024-01-01 10:00:03 | Info  | Test.Manual.. | This is test entry 4
4    | 2024-01-01 10:00:04 | Debug | Test.Manual.. | This is test entry 5...
```

**In Status Bar:**
```
Loaded 5 log entries from test-manual.log (Displaying: 5) | Total: 5 entries | Displayed: 5 | Single file
```

**In Output Window (Debug mode only):**
```
LogService.OpenFile: File '...\test-manual.log' opened
  Total lines: 5
  Parser detected: SerilogTextParser
...
ObservableCollection now has 5 entries
```

## 📊 What to Check

| Component | Expected | Pass/Fail |
|-----------|----------|-----------|
| Level ComboBox shows "All" | ✅ Shows "All" | ⬜ |
| Can select different levels | ✅ All selectable | ⬜ |
| Can select "All" again | ✅ Works | ⬜ |
| DataGrid shows 5 rows | ✅ 5 visible | ⬜ |
| Status shows "5 entries" | ✅ Correct count | ⬜ |
| Click row shows details | ✅ Details panel fills | ⬜ |
| Output shows debug info | ✅ Parser detected | ⬜ |

## ❌ If Still Not Working

### Issue A: DataGrid Still Empty

**Check Output Window for:**
```
Total lines: 5        ← Should be 5
Parser detected: [?]  ← Should be "SerilogTextParser"
Retrieved X entries   ← Should be 5
ObservableCollection now has X ← Should be 5
```

**If any number is 0, report which one!**

### Issue B: Level ComboBox Still Broken

**Test:**
1. Click Level dropdown
2. What do you see? (Take screenshot)
3. Try to select "All"
4. What happens? (Error? Nothing? Works?)

### Issue C: Output Window Shows Error

**Copy the exact error and report it**

## 🎯 Next Test Files (if test-manual.log works)

1. ✅ `tiny.log` - 50 entries (should work)
2. ✅ `tiny.json` - 50 JSON entries (different parser)
3. ✅ `small.log` - 500 entries (performance test)

## 📝 Report Format (if issues persist)

```
Environment:
- Running in: [ ] Visual Studio Debug  [ ] dotnet run  [ ] Release exe
- .NET Version: [from dotnet --version]

Test Results:
- Level ComboBox: [ ] Works  [ ] Broken - [describe]
- DataGrid: [ ] Has entries  [ ] Empty
- Status Bar: [paste text]

Debug Output (first 20 lines):
[paste from Output window]

Binding Errors:
[search Output for "Error" and paste any found]
```

## 🔧 Emergency Test

If nothing works, try this absolute minimum test:

1. Open `LogViewer2026.UI\ViewModels\MainViewModel.cs`
2. Find the constructor
3. Add after `SelectedLogLevelOption = ...`:
```csharp
// EMERGENCY TEST
LogEntries.Add(new LogEntry 
{
    Timestamp = DateTime.Now,
    Level = LogLevel.Information,
    Message = "EMERGENCY TEST ENTRY - If you see this, DataGrid binding works!",
    LineNumber = 999,
    FileOffset = 0
});
```
4. Run app
5. If you see this entry, DataGrid IS working and issue is with file loading/parsing

## ✨ Success Criteria

**ALL these should be true:**
- ✅ Level dropdown shows "All" and is selectable
- ✅ DataGrid shows 5 entries from test-manual.log
- ✅ Can click entries and see details
- ✅ Status bar shows correct counts
- ✅ No errors in Output window

**If yes → SUCCESS! 🎉**

**If no → Report which specific item failed**
