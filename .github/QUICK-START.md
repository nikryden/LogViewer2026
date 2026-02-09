# CI/CD Pipeline Quick Start

## Overview

This repository includes three GitHub Actions workflows for automated building, signing, and distribution:

1. **build-and-sign.yml** - Main pipeline (builds + signs + releases)
2. **build-only.yml** - Development pipeline (builds without signing)
3. **create-installer.yml** - Creates Windows installer packages

## Quick Setup (5 Minutes)

### Step 1: Create Code Signing Certificate

Run this PowerShell command on Windows:

```powershell
# Navigate to the scripts directory
cd .github/scripts

# Create certificate and export for GitHub
.\Setup-CodeSigning.ps1 -ExportForGitHub
```

This will:
- Create a self-signed certificate
- Export it as a PFX file
- Convert it to Base64 for GitHub
- Copy the Base64 string to your clipboard

### Step 2: Add GitHub Secrets

1. Go to: `https://github.com/nikryden/LogViewer2026/settings/secrets/actions`
2. Click **New repository secret**
3. Add these two secrets:

   **Secret 1:**
   - Name: `SIGNING_CERTIFICATE_BASE64`
   - Value: [Paste from clipboard]

   **Secret 2:**
   - Name: `SIGNING_CERTIFICATE_PASSWORD`
   - Value: [The password you entered when running the script]

### Step 3: Test the Pipeline

```bash
# Create a test tag to trigger a release build
git tag v1.0.0-test
git push origin v1.0.0-test
```

Then check: `https://github.com/nikryden/LogViewer2026/actions`

## Workflows Explained

### 1. Build and Sign (Main Pipeline)

**File**: `.github/workflows/build-and-sign.yml`

**Triggers:**
- Push to master/main
- New version tags (`v*.*.*`)
- Pull requests
- Manual trigger

**Steps:**
1. ✅ Checkout code
2. ✅ Setup .NET 10
3. ✅ Restore dependencies
4. ✅ Build solution
5. ✅ Run tests
6. ✅ Publish application
7. ✅ Sign executable (production only)
8. ✅ Create ZIP archive
9. ✅ Upload artifacts
10. ✅ Create GitHub release (for tags)

**Output:**
- Signed `LogViewer2026.UI.exe`
- ZIP archive with all files
- GitHub Release (for version tags)

### 2. Build Only (Development)

**File**: `.github/workflows/build-only.yml`

**Triggers:**
- Push to any branch except master/main
- Manual trigger

**Use Case:** Quick validation builds without signing overhead

### 3. Create Installer

**File**: `.github/workflows/create-installer.yml`

**Triggers:**
- New version tags (`v*.*.*`)
- Manual trigger

**Output:**
- Windows installer (`.exe`)
- Automated installation experience

## Creating Releases

### Method 1: Git Tags

```bash
# Create and push a version tag
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

### Method 2: GitHub UI

1. Go to **Releases** → **Draft a new release**
2. Create a new tag (e.g., `v1.0.0`)
3. Publish release
4. Pipeline will automatically build and attach binaries

## Local Testing

### Test the build locally:

```powershell
# Build the solution
dotnet build LogViewer2026.sln --configuration Release

# Publish the application
dotnet publish LogViewer2026.UI/LogViewer2026.UI.csproj `
  --configuration Release `
  --output ./publish
```

### Test signing locally:

```powershell
# Create certificate and sign executable
.\.github\scripts\Setup-CodeSigning.ps1 `
  -ExePath ".\publish\LogViewer2026.UI.exe"
```

## Monitoring Builds

### View Build Status

- **Actions Tab**: https://github.com/nikryden/LogViewer2026/actions
- **Build Logs**: Click on any workflow run to see detailed logs

### Download Artifacts

1. Go to the workflow run
2. Scroll to **Artifacts** section
3. Download the ZIP file

## Troubleshooting

### .NET 10 Not Available

If .NET 10 isn't released yet, update `.github/workflows/*.yml`:

```yaml
env:
  DOTNET_VERSION: '8.0.x'  # Change from 10.0.x to 8.0.x
```

### Signing Fails

**Common issues:**
- Certificate password is incorrect
- Certificate has expired
- SignTool not found on runner

**Solution:**
- Verify secrets in GitHub settings
- Check certificate expiration
- Review build logs for details

### Build Fails

**Check:**
1. All dependencies are restored
2. .NET version matches project requirements
3. No syntax errors in code
4. All tests pass

## Advanced Configuration

### Customize Build Settings

Edit `.github/workflows/build-and-sign.yml`:

```yaml
# Change publish settings
- name: Publish application
  run: |
    dotnet publish ... `
      --self-contained true `        # Include runtime
      --runtime win-x64 `             # Target Windows x64
      -p:PublishSingleFile=true `     # Single EXE file
      -p:IncludeNativeLibrariesForSelfExtract=true
```

### Add Code Coverage

```yaml
- name: Code coverage
  run: |
    dotnet test --collect:"XPlat Code Coverage" --no-build
    
- name: Upload coverage
  uses: codecov/codecov-action@v3
```

### Add CHANGELOG Generation

```yaml
- name: Generate changelog
  uses: mikepenz/release-changelog-builder-action@v4
  with:
    configuration: ".github/changelog-config.json"
```

## Best Practices

1. ✅ **Always tag releases** with semantic versioning (v1.0.0)
2. ✅ **Test on feature branches** before merging to main
3. ✅ **Review build logs** for warnings
4. ✅ **Keep secrets updated** when certificates expire
5. ✅ **Use signed builds** for production releases
6. ✅ **Maintain CHANGELOG** for version history

## Example Release Workflow

```bash
# 1. Finish your feature
git add .
git commit -m "feat: Add new logging feature"

# 2. Merge to main
git checkout main
git merge feature/new-logging-feature

# 3. Create version tag
git tag -a v1.1.0 -m "Version 1.1.0 - New logging features"

# 4. Push everything
git push origin main
git push origin v1.1.0

# 5. Wait for pipeline (check Actions tab)
# 6. Download signed binaries from Releases
```

## Security Notes

- Secrets are only available on the main repository
- Forks cannot access secrets (PRs from forks won't sign)
- Certificate files are never committed to git
- Certificate is cleaned up after each build

## Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET Publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
- [Code Signing Best Practices](https://learn.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools)
