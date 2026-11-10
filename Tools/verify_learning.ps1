param([string]$Label='learning-verified',[switch]$Full)
$ErrorActionPreference='Stop'
$courtFolder='Preview/Playtests/Learning-20261111'
$courtRunner=Join-Path $PSScriptRoot 'run_reactive_review.ps1'
foreach($courtCheck in @(@('LearningReview.RunRules','catalog'),@('LearningPlaytest.Run','lifecycle'),@('RecapReview.RunRules','recap-rules'),@('RecapPlaytest.Run','recap-scene'))){
    $courtArgs=@{Method=$courtCheck[0];Label=($Label+'-'+$courtCheck[1]);OutputFolder=$courtFolder}
    if($courtCheck[0] -like '*.RunRules'){$courtArgs.Quit=$true}
    & $courtRunner @courtArgs
    if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
}
if($Full){& (Join-Path $PSScriptRoot 'verify_reactive.ps1') -Label $Label -OutputFolder $courtFolder;exit $LASTEXITCODE}
