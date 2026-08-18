function Get-LlmWikiSmokeSandboxRoot {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    $root = if (-not [string]::IsNullOrWhiteSpace($env:LLM_WIKI_SMOKE_SANDBOX)) {
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
        [Parameter(Mandatory)][string]$Name
    )

    $root = Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $RepositoryRoot
    $path = Join-Path $root "$Name-$([guid]::NewGuid().ToString('N'))"
    $null = New-Item -ItemType Directory -Path $path -Force
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
