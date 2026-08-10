function Restore-LlmWikiSemanticNoOpArtifacts {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$GeneratedRoot,
        [Parameter(Mandatory)][string]$BackupRoot
    )

    $restored = [Collections.Generic.List[string]]::new()
    foreach ($generatedFile in @(Get-ChildItem -LiteralPath $GeneratedRoot -File -Recurse)) {
        $relativePath = $generatedFile.FullName.Substring($GeneratedRoot.Length).TrimStart('\', '/')
        $backupPath = Join-Path $BackupRoot $relativePath
        if (-not (Test-Path -LiteralPath $backupPath -PathType Leaf)) { continue }
        $currentHash = (Get-FileHash -LiteralPath $generatedFile.FullName -Algorithm SHA256).Hash
        $backupHash = (Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash
        if ($currentHash -ceq $backupHash) { continue }
        $equivalent = if ($generatedFile.Extension -eq '.json') {
            Test-LlmWikiJsonEquivalent -ActualPath $backupPath -ExpectedJson ([IO.File]::ReadAllText($generatedFile.FullName))
        } else {
            Test-LlmWikiTextEquivalent -ActualPath $backupPath -ExpectedText ([IO.File]::ReadAllText($generatedFile.FullName))
        }
        if ($equivalent) {
            Copy-Item -LiteralPath $backupPath -Destination $generatedFile.FullName -Force
            $restored.Add($relativePath.Replace('\', '/'))
        }
    }
    return @($restored)
}
