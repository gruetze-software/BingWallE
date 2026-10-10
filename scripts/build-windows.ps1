param([ValidateSet('x64', 'arm64')][string]$Architecture = 'x64')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'BingWallE/BingWallE.csproj'
[xml]$projectXml = Get-Content -LiteralPath $project
$version = [string]($projectXml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1)
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid project version: $version" }
$rid = "win-$Architecture"
$output = Join-Path $repoRoot 'artifacts'
$staging = Join-Path $output ("stage-$rid-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging -Force | Out-Null
try {
    dotnet publish $project -c Release -r $rid --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o $staging
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }
    Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE'),(Join-Path $repoRoot 'README.md') -Destination $staging
    Copy-Item -LiteralPath (Join-Path $repoRoot 'licenses') -Destination $staging -Recurse
    $archive = Join-Path $output "BingWallE-$version-$rid.zip"
    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $archive -Force
    Write-Output "Created $archive"
} finally {
    $resolvedStaging = [IO.Path]::GetFullPath($staging)
    $resolvedOutput = [IO.Path]::GetFullPath($output) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedStaging.StartsWith($resolvedOutput, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe staging path' }
    if (Test-Path -LiteralPath $resolvedStaging) { Remove-Item -LiteralPath $resolvedStaging -Recurse -Force }
}
