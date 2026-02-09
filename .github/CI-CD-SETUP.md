# CI/CD Pipeline Setup Guide

This document explains how to set up the GitHub Actions CI/CD pipeline for building and signing LogViewer2026.

## Pipeline Features

- ✅ Automatic builds on push to master/main
- ✅ Pull request validation
- ✅ Code signing with certificate
- ✅ Automated versioning
- ✅ Artifact creation and storage
- ✅ Automatic GitHub releases for version tags
- ✅ .NET 10 support

## Required GitHub Secrets

To enable code signing, you need to add the following secrets to your GitHub repository:

### 1. SIGNING_CERTIFICATE_BASE64

This is your code signing certificate in Base64 format.

**To create this secret:**

```powershell
# Convert your PFX certificate to Base64
$pfxPath = "path\to\your\certificate.pfx"
$bytes = [System.IO.File]::ReadAllBytes($pfxPath)
$base64 = [System.Convert]::ToBase64String($bytes)
$base64 | Set-Clipboard
# Now paste this into GitHub Secrets
```

### 2. SIGNING_CERTIFICATE_PASSWORD

The password for your PFX certificate.

## Setting Up GitHub Secrets

1. Go to your GitHub repository
2. Click on **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add both secrets:
   - Name: `SIGNING_CERTIFICATE_BASE64`
   - Value: [Your Base64-encoded certificate]
   
   - Name: `SIGNING_CERTIFICATE_PASSWORD`
   - Value: [Your certificate password]

## How to Obtain a Code Signing Certificate

### Option 1: Commercial Certificate (Recommended for Production)

Purchase a code signing certificate from a trusted Certificate Authority (CA):
- **DigiCert**: https://www.digicert.com/signing/code-signing-certificates
- **Sectigo**: https://sectigo.com/ssl-certificates-tls/code-signing
- **GlobalSign**: https://www.globalsign.com/en/code-signing-certificate

### Option 2: Self-Signed Certificate (For Testing Only)

```powershell
# Create a self-signed certificate (Windows only)
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=LogViewer2026 Development" `
    -KeyUsage DigitalSignature `
    -FriendlyName "LogViewer2026 Code Signing" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
    -NotAfter (Get-Date).AddYears(2)

# Export to PFX
$password = ConvertTo-SecureString -String "YourPasswordHere" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "LogViewer2026-CodeSign.pfx" -Password $password
```

⚠️ **Warning**: Self-signed certificates will show security warnings to users. Use only for development/testing.

## Triggering Builds

### Automatic Triggers

- **Push to master/main**: Builds and signs the application
- **Pull Requests**: Builds without signing (validation only)
- **Version Tags**: Creates a GitHub release with signed binaries

### Creating a Release

To create a new release:

```bash
# Tag the commit
git tag -a v1.0.0 -m "Release version 1.0.0"

# Push the tag
git push origin v1.0.0
```

This will:
1. Build the application
2. Sign the executable
3. Create a ZIP archive
4. Create a GitHub Release
5. Upload the signed binaries

### Manual Trigger

You can also manually trigger the workflow:
1. Go to **Actions** tab in GitHub
2. Select **Build and Sign LogViewer2026**
3. Click **Run workflow**

## Build Artifacts

After each successful build, the following artifacts are available:

### Artifacts (30-day retention)
- **LogViewer2026-v{VERSION}**: Complete build output with all files

### ZIP Archive (90-day retention)
- **LogViewer2026-v{VERSION}-windows.zip**: Ready-to-distribute package

## Versioning

- **Tagged releases**: Use the tag version (e.g., `v1.0.0` → version `1.0.0`)
- **Development builds**: Use `1.0.0-dev.{BUILD_NUMBER}` format

## Workflow Configuration

The workflow file is located at: `.github/workflows/build-and-sign.yml`

### Key Environment Variables

```yaml
DOTNET_VERSION: '10.0.x'              # .NET SDK version
PROJECT_PATH: 'LogViewer2026.UI/...'   # Path to main project
BUILD_CONFIGURATION: 'Release'         # Build configuration
```

### Customizing the Build

You can customize the build by modifying the workflow file:

- **Change output directory**: Modify `OUTPUT_DIR`
- **Add additional steps**: Insert new steps in the workflow
- **Modify publish settings**: Edit the `dotnet publish` command
- **Change signing parameters**: Update the `signtool sign` command

## Troubleshooting

### Build Fails: .NET 10 SDK Not Found

The .NET 10 SDK may not be generally available yet. Options:
1. Use a preview version: `dotnet-version: '10.0.x-preview'`
2. Downgrade to .NET 8: `dotnet-version: '8.0.x'`

### Signing Fails: SignTool Not Found

The workflow automatically searches for SignTool. If it fails:
- Verify Windows SDK is installed on the runner
- Check the SignTool path in the workflow

### Certificate Issues

- Ensure the Base64 string is complete (no line breaks)
- Verify the password is correct
- Check certificate hasn't expired
- Ensure certificate is a valid code signing certificate

### Unsigned Builds on PRs

This is intentional. Pull requests don't have access to secrets for security reasons.

## Security Best Practices

1. **Never commit certificates** to the repository
2. **Use GitHub secrets** for sensitive data
3. **Rotate certificates** before expiration
4. **Limit secret access** to necessary workflows only
5. **Review PRs** before merging to protected branches

## Testing the Pipeline

To test without signing:

1. Comment out the signing steps in the workflow
2. Push to a test branch
3. Verify the build succeeds
4. Once confirmed, add secrets and test signing

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Code Signing Guide](https://learn.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools)
- [.NET Application Publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/)

## Support

For issues with the CI/CD pipeline:
1. Check the Actions logs in GitHub
2. Review this setup guide
3. Open an issue in the repository
