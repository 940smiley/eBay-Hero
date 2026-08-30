[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common.ps1')
$config = Get-PathConfig
$checks = [ordered]@{
    RepositoryRoot = Test-Path -LiteralPath $config.RepositoryRoot
    OperationsRootParent = Test-Path -LiteralPath (Split-Path -Parent $config.OperationsRoot)
    InventorySourceParent = Test-Path -LiteralPath (Split-Path -Parent $config.InventorySource)
    SharedAppsRoot = Test-Path -LiteralPath $config.SharedAppsRoot
    TesseractPath = Test-Path -LiteralPath $config.TesseractPath
}
$checks.GetEnumerator() | ForEach-Object { [pscustomobject]@{ Name = $_.Key; Ok = $_.Value } } | Format-Table -AutoSize
if ($checks.RepositoryRoot -eq $false) { exit 2 }
