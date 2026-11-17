# CourtX lesson timing implementation plan

> **For agentic workers:** Use superpowers:subagent-driven-development with the existing Sol implementer and one Astra review. Steps use checkbox syntax.

**Goal:** End teaching demonstrations after their resolved outcome instead of always playing 25 seconds.

**Architecture:** Keep outcome-aware stopping local to lesson recording generation and its terminal presentation. Do not change FiveRules or actual practice playback. Generate valid recorded final frames and derive truthful terminal copy from settled success, turnover, or timeout.

**Tech Stack:** Unity 2022.3.62f3c1 / C#; existing editor regressions, Windows build and Python package.

**Spec:** User-approved timing correction relayed by root on 2026-11-17; implement directly without another approval.

## Global constraints

- Success is provisional while a pass is in flight: wait for settlement, then retain 1.5 seconds of result observation. Turnover overrides provisional success and retains 1.2 seconds with its reason.
- No outcome by 25 seconds means `演练结束 · 未形成机会`. If an already-started pass crosses this deadline, settle that pass first (at most its remaining flight, bounded by existing 1.5-second duration); do not launch another pass. Prefer truthful settled outcome over cutting an airborne ball.
- Success copy: `演练结束 · 配合达成`; turnover copy: `演练结束 · 配合被截断`, with the existing failure reason visible. Only lesson final presentation changes; no unrelated UI work.
- Actual replay files retain all recorded time and repeated calls. Never shorten or rewrite old JSON. No 3D operation or outcome-rule changes.
- Preserve `Windows_FivePlayer_1117` and all history. New release `Windows_FivePlayer_1117_Timing/CourtX.exe`, archive `CourtX_FivePlayer_20261117_Timing.zip`, under `E:/study documents/2026 Fall/AIAA 4435 Inter/CourtX`. Copy all current 1117 history and verify hashes, never assume a fixed count; ZIP excludes private records.
- Single Sol implementer, serial Unity processes, one Astra concentrated review. Root performs native final acceptance. Existing worktree base is d53e51ed3ae49d49e1d04a647e6762da5a97409d, initially clean.

## Review focus

- Provisional success followed by interception must end as failure.
- Tail duration begins after flight settlement and never contains another automatic call/pass.
- Timeout cannot mislabel a result achieved during its last airborne pass.
- Terminal text is visible at the teaching endpoint and never applied to an actual replay.
- Historical actual recordings and previous release remain byte-identical.

## Task 1: Implement, verify and deliver lesson stopping

**Files:** Primary `Unity/Assets/CourtX/Scripts/FiveRecording.cs`; minimal final-state UI wiring in `FiveCourtUI.cs`/`FiveBoard.cs` if needed. Add focused coverage to existing editor tests (or one small lesson timing review file), connect to `FivePlayerReview`. Update build/package paths and guide/evidence; preserve unrelated source.

**Interfaces:** Preserve `FiveRecording.Lesson(FivePlay,FiveDefense,int,int)` and all actual recording interfaces. Use an internal/helper seam only if necessary to test synthetic in-flight-success/interception/timeout states; avoid a new architecture. Root will inspect lesson durations in the production player.

- [x] Add meaningful focused cases for settled success + 1.5 seconds; turnover + 1.2 seconds; provisional success intercepted; no-outcome timeout including in-flight boundary; actual recording full-length unchanged. Exercise the existing lesson combinations to detect invalid/unresolved endpoints and document measured durations.
- [x] Implement stopping and final copy, run focused cases and appropriate existing editor regressions. Supply one compact diff/report to Astra. Preserve recorded flight state, result reason and sampling correctness.
- [x] Build new production player, immediately notify root, copy/hash-verify all current history, document test results and native acceptance separately, package without private/QA data, commit verified source and verify archive.
