# Stop running instances of the app to avoid file lock issues
Write-Host "Checking for running instances of SystemHub..."
Stop-Process -Name "SystemHub" -ErrorAction SilentlyContinue

# Read current version from SystemHub.csproj
[xml]$csproj = Get-Content SystemHub.csproj
$currentVersion = $csproj.Project.PropertyGroup.Version
Write-Host "Current version: $currentVersion"

# Use the version defined in csproj directly
$newVersion = $currentVersion
Write-Host "Releasing version: $newVersion"

# Update version in setup.iss
$setupContent = Get-Content setup.iss
$setupContent = $setupContent -replace '#define AppVersion "[^"]+"', "#define AppVersion `"$newVersion`""
$setupContent | Set-Content setup.iss

# Clean and publish app
Write-Host "Publishing application..."
dotnet publish -c Release -r win-x86 -o publish --self-contained true

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed!"
    exit 1
}

# Compile setup with Inno Setup
Write-Host "Compiling setup installer..."
$isccPath = "$env:USERPROFILE\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $isccPath)) {
    # Fallback to Antigravity IDE node_modules path
    $isccPath = "$env:USERPROFILE\AppData\Local\Programs\Antigravity IDE\resources\app\node_modules\innosetup\bin\ISCC.exe"
}

if (-not (Test-Path $isccPath)) {
    Write-Error "ISCC.exe not found! Please check your Inno Setup installation path."
    exit 1
}

Write-Host "Using ISCC at: $isccPath"
& $isccPath setup.iss

if ($LASTEXITCODE -ne 0) {
    Write-Error "Installer compilation failed!"
    exit 1
}

Write-Host "Committing changes to Git..."
git add -u
git add -f SystemHubSetup.exe
git commit -m "Release v$newVersion"
git tag -f "v$newVersion"
git tag -f "latest"

Write-Host "Pushing to remote repository..."
git push origin -f "v$newVersion"
git push origin -f "latest"

Write-Host "Successfully released v$newVersion!"
