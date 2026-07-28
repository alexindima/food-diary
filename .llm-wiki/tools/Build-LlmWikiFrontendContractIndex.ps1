[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$frontendRoot = Join-Path $repositoryRoot 'FoodDiary.Web.Client'
$outputPath = Join-Path $wikiRoot 'generated/frontend-contract-index.json'

function ConvertTo-RepositoryPath {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath($Path).Substring($repositoryRoot.Length + 1).Replace('\', '/')
}

function Get-Feature {
    param([string]$Path)
    $match = [regex]::Match($Path, '/features/(?<feature>[^/]+)/')
    if ($match.Success) { return $match.Groups['feature'].Value }
    if ($Path -match '/projects/(?<project>[^/]+)/') { return $Matches['project'] }
    return 'shell'
}

$components = [System.Collections.Generic.List[object]]::new()
$apiCalls = [System.Collections.Generic.List[object]]::new()
$translationUsage = [System.Collections.Generic.List[object]]::new()
$tsFiles = @(
    Get-ChildItem -LiteralPath $frontendRoot -Recurse -File -Filter '*.ts' |
        Where-Object {
            $_.FullName -notmatch '[\\/](node_modules|dist|coverage|\.angular)[\\/]' -and
            $_.Name -notmatch '\.(spec|test)\.ts$'
        } |
        Sort-Object { Get-LlmWikiOrdinalSortKey $_.FullName }
)

foreach ($file in $tsFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $path = ConvertTo-RepositoryPath $file.FullName
    $feature = Get-Feature $path
    $componentMatch = [regex]::Match(
        $content,
        '(?ms)@Component\s*\(\s*\{(?<metadata>.*?)\}\s*\)\s*(?:export\s+)?(?:default\s+)?class\s+(?<class>[A-Za-z_][A-Za-z0-9_]*)')
    if ($componentMatch.Success) {
        $metadata = $componentMatch.Groups['metadata'].Value
        $selectorMatch = [regex]::Match($metadata, "selector\s*:\s*['""](?<value>[^'""]+)['""]")
        $templateMatch = [regex]::Match($metadata, "templateUrl\s*:\s*['""](?<value>[^'""]+)['""]")
        $templatePath = $null
        if ($templateMatch.Success) {
            $templateAbsolute = Join-Path $file.DirectoryName $templateMatch.Groups['value'].Value
            if (Test-Path -LiteralPath $templateAbsolute) { $templatePath = ConvertTo-RepositoryPath $templateAbsolute }
        }
        $inputs = @(
            [regex]::Matches($content, '(?m)(?:readonly\s+)?(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*input(?:\.required)?\s*(?:<(?<type>[^>]+)>)?\s*\(') |
                ForEach-Object {
                    [pscustomobject]@{
                        name = $_.Groups['name'].Value
                        type = $_.Groups['type'].Value
                        required = $_.Value -match 'input\.required'
                    }
                }
        )
        $outputs = @(
            [regex]::Matches($content, '(?m)(?:readonly\s+)?(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*output\s*(?:<(?<type>[^>]+)>)?\s*\(') |
                ForEach-Object {
                    [pscustomobject]@{ name = $_.Groups['name'].Value; type = $_.Groups['type'].Value }
                }
        )
        $specPath = $file.FullName -replace '\.ts$', '.spec.ts'
        $components.Add([pscustomobject]@{
            class = $componentMatch.Groups['class'].Value
            selector = if ($selectorMatch.Success) { $selectorMatch.Groups['value'].Value } else { $null }
            feature = $feature
            path = $path
            templatePath = $templatePath
            inputs = $inputs
            outputs = $outputs
            specPath = if (Test-Path -LiteralPath $specPath) { ConvertTo-RepositoryPath $specPath } else { $null }
        })
    }

    foreach ($match in [regex]::Matches(
        $content,
        '(?m)\b(?:this\.)?(?:http|httpClient)\.(?<method>get|post|put|patch|delete)\s*(?:<(?<response>[^>]+)>)?\s*\(\s*(?<url>[^,\r\n\)]+)')) {
        $apiCalls.Add([pscustomobject]@{
            feature = $feature
            method = $match.Groups['method'].Value.ToUpperInvariant()
            responseType = $match.Groups['response'].Value.Trim()
            urlExpression = $match.Groups['url'].Value.Trim()
            path = $path
            line = 1 + [regex]::Matches($content.Substring(0, $match.Index), "`n").Count
        })
    }
}

$templateFiles = @(
    Get-ChildItem -LiteralPath $frontendRoot -Recurse -File -Filter '*.html' |
        Where-Object { $_.FullName -notmatch '[\\/](node_modules|dist|coverage|\.angular)[\\/]' } |
        Sort-Object { Get-LlmWikiOrdinalSortKey $_.FullName }
)
$templateContents = @{}
foreach ($file in $templateFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $templateContents[$file.FullName] = $content
    $path = ConvertTo-RepositoryPath $file.FullName
    $keys = @(
        [regex]::Matches($content, "['""](?<key>[A-Z][A-Z0-9_.-]+)['""]\s*\|\s*translate") |
            ForEach-Object { $_.Groups['key'].Value } |
            Sort-Object { Get-LlmWikiOrdinalSortKey $_ } -Unique
    )
    if ($keys.Count -gt 0) {
        $translationUsage.Add([pscustomobject]@{
            feature = Get-Feature $path
            path = $path
            keys = $keys
        })
    }
}

$consumerEdges = [System.Collections.Generic.List[object]]::new()
$componentsBySelector = @{}
foreach ($component in $components | Where-Object { -not [string]::IsNullOrWhiteSpace($_.selector) }) {
    $componentsBySelector[$component.selector] = $component
}
$selectorAlternation = @(
    $componentsBySelector.Keys |
        Sort-Object `
            @{ Expression = { $_.Length }; Descending = $true },
            @{ Expression = { Get-LlmWikiOrdinalSortKey $_ } } |
        ForEach-Object { [regex]::Escape($_) }
) -join '|'
foreach ($file in $templateFiles) {
    $content = $templateContents[$file.FullName]
    $usagesBySelector = @(
        [regex]::Matches($content, "(?is)<(?<selector>$selectorAlternation)\b(?<attributes>[^>]*)>") |
            Group-Object { $_.Groups['selector'].Value }
    )
    foreach ($usageGroup in $usagesBySelector) {
        $component = $componentsBySelector[$usageGroup.Name]
        $inputsUsed = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        $outputsHandled = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($usage in $usageGroup.Group) {
            $attributes = $usage.Groups['attributes'].Value
            foreach ($inputContract in @($component.inputs)) {
                $name = [regex]::Escape($inputContract.name)
                if ($attributes -match "(?i)(?:\[\($name\)\]|\[$name\]|\b$name)\s*=") {
                    $null = $inputsUsed.Add($inputContract.name)
                }
            }
            foreach ($outputContract in @($component.outputs)) {
                $name = [regex]::Escape($outputContract.name)
                if ($attributes -match "(?i)\($name\)\s*=") {
                    $null = $outputsHandled.Add($outputContract.name)
                }
            }
        }
        $consumerPath = ConvertTo-RepositoryPath $file.FullName
        $consumerEdges.Add([pscustomobject]@{
            component = $component.class
            selector = $component.selector
            componentPath = $component.path
            consumerFeature = Get-Feature $consumerPath
            consumerPath = $consumerPath
            occurrences = $usageGroup.Count
            inputsUsed = @($inputsUsed | Sort-Object { Get-LlmWikiOrdinalSortKey $_ })
            outputsHandled = @($outputsHandled | Sort-Object { Get-LlmWikiOrdinalSortKey $_ })
        })
    }
}

$result = [ordered]@{
    schemaVersion = 1
    summary = [ordered]@{
        components = $components.Count
        componentsWithoutDirectSpecs = @($components | Where-Object { $null -eq $_.specPath }).Count
        inputs = @($components.inputs).Count
        outputs = @($components.outputs).Count
        apiCalls = $apiCalls.Count
        templatesWithTranslations = $translationUsage.Count
        translationKeys = @($translationUsage.keys | Sort-Object { Get-LlmWikiOrdinalSortKey $_ } -Unique).Count
        consumerEdges = $consumerEdges.Count
        consumedComponents = @($consumerEdges.component | Sort-Object { Get-LlmWikiOrdinalSortKey $_ } -Unique).Count
        unconsumedComponents = @(
            $components |
                Where-Object { $_.class -notin @($consumerEdges.component) }
        ).Count
    }
    components = @(
        $components |
            Sort-Object {
                Get-LlmWikiOrdinalSortKey "$($_.feature)`0$($_.class)`0$($_.path)"
            }
    )
    apiCalls = @(
        $apiCalls |
            Sort-Object `
                @{ Expression = { Get-LlmWikiOrdinalSortKey "$($_.feature)`0$($_.path)" } },
                line
    )
    translationUsage = @(
        $translationUsage |
            Sort-Object {
                Get-LlmWikiOrdinalSortKey "$($_.feature)`0$($_.path)"
            }
    )
    consumerEdges = @(
        $consumerEdges |
            Sort-Object {
                Get-LlmWikiOrdinalSortKey "$($_.component)`0$($_.consumerPath)"
            }
    )
}
$jsonText = ($result | ConvertTo-Json -Depth 10) + [Environment]::NewLine
if ($Check) {
    if (-not (Test-LlmWikiJsonEquivalent -ActualPath $outputPath -ExpectedJson $jsonText -Depth 10)) {
        Write-Host 'Frontend contract index is stale. Run ./.llm-wiki/wiki.ps1 update.'
        exit 1
    }
    Write-Host "Frontend contract index is current: $($result.summary.components) components, $($result.summary.apiCalls) API calls."
    exit 0
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $jsonText, $utf8WithoutBom)
Write-Host "Generated .llm-wiki/generated/frontend-contract-index.json."
