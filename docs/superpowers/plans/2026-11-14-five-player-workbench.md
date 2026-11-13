# Five-player Workbench Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement task-by-task, with one fresh whole-branch reviewer at the end.

**Goal:** Deliver a runnable Windows CourtX with shared editable 2D lessons/replays and ordinary 5v5 decision practice.
**Architecture:** New serializable `FiveState` and deterministic `FiveRules` serve a shared `FiveBoard` timeline/branch controller and `FiveCourtGame` runtime. Existing six-player modules remain available only in their legacy scenes. A new scene reuses court/character geometry.
**Tech Stack:** Unity 2022.3.62f3c1, C#, IMGUI, existing assets, local JSON.
**Spec:** `docs/superpowers/specs/2026-11-14-five-player-workbench-design.md`

## Global Constraints
- Two main entrances; no challenge UI, no 3D What-if.
- Three offense presets, three team defense strategies, ten actors, five selectable offensive roles.
- Source and recorded episodes never mutated by branches. Airborne state is restorable.
- Q is single-press pass; E requests; sprint retained; tactical success independent of charge timing.
- Preserve legacy builds/data. New build `Windows_FivePlayer`, new `Replays5v5`.
- Caches and release outputs on E:. Only one Unity process per checkout.

## Review Focus
- Drag at a fractional/airborne timestamp and repeated edit/undo must not rewrite history or reset flight.
- End-of-record playback and long/damaged records must remain bounded and recoverable.
- Player role and possession diverge after passes; target selection must not transfer control.
- Repeated call after success/turnover must rearm AI; defense must remain on court with distinct responsibilities.
- Small-window scale and drag outside the board must preserve coordinate accuracy and release pointer state.

### Task 1: Complete five-player state and deterministic rules
Files: new `Scripts/FiveState.cs`, `Scripts/FiveRules.cs`; tests `Editor/FivePlayerReview.cs`.
Interfaces: `FiveRules.Create(FivePlay,FiveDefense,int role,int holder) -> FiveState`; `Step(FiveState,float,int human=-1)`; `Pass(FiveState,int) -> bool`; `Request(FiveState,int)`; `Call(FiveState)`; `Refresh(FiveState)`; `FiveState.Copy()`.
- [x] Run unchanged legacy baseline (reactive, recap, learning, readability).
- [x] Write behavioral tests for ten actors, role/holder separation, reproducibility, defensive continuity, one-click pass/catch, requests and call reset; watch RED using minimal API stubs.
- [x] Implement bounded deterministic motion, three play stages and zone responsibilities, pass assist and observable events.
- [x] Run rules GREEN and unchanged baseline; commit.

### Task 2: Shared editable timeline and durable recordings
Files: `Scripts/FiveBoard.cs`, `Scripts/FiveRecording.cs`; extend `FivePlayerReview.cs`.
Interfaces: `FiveRecording` stores `List<FiveState> frames`, `FiveBoard.Load(record)`, `Seek(float)`, `Tick(float)`, `Move(int,Vector2)`, `Undo()`, `Restore()`, `SetPinned(int,bool)`, `Pass(int)`, `State`, `Record`, `IsBranch`, `Playing`.
- [x] RED tests for fractional sampling without future events, immutable source, editing both teams, in-flight edit, undo after advancing, branching from an earlier cursor, roundtrip persistence and malformed input.
- [x] Implement one shared board for lessons and actual replays, branch-local clocks/frames, bounded records and atomic saves with visible failures.
- [x] Verify GREEN; commit.

### Task 3: Unified 2D workbench and reference styling
Files: `Scripts/FiveCourtUI.cs`, `Scripts/FiveCourtView.cs`, `Scripts/FiveCourtGame.cs` board integration.
Interfaces: `FiveCourtView.Draw(Rect,FiveBoard,int role)` and `Pick(Rect,FiveState,Vector2)` / `WorldPoint`; `FiveCourtUI.Draw(FiveCourtGame)`.
- [x] Add view-coordinate/hit-test behavior tests before integration; verify RED.
- [x] Implement two tabs, preset/role/possession/defense controls, hover tasks, compact intent, dark green enlarged court, screen marker and 2.5-second tapering trails.
- [x] Integrate drag/undo/restore/branch actions and playback/seek/speed/nodes without challenge UI.
- [x] Render small/large actual windows and inspect; adjust without superficial style tests; commit.

### Task 4: Normal 5v5 first-person practice and replay loop
Files: `Scripts/FiveCourtGame.cs`, `Scripts/FiveActors.cs`, `Editor/FivePlayerAuthoring.cs`, `Tests/FivePlayerPlaytest.cs`.
Interfaces: `StartPractice()` always creates a fresh configured state; `FinishPractice()` records and loads replay; `CallPlay()` resets sequence, all use same rules; authoring `Build` / `BuildQA`.
- [x] RED integration tests for ten visible actors (nine NPC + human), role mapping, immediate pass without charging, request feedback, repeated call, episode save/reload and 2D branch not seeding 3D.
- [x] Reuse scene/court/player driver, clone extra actors, fix labels/defense classification, add front eyes and live target highlight. Keep walk/run, Q pass, E request, C recall tactic, R restart, V finish, Esc board.
- [x] Run automated player cycle, inspect first-person and overview, refine reactions; commit.

### Task 5: Release validation and delivery
Files: `Tools/run_five_review.ps1`, `docs/FIVE_PLAYER_GUIDE_ZH.md`, `docs/verification/five-player-20261114/*`, README.
- [ ] All existing/new rule suites and player QA green; render 1024×576, 1280×720, 1920×1080.
- [ ] Fresh independent whole-branch review; fix important findings with reproductions and regression checks.
- [ ] Build release without QA code, run actual mouse/keyboard flows, inspect logs and screenshots.
- [ ] Package EXE folder/ZIP on E:, verify source record isolation and old output hashes, write guide and evidence; commit locally, no remote publish needed.
