# LogViewer2026 Test File Generator
# Creates test log files with unique content for testing multi-tab functionality

Write-Host "Creating test log files..." -ForegroundColor Green

# Create test directory
$testDir = Join-Path $PSScriptRoot "TestLogs"
if (!(Test-Path $testDir)) {
    New-Item -ItemType Directory -Force -Path $testDir | Out-Null
    Write-Host "Created directory: $testDir" -ForegroundColor Yellow
}

# Test 1: Simple error log
$test1Content = @"
[2024-01-15 10:00:01] ERROR: Database connection failed in test1.log
[2024-01-15 10:00:02] INFO: Retrying connection attempt 1
[2024-01-15 10:00:03] ERROR: Connection timeout after 30 seconds
[2024-01-15 10:00:04] DEBUG: Stack trace: at Database.Connect()
[2024-01-15 10:00:05] ERROR: Max retries exceeded in test1.log
"@
$test1Path = Join-Path $testDir "test1.log"
$test1Content | Out-File -FilePath $test1Path -Encoding UTF8 -Force
Write-Host "Created: test1.log (5 lines, ERROR focused)" -ForegroundColor Cyan

# Test 2: Info-heavy log
$test2Content = @"
[2024-01-15 11:00:01] INFO: Application started successfully in test2.log
[2024-01-15 11:00:02] INFO: Loading configuration from appsettings.json
[2024-01-15 11:00:03] INFO: Connecting to database server
[2024-01-15 11:00:04] WARNING: Slow query detected (500ms)
[2024-01-15 11:00:05] INFO: Application ready to accept requests in test2.log
"@
$test2Path = Join-Path $testDir "test2.log"
$test2Content | Out-File -FilePath $test2Path -Encoding UTF8 -Force
Write-Host "Created: test2.log (5 lines, INFO focused)" -ForegroundColor Cyan

# Test 3: Warning-heavy log
$test3Content = @"
[2024-01-15 12:00:01] DEBUG: Starting background service in test3.log
[2024-01-15 12:00:02] WARNING: Memory usage at 85% in test3.log
[2024-01-15 12:00:03] WARNING: CPU usage spike detected
[2024-01-15 12:00:04] ERROR: Service crashed unexpectedly
[2024-01-15 12:00:05] FATAL: System unable to recover in test3.log
"@
$test3Path = Join-Path $testDir "test3.log"
$test3Content | Out-File -FilePath $test3Path -Encoding UTF8 -Force
Write-Host "Created: test3.log (5 lines, WARNING/FATAL focused)" -ForegroundColor Cyan

# Test 4: Large file for performance testing
$test4Content = ""
for ($i = 1; $i -le 1000; $i++) {
    $level = @("ERROR", "INFO", "WARNING", "DEBUG")[(Get-Random -Maximum 4)]
    $test4Content += "[2024-01-15 13:00:$(($i % 60).ToString('00'))] $level`: Message $i in large test4.log file`n"
}
$test4Path = Join-Path $testDir "test4_large.log"
$test4Content | Out-File -FilePath $test4Path -Encoding UTF8 -Force
Write-Host "Created: test4_large.log (1000 lines, mixed levels)" -ForegroundColor Cyan

# Test 5: Mixed content for filter testing
$test5Content = @"
[2024-01-15 14:00:01] ERROR: Critical error in test5.log
[2024-01-15 14:00:02] ERROR: Another error in test5.log
[2024-01-15 14:00:03] INFO: Some info message
[2024-01-15 14:00:04] ERROR: Third error in test5.log
[2024-01-15 14:00:05] WARNING: Warning message
[2024-01-15 14:00:06] INFO: More info
[2024-01-15 14:00:07] ERROR: Fourth error in test5.log
[2024-01-15 14:00:08] DEBUG: Debug information
[2024-01-15 14:00:09] ERROR: Fifth error in test5.log
[2024-01-15 14:00:10] INFO: Final info message
"@
$test5Path = Join-Path $testDir "test5_mixed.log"
$test5Content | Out-File -FilePath $test5Path -Encoding UTF8 -Force
Write-Host "Created: test5_mixed.log (10 lines, 5 ERRORs for filtering)" -ForegroundColor Cyan

Write-Host "`nTest files created successfully!" -ForegroundColor Green
Write-Host "Location: $testDir" -ForegroundColor Yellow
Write-Host "`nTest Instructions:" -ForegroundColor Magenta
Write-Host "1. Open test1.log - should see 'test1.log' content" -ForegroundColor White
Write-Host "2. Open test2.log - should see 'test2.log' content (DIFFERENT!)" -ForegroundColor White
Write-Host "3. Switch between tabs - content should change" -ForegroundColor White
Write-Host "4. Filter test5_mixed.log by ERROR - should show 5 lines only" -ForegroundColor White

# Create a verification script
$verifyScript = @"
# Quick verification of tab content
Write-Host "`n=== Tab Content Verification ===" -ForegroundColor Green
Write-Host "Expected unique identifiers in each file:" -ForegroundColor Yellow
Write-Host "  test1.log: 'test1.log' appears 2 times"
Write-Host "  test2.log: 'test2.log' appears 2 times"
Write-Host "  test3.log: 'test3.log' appears 3 times"
Write-Host "  test4_large.log: 'test4.log' appears 1000 times"
Write-Host "  test5_mixed.log: 'test5.log' appears 5 times"
Write-Host "`nIf all tabs show the same content, there's a bug!" -ForegroundColor Red
"@
$verifyPath = Join-Path $testDir "VerifyContent.ps1"
$verifyScript | Out-File -FilePath $verifyPath -Encoding UTF8 -Force
Write-Host "`nVerification script created: VerifyContent.ps1" -ForegroundColor Green
