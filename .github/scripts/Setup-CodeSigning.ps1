# Helper script for creating and testing code signing certificates locally
# Run this script on Windows to create a self-signed certificate for testing

param(
    [Parameter(Mandatory=$false)]
    [string]$CertificateName = "LogViewer2026 Development",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\LogViewer2026-CodeSign.pfx",
    
    [Parameter(Mandatory=$false)]
    [SecureString]$Password,
    
    [Parameter(Mandatory=$false)]
    [switch]$ExportForGitHub,
    
    [Parameter(Mandatory=$false)]
    [string]$ExePath
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LogViewer2026 - Code Signing Helper" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Warning "Not running as administrator. Some operations may fail."
    Write-Host "Consider running this script as administrator." -ForegroundColor Yellow
    Write-Host ""
}

# Function to create certificate
function New-CodeSigningCertificate {
    Write-Host "Creating self-signed code signing certificate..." -ForegroundColor Green
    
    if (-not $script:Password) {
        $script:Password = Read-Host "Enter password for certificate" -AsSecureString
    }
    
    # Create certificate
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=$CertificateName" `
        -KeyUsage DigitalSignature `
        -FriendlyName $CertificateName `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
        -NotAfter (Get-Date).AddYears(2)
    
    Write-Host "? Certificate created successfully" -ForegroundColor Green
    Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
    Write-Host "  Expires: $($cert.NotAfter)" -ForegroundColor Gray
    
    # Export to PFX
    Export-PfxCertificate -Cert $cert -FilePath $OutputPath -Password $script:Password | Out-Null
    Write-Host "? Certificate exported to: $OutputPath" -ForegroundColor Green
    
    return $cert, $script:Password
}

# Function to convert certificate to Base64 for GitHub
function ConvertTo-GitHubSecret {
    param($PfxPath, $SecPassword)
    
    Write-Host ""
    Write-Host "Converting certificate for GitHub Secrets..." -ForegroundColor Green
    
    $bytes = [System.IO.File]::ReadAllBytes($PfxPath)
    $base64 = [System.Convert]::ToBase64String($bytes)
    
    Write-Host "? Certificate converted to Base64" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "GitHub Secrets Configuration" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. SIGNING_CERTIFICATE_BASE64:" -ForegroundColor Cyan
    Write-Host "   (Base64 string has been copied to clipboard)" -ForegroundColor Gray
    $base64 | Set-Clipboard
    Write-Host "   ? Copied to clipboard - paste in GitHub" -ForegroundColor Green
    Write-Host ""
    Write-Host "2. SIGNING_CERTIFICATE_PASSWORD:" -ForegroundColor Cyan
    $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecPassword))
    Write-Host "   Password: $plainPassword" -ForegroundColor Gray
    Write-Host ""
    Write-Host "To add these secrets:" -ForegroundColor Yellow
    Write-Host "  1. Go to: https://github.com/nikryden/LogViewer2026/settings/secrets/actions" -ForegroundColor Gray
    Write-Host "  2. Click 'New repository secret'" -ForegroundColor Gray
    Write-Host "  3. Add both secrets listed above" -ForegroundColor Gray
    Write-Host ""
}

# Function to sign an executable
function Invoke-SignExecutable {
    param($ExeFilePath, $CertPath, $SecPassword)
    
    Write-Host ""
    Write-Host "Signing executable..." -ForegroundColor Green
    
    # Find SignTool
    $signtool = Get-ChildItem "C:\Program Files (x86)\Windows Kits\*\bin\*\x64\signtool.exe" -Recurse -ErrorAction SilentlyContinue | 
        Sort-Object FullName -Descending | 
        Select-Object -First 1
    
    if (-not $signtool) {
        Write-Error "SignTool.exe not found. Please install Windows SDK."
        return
    }
    
    Write-Host "Using SignTool: $($signtool.FullName)" -ForegroundColor Gray
    
    $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecPassword))
    
    & $signtool.FullName sign `
        /f $CertPath `
        /p $plainPassword `
        /tr http://timestamp.digicert.com `
        /td sha256 `
        /fd sha256 `
        /d "LogViewer2026 - High-Performance Serilog Log Viewer" `
        $ExeFilePath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Executable signed successfully" -ForegroundColor Green
        
        # Verify signature
        & $signtool.FullName verify /pa $ExeFilePath
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Signature verified" -ForegroundColor Green
        }
    }
    else {
        Write-Error "Signing failed with exit code: $LASTEXITCODE"
    }
}

# Main execution
try {
    # Convert to absolute path
    $OutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)

    if (Test-Path $OutputPath) {
        $overwrite = Read-Host "Certificate file already exists. Overwrite? (y/n)"
        if ($overwrite -ne 'y') {
            Write-Host "Operation cancelled." -ForegroundColor Yellow
            exit 0
        }
    }
    
    # Create certificate
    $cert, $secPassword = New-CodeSigningCertificate
    
    # Export for GitHub if requested
    if ($ExportForGitHub) {
        ConvertTo-GitHubSecret -PfxPath $OutputPath -SecPassword $secPassword
    }
    
    # Sign executable if provided
    if ($ExePath) {
        # Convert to absolute path
        $ExePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ExePath)

        if (-not (Test-Path $ExePath)) {
            Write-Warning "Executable not found: $ExePath"
        }
        else {
            Invoke-SignExecutable -ExeFilePath $ExePath -CertPath $OutputPath -SecPassword $secPassword
        }
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Setup Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Add the GitHub secrets (if using -ExportForGitHub)" -ForegroundColor Gray
    Write-Host "  2. Push code to trigger the build pipeline" -ForegroundColor Gray
    Write-Host "  3. Check the Actions tab for build progress" -ForegroundColor Gray
    Write-Host ""
    Write-Host "??  WARNING: This is a self-signed certificate!" -ForegroundColor Yellow
    Write-Host "    Users will see security warnings." -ForegroundColor Yellow
    Write-Host "    For production, use a commercial certificate." -ForegroundColor Yellow
    Write-Host ""
}
catch {
    Write-Error "An error occurred: $_"
    exit 1
}
