param([string]$EditorPath, [switch]$Build)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'Unity'
$editor = & (Join-Path $PSScriptRoot 'Get-UnityEditor.ps1') -EditorPath $EditorPath -AllowMissing:(!$Build -and !$EditorPath)

if (!$editor) {
    if (Test-Path 'Registry::HKEY_CLASSES_ROOT\unityhub\shell\open\command') {
        Write-Host 'Finish installing the project editor in Unity Hub, then run Open-Unity again.'
        Start-Process 'unityhub://2022.3.62f3c1/1623fc0bbb97'
    } else {
        Write-Host 'Install Unity Hub for Windows from the historical Unity versions section, then run Open-Unity again.'
        Start-Process 'https://unity.cn/releases/full/2022'
    }
    return
}

if ($Build) {
    $logDir = Join-Path $project 'Logs'
    New-Item -Path $logDir -ItemType Directory -Force | Out-Null
    $log = Join-Path $logDir 'CourtX-build.log'
    Write-Host "Building CourtX. Log: $log"
    $unityArgs = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
        '-buildTarget', 'Win64', '-executeMethod', 'FivePlayerAuthoring.BuildPortable',
        '-logFile', ('"' + $log + '"'))
    $process = Start-Process -FilePath $editor -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity exited with code $($process.ExitCode). See $log." }
    Write-Host (Join-Path $project 'Builds/Windows/CourtX.exe')
} else {
    $scene = Join-Path $project 'Assets/CourtX/Scenes/FivePlayerTraining.unity'
    Start-Process -FilePath $editor -ArgumentList @('-openfile', ('"' + $scene + '"'))
}
