# SDD ledger — plan: docs/superpowers/plans/2026-11-12-replay-readability.md

Base: 0d6fd73. Branch: feature/replay-readability.

Ruling: User delegates visual/interaction decisions with “你自己想想…试试”; implement the concrete local iteration without another approval loop.
Ruling: Retain the existing single Unity checkout on a new branch and use separate builds, consistent with project practice; editors/builds run sequentially. A second checkout would duplicate the import cache.
Ruling: Keep a compact repository ledger instead of Bash-only workflow helpers on this Windows host. No step evidence is omitted.
Ruling: Screen styles and reversible focus controls are validated by actual rendering/native actions plus existing suites, following the developer instruction against low-impact or implementation-mirroring tests.
Pre-flight: Task 2 consumes Task 1's optional focus argument and shared hit-test mapping; Task 3 exercises both through the real learning UI. No interface conflict.

Task 1: in progress. Old build SHA-256 inventory and baseline images saved under Preview/Playtests/Readability-20261112.

Task 1: complete. Actual Unity images show separate team paths, directional cues, gold flight and ivory target. Verified small-window and larger/letterboxed captures. Accurate two-second clipping and duplicate timestamp guards keep displayed paths within recorded times.
Task 2: implementation complete. Four existing suites: 79+27+22+10 = 138 PASS, zero failures. Visual QA: 64 PASS, 32 screenshots, four resolutions. Native input check remains in Task 3.
Task 3: in progress. Read-only fresh reviewer dispatched; separate release build and native input validation pending.
Ruling: First hidden-player capture was invalid (eight black frames and resulting UI-reset assertions failed). Retained that run as failed evidence, added a nonblack-surface check, and reran via a normal app launch; final capture and assertions passed without changing production code.

Final review: one Important finding, no Critical or Minor findings. Overlapping marker selection was independently reproduced with 3 FAIL / 3 PASS before the fix. Changed hit testing to reverse paint order for actual badge interiors, keeping nearest-centre fallback only for extra hit padding.
Final: Ruling: Native input and final hashes were set aside by the read-only reviewer; these are required delivery checks and remain assigned to the primary agent. QA images do not establish learning effectiveness or user preference; no such claim is made.

Final: fixed overlapping badge selection — three recorded-input failures became PASS; all six dedicated checks PASS. Reran all four suites after the fix: 138/138 PASS. Total functional checks 144/144.
Task 3: release and QA builds succeeded. Native mouse checks passed (timeline seek/drag, attack and defense focus, repeat-click/reset control, original three replay records). Release contains no QA classes. All 139 previous-build/replay files match their original SHA-256 values; six replay files copied to the new local program. Final QA refresh and artifact packaging remain.
