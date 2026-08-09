[CmdletBinding()]
param(
    [ValidateSet('All', 'Development', 'Public')]
    [string]$Flavor = 'All'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Set-ProjectProcessEnvironment
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$outputRoot = if ($env:EA_RELEASE_ROOT) { Join-Path $env:EA_RELEASE_ROOT 'flavors' } else { Join-Path $repo 'artifacts\release\flavors' }
$flavors = if ($Flavor -eq 'All') { @('Development', 'Public') } else { @($Flavor) }

Push-Location $repo
try {
    foreach ($item in $flavors) {
        $output = Join-Path $outputRoot $item
        & dotnet publish .\src\eBayHero.App\eBayHero.App.csproj -c Release -r win-x64 --self-contained true -p:ProductFlavor=$item -o $output
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        [ordered]@{
            product = 'eBay Assistance'
            flavor = $item
            trialDays = if ($item -eq 'Public') { 14 } else { $null }
            premiumTrialActions = if ($item -eq 'Public') { 25 } else { $null }
            developerConfigurationIncluded = $item -eq 'Development'
            liveEbayPublishingEnabled = $false
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'build-flavor.json') -Encoding UTF8
    }
}
finally {
    Pop-Location
}
