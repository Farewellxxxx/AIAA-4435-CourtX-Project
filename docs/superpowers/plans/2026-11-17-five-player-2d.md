# CourtX 2D readability implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox syntax for tracking.

**Goal:** Expand the usable 2D court and clarify controls, immediate defensive feedback, and replay history without changing simulation semantics.

**Architecture:** Keep the existing Unity IMGUI board, state, and recording architecture. Share layout geometry between rendering and hit testing; store transient defensive emphasis outside simulation data. One implementation owner changes the coupled UI and its regression coverage, then one coordinator reviews the final diff.

**Tech Stack:** Unity 2022.3.62f3c1, C#, PowerShell, Python packaging.

**Spec:** Approved scope below, supplied by the user through the root coordinator on 2026-11-17.

## Approved scope and global constraints

- Enlarge the actual court by reducing header, inner canvas, and sidebar whitespace while preserving court geometry, cream/dark-green palette, and attack/defense colors.
- Keep tactics and overall defense readily discoverable on the left. Put `你控制谁` and `开局谁持球` inside expandable practice settings; collapsed state still shows selected role and possession.
- Condense right information to current phase, main offensive objective, and defensive reaction. Keep individual tasks as short hover text, never a permanent ten-player task list.
- Always show play/pause, previous/next node, and speed. Put single-step and other low-frequency controls under more. Emphasize undo/restore when edited; exact labels `恢复原站位`, `恢复并播放`, `允许自动补防`. Edited playback restores original footage before advancing. Add a few clickable phase/event timeline markers. Replace PLAYBOOK/REPLAY/FRAME prefixes with Chinese.
- Preserve paused-frame editing: dragged player pinned; other defenders recompute by rules; other attackers, time, phase, flight remain unchanged. Briefly highlight defenders that just shifted, without advancing simulation or reinstating historical trails. Confine concise tooltips away from right sidebar and buttons; retain one stable drag explanation.
- History cards prioritize tactic, controlled role, and existing team cooperation outcome (achieved/interrupted/ended), never personal mastery. Group by today/yesterday/date; date/time/duration subordinate. Preserve every record, scroll, hit targets, ID-based selection, and old replay copying.
- No 3D tutorial, What-if expansion, or simulation redesign. Preserve old builds and all Replays5v5. New build: `E:/study documents/2026 Fall/AIAA 4435 Inter/CourtX/Windows_FivePlayer_1117/CourtX.exe`; new archive: `CourtX_FivePlayer_20261117.zip`. Copy all existing 1116 history, hash verify, exclude personal records from ZIP.
- The user authorized direct implementation, Sol workers, one Astra coordinator/reviewer, no further approval. This overrides repetitive skill approval and reviewer dispatch. Only one Unity editor at a time. Native acceptance is the root coordinator's responsibility.

## Review focus

- Group headers and card gaps must not select a neighboring replay; bottom cards remain reachable after scrolling.
- Current replay identity remains stable across history refresh and selecting old records.
- A second drag or restoration must not leave stale highlights, move attackers, or advance flight/time.
- Expanded settings, more controls, and long Chinese text remain inside boundaries at supported player sizes.
- Timeline markers and edited playback must preserve seek/restore semantics and original stored recordings.

## Task 1: Deliver the integrated 2D revision

**Files:** Modify `Unity/Assets/CourtX/Scripts/FiveCourtUI.cs`, `FiveCourtView.cs`, and only necessary parts of `FiveBoard.cs`, `FiveRecording.cs`, `FiveCourtGame.cs`; update `Unity/Assets/CourtX/Editor/FiveViewReview.cs`, appropriate editor regressions, `Tests/FivePlayerPlaytest.cs`, authoring/build paths, `Tools/run_five_review.ps1`, `Tools/package_five_release.py`, and guide/results documentation.

**Interfaces:** Consume existing `FiveBoard.PlaceFrame`, `Seek`, `Jump`, `Restore`, `Playing`, `SetPinned`, `FiveCourtGame.History`, `SelectedReplayId`, `SelectReplay`, and `FiveCourtView` coordinate transforms. Preserve those public behaviors. Expose only minimal shared geometry/history-row/event-marker helpers needed to make rendering and tests agree; maintain compatibility or update all call sites in the same task. UI highlight timing uses real time, not `FiveBoard.Time` or simulation ticks.

- [x] Inspect relevant source and existing checks. Add meaningful coverage for grouped row hit testing/scroll/selection identity, enlarged layout boundaries and aspect ratio, and defender highlight without simulation advancement. Do not add mirror tests for spacing or wording. Update any executable QA coordinates before use.
- [x] Implement approved scope and self-review against invariants. Run appropriate editor regression and necessary player QA serially using `Tools/run_five_review.ps1` and current authoring entrypoints. Record command, result, check count, log path, and any limitation.
- [x] Build formal 1117 player, copy/hash-verify all 1116 history, update guide and 1117 verification evidence, create ZIP without history, verify contents/hash. Supply full diff and concise report for one coordinator review, then fix actionable findings with bounded focused reruns.

**Acceptance:** Successful editor regression, necessary player checks, production assembly without QA harness, preserved history hashes, valid private-record-free ZIP, coordinator review, and root native acceptance recorded separately.
