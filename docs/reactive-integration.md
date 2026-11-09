# Reactive training integration

Baseline `dc4b774`; branch `feature/reactive-tactics-review`. Keep builds separate: `TacticalAuthoring.BuildReactive` writes `Windows_Reactive`, while the original builder still names `Windows_Tactics`. Do not call the old builder on this branch if preserving the previous executable.

## Runtime boundaries

- `TacticalSession.StartScenario(TacticalDrill,bool,DefenseCoverage,int seed)` starts a seeded scenario; `RetryScenario` preserves settings; `NextScenario` increments the seed. `StartDrill` remains compatible and retains coverage settings.
- `ReactiveTactics.Reset` selects a concrete seeded coverage and reaction delay. `Step` consumes actual positions, phase, holder and flight target. `Marks[defender-3]` is the current defensive assignment; it survives switches and drives facing as well as target movement.
- Coverage requires a planted screen followed by nearby screen use. No early pass or rejected screen should activate it. Blitz exits when the ball leaves, the handler retreats/separates, or the short trap window expires. Weak-side help uses entry/exit delays.
- `ReceiverTarget` implements roll/short-roll and weak-side hold/lift/cut. `ChooseReceiver` consumes physical-lane previews and receiver space. Its input `pressure` is an adapted relative measure, not a steal probability.
- `TacticalBall` alone changes ownership. Existing swept contact, facing-aware interception, blind deflection, fixed lead, catch blending and final-arrival timing remain unchanged. Recording happens before release clears the holder.

## Episode data

`TacticalJournal` records a 5 Hz position/assignment trace, defense and phase changes, requests, and exact release snapshots. Release snapshots include both receiver options, risk reasons, separation, intended trajectory and actual final outcome. Positions and options are copied; later movement cannot rewrite the decision.

`windowAge` is a sampled duration for which the preview was open at the same pass speed, not a cognitive reaction-time measurement. Changing the sampled passer or speed resets the window; a blocked actual release or speed mismatch records -1 (unknown) and does not claim continuous openness. `passSpeed` records the release speed used for both receiver options. `pressure` and alternate suggestions are heuristic, not validated shooting probabilities. Limits: 1800 trace frames, 500 decisions, 1000 events; `truncated` identifies a recording that reached a limit.

`V` pauses and closes the episode; JSON and default Chinese TXT are written under `Replays` beside the program. In Editor they go to `Preview/Playtests/Reactive-20261109/Sessions`. Reopening or resetting a drill archives the prior active segment. The text default selects the last human pass, or explicitly labels the last AI pass if the human never passed. The timeline is an actual-state inspection view; it is not a simulated counterfactual.

## Optional LLM seam

Assign an implementation of `ITacticalRecapProvider` to `TacticalSession.RecapProvider`. Its signature is:

```csharp
Task<string> GenerateAsync(string episodeJson, string localReview,
                           CancellationToken cancellation);
```

Default is null: no network, credentials or model dependencies. The provider receives immutable serialized facts and the local rule assessment. The local review remains visible while the provider runs. A cancellation/version gate and episode identity prevent an older reply overwriting a new episode; failures preserve the local review. Implement provider-side authentication, timeouts and factual-output constraints when a real service is selected. Do not put credentials into a Unity asset or repository. Do not let an LLM own runtime NPC control or produce ungrounded tactical scores.

## Validation entry points

Run `Tools/run_reactive_review.ps1` with `-Method`:

- `ReactiveReview.RunRules -Quit`: defense, teammates, pressure adaptation, snapshot/assessment and stale provider tests.
- `ReactiveReviewFixes.Run`: real first-leg high-post relays and release-time sampling regressions from the independent review.
- `ReactivePlaytest.Run`: all 8 scenario/role combinations, actual movement and passing, exports, retry and edge cases; emits camera recording.
- `TacticalReview.Run -Round final`: original 45 route/control checks and video.
- `TacticalRefinementReview.Run`: 31 interaction cases including 15/30/60 fps contact/arrival behavior.
- `CourtControlsReview.Run`: original 28 free-practice checks.
- `TacticalAuthoring.BuildReactive -Quit`: standalone Windows output.

Camera recordings exclude OnGUI HUD. The separate `native-review-ui.mp4` captures the real standalone UI and local review. Never describe camera-only recordings as proof that the UI was rendered.

`Tools/verify_reactive.ps1 -Label <run-name>` executes all six suites sequentially and stops at the first failing exit code. Do not launch a second Unity editor on this project while it runs.
