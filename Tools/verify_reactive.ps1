param([string]$Label='verified')
$ErrorActionPreference='Stop'
$runner=Join-Path $PSScriptRoot 'run_reactive_review.ps1'
& $runner -Method ReactiveReview.RunRules -Label ($Label+'-rules') -Quit
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $runner -Method ReactiveReviewFixes.Run -Label ($Label+'-review-fixes')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $runner -Method ReactivePlaytest.Run -Label ($Label+'-scenarios')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $runner -Method TacticalRefinementReview.Run -Label ($Label+'-interactions')
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $runner -Method TacticalReview.Run -Label ($Label+'-routes') -Round final
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $runner -Method CourtControlsReview.Run -Label ($Label+'-practice')
exit $LASTEXITCODE
