param([string]$Method='FivePlayerReview.Run',[string]$Label='rules',[switch]$KeepOpen,[string]$EvidenceDir,[string]$EditorPath)
$ErrorActionPreference='Stop'
$courtRoot=Split-Path $PSScriptRoot -Parent
$fiveEvidence=Join-Path $courtRoot 'Preview/Playtests/FivePlayer'
if($EvidenceDir){$fiveEvidence=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($EvidenceDir)}
$fiveEditor=& (Join-Path $PSScriptRoot 'Get-UnityEditor.ps1') -EditorPath $EditorPath
New-Item -ItemType Directory -Path $fiveEvidence -Force | Out-Null
$fiveLog=Join-Path $fiveEvidence ($Label+'.log')
$fiveArgs=@('-batchmode','-projectPath',('"'+(Join-Path $courtRoot 'Unity')+'"'),'-executeMethod',$Method,'-reactiveLabel',$Label,'-reviewOutput',('"'+$fiveEvidence+'"'),'-logFile',('"'+$fiveLog+'"'))
if(!$KeepOpen){$fiveArgs+='-quit'}
$fiveProcess=Start-Process -FilePath $fiveEditor -ArgumentList $fiveArgs -WindowStyle Hidden -PassThru
Write-Output "Unity PID $($fiveProcess.Id) | $Method | $fiveLog"
$fiveProcess.WaitForExit()
Get-Content -LiteralPath $fiveLog -Tail 18
exit $fiveProcess.ExitCode
