[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$sandboxParent = Join-Path $repositoryRoot '.artifacts/llm-wiki/json-cold-checkout'
$sandboxRoot = Join-Path $sandboxParent ([Guid]::NewGuid().ToString('N'))
$checkoutRoot = Join-Path $sandboxRoot 'repository'

function Assert-ColdCheckout([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Invoke-JsonFacade([string]$Facade, [string]$FacadeCommand, [hashtable]$FacadeParameters) {
    $result = & $Facade $FacadeCommand @FacadeParameters | ConvertFrom-Json
    if (-not $?) { throw "JSON facade failed: $FacadeCommand" }
    return $result
}

function Invoke-JsonTool([string]$ToolPath, [hashtable]$ToolParameters) {
    $result = & $ToolPath @ToolParameters | ConvertFrom-Json
    if (-not $?) { throw "JSON tool failed: $ToolPath" }
    return $result
}

try {
    $null = New-Item -ItemType Directory -Path $sandboxRoot -Force
    & git clone --shared --quiet --no-checkout $repositoryRoot $checkoutRoot
    if ($LASTEXITCODE -ne 0) { throw 'Unable to create the cold-checkout fixture.' }
    & git -C $checkoutRoot checkout --quiet --force HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Unable to check out the cold-checkout fixture.' }

    $sourceWikiRoot = Join-Path $repositoryRoot '.llm-wiki'
    $checkoutWikiRoot = Join-Path $checkoutRoot '.llm-wiki'
    Get-ChildItem -LiteralPath $sourceWikiRoot -Force |
        Copy-Item -Destination $checkoutWikiRoot -Recurse -Force

    Assert-ColdCheckout (-not (Test-Path -LiteralPath (Join-Path $checkoutRoot 'FoodDiary.Web.Client/node_modules'))) `
        'Cold-checkout fixture unexpectedly contains frontend node_modules.'

    $graphToolPath = Join-Path $checkoutWikiRoot 'tools/code-graph.mjs'
    [IO.File]::WriteAllText(
        $graphToolPath,
        "throw new Error('JSON cold-checkout contract invoked the code graph.');`n",
        [Text.UTF8Encoding]::new($false))

    $facade = Join-Path $checkoutWikiRoot 'wiki.ps1'
    $coldStartObjective = 'независимый широкий аудит всего проекта'
    $start = Invoke-JsonFacade -Facade $facade -FacadeCommand start -FacadeParameters @{
        Objective = $coldStartObjective; Format = 'Json'; Limit = 3
        TaskSessionId = "json-cold-checkout-$([Guid]::NewGuid().ToString('N'))"
    }
    Assert-ColdCheckout (
        [string]$start.objective -eq $coldStartObjective -and
        [string]$start.research.discovery.runtimeFlow.status -eq 'not-requested-json-baseline') `
        'Cold-checkout start did not complete through the JSON baseline.'

    $defaultBrief = Invoke-JsonFacade -Facade $facade -FacadeCommand brief -FacadeParameters @{
        Objective = 'audit wearable synchronization'; ProposedPath = @('FoodDiary.Application.Wearables')
        Compact = $true; Format = 'Json'; Limit = 3
    }
    Assert-ColdCheckout ([string]$defaultBrief.analysis.mode -eq 'planned-paths') 'Cold-checkout default brief did not fall back to the JSON baseline.'

    $develop = Invoke-JsonFacade -Facade $facade -FacadeCommand develop -FacadeParameters @{
        Objective = 'audit wearable synchronization'; ProposedPath = @('FoodDiary.Application.Wearables')
        Format = 'Json'; Limit = 3; TaskSessionId = "json-cold-develop-$([Guid]::NewGuid().ToString('N'))"
    }
    Assert-ColdCheckout (-not [string]::IsNullOrWhiteSpace([string]$develop.profile)) 'Cold-checkout develop did not complete through the automatic JSON baseline.'

    $research = Invoke-JsonFacade -Facade $facade -FacadeCommand research -FacadeParameters @{
        Objective = 'audit wearable synchronization'; ProposedPath = @('FoodDiary.Application.Wearables')
        Compact = $true; SkipHistory = $true; CompiledIndexSource = 'Json'; Format = 'Json'; Limit = 3
    }
    Assert-ColdCheckout (@($research.discovery.groundedPaths).Count -gt 0) 'Cold-checkout research did not ground any current-source path.'
    Assert-ColdCheckout ([string]$research.discovery.runtimeFlow.status -eq 'not-requested-json-baseline') 'Cold-checkout research attempted graph expansion in JSON mode.'

    $trace = Invoke-JsonTool -ToolPath (Join-Path $checkoutWikiRoot 'tools/Find-LlmWikiTraceCandidates.ps1') -ToolParameters @{
        Query = 'Trace primary user scenario end to end for wearable synchronization.'; CompiledIndexSource = 'Json'; Format = 'Json'; Limit = 3
    }
    Assert-ColdCheckout (@($trace.entryCandidates).Count -gt 0) 'Cold-checkout trace did not return bounded candidates.'

    $ownership = Invoke-JsonTool -ToolPath (Join-Path $checkoutWikiRoot 'tools/Find-LlmWikiIntentOwnership.ps1') -ToolParameters @{
        Query = 'SyncWearableDataCommand'; CompiledIndexSource = 'Json'; Format = 'Json'; Limit = 3
    }
    Assert-ColdCheckout (@($ownership.candidates).Count -gt 0 -and [string]$ownership.index.source -eq 'json-baseline') 'Cold-checkout ownership did not use the JSON baseline.'

    $topology = Invoke-JsonTool -ToolPath (Join-Path $checkoutWikiRoot 'tools/Find-LlmWikiRuntimeTopology.ps1') -ToolParameters @{
        Query = 'wearable'; CompiledIndexSource = 'Json'; Format = 'Json'; Limit = 3
    }
    Assert-ColdCheckout (@($topology.PSObject.Properties).Count -gt 0) 'Cold-checkout topology returned no groups.'

    $privacy = Invoke-JsonTool -ToolPath (Join-Path $checkoutWikiRoot 'tools/Find-LlmWikiSensitiveData.ps1') -ToolParameters @{
        Query = 'wearable health data'; RepositoryWide = $true; CompiledIndexSource = 'Json'; Format = 'Json'; Limit = 3
    }
    Assert-ColdCheckout (@($privacy.PSObject.Properties).Count -gt 0) 'Cold-checkout privacy returned no evidence.'

    $security = Invoke-JsonTool -ToolPath (Join-Path $checkoutWikiRoot 'tools/Find-LlmWikiSecurityReview.ps1') -ToolParameters @{
        Query = 'wearable token protection'; CompiledIndexSource = 'Json'; Format = 'Json'; Limit = 3
    }
    Assert-ColdCheckout (@($security.limitations).Count -gt 0) 'Cold-checkout security review omitted its evidence boundary.'

    $health = Invoke-JsonTool -ToolPath (Join-Path $checkoutWikiRoot 'tools/Find-LlmWikiArchitectureHealth.ps1') -ToolParameters @{
        View = 'all'; CompiledIndexSource = 'Json'; Format = 'Json'; Limit = 3
    }
    $healthGroups = @($health.PSObject.Properties.Name)
    Assert-ColdCheckout ($healthGroups.Count -eq 10) "Cold-checkout health all returned $($healthGroups.Count) groups instead of 10."

    Write-Host 'LLM Wiki JSON cold-checkout start/brief/develop/research facades passed without node_modules or code-graph preparation.'
} finally {
    if (Test-Path -LiteralPath $sandboxRoot) {
        Remove-Item -LiteralPath $sandboxRoot -Recurse -Force
    }
}
