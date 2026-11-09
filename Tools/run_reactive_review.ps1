param([string]$Method='ReactiveReview.RunRules',[string]$Label='rules',[string]$Round='round1',[switch]$Quit)
$ErrorActionPreference='Stop'
$courtRoot=Split-Path $PSScriptRoot -Parent
$folder=Join-Path $courtRoot 'Preview/Playtests/Reactive-20261109'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$project=Join-Path $courtRoot 'Unity'
$log=Join-Path $folder ($Label+'.log')
$argsList=@('-batchmode','-projectPath',('"'+$project+'"'),'-executeMethod',$Method,'-reactiveLabel',$Label,'-tacticsRound',$Round,'-logFile',('"'+$log+'"'))
if($Quit){$argsList+='-quit'}
$editor=Start-Process 'E:/Unity/Editors/Editor/Unity.exe' -ArgumentList $argsList -WindowStyle Hidden -PassThru
Write-Output "Unity PID $($editor.Id) | $Method | $Label"
if(!$editor.WaitForExit(600000)){throw "Timed out: inspect $log and PID $($editor.Id)"}
exit $editor.ExitCode
