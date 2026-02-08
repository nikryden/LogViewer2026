# PowerShell script to generate test log files
Write-Host "LogViewer2026 Test Log Generator" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

$testDataDir = "TestData"
if (-not (Test-Path $testDataDir)) {
    New-Item -ItemType Directory -Path $testDataDir | Out-Null
}

$levels = @("VRB", "DBG", "INF", "WRN", "ERR", "FTL")
$levelsFull = @("Verbose", "Debug", "Information", "Warning", "Error", "Fatal")
$messages = @(
    "Application started successfully",
    "User logged in with ID: {0}",
    "Processing payment transaction for amount: ${0}",
    "Database query executed in {0}ms",
    "Cache miss occurred for key: {0}",
    "Email sent to user@example.com",
    "File uploaded: document_{0}.pdf",
    "API request received from IP: 192.168.1.{0}",
    "Background job started: Job_{0}",
    "Configuration loaded from settings.json",
    "Service initialized successfully",
    "Connection established to database",
    "Request completed with status code: {0}",
    "Invalid input received from user",
    "Timeout occurred while waiting for response",
    "Exception caught: NullReferenceException",
    "Retry attempt {0} of 3",
    "Session expired for user ID: {0}",
    "File not found: missing_{0}.log",
    "Critical error: System out of memory"
)

$sourceContexts = @(
    "MyApp.Controllers.UserController",
    "MyApp.Services.PaymentService",
    "MyApp.Data.Repository",
    "MyApp.Services.CacheService",
    "MyApp.Services.EmailService",
    "MyApp.Controllers.FileController",
    "MyApp.API.Gateway",
    "MyApp.Jobs.BackgroundWorker",
    "MyApp.Configuration.Loader",
    "MyApp.Services.Initializer"
)

function Generate-TextLog {
    param(
        [string]$FilePath,
        [int]$LineCount,
        [string]$Description
    )
    
    Write-Host "Generating: $Description..." -NoNewline
    $startTime = Get-Date
    
    $content = @()
    $baseTime = (Get-Date).AddDays(-7)
    
    for ($i = 0; $i -lt $LineCount; $i++) {
        $timestamp = $baseTime.AddSeconds($i * 5)
        
        # Determine log level based on position
        $level = "INF"
        if ($i % 100 -eq 0) { $level = "ERR" }
        elseif ($i % 50 -eq 0) { $level = "WRN" }
        elseif ($i % 20 -eq 0) { $level = "DBG" }
        elseif ($i % 200 -eq 0) { $level = "FTL" }
        elseif ($i % 10 -eq 0) { $level = "VRB" }
        
        $message = $messages | Get-Random
        $message = $message -f (Get-Random -Minimum 1 -Maximum 999)
        $sourceContext = $sourceContexts | Get-Random
        
        $line = "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2} {3}" -f $timestamp, $level, $sourceContext, $message
        $content += $line
    }
    
    $content | Out-File -FilePath $FilePath -Encoding UTF8
    Write-Host " Done! ($LineCount lines)" -ForegroundColor Green
}

function Generate-JsonLog {
    param(
        [string]$FilePath,
        [int]$LineCount,
        [string]$Description
    )
    
    Write-Host "Generating: $Description..." -NoNewline
    
    $content = @()
    $baseTime = (Get-Date).AddDays(-7)
    
    for ($i = 0; $i -lt $LineCount; $i++) {
        $timestamp = $baseTime.AddSeconds($i * 5)
        
        # Determine log level
        $levelIndex = 2  # Information
        if ($i % 100 -eq 0) { $levelIndex = 4 }      # Error
        elseif ($i % 50 -eq 0) { $levelIndex = 3 }   # Warning
        elseif ($i % 20 -eq 0) { $levelIndex = 1 }   # Debug
        elseif ($i % 200 -eq 0) { $levelIndex = 5 }  # Fatal
        elseif ($i % 10 -eq 0) { $levelIndex = 0 }   # Verbose
        
        $level = $levelsFull[$levelIndex]
        $message = $messages | Get-Random
        $message = $message -f (Get-Random -Minimum 1 -Maximum 999)
        $sourceContext = $sourceContexts | Get-Random
        
        $logEntry = @{
            Timestamp = $timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            Level = $level
            MessageTemplate = $message
            RenderedMessage = $message
            SourceContext = $sourceContext
            Properties = @{
                UserId = Get-Random -Minimum 1 -Maximum 1000
                RequestId = [Guid]::NewGuid().ToString()
            }
        } | ConvertTo-Json -Compress
        
        $content += $logEntry
    }
    
    $content | Out-File -FilePath $FilePath -Encoding UTF8
    Write-Host " Done! ($LineCount lines)" -ForegroundColor Green
}

function Generate-ErrorHeavyLog {
    param(
        [string]$FilePath,
        [int]$LineCount
    )
    
    Write-Host "Generating: Error-heavy log..." -NoNewline
    
    $content = @()
    $baseTime = (Get-Date).AddDays(-1)
    
    for ($i = 0; $i -lt $LineCount; $i++) {
        $timestamp = $baseTime.AddSeconds($i * 2)
        
        # 40% errors, 30% warnings, 30% info
        $rand = Get-Random -Minimum 0 -Maximum 100
        if ($rand -lt 40) { $level = "ERR" }
        elseif ($rand -lt 70) { $level = "WRN" }
        else { $level = "INF" }
        
        $message = $messages | Get-Random
        $message = $message -f (Get-Random -Minimum 1 -Maximum 999)
        $sourceContext = $sourceContexts | Get-Random
        
        $line = "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2} {3}" -f $timestamp, $level, $sourceContext, $message
        $content += $line
    }
    
    $content | Out-File -FilePath $FilePath -Encoding UTF8
    Write-Host " Done! ($LineCount lines)" -ForegroundColor Green
}

# Generate test files
Write-Host "Creating test log files in '$testDataDir' directory..." -ForegroundColor Yellow
Write-Host ""

# Small files for quick testing
Generate-TextLog -FilePath "$testDataDir\tiny.log" -LineCount 50 -Description "Tiny text log (50 lines)"
Generate-JsonLog -FilePath "$testDataDir\tiny.json" -LineCount 50 -Description "Tiny JSON log (50 lines)"

# Small files
Generate-TextLog -FilePath "$testDataDir\small.log" -LineCount 500 -Description "Small text log (500 lines)"
Generate-JsonLog -FilePath "$testDataDir\small.json" -LineCount 500 -Description "Small JSON log (500 lines)"

# Medium files
Generate-TextLog -FilePath "$testDataDir\medium.log" -LineCount 5000 -Description "Medium text log (5,000 lines)"
Generate-JsonLog -FilePath "$testDataDir\medium.json" -LineCount 5000 -Description "Medium JSON log (5,000 lines)"

# Large files
Generate-TextLog -FilePath "$testDataDir\large.log" -LineCount 50000 -Description "Large text log (50,000 lines)"
Generate-JsonLog -FilePath "$testDataDir\large.json" -LineCount 50000 -Description "Large JSON log (50,000 lines)"

# Extra large files
Write-Host "Generating: Extra large text log (500,000 lines)..." -NoNewline
Generate-TextLog -FilePath "$testDataDir\xlarge.log" -LineCount 500000 -Description "Extra large text log (500,000 lines)"

# Error-heavy log for testing error filtering
Generate-ErrorHeavyLog -FilePath "$testDataDir\errors.log" -LineCount 1000

Write-Host ""
Write-Host "Summary of generated files:" -ForegroundColor Cyan
Write-Host "===========================" -ForegroundColor Cyan

Get-ChildItem -Path $testDataDir | ForEach-Object {
    $size = if ($_.Length -gt 1MB) {
        "{0:N2} MB" -f ($_.Length / 1MB)
    } elseif ($_.Length -gt 1KB) {
        "{0:N2} KB" -f ($_.Length / 1KB)
    } else {
        "{0} bytes" -f $_.Length
    }
    
    Write-Host ("  {0,-20} {1,12}" -f $_.Name, $size)
}

Write-Host ""
Write-Host "Test files ready! You can now open them in LogViewer2026." -ForegroundColor Green
Write-Host "Path: $((Get-Item $testDataDir).FullName)" -ForegroundColor Gray
