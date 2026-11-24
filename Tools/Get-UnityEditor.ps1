param([string]$EditorPath, [switch]$AllowMissing)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$versionFile = Join-Path $repoRoot 'Unity/ProjectSettings/ProjectVersion.txt'
$revision = Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersionWithRevision: (\S+) \(([0-9a-f]+)\)$'
$version = $revision.Matches[0].Groups[1].Value
$productVersion = $version + '_' + $revision.Matches[0].Groups[2].Value

if ($EditorPath) {
    $candidates = @($EditorPath)
} else {
    $candidates = @()
    $hubList = Join-Path $env:APPDATA 'UnityHub/editors-v2.json'
    if (Test-Path -LiteralPath $hubList) {
        try {
            $hub = Get-Content -LiteralPath $hubList -Raw -Encoding UTF8 | ConvertFrom-Json
            $candidates += @($hub.data | Where-Object version -eq $version | ForEach-Object { $_.location })
        } catch {
            Write-Verbose "Could not read the Hub editor list: $_"
        }
    }
    $candidates += Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"
    $candidates += Join-Path $env:ProgramFiles 'Unity/Editor/Unity.exe'
}

foreach ($candidate in $candidates) {
    if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        $file = Get-Item -LiteralPath $candidate
        if ($file.VersionInfo.ProductVersion -eq $productVersion) {
            return $file.FullName
        }
    }
}
if (!$AllowMissing) {
    throw "Unity Editor $productVersion was not found. See docs/FIVE_PLAYER_GUIDE_ZH.md for installation, or pass -EditorPath with the full path to the matching Unity.exe."
}
