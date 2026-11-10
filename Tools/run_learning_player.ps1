param([ValidateSet('round1','round2','round3')][string]$Round='round1')
$ErrorActionPreference='Stop'
$courtRoot=Split-Path $PSScriptRoot -Parent
$courtFolder=Join-Path $courtRoot 'Preview/Playtests/Learning-20261111'
$courtExe=Join-Path $courtFolder 'QA/CourtX Learning QA.exe'
$courtLog=Join-Path $courtFolder ($Round+'-player.log')
# A visible game surface is necessary for full-frame IMGUI capture.
$courtProcess=Start-Process -FilePath $courtExe -ArgumentList @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-learningRound',$Round,'-logFile',('"'+$courtLog+'"')) -PassThru
Write-Output "QA player PID $($courtProcess.Id) | $Round"
if(!$courtProcess.WaitForExit(600000)){throw "QA player timed out: $courtLog"}
Write-Output "QA player exit $($courtProcess.ExitCode)"
Get-Content -LiteralPath (Join-Path $courtFolder ($Round+'-player-checks.txt'))
exit $courtProcess.ExitCode
