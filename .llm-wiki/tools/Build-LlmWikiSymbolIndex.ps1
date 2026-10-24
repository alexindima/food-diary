[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$ReuseUnchangedCheck
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$outputPath = Join-Path $wikiRoot 'generated/csharp-symbol-index.json'
$cachePath = Join-Path $repositoryRoot '.artifacts/llm-wiki/index-cache/csharp-symbol-index.json'

function ConvertTo-RepositoryPath {
    param([string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pathUri = [System.Uri]::new($resolvedPath)
    return [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pathUri).ToString())
}

function Get-SymbolRole {
    param(
        [string]$Name,
        [string]$Path,
        [string]$Kind
    )

    $nameWithoutInterfacePrefix = if ($Kind -eq 'interface' -and $Name.StartsWith('I')) {
        $Name.Substring(1)
    } else {
        $Name
    }
    $roles = @(
        'CommandHandler'
        'QueryHandler'
        'Handler'
        'Validator'
        'ReadModelRepository'
        'LookupRepository'
        'ReadRepository'
        'WriteRepository'
        'Repository'
        'Controller'
        'Service'
        'Factory'
        'Mapper'
        'Mapping'
        'Options'
        'Configuration'
        'Entity'
        'ValueObject'
        'Event'
        'Command'
        'Query'
        'Request'
        'Response'
    )
    foreach ($role in $roles) {
        if ($nameWithoutInterfacePrefix.EndsWith($role, [System.StringComparison]::Ordinal)) {
            return $role
        }
    }
    if ($Path -match '/Entities/') {
        return 'Entity'
    }
    if ($Path -match '/ValueObjects?/' -or $Path -match '/StronglyTypedIds?') {
        return 'ValueObject'
    }
    return 'Other'
}

function Get-SymbolSourceFiles {
    $arguments = @(
        'ls-files', '--cached', '--others', '--exclude-standard', '--', ':(icase)*.cs'
    )
    foreach ($directory in @('tests', 'node_modules', 'obj', 'bin', '.artifacts', 'TestResults', 'Migrations')) {
        $arguments += ":(glob,icase,exclude)**/$directory/**"
    }
    $arguments += @(':(glob,icase,exclude)**/*.Designer.cs', ':(glob,icase,exclude)**/*.g.cs',
        ':(glob,icase,exclude)**/*ModelSnapshot.cs', ':(glob,icase,exclude).llm-wiki/tools/**')
    $paths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments $arguments)
    $files = [Collections.Generic.List[IO.FileInfo]]::new()
    foreach ($path in $paths) {
        $fullPath = [IO.Path]::Combine($repositoryRoot, $path)
        if (-not [IO.File]::Exists($fullPath) -or
            $fullPath -match '[\\/](tests|node_modules|obj|bin|\.artifacts|TestResults|Migrations)[\\/]' -or
            $fullPath -match '[\\/]\.llm-wiki[\\/]tools[\\/]') { continue }
        $file = [IO.FileInfo]::new($fullPath)
        if ($file.Name -match '\.(Designer|g)\.cs$' -or $file.Name -match 'ModelSnapshot\.cs$') { continue }
        $files.Add($file)
    }
    $files | Sort-Object FullName
}

function Get-SymbolInputFingerprint([IO.FileInfo[]]$SourceFile) {
    $inputs = @($SourceFile | ForEach-Object { $_.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/') })
    $inputs += @(
        '.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1'
        '.llm-wiki/tools/LlmWikiJson.ps1'
        '.llm-wiki/tools/LlmWikiGitPaths.ps1'
        '.llm-wiki/tools/LlmWikiIndexCache.ps1'
    )
    # Parsing, default text decoding and culture-sensitive sorting can differ
    # between engines. A receipt from another runtime must take the full path.
    $runtime = "$($PSVersionTable.PSVersion)|$($PSVersionTable.PSEdition)|$([Environment]::Version)|$([Environment]::OSVersion)|$([Globalization.CultureInfo]::CurrentCulture.Name)|$([Text.Encoding]::Default.CodePage)"
    return Get-LlmWikiIndexEntryFingerprint @(
        ('inputs:' + (Get-LlmWikiIndexInputFingerprint $repositoryRoot $inputs))
        ('runtime:' + $runtime)
    )
}

function Write-SymbolCache([string]$ExpectedJson, [string]$InputFingerprint) {
    # Bind the receipt to the bytes actually compared with the expected result.
    # A later output edit then invalidates reuse instead of being certified by
    # a second disk read. Equivalent custom JSON formatting keeps full checks.
    $bytes = [IO.File]::ReadAllBytes($outputPath)
    $actual = [Text.Encoding]::UTF8.GetString($bytes).TrimStart([char]0xFEFF)
    if ($actual.Replace("`r`n", "`n").Replace("`r", "`n") -cne
        $ExpectedJson.Replace("`r`n", "`n").Replace("`r", "`n")) { return }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $outputFingerprint = ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
    Write-LlmWikiIndexSetCache $cachePath $InputFingerprint $outputFingerprint
}

$sourceFiles = @(Get-SymbolSourceFiles)
$inputFingerprint = Get-SymbolInputFingerprint $sourceFiles
if ($ReuseUnchangedCheck -and (Test-Path -LiteralPath $outputPath -PathType Leaf) -and
    (Test-LlmWikiIndexSetCache $cachePath $inputFingerprint (Get-LlmWikiRequiredFileSha256 $outputPath))) {
    Write-Host 'C# symbol index cache hit: source set, contents, generator, runtime and output are unchanged.'
    return
}

$typePattern = '(?m)^\s*(?:public|internal)\s+(?:(?:sealed|abstract|static|partial|readonly)\s+)*(?<kind>class|interface|record(?:\s+struct)?|struct|enum)\s+(?<name>[A-Za-z_]\w*)'
$symbols = [System.Collections.Generic.List[object]]::new()
foreach ($sourceFile in $sourceFiles) {
    $content = Get-Content -LiteralPath $sourceFile.FullName -Raw
    $path = ConvertTo-RepositoryPath $sourceFile.FullName
    foreach ($match in [regex]::Matches($content, $typePattern)) {
        $kind = $match.Groups['kind'].Value
        $name = $match.Groups['name'].Value
        $line = ($content.Substring(0, $match.Index) -split "`n").Count
        $symbols.Add([pscustomobject][ordered]@{
            name = $name
            kind = $kind
            role = Get-SymbolRole $name $path $kind
            path = $path
            line = $line
        })
    }
}

$symbolNames = @{}
foreach ($symbol in $symbols) {
    if (-not $symbolNames.ContainsKey($symbol.name)) {
        $symbolNames[$symbol.name] = [System.Collections.Generic.List[object]]::new()
    }
    $symbolNames[$symbol.name].Add($symbol)
}

$interfaceImplementations = [System.Collections.Generic.List[object]]::new()
foreach ($symbol in $symbols | Where-Object { $_.kind -eq 'interface' -and $_.name -match '^I[A-Z]' }) {
    $implementationName = $symbol.name.Substring(1)
    if ($symbolNames.ContainsKey($implementationName)) {
        foreach ($implementation in $symbolNames[$implementationName]) {
            if ($implementation.kind -eq 'interface') {
                continue
            }
            $interfaceImplementations.Add([pscustomobject][ordered]@{
                interface = $symbol.name
                interfacePath = $symbol.path
                implementation = $implementation.name
                implementationPath = $implementation.path
                confidence = 'naming-convention'
            })
        }
    }
}

$registrationFiles = @(
    $sourceFiles | Where-Object {
        $_.Name -like '*DependencyInjection*.cs' -or
        $_.Name -eq 'Program.cs' -or
        $_.Name -like '*ServiceCollectionExtensions.cs'
    }
)
$registrations = [System.Collections.Generic.List[object]]::new()
$registrationPattern = '(?:Try)?Add(?<lifetime>Scoped|Transient|Singleton)\s*<\s*(?<service>[\w.]+)\s*,\s*(?<implementation>[\w.]+)\s*>'
foreach ($registrationFile in $registrationFiles) {
    $content = Get-Content -LiteralPath $registrationFile.FullName -Raw
    $path = ConvertTo-RepositoryPath $registrationFile.FullName
    foreach ($match in [regex]::Matches($content, $registrationPattern)) {
        $line = ($content.Substring(0, $match.Index) -split "`n").Count
        $registrations.Add([pscustomobject][ordered]@{
            service = $match.Groups['service'].Value
            implementation = $match.Groups['implementation'].Value
            lifetime = $match.Groups['lifetime'].Value
            path = $path
            line = $line
        })
    }
}

$roleCounts = [ordered]@{}
foreach ($roleGroup in @($symbols | Group-Object role | Sort-Object Name)) {
    $roleCounts[$roleGroup.Name] = $roleGroup.Count
}
$kindCounts = [ordered]@{}
foreach ($kindGroup in @($symbols | Group-Object kind | Sort-Object Name)) {
    $kindCounts[$kindGroup.Name] = $kindGroup.Count
}

$index = [ordered]@{
    schemaVersion = 1
    generator = '.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1'
    extraction = [ordered]@{
        scope = 'Production C# source excluding tests, migrations, generated files, obj, and bin'
        symbols = 'Public and internal top-level type declarations'
        interfaceImplementations = 'IName to Name naming convention'
        registrations = 'Literal AddScoped/AddTransient/AddSingleton service-to-implementation registrations'
    }
    summary = [ordered]@{
        sourceFiles = $sourceFiles.Count
        symbols = $symbols.Count
        interfaceImplementationLinks = $interfaceImplementations.Count
        dependencyInjectionRegistrations = $registrations.Count
        kinds = $kindCounts
        roles = $roleCounts
    }
    symbols = @($symbols | Sort-Object path, line, name)
    interfaceImplementations = @($interfaceImplementations | Sort-Object interface, implementationPath)
    dependencyInjectionRegistrations = @($registrations | Sort-Object path, line, service)
}

$expectedContent = ConvertTo-LlmWikiCanonicalJson $index -Depth 12

# Re-enumerate as well as re-hash: a new/deleted source during extraction must
# not publish a receipt for a partial inventory or a mixed source snapshot.
if ((Get-SymbolInputFingerprint @(Get-SymbolSourceFiles)) -cne $inputFingerprint) {
    throw 'C# symbol index inputs changed during extraction; retry the command.'
}

if ($Check) {
    if (-not (Test-Path -LiteralPath $outputPath)) {
        Write-Host 'C# symbol index is missing. Run Build-LlmWikiSymbolIndex.ps1.'
        exit 1
    }
    if (-not (Test-LlmWikiJsonEquivalent -ActualPath $outputPath -ExpectedJson $expectedContent -Depth 12)) {
        Write-Host 'C# symbol index is stale. Regenerate it with:'
        Write-Host '  ./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1'
        exit 1
    }
    Write-SymbolCache $expectedContent $inputFingerprint
    Write-Host "C# symbol index is current: $($symbols.Count) symbols, $($registrations.Count) DI registrations."
    return
}

$outputDirectory = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}
if (Test-LlmWikiTextEquivalent -ActualPath $outputPath -ExpectedText $expectedContent) {
    Write-SymbolCache $expectedContent $inputFingerprint
    Write-Host 'C# symbol index unchanged; preserved byte-identical output.'
    return
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $expectedContent, $utf8WithoutBom)
Write-SymbolCache $expectedContent $inputFingerprint
Write-Host "Generated $(ConvertTo-RepositoryPath $outputPath)."
