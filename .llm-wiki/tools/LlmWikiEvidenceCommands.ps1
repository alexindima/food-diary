. (Join-Path $PSScriptRoot 'LlmWikiVerificationReceipts.ps1')

function Test-LlmWikiEvidenceExecutionCommand {
    param([string]$Canonical, [string]$Executed, [string]$RepositoryRoot)
    if ($Canonical.Trim() -ceq $Executed.Trim()) { return $true }
    if ($Canonical -notmatch '^dotnet test ' -or $Executed -notmatch '^dotnet test ' -or $Executed -match '[;&|<>`$]') { return $false }
    function Get-ExecutionTokens([string]$Text, [bool]$StripOutputArguments) {
        $tokens = @([regex]::Matches($Text, '"[^"\r\n]*"|''[^''\r\n]*''|[^\s]+') | ForEach-Object { $_.Value.Trim('"', "'") })
        $result = [Collections.Generic.List[string]]::new()
        for ($index = 0; $index -lt $tokens.Count; $index++) {
            $token = $tokens[$index]
            if ($token -eq '--no-restore') { continue }
            if ($StripOutputArguments -and $token -eq '--no-build') { continue }
            if ($StripOutputArguments -and $token -in @('--artifacts-path', '--results-directory', '--logger')) {
                if ($index + 1 -ge $tokens.Count -or $tokens[$index + 1].StartsWith('-')) { return $null }
                $index++; continue
            }
            if ($StripOutputArguments -and $token -match '^--maxcpucount:[1-9][0-9]*$') { continue }
            $result.Add($token)
        }
        return ,@($result)
    }
    $expected = Get-ExecutionTokens $Canonical $false
    $actual = Get-ExecutionTokens $Executed $true
    if ($null -eq $actual -or ($expected -join "`n") -cne ($actual -join "`n")) { return $false }
    if ($Executed -notmatch '(?:^|\s)--no-build(?:\s|$)') { return $true }
    # Skipping compilation requires a successful build receipt for the current
    # exact repository contents. A supplemental/manual test cannot replace it.
    $project = [string]$expected[2]
    function Get-BuildOption([string]$Text, [string]$Option, [string]$Default) {
        if ($Text -match ('(?:^|\s)(?:' + $Option + ')(?:=|\s+)("[^"\r\n]+"|''[^''\r\n]+''|\S+)')) { return $Matches[1].Trim('"', "'").Replace('\', '/') }
        return $Default
    }
    foreach ($receipt in @(Get-LlmWikiVerificationReceipts $RepositoryRoot | Where-Object validForCurrentState)) {
        if ([string]$receipt.command -notmatch '^dotnet build ("[^"\r\n]+"|''[^''\r\n]+''|\S+)(?:\s|$)') { continue }
        $builtProject = $Matches[1].Trim('"', "'").Replace('\', '/')
        if ((Get-BuildOption $receipt.command '--configuration|-c' 'Debug') -cne (Get-BuildOption $Executed '--configuration|-c' 'Debug')) { continue }
        if ((Get-BuildOption $receipt.command '--artifacts-path' '<default>') -cne (Get-BuildOption $Executed '--artifacts-path' '<default>')) { continue }
        if ((Get-BuildOption $receipt.command '--framework|-f' '<default>') -cne (Get-BuildOption $Executed '--framework|-f' '<default>')) { continue }
        if ($builtProject -ceq $project.Replace('\', '/') -or $builtProject -in @('FoodDiary.slnx', 'FoodDiary.sln')) { return $true }
    }
    return $false
}
