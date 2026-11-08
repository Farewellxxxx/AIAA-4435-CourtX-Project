param(
    [Parameter(Mandatory=$true)][string]$Method,
    [Parameter(Mandatory=$true)][string]$LogName,
    [string]$Round='final',
    [int]$Stage=1,
    [switch]$Quit
)
$ErrorActionPreference='Stop'
$courtRoot=Split-Path $PSScriptRoot -Parent
$logFolder=Join-Path $courtRoot 'Preview/Playtests/Tactics/Refinement-20261108'
New-Item -ItemType Directory -Path $logFolder -Force | Out-Null
$project=Join-Path $courtRoot 'Unity'
$log=Join-Path $logFolder ($LogName+'.log')
$reviewArgs=@('-batchmode','-projectPath',('"'+$project+'"'),'-executeMethod',$Method,
    '-tacticsRound',$Round,'-refinement','-refineStage',$Stage,'-refineLabel',$LogName,'-logFile',('"'+$log+'"'))
if($Quit){$reviewArgs+='-quit'}
$editor=Start-Process -FilePath 'E:\Unity\Editors\Editor\Unity.exe' -ArgumentList $reviewArgs -WindowStyle Hidden -PassThru
Write-Output "Unity review PID $($editor.Id) | $Method | $LogName"
if(!$editor.WaitForExit(600000)) {throw "Review timed out. Inspect $log and editor PID $($editor.Id) before starting another instance."}
exit $editor.ExitCode
