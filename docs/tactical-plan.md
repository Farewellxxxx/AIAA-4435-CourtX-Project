# Approved tactical training implementation

The user approved autonomous implementation, GitHub backup, a feature branch, repeated playtests and recording, then normal shutdown on 2026-11-07.

## Scope

- Preserve the complete stable free-practice scene and Windows build.
- Two local 3v3 drills: high pick-and-roll, high-post pass-and-cut. These demonstrate local help/recovery, not a five-player 2–3 zone.
- Ball-handler / off-ball starts; controlled identity does not change with ball possession.
- Five responding NPCs, procedural defensive stance, receiving, screens and movement; framework with bounded freedom and recovery.
- Single authoritative possession; flight, receiving, denied passing lanes, interception; call-for-pass feedback.
- Preserve WASD, Shift and live hold-Tab behavior. Separate tactical scene/build and replaceable character interfaces.
- Record and inspect at least two playtest iterations, verify all four drill/mode combinations and regressions, final fresh review, commit and push results.

## Implementation ledger

1. Stable source: main / v0.1-free-practice (8069764), original binary retained in Windows.
2. Isolation: user requested a feature branch; work in place on feature/tactical-training, use a new scene and Windows_Tactics output. No additional worktree requested or needed for this single-editor project.
3. First verify stable input regression and missing tactical contract, then implement rules, session, ball, poses, authoring and review harness.
4. Validate and record normal session actions across modes; inspect captures; revise observed problems.
5. Fresh whole-change review, final build and source push, results and operation guide.

## Completion ledger

- Stable commit/tag pushed before feature work; original scene, runtime scripts, prefab, project settings and Windows output preserved.
- Two drills and both starts implemented; actual ball flights, fixed player identity, denied requests and reactive poses verified.
- Seven recorded tactical regression runs produced the fixes documented in tactical-results.md. Final: 45 PASS / 0 FAIL. Existing practice: 28 PASS / 0 FAIL.
- Fresh independent review found three guard/interception issues. Added red tests, fixed them, reran and obtained reviewer confirmation. Also fixed stale success feedback and improved HUD contrast/layout after standalone use.
- Native standalone menu, target selection, pass, request, reset, help toggle, brief movement and focus-loss pause checked. Final 45-second desktop-region capture inspected and decoded; four-route 32.6-second demonstration decoded.
- Final Windows build succeeded (74,174,524 reported bytes). Managed game assembly SHA256: D26389301630E1FC997B367B74A14636557C4AE0339B72EE54DCD2BB58F930A5.
- User selected keeping changes on a feature branch and pushing it, so no integration-choice prompt or merge into stable main is needed. Shutdown remains the final action after push verification.

## Acceptance

The player can start any of the four combinations, move while holding/releasing Tab, sprint, pass to an actual teammate, move off ball, request a pass with visible denial feedback, see defenders react, reset repeatedly and return to the menu. The original practice remains playable. Recordings and test results must identify automated engine-driven routes separately from manual standalone checks.

## Boundaries

Rules and finite states, no trained model or claim of NBA 2K source reuse. No full scoring, rebounding, complete match rules, or production animation system in this iteration. Save prototype limitations honestly.
