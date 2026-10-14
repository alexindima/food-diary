function Get-LlmWikiCompiledToolType {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$SourceFile,
        [Parameter(Mandatory)][string]$ClassName)

    # The legacy runtime retains the PowerShell scanner. Modern runtimes use the
    # same .NET regular expressions and filesystem rules in a single native call.
    if ($null -eq ('System.IO.EnumerationOptions' -as [type])) { return $null }
    $nativeTimer = [Diagnostics.Stopwatch]::StartNew()
    . (Join-Path $PSScriptRoot 'LlmWikiQueryCache.ps1')
    $source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot $SourceFile))
    $key = Get-LlmWikiSha256 ($source + "`n$($PSVersionTable.PSVersion)`n$([Runtime.InteropServices.RuntimeInformation]::FrameworkDescription)")
    $namespace = "LlmWiki.TestDiscovery.H$key"
    $typeName = "$namespace.$ClassName"
    $loaded = $typeName -as [type]
    if ($null -ne $loaded) { Write-Verbose "Native tool ${ClassName}: loaded type reused; elapsedMs=$([Math]::Round($nativeTimer.Elapsed.TotalMilliseconds,2))"; return $loaded }
    $artifactRepositoryRoot = $RepositoryRoot
    if (-not [string]::IsNullOrWhiteSpace($env:LLM_WIKI_READ_ONLY_SOURCE_ROOT) -and [IO.Directory]::Exists($env:LLM_WIKI_READ_ONLY_SOURCE_ROOT)) {
        $artifactRepositoryRoot = [IO.Path]::GetFullPath($env:LLM_WIKI_READ_ONLY_SOURCE_ROOT)
    }
    $directory = [IO.Path]::Combine($artifactRepositoryRoot, '.artifacts', 'llm-wiki', 'native-tools')
    $path = [IO.Path]::Combine($directory, "$key.dll")
    $receiptPath = "$path.json"
    $null = [IO.Directory]::CreateDirectory($directory)
    $lease = $null
    $deadline = [DateTime]::UtcNow.AddMinutes(2)
    while ($null -eq $lease) {
        try { $lease = [IO.File]::Open("$path.lock", [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
        catch [IO.IOException] {
            if ([DateTime]::UtcNow -ge $deadline) { throw 'Timed out waiting for native Wiki tool compilation.' }
            Start-Sleep -Milliseconds 50
        }
    }
    try {
        try {
            $receipt = [IO.File]::ReadAllText($receiptPath) | ConvertFrom-Json
            $bytes = [IO.File]::ReadAllBytes($path)
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $payloadHash = ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
            if ($receipt.schemaVersion -eq 1 -and $receipt.key -ceq $key -and $receipt.payloadHash -ceq $payloadHash) {
                $assembly = [Reflection.Assembly]::Load($bytes)
                $loaded = $assembly.GetType($typeName, $true)
                Write-Verbose "Native tool ${ClassName}: compiled assembly cache hit; elapsedMs=$([Math]::Round($nativeTimer.Elapsed.TotalMilliseconds,2))"
                return $loaded
            }
        } catch {
            # Missing, incomplete or corrupt derived artifacts are rebuilt below.
        }
        $temporary = [IO.Path]::Combine($directory, "$key.$([guid]::NewGuid().ToString('N')).dll")
        $temporaryReceipt = "$temporary.json"
        try {
            Write-Verbose "Native tool ${ClassName}: compiling; elapsedMs=$([Math]::Round($nativeTimer.Elapsed.TotalMilliseconds,2))"
            Add-Type -TypeDefinition $source.Replace('namespace LlmWiki.TestDiscovery {', "namespace $namespace {") -OutputAssembly $temporary -ErrorAction Stop
            Write-Verbose "Native tool ${ClassName}: compilation complete; elapsedMs=$([Math]::Round($nativeTimer.Elapsed.TotalMilliseconds,2))"
            $bytes = [IO.File]::ReadAllBytes($temporary)
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $payloadHash = ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
            $receipt = @{ schemaVersion = 1; key = $key; payloadHash = $payloadHash } | ConvertTo-Json -Compress
            [IO.File]::WriteAllText($temporaryReceipt, $receipt, [Text.UTF8Encoding]::new($false))
            Move-LlmWikiQueryCacheFile -TemporaryPath $temporary -DestinationPath $path
            Move-LlmWikiQueryCacheFile -TemporaryPath $temporaryReceipt -DestinationPath $receiptPath
            $assembly = [Reflection.Assembly]::Load($bytes)
            Write-Verbose "Native tool ${ClassName}: compiled from current source; elapsedMs=$([Math]::Round($nativeTimer.Elapsed.TotalMilliseconds,2))"
            return $assembly.GetType($typeName, $true)
        } finally {
            [IO.File]::Delete($temporary)
            [IO.File]::Delete($temporaryReceipt)
        }
    } finally { $lease.Dispose() }
}
