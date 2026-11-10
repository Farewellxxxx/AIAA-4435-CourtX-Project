# Learning hub integration

`TacticalSession` remains the sole owner of live actors, ball physics, possession and tactical decisions. `LearningHub` owns page state, selected scenario, question answers, history, and a separate `RecapPlayback`. Viewing a lesson or replay never writes actors or the live journal.

`LearningScenario` normalizes the two drills, two controlled roles, coverage choice and seed. New journals save `hasScenarioConfig` and `coverageChoice`; legacy records are retriable only when their coverage is recognizable. High-post practice uses its existing help/recovery policy.

`LearningCatalog` loads eight internal recorded examples from Resources/CourtXLearning. Phase changes and actual pass timestamps form lesson steps. Six questions carry exact configuration, timestamp, conditions, three options, feedback and the selected answer index. Examples are display-only records, not live counterfactual simulation.

`LearningHistory` clones accepted records, validates structure and finite court coordinates, bounds file size/startup work, and deduplicates IDs. Comparability checks full opening positions, ball, possession, drill, role and seed. A/B rounds are actual completed records, never altered to match. Pass outcomes and release-time lane quality remain separate.

`LearningHubUI` renders a 1280×720 virtual canvas, uniformly scaled and centered. Scroll panes handle history, Chinese feedback and optional narration. `RecapCourtView.DrawAt` composes its transform with the existing canvas, retaining court and ball geometry. Challenge playback is fixed; the linked lesson supports playback.

Existing `ReviewOpen`, `Review`, `Replay`, events and `ITacticalRecapProvider` remain available. The learning UI cursor is independent of the legacy integration replay cursor. Optional narration is visible only on the matching current episode; local feedback remains available on failure. No new packages or online services are required.

## Build and verify

- `LearningAuthoring.Build`: production `Windows_Learning/CourtX Learning.exe`.
- `LearningAuthoring.BuildQA`: separate QA player with `COURTX_LEARNING_REVIEW` only. The release must not contain `LearningPlayerReview`.
- `LearningReview.RunRules`: catalog, historical metadata, validation, comparisons and metrics.
- `LearningPlaytest.Run`: actual scene lifecycle, settings transfer, frozen playback, archive identity, comparison and challenge navigation.
- `Tools/verify_reactive.ps1`: six existing regression suites. Run RecapReview and RecapPlaytest additionally.
- `Tools/run_learning_player.ps1`: standalone full-UI videos and screenshots. QA drives real gameplay APIs; it is distinct from native keyboard/mouse checks.

Unity tests/builds must run sequentially against this checkout. Do not edit Assets while the Editor imports or compiles. New artifacts use Preview/Playtests/Learning-20261111; older releases and evidence remain separate.
