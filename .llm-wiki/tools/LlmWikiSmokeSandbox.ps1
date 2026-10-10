function Get-LlmWikiSmokeSandboxRoot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [switch]$IndependentRepository
    )

    $root = if ($IndependentRepository) {
        # Native Git/SQLite paths must not grow with an isolated checkout's depth.
        # Source-inventory fixtures still use the repository sandbox below.
        if (-not [string]::IsNullOrWhiteSpace($env:LLM_WIKI_SMOKE_EXTERNAL_SANDBOX)) {
            [IO.Path]::GetFullPath($env:LLM_WIKI_SMOKE_EXTERNAL_SANDBOX)
        } else {
            Join-Path ([IO.Path]::GetTempPath()) "fd-wiki-fixtures-$PID"
        }
    } elseif (-not [string]::IsNullOrWhiteSpace($env:LLM_WIKI_SMOKE_SANDBOX)) {
        [IO.Path]::GetFullPath($env:LLM_WIKI_SMOKE_SANDBOX)
    } else {
        Join-Path $RepositoryRoot ".artifacts/llm-wiki/smoke-standalone/$PID"
    }
    $null = New-Item -ItemType Directory -Path $root -Force
    return $root
}

function New-LlmWikiSmokeFixtureDirectory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$Name,
        [switch]$IndependentRepository
    )

    $root = Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $RepositoryRoot -IndependentRepository:$IndependentRepository
    $path = Join-Path $root "$Name-$([guid]::NewGuid().ToString('N'))"
    if ($IndependentRepository -and
        -not [string]::Equals([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($path)),
            [IO.Path]::GetFullPath($root).TrimEnd('\', '/'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Independent fixture escaped its owned temporary parent: $path"
    }
    $null = New-Item -ItemType Directory -Path $path -Force
    if ($IndependentRepository) {
        & git -C $path init --quiet
        if ($LASTEXITCODE -ne 0 -or -not [IO.Directory]::Exists((Join-Path $path '.git'))) {
            throw "Unable to initialize the owned native fixture repository: $path"
        }
        $gitDirectory = @(& git -C $path rev-parse --git-dir)
        if ($LASTEXITCODE -ne 0 -or $gitDirectory.Count -ne 1 -or
            [string]$gitDirectory[0] -cne '.git') {
            throw "Native fixture resolved a Git directory outside its owned scope: $path"
        }
    }
    return $path
}

function New-LlmWikiSmokeFixtureRepositoryPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$Name
    )

    $taskPrefix = if (-not [string]::IsNullOrWhiteSpace($env:LLM_WIKI_SMOKE_TASK_PREFIX)) {
        $env:LLM_WIKI_SMOKE_TASK_PREFIX
    } else {
        "standalone-$PID"
    }
    $safeName = $Name -replace '[^a-zA-Z0-9_.-]', '-'
    $taskName = ".smoke-$taskPrefix-$safeName-$([guid]::NewGuid().ToString('N'))"
    $path = Join-Path $RepositoryRoot ".artifacts/llm-wiki/tasks/$taskName"
    return $path.Substring(([IO.Path]::GetFullPath($RepositoryRoot)).TrimEnd('\', '/').Length + 1).Replace('\', '/')
}
