# CourtX Learning Hub Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans for inline implementation and one fresh whole-branch review.

**Goal:** Deliver the approved integrated 3v3 learning, practice, comparison and review loop.
**Architecture:** Pure scenario/catalog/history models feed a session-owned LearningHub controller. A separate IMGUI renderer displays recorded examples and completed episodes using RecapPlayback and RecapCourtView. Existing simulation remains authoritative.
**Tech Stack:** Unity 2022.3 / C# / IMGUI / JsonUtility. No new packages or services.
**Spec:** docs/superpowers/specs/2026-11-11-learning-hub-design.md (approved chat prompt captured).

## Global constraints
- Branch feature/tactical-learning-hub from 08ce3ae. Stable tag v0.4-recap-stable-20261111. Independent Windows_Learning build.
- Two drills, six actors, controlled 01 or 04; no physics or tactical policy rewrite.
- Examples labeled recorded; comparisons are actual rounds with explicit shared-start checks.
- No product or new documentation attribution to another prototype. Preserve existing third-party licenses.
- Preserve Tab, Shift, Q/E, R/Esc, local review and optional provider cancellation behavior.

## Review focus
- Quiz navigation and scenario changes never show explanation for a different snapshot (Tasks 1–3).
- Comparing different seeds/roles/initial positions or incomplete records is refused without inventing results (Task 1).
- Returning from practice, menu, history, quiz or review preserves valid ownership and never unpauses an ended round (Task 2).
- Bad/legacy replay files and empty histories cannot crash startup or silently relabel examples as personal practice (Tasks 1–2).
- Small windows, long Chinese feedback, playback and real UI input remain usable; instrumented QA absent from release (Task 3).

### Task 1 — Shared learning data and record contracts
Files: LearningScenario.cs, LearningCatalog.cs, LearningHistory.cs; Resources/CourtXLearning/*.json; Editor/LearningReview.cs.
Interfaces: LearningScenario.FromEpisode / Normalize / Key / ResolveCoverage; LearningCatalog.Load(config), Steps(episode), Questions(); LearningHistory.Add/Load/CanCompare/Summary.
- [ ] Write behavior tests for eight selections, actual phase/pass anchors, six grounded questions, wrong-choice feedback, same-start comparisons, no-pass outcomes, history cloning/deduplication, invalid/legacy files.
- [ ] Run LearningReview.RunRules for RED; implement minimal production behavior; rerun for GREEN and RecapReview/ReactiveReview.
- [ ] Commit task and ledger.

### Task 2 — Session lifecycle and learning controller
Files: LearningHub.cs, TacticalSession.Learning.cs; minimal hooks in TacticalSession.cs/Review.cs; Editor/LearningPlaytest.cs.
Interfaces: Show(tab), SelectScenario/Question/History, StartPractice, CaptureCompleted, PinBaseline, Replay/Comparison cursors.
- [ ] Scene tests RED for hub pause, settings transfer, archived review round identity, replay freeze, retries, answer persistence and actual comparison capture.
- [ ] Implement controller; preserve legacy ReviewOpen and default report behavior for existing integrations.
- [ ] Run scene tests GREEN, plus existing reactive/recap regression; commit.

### Task 3 — Page UI and three rounds of playtesting
Files: LearningHubUI.cs (+focused partial files as needed), RecapCourtView.cs adapter; Editor/LearningAuthoring.cs, Tests/LearningPlayerReview.cs (conditional).
- [ ] Implement four page layouts, recorded-step navigation, quiz feedback, practice launch, history and A/B synchronized-view comparison using the tested controller.
- [ ] Round 1: complete two-drill learning/practice/review loop. Round 2: variation/role/failure/empty history. Round 3: small window, long content, input and return paths.
- [ ] Add meaningful regressions for discovered defects; record native screenshots/video and inspect them. Native input tool fallback must be disclosed.
- [ ] Full existing six-suite + recap + learning checks GREEN; commit.

### Task 4 — Review, release and preservation
Files: user guide/results/integration notes, verification evidence, packaging/verification helpers.
- [ ] One fresh-context whole-branch review; fix material findings in one RED→GREEN pass; record rulings/minors.
- [ ] Build final Windows_Learning without test instrumentation; smoke test, package, verify contents/hashes and unchanged prior releases.
- [ ] Save recordings/results, commit/push branch and stable tag; close test instances; mark goal complete only after all delivery checks.
