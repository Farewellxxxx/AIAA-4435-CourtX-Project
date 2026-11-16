param([string]$Method='FivePlayerReview.Run',[string]$Label='rules',[switch]$KeepOpen)
$ErrorActionPreference='Stop'
$courtRoot=Split-Path $PSScriptRoot -Parent
$fiveEvidence='E:\study documents\2026 Fall\AIAA 4435 Inter\CourtX\Preview\Playtests\FivePlayer-20261116'
New-Item -ItemType Directory -Path $fiveEvidence -Force | Out-Null
$fiveLog=Join-Path $fiveEvidence ($Label+'.log')
$fiveArgs=@('-batchmode','-projectPath',('"'+(Join-Path $courtRoot 'Unity')+'"'),'-executeMethod',$Method,'-reactiveLabel',$Label,'-reviewOutput',('"'+$fiveEvidence+'"'),'-logFile',('"'+$fiveLog+'"'))
if(!$KeepOpen){$fiveArgs+='-quit'}
$fiveProcess=Start-Process -FilePath 'E:\Unity\Editors\Editor\Unity.exe' -ArgumentList $fiveArgs -WindowStyle Hidden -PassThru
Write-Output "Unity PID $($fiveProcess.Id) | $Method | $fiveLog"
$fiveProcess.WaitForExit()
Get-Content -LiteralPath $fiveLog -Tail 18
exit $fiveProcess.ExitCode
