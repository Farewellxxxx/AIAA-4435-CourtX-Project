# Replay Readability Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Make player motion and ball decisions easy to distinguish in the 2D viewer.
**Architecture:** Preserve the recorded-data and live-game controllers. Extend the shared court renderer and learning UI's presentation state, with separate authoring/QA outputs.
**Tech Stack:** Existing Unity 2022.3.62f3c1, C#, IMGUI.
**Spec:** `docs/superpowers/specs/2026-11-12-replay-readability-design.md`

## Global Constraints

- Single sequential Unity checkout; no gameplay/data schema change, new packages or online service.
- New build `Windows_Readability`; preserve old releases and user records.
- User delegated implementation choices; proceed through local implementation and review.
- Pixel/style edits and reversible focus UI use rendered/native verification; no style-mirroring unit tests.

## Review Focus

- Duplicate sample timestamps, tiny movements and the clipped beginning of a two-second trail must not show phantom lines or future positions.
- Focus hit testing must match the visible badge under small and letterboxed windows, including overlapping badges.
- Quiz state must remain frozen and target arrows stay hidden before answering.
- Switching records or A/B sides must not retain a misleading hidden-player filter or mutate episode data.
- Release player must exclude QA instrumentation and preserve previous build/replay bytes.

### Task 1: Paths and symbols

Files: `RecapCourtView.cs`, `TacticalRecapUI.cs`.
Interfaces: preserve existing `Draw`/`DrawAt` calls with optional `focusActor=-1`; add `PickActor(Rect,TacticalFrame,Vector2)` returning closest visible actor or -1.
- [ ] Keep baseline images and hash the old program.
- [ ] Implement contrasted/clipped paths, direction cues, ivory target arrows and dark badge borders.
- [ ] Inspect actual renders for crossing routes and motion endpoints.

### Task 2: Focus and timeline

Files: `LearningHubUI.cs`.
Interfaces: consume renderer focus argument and hit test. Focus belongs only to this UI; clear on record/page changes.
- [ ] Add click-to-focus and reset with clear text, plus timeline pass ticks.
- [ ] Update legends to match the rendered meanings.
- [ ] Validate selecting, clearing and record switching using native input; run four existing suites (138 checks).

### Task 3: Verified local build

Files: `LearningAuthoring.cs`, `ReadabilityPlayerReview.cs`, documentation.
- [ ] Add separate release and QA build entries, QA capture matrix and functional checks for the view interactions.
- [ ] Build/capture sequentially, visually inspect the four sizes and refine any clipping.
- [ ] Run fresh review, verify old hashes and absence of QA in release; copy existing user records only to the local new build.
- [ ] Record evidence and deliver local executable; retain work locally without push.
