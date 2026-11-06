function Get-FoodDiaryCoverageAttributeFilters {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][bool]$WindowsPlatform,
        [AllowEmptyCollection()][string[]]$ExcludeAttribute = @('System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute')
    )

    $windowsMarker = 'FoodDiary.Modules.Ai.Infrastructure.Diagnostics.ExcludeFromWindowsCoverageAttribute'
    foreach ($filter in $ExcludeAttribute) {
        if (-not [string]::IsNullOrWhiteSpace($filter) -and
            -not [string]::Equals($filter, $windowsMarker, [StringComparison]::OrdinalIgnoreCase)) {
            $filter
        }
    }
    if ($WindowsPlatform) { $windowsMarker }
}
