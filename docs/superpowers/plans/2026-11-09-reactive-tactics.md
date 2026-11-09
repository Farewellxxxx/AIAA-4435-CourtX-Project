# Reactive tactics and local recap implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this approved plan task by task.

**Goal:** Add readable Drop/Switch/Blitz defense, contextual teammate responses and evidence-based Chinese episode review while preserving the stable programs.

**Architecture:** Keep TacticalBall as the only possession authority. Add a small deterministic defensive coordinator, contextual offensive targets and a session-owned event journal. Recap consumes immutable decision-time snapshots; an asynchronous provider interface may verbalize these later, while the default is entirely local.

**Tech Stack:** Unity 2022.3.62f3c1, C#, Built-in renderer, existing OnGUI UI and Unity play-mode regression harness.

**Spec:** User-approved execution prompt in this conversation (2026-11-09); its requirements are recorded below.

## Global Constraints

- Preserve main, feature/tactical-training at dc4b774, Windows, Windows_Tactics and all previous recordings.
- Work on feature/reactive-tactics-review; build to Windows_Reactive. Tag and archive dc4b774 before edits.
- Preserve fixed identity, Shift, held live Tab, Q/E, real ball contact and catch rules. No shooting/scoring/full-match or RL stack.
- Three PNR coverages with delayed spatial triggers, assignments, help and recovery; retain high-post drill with normal help/recovery.
- Six PNR coverage/role combinations plus two high-post roles must be playable; random selection has a saved seed and retry.
- Teammates react to the current defense; player movement remains free. Clear/unsafe passing uses the same physical preview.
- Recap records time, positions, assignment changes, help/recovery, pass choices and actual outcomes; separate estimated decision quality from execution.
- Show an overhead keyframe/trajectory view and retry/next controls. Default Chinese local rules; optional cancellable LLM interface with stale-result protection.
- Three play/review/refinement rounds; preserve test evidence, videos, source, distributable and instructions. Push only the new branch and stable tag.

## Review Focus

1. Early pass/rejected screen/reversal must not force a trap or repeatedly swap assignments (task 1).
2. Pausing, resetting and switching roles must clear requests, simulation timers and pending recap work (tasks 1/3).
3. Threat evaluation must not alter ownership or gift an unsafe requested pass (task 2).
4. Uncompleted possession and a late provider response must not fabricate completed events or overwrite a newer review (task 3).
5. 15/30/60 fps arrival contact remains aligned; exported journal and displayed recap must agree (task 4).

### Task 1: Reactive defense

**Files:** create Scripts/ReactiveTactics.cs, Editor/ReactiveReview.cs; modify TacticalSession.cs, TacticalUI.cs, TacticalAuthoring.cs; add Tools/run_reactive_review.ps1.
**Interfaces:** coordinator Reset(coverage, seed); Tick(session,dt); ApplyTargets(session,targets); MarkFor(defender), Stage, HelpActive. Session exposes Coverage/Seed/ActiveTime and StartScenario/RetryScenario/NextScenario.
- [x] Write behavioral checks: no trigger before screen use; Drop protects paint, Switch exchanges 0/1 exactly once, Blitz converges then recovers after pass; third defender helps and returns; seeded reset deterministic.
- [x] Run Unity ReactiveReview round1 RED, then implement and run GREEN. Baseline interactions 31 checks first.
- [x] Play and record PNR variants with both roles; correct actual observed faults. Commit task.

### Task 2: Teammate response

**Files:** ReactiveTactics.cs, TacticalSession.cs, TacticalActor.cs, Editor/ReactiveReview.cs; optional small licensed PressureModel.cs port + third-party notice.
**Interfaces:** stable target arrays and assignments from task 1; scoring consumes CanPass and decision-time receiver separation. Produces short-roll, roll, weakside relocate/cut and AI safe receiver choice.
- [x] Write checks: short roll vs trap; weakside lift after help and hold spacing otherwise; unsafe request does not force pass; AI has another safe option without fixed-jersey bias.
- [x] Observe failures; implement minimal local rules. Validate external-license compatibility before port, and compare simple pressure fixtures in Unity.
- [x] Play round2, check space and poses and flight; commit after GREEN.

### Task 3: Fact-based recap

**Files:** create TacticalJournal.cs, TacticalRecapUI.cs; integrate Session and ball release event; tests in ReactiveReview.
**Interfaces:** journal Begin/Tick/RecordDecision/RecordEvent/Finish; serializable episode/frames/decision options; ITacticalRecapProvider.GenerateAsync(immutable episode, cancellation). Session OpenReview closes the current episode, pauses, saves JSON + text; version guard prevents stale overwrite.
- [x] Tests first: release-time risk retained after actors move; good-choice/missed-catch separated; no-pass round has no invented pass; paused time frozen; reset invalidates pending provider; JSON roundtrip preserves facts.
- [x] Observe RED; implement Chinese rules, export, keyframe diagram, retry/next and menu controls. No API keys/network required.
- [x] Play round3 and record UI; commit after GREEN.

### Task 4: Whole-branch verification and delivery

**Files:** tests, documentation and evidence; no old build overwrite.
- [ ] Run 31 interaction, 45 route/control, 28 free-practice baseline checks (adapt tactical route inputs only if the changed defense warrants it; retain assertions on physical correctness).
- [ ] Exercise 8 scenarios, adversarial early/reverse/blocked/pass/reset inputs, deterministic sequence and all recap cases; inspect video and native build UI.
- [ ] Fresh independent whole-branch code review; fix material findings with RED/GREEN and suite rerun.
- [ ] Build Windows_Reactive, save full zip and SHA256, actual videos, journal samples, Chinese manual and third-party reuse report; verify archive and stable hashes, push new branch/tag.

## Execution ledger

- Baseline: dc4b774; clean branch; v0.3-tactics-stable-20261109 tagged. Source zip and binary copy in Preview/Backups/Stable-20261109; binary SHA256 298AE1C453D578CE460E2266EA5205F639D529944F8A983B8AAA509D8BA7D93C.
- Ruling: use the requested new branch in the existing Unity checkout, preserving stable tags, source archive and separate builds — avoids duplicating Unity's large imported Library and leaves the user's known project path usable; changing branches later requires closing Unity first.
- Ruling: this written plan records the already approved prompt; no further approval round is needed.
- Pre-flight: defense produces target/assignment state used by offense and journal; ball keeps sole ownership. Journal release snapshot must be captured before Launch clears Holder, not in the later ReportPass event.
- Ruling: retain this ledger and verification artifacts because the user explicitly asked to save results. Windows-native tooling may replace Bash-only skill bookkeeping.
- Task 1: complete — 20 defensive checks (9 RED failures to 0), 45 route/control checks green; first real-scene video 1727 frames / 30 fps. Existing free movement and passing routes remain usable. Video snapshot inspected.
- Task 2: 31 rule checks green after 7 failing feature checks, plus round2 real-scene recording. BasketWorld pressure port compiled in Unity 2022 with no additional dependency. Helper response test waits for both the trap and the independent help reaction delay.
- Task 2 iteration finding: 44/45 real-scene checks passed; the cutter received via 04→05→01 but the old objective required lastPasser==04. Keep physical receipt and location requirements, allow a legitimate third-player relay after high-post reception. Regression run is in progress; task 2 is not complete until it passes.
- Task 2: complete after relay fix — 45/45 real-scene checks pass; all actual ball and objective assertions retained. The prior failing record is round2-relay-before-fix.txt.
- Task 3 feature checks: 39/39 pure rules/recap checks green (6 recap failures first). Eight scenarios pass including direct/relay/weak-side outcomes. Added built-in Unity JSONSerialize module after verifying the installed editor package; no external runtime package needed.
- Round 3 refinement: weak-side advantage previously failed the fixed-receiver completion gate (3 observed failures); now accepted with actual reception, offensive court position and >1.2 m space. Third-player high-post relay follows the cutter. Edge run reproduced same-seed events and positions but exposed focus loss closing a review; fix preserves the review while already paused. Full suite pending.
- Native standalone UI checked through Computer Use: selected Blitz/04, started, E requested and caught a real pass, V opened review, same-scenario retry restored ownership and identity. Full 1280x720 recording and JSON/TXT preserved. Initial smaller crop retained as diagnostic; final native-review-ui.mp4 is the verified full client crop.
- Task 3: complete — 39 rule/recap tests and 83 real-scene scenario/edge checks pass. Focus-loss regression RED→GREEN. Deterministic repeated input matches positions and key events. Final full suite: 226/226 (39 rules, 83 scenarios, 31 interaction, 45 tactical/control, 28 free practice). Stable executable, Assembly-CSharp and UnityPlayer hashes match the archived stable package.
- Final: fixed stale armed screen after early pass/retreat — 6 state regressions RED→GREEN; new valid screen cycles still engage. Fixed initial high-post relay for both roles and AI continuation — 3 live-scene regressions RED→GREEN. Fixed mismatched/closed release-window timing — 1 recap and 2 real snapshot regressions RED→GREEN. Full reviewed suite 245/245 (49 rules, 9 review regressions, 83 scenarios, 31 interaction, 45 original routes, 28 practice).
- Final: Ruling: pause freezes active requests rather than clearing them — preserves the explicitly approved original control behavior; pending valid request can still execute after resume. Reset/role changes clear all old state.
- Final: minor (deferred): recap diagram navigation and fixed default text use different time scopes; timestamps/manual identify them, UI labels can be clearer.
- Final: minor (deferred): a future long optional provider narrative can exceed the fixed text area; current local short review is unaffected.
