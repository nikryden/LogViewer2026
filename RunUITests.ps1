# LogViewer2026 - Automated UI Test Runner
# Runs all UI tests with detailed output

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "  LogViewer2026 - UI Test Runner" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Check if solution exists
if (!(Test-Path "LogViewer2026.sln")) {
    Write-Host "ERROR: LogViewer2026.sln not found!" -ForegroundColor Red
    Write-Host "Please run this script from the solution directory." -ForegroundColor Yellow
    exit 1
}

# Step 1: Build the UI application
Write-Host "[1/4] Building LogViewer2026.UI..." -ForegroundColor Yellow
dotnet build LogViewer2026.UI\LogViewer2026.UI.csproj --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful" -ForegroundColor Green
Write-Host ""

# Step 2: Check if application exists
$appPath = "LogViewer2026.UI\bin\Debug\net10.0-windows\LogViewer2026.UI.exe"
if (!(Test-Path $appPath)) {
    Write-Host "ERROR: Application not found at $appPath" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Application found at $appPath" -ForegroundColor Green
Write-Host ""

# Step 3: Close any running instances
Write-Host "[2/4] Checking for running instances..." -ForegroundColor Yellow
$processes = Get-Process -Name "LogViewer2026.UI" -ErrorAction SilentlyContinue
if ($processes) {
    Write-Host "⚠️ Found $($processes.Count) running instance(s). Closing..." -ForegroundColor Yellow
    $processes | Stop-Process -Force
    Start-Sleep -Seconds 2
    Write-Host "✅ Instances closed" -ForegroundColor Green
} else {
    Write-Host "✅ No running instances found" -ForegroundColor Green
}
Write-Host ""

# Step 4: Run tests
Write-Host "[3/4] Running UI Tests..." -ForegroundColor Yellow
Write-Host "This may take 1-2 minutes. The application will launch and close automatically." -ForegroundColor Cyan
Write-Host ""

# Run with detailed output
dotnet test LogViewer2026.UITests\LogViewer2026.UITests.csproj `
    --configuration Debug `
    --logger "console;verbosity=detailed" `
    --no-build

$exitCode = $LASTEXITCODE
Write-Host ""

# Step 5: Summary
Write-Host "[4/4] Test Summary" -ForegroundColor Yellow
Write-Host "====================================" -ForegroundColor Cyan

if ($exitCode -eq 0) {
    Write-Host "✅ ALL TESTS PASSED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "The multi-tab functionality is working correctly!" -ForegroundColor Green
    Write-Host "Each tab shows its own unique content." -ForegroundColor Green
} else {
    Write-Host "❌ SOME TESTS FAILED!" -ForegroundColor Red
    Write-Host ""
    Write-Host "This indicates there are still issues with multi-tab functionality." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Key test to check:" -ForegroundColor Yellow
    Write-Host "  - Test03_TwoTabs_ShowDifferentContent" -ForegroundColor White
    Write-Host ""
    Write-Host "If Test03 failed:" -ForegroundColor Yellow
    Write-Host "  → Tabs are showing the same content instead of unique content" -ForegroundColor White
    Write-Host "  → Check debug output above for 'Content after opening' messages" -ForegroundColor White
    Write-Host "  → Review TextEditor_Loaded in MainWindow.xaml.cs" -ForegroundColor White
}

Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Cleanup
Write-Host "Cleaning up..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
$remainingProcesses = Get-Process -Name "LogViewer2026.UI" -ErrorAction SilentlyContinue
if ($remainingProcesses) {
    Write-Host "⚠️ Closing remaining instances..." -ForegroundColor Yellow
    $remainingProcesses | Stop-Process -Force
}
Write-Host "✅ Cleanup complete" -ForegroundColor Green
Write-Host ""

# Return exit code
exit $exitCode
