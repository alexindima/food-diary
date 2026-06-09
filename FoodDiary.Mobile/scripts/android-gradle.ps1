param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $GradleArgs
)

$ErrorActionPreference = 'Stop'

if ($GradleArgs.Count -eq 0) {
    $GradleArgs = @('assembleDebug')
}

$mobileRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$repoRoot = Resolve-Path (Join-Path $mobileRoot '..')
$androidRoot = Join-Path $mobileRoot 'android'

function Get-EnvironmentValue {
    param([string] $Name)

    $value = [Environment]::GetEnvironmentVariable($Name, 'Process')
    if (-not [string]::IsNullOrWhiteSpace($value)) {
        return $value
    }

    $value = [Environment]::GetEnvironmentVariable($Name, 'User')
    if (-not [string]::IsNullOrWhiteSpace($value)) {
        return $value
    }

    return [Environment]::GetEnvironmentVariable($Name, 'Machine')
}

function Resolve-JavaHome {
    $javaHome = Get-EnvironmentValue 'JAVA_HOME'
    if (-not [string]::IsNullOrWhiteSpace($javaHome) -and (Test-Path (Join-Path $javaHome 'bin\java.exe'))) {
        return $javaHome
    }

    $adoptiumRoot = 'C:\Program Files\Eclipse Adoptium'
    if (Test-Path $adoptiumRoot) {
        $candidate = Get-ChildItem $adoptiumRoot -Directory |
            Where-Object { $_.Name -like 'jdk-21*' } |
            Sort-Object Name -Descending |
            Select-Object -First 1

        if ($candidate -and (Test-Path (Join-Path $candidate.FullName 'bin\java.exe'))) {
            return $candidate.FullName
        }
    }

    throw 'JAVA_HOME is not set and a JDK 21 installation was not found under C:\Program Files\Eclipse Adoptium.'
}

function Resolve-AndroidHome {
    $androidHome = Get-EnvironmentValue 'ANDROID_HOME'
    if (-not [string]::IsNullOrWhiteSpace($androidHome) -and (Test-Path $androidHome)) {
        return $androidHome
    }

    $androidHome = Get-EnvironmentValue 'ANDROID_SDK_ROOT'
    if (-not [string]::IsNullOrWhiteSpace($androidHome) -and (Test-Path $androidHome)) {
        return $androidHome
    }

    $defaultSdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk'
    if (Test-Path $defaultSdk) {
        return $defaultSdk
    }

    throw 'Android SDK was not found. Install it with Android Studio or set ANDROID_HOME.'
}

function Get-SubstMappings {
    $lines = & subst
    $mappings = @{}
    foreach ($line in $lines) {
        if ($line -match '^([A-Z]:)\\: => (.+)$') {
            $mappings[$Matches[1]] = $Matches[2]
        }
    }

    return $mappings
}

function Get-AsciiRepoRoot {
    param([string] $TargetPath)

    $mappings = Get-SubstMappings
    foreach ($entry in $mappings.GetEnumerator()) {
        if ([string]::Equals($entry.Value.TrimEnd('\'), $TargetPath.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
            return @{
                Path = "$($entry.Key)\"
                Created = $false
            }
        }
    }

    foreach ($letter in @('R:', 'M:', 'Z:', 'Y:', 'X:', 'W:', 'V:')) {
        if (-not (Get-PSDrive -Name $letter.TrimEnd(':') -ErrorAction SilentlyContinue)) {
            & subst $letter $TargetPath
            return @{
                Path = "$letter\"
                Created = $true
                Drive = $letter
            }
        }
    }

    throw 'No free drive letter was available for a temporary ASCII path.'
}

$env:JAVA_HOME = Resolve-JavaHome
$env:ANDROID_HOME = Resolve-AndroidHome
$env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
$env:Path = "$env:JAVA_HOME\bin;$env:ANDROID_HOME\platform-tools;$env:ANDROID_HOME\cmdline-tools\latest\bin;$env:Path"

$mappedRoot = Get-AsciiRepoRoot $repoRoot.Path
try {
    $androidPath = Join-Path $mappedRoot.Path 'FoodDiary.Mobile\android'
    Set-Location $androidPath
    & .\gradlew.bat @GradleArgs
    exit $LASTEXITCODE
} finally {
    if ($mappedRoot.Created -and $mappedRoot.Drive) {
        & subst $mappedRoot.Drive /D
    }

    Set-Location $repoRoot
}
