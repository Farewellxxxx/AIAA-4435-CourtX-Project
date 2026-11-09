param([string]$Label='verified',[string]$OutputFolder='Preview/Playtests/Reactive-20261109')
$ErrorActionPreference='Stop'
$runner=Join-Path $PSScriptRoot 'run_reactive_review.ps1'
& $runner -Method ReactiveReview.RunRules -Label ($Label+'-rules') -OutputFolder $OutputFolder -Quit
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $runner -Method ReactiveReviewFixes.Run -Label ($Label+'-review-fixes') -OutputFolder $OutputFolder
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $runner -Method ReactivePlaytest.Run -Label ($Label+'-scenarios') -OutputFolder $OutputFolder
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $runner -Method TacticalRefinementReview.Run -Label ($Label+'-interactions') -OutputFolder $OutputFolder
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $runner -Method TacticalReview.Run -Label ($Label+'-routes') -Round final -OutputFolder $OutputFolder
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $runner -Method CourtControlsReview.Run -Label ($Label+'-practice') -OutputFolder $OutputFolder
exit $LASTEXITCODE
