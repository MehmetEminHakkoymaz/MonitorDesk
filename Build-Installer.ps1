[CmdletBinding()]
param(
    # Optional full path to ISCC.exe if Inno Setup is installed elsewhere.
    [string]$CompilerPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src/MonitorDesk/MonitorDesk.csproj'
$checksPath = Join-Path $repoRoot 'tests/MonitorDesk.Checks/MonitorDesk.Checks.csproj'
$installerScript = Join-Path $repoRoot 'installer/MonitorDesk.iss'
$restoreConfig = Join-Path $repoRoot 'installer/NuGet.Config'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Install the .NET 10 SDK to build Monilivo. End users do not need the SDK or runtime.'
}
if (-not $CompilerPath) {
    $compilerCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($compilerCommand) { $CompilerPath = $compilerCommand.Source }
    else {
        $compilerCandidates = @(
            (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'),
            (Join-Path $env:ProgramFiles 'Inno Setup 6/ISCC.exe'),
            (Join-Path $env:ProgramFiles 'Inno Setup 7/ISCC.exe'),
            (Join-Path $env:LOCALAPPDATA 'Programs/Inno Setup 6/ISCC.exe'),
            (Join-Path $env:LOCALAPPDATA 'Programs/Inno Setup 7/ISCC.exe')
        )
        $CompilerPath = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    }
}
if (-not $CompilerPath -or -not (Test-Path -LiteralPath $CompilerPath -PathType Leaf)) {
    throw 'Inno Setup compiler not found. Run: winget install --id JRSoftware.InnoSetup -e -s winget -i. Then rerun Build-Installer.cmd. Alternatively pass -CompilerPath to Build-Installer.ps1.'
}
$CompilerPath = (Resolve-Path -LiteralPath $CompilerPath).Path
[xml]$projectXml = Get-Content -LiteralPath $projectPath -Raw
$appVersion = [string]$projectXml.Project.PropertyGroup.Version
if ($appVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a numeric three-part version in MonitorDesk.csproj.' }

# Each build has a clean staging directory without deleting existing build artifacts.
$stagePath = Join-Path $repoRoot ('artifacts/installer-stage/' + [Guid]::NewGuid().ToString('N'))
$publishPath = Join-Path $stagePath 'publish'
$installerOutput = Join-Path $repoRoot 'artifacts/installers'
New-Item -ItemType Directory -Path $stagePath, $installerOutput -Force | Out-Null

$previousCliHome = $env:DOTNET_CLI_HOME
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$env:DOTNET_CLI_HOME = Join-Path $repoRoot 'artifacts/dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
Push-Location $repoRoot
try {
    Write-Host 'Building and checking Monilivo without changing display settings...'
    & dotnet restore $checksPath --configfile (Join-Path $repoRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'Check project restore failed.' }
    & dotnet build $checksPath -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    & dotnet run --project $checksPath -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Validation failed; no installer was compiled.' }

    Write-Host 'Publishing Windows x64 with the .NET Windows Desktop runtime included...'
    & dotnet restore $projectPath -r win-x64 --configfile $restoreConfig -p:SelfContained=true
    if ($LASTEXITCODE -ne 0) { throw 'Runtime restore failed. Internet access to nuget.org is required on the build machine.' }
    & dotnet publish $projectPath -c Release -r win-x64 --self-contained true --no-restore -o $publishPath -p:PublishTrimmed=false -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw 'Self-contained publish failed.' }
    foreach ($requiredFile in @('Monilivo.exe', 'coreclr.dll', 'hostfxr.dll', 'PresentationFramework.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $publishPath $requiredFile) -PathType Leaf)) {
            throw "Self-contained publish is incomplete: missing $requiredFile."
        }
    }

    # Confirm the staged executable starts with its bundled runtime and reads displays.
    $probePath = Join-Path $stagePath 'probe.json'
    $probe = Start-Process -FilePath (Join-Path $publishPath 'Monilivo.exe') -ArgumentList @('--probe', ('"' + $probePath + '"')) -WindowStyle Hidden -PassThru
    if (-not $probe.WaitForExit(60000)) {
        $probe.Kill()
        throw 'The packaged app did not finish the read-only startup check within 60 seconds.'
    }
    $probe.Refresh()
    if ($probe.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $probePath)) {
        throw 'The packaged app failed its read-only startup check.'
    }

    Write-Host 'Compiling the single setup EXE...'
    & $CompilerPath "/DAppVersion=$appVersion" "/DPublishDir=$publishPath" "/DInstallerOutput=$installerOutput" $installerScript
    if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compilation failed.' }
    $setupPath = Join-Path $installerOutput "Monilivo-Setup-$appVersion-win-x64.exe"
    if (-not (Test-Path -LiteralPath $setupPath -PathType Leaf)) { throw 'Installer output was not found.' }
    $checksum = Get-FileHash -LiteralPath $setupPath -Algorithm SHA256
    $checksum.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($setupPath) | Set-Content -LiteralPath ($setupPath + '.sha256') -Encoding ascii
    Write-Host "Installer ready: $setupPath"
    Write-Host 'Upload the EXE and its .sha256 file as assets of a GitHub Release.'
}
finally {
    Pop-Location
    $env:DOTNET_CLI_HOME = $previousCliHome
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
}
