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
- [ ] Write behavioral checks: no trigger before screen use; Drop protects paint, Switch exchanges 0/1 exactly once, Blitz converges then recovers after pass; third defender helps and returns; seeded reset deterministic.
- [ ] Run Unity ReactiveReview round1 RED, then implement and run GREEN. Baseline interactions 31 checks first.
- [ ] Play and record PNR variants with both roles; correct actual observed faults. Commit task.

### Task 2: Teammate response

**Files:** ReactiveTactics.cs, TacticalSession.cs, TacticalActor.cs, Editor/ReactiveReview.cs; optional small licensed PressureModel.cs port + third-party notice.
**Interfaces:** stable target arrays and assignments from task 1; scoring consumes CanPass and decision-time receiver separation. Produces short-roll, roll, weakside relocate/cut and AI safe receiver choice.
- [ ] Write checks: short roll vs trap; weakside lift after help and hold spacing otherwise; unsafe request does not force pass; AI has another safe option without fixed-jersey bias.
- [ ] Observe failures; implement minimal local rules. Validate external-license compatibility before port, and compare simple pressure fixtures in Unity.
- [ ] Play round2, check space and poses and flight; commit after GREEN.

### Task 3: Fact-based recap

**Files:** create TacticalJournal.cs, TacticalRecapUI.cs; integrate Session and ball release event; tests in ReactiveReview.
**Interfaces:** journal Begin/Tick/RecordDecision/RecordEvent/Finish; serializable episode/frames/decision options; ITacticalRecapProvider.GenerateAsync(immutable episode, cancellation). Session OpenReview closes the current episode, pauses, saves JSON + text; version guard prevents stale overwrite.
- [ ] Tests first: release-time risk retained after actors move; good-choice/missed-catch separated; no-pass round has no invented pass; paused time frozen; reset invalidates pending provider; JSON roundtrip preserves facts.
- [ ] Observe RED; implement Chinese rules, export, keyframe diagram, retry/next and menu controls. No API keys/network required.
- [ ] Play round3 and record UI; commit after GREEN.

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
`n- Task 1: complete — 20 defensive checks (9 RED failures to 0), 45 route/control checks green; first real-scene video 1727 frames / 30 fps. Existing free movement and passing routes remain usable. Video snapshot inspected.
