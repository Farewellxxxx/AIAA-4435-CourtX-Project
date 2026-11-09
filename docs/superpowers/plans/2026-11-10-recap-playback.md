# Readable recap playback implementation plan

> **For agentic workers:** Use superpowers:executing-plans for inline implementation and one final independent review.

**Goal:** Make the recap understandable as a basketball sequence, with synchronized pass feedback.

**Architecture:** A pure replay cursor samples the recorded episode without moving the live simulation. The IMGUI recap draws a halfcourt, six actors and the recorded ball; reports are rebuilt only when the selected decision changes. Existing local rules and optional episode-level provider remain available.

**Tech Stack:** Unity 2022.3, C#, existing IMGUI, existing Editor test runners.

**Spec:** User-approved design in this chat: complete halfcourt markings, recognizable basketball, all-player and ball playback, play/pause/slow motion, pass/feedback synchronization and dynamically sized long text. Preserve stable executables.

## Global constraints
- No gameplay/AI/physics changes, no new service or API key.
- Preserve Windows_Reactive, Windows_Tactics and Windows; build Windows_Recap separately.
- Work on feature/recap-playback from 169c80d; do not merge main.
- Replays are sampled (~0.2 seconds) with interpolated positions, not exact physical resimulation.
- Distinguish the recorded ball trail from the intended pass target line and from player movement.
- Optional provider text summarizes the whole episode and must be labeled as such.

## Review focus
- Empty/no-pass/single-frame episodes remain usable without invented passes (Task 1).
- Duplicate release timestamps and ownership transitions do not anticipate possession (Task 1).
- Scrubbing before the first pass and crossing later passes keep feedback synchronized (Tasks 1–2).
- Long Chinese narration and changing reports remain fully scrollable (Task 2; native visual check).
- Rendering/playing a recap never advances or mutates the live session or archived episode (Tasks 1–2).

### Task 1: Replay cursor and selected-pass reports
**Files:** Scripts/RecapPlayback.cs, TacticalJournal.cs; Editor/RecapReview.cs.
**Interfaces:** Reset(TacticalEpisode,int decisionIndex), Seek(float), Toggle(), Tick(float), Jump(int); Time, Speed, Playing, DecisionIndex, FrameIndex, Sample(); Build(TacticalEpisode,int).
- [ ] Write checks for selecting first/last pass, no future defense context, 6-player/ball interpolation, duplicate times, no early holder transfer, rewind/end/0.5 speed and source immutability.
- [ ] Run RecapReview.RunRules: expect failing assertions against initial API shells.
- [ ] Implement cursor using recorded timestamps, latest discrete state at or before cursor; preserve Build(episode) default selection.
- [ ] Run RecapReview.RunRules and ReactiveReview.RunRules: expect zero failures, then commit.

### Task 2: Court and playback UI
**Files:** TacticalRecapUI.cs, TacticalSession.Review.cs, Editor/RecapPlaytest.cs.
**Interfaces:** Session.Replay owns cursor; session LateUpdate ticks once with unscaled delta; UI reads Sample and selected report.
- [ ] Add scene tests for clock freeze during review, replay advancement, selected-report synchronization and retry reset; run for RED.
- [ ] Draw court matching paint_floor.py geometry, circular jersey markers, controlled/possession rings, basketball seams, short trails with legend and dashed target arrow.
- [ ] Add play/pause, restart, 0.5x/1x and timestamp slider; dynamically calculate text block/scroll heights.
- [ ] Run targeted tests for GREEN; native app checks at normal/small window sizes, multi-pass timeline, empty replay and long text fixture; save screenshots/video.

### Task 3: Regression, fresh review and delivery
**Files:** Test runner output routing, separate build/package helpers, docs/recap-results.md, docs/recap-user-guide.md.
- [ ] Run full existing six-suite regression plus new recap tests; expect zero failures.
- [ ] One fresh-context whole-branch review; fix material findings with reproducing tests in one pass.
- [ ] Build Windows_Recap, play final binary, record actual UI and verify video decoding.
- [ ] Package separately, verify archive entries/hash, preserve old build hashes, commit/push feature branch and link deliverables.
