# SDD ledger — plan: docs/superpowers/plans/2026-11-14-five-player-workbench.md

Base: e60ac7e; native managed worktree on feature/five-player-workbench.
Execution authorization: user approved full scope, multi-round self-testing, and routine permissions.
Pre-flight: Task 1 FiveState/Rules feeds Tasks 2/4; Task 2 FiveBoard/Recording feeds Tasks 3/4; all share the same state shape and coordinate convention (x -7.5..7.5, y depth 0..14).
Ruling: Keep legacy 3v3 classes/scenes and add a new complete 5v5 path — fixed six-person schema cannot safely represent ten — costs additional adapter code but protects original records.
Ruling: New recordings use Replays5v5, old records remain in their original programs — avoids inventing absent players — new viewer does not migrate old 3v3 recordings.
Ruling: Large Unity caches and builds use E:; managed source remains C: — C: has ~4 GB free — no user files moved.
Task 1: started; base 0053ea6. Baseline running before production changes.
Task 1 Ruling: Corrected overconstrained test requiring three defenders to move with a stationary perimeter ball; two upper defenders react while low defenders may legitimately hold zone. Added explicit baseline-ball test requiring the low defender to close distance. Initial implementation: 4 failures solely from the overconstrained count.
Task 1: complete (0053ea6..HEAD). Tests: Tools/run_five_review.ps1 -Method FivePlayerReview.RunAll -Label round1-all => exit 0; 70 new checks and four legacy suites passed. RED captured 40 expected missing behaviors; final count includes baseline-ball behavior.
Task 2: complete. Tests round2-all => exit 0, legacy baseline plus new core/board/persistence/9 lesson combinations. Fixed Unity null-object serialization via explicit flight presence; added high-post entry reversal when pressure denies first pass. Raw evidence on E: Preview/Playtests/FivePlayer-20261114.
Task 3: controller/view implemented, 106 checks green; visual rendering waits for new scene authoring in Task 4. Ruling: integrate Task 4 scene before final Task 3 visual gate because a runnable scene is the shared prerequisite; no task marked complete before visual evidence.
Task 3: complete. final-rules => 117 checks plus four legacy suites green. Actual rendered 1024x576, 1280x720, 1920x1080 inspected. First hidden-window captures did not render; launching the same QA build visibly passed all 32 player checks without errors.
Task 4: complete. QA validates ten models / nine NPCs for all five roles, front eyes, immediate passes, repeated call, archive/reload and immutable replay edits. Fixed stripped indicator shader by serialized material dependency and two-sided overlapping jersey text using textured quads. Added pending-save queue after observed RED single-slot data-loss regression; final-rules GREEN 117 checks.
Task 5: started. Fresh independent whole-branch review, native mouse/keyboard validation and final packaging remain.
Final review: fresh gpt-6-astra / high, read-only whole branch 1224ca1; two P2 findings, no critical or minor.
Final: fixed fractional edits rewriting prior branch interpolation — pre-edit anchor + same-time edited pose, exact sampling uses last pose; fractional/same-time/undo tests RED→GREEN, suite 126/126 plus legacy baseline.
Final: fixed terminal-state branches unable to continue — editing reopens current positions with offensive possession, preserving in-flight state; success/turnover/airborne tests RED→GREEN, suite 126/126 plus legacy baseline.
Final: Ruling: Full scoring/shooting/rebounds/fouls/transition — excluded from this tactical decision prototype as documented — not a complete basketball match.
Final: Ruling: Headset/VR-controller inputs — desktop first-person delivery matches current runnable prototype — headset acceptance remains future work.
Final: Ruling: 3D What-if — explicitly excluded by user — 3D always starts configured preset.
Final: Ruling: Old 3v3 conversion — retain original builds and records, no invented extra players — old recordings require old executable.
Final: Ruling: What-if branch export — only real rounds persist in approved scope — temporary hypothetical branches reset when changing lesson/replay or closing.
Final: Ruling: Empirical tactical prediction accuracy — local deterministic teaching rules, not validated probabilities — do not interpret this as real-game predictive accuracy.
Native acceptance: actual release mouse drag defender, hover task, single-step, drag outside court with release/clamp, undo, restore, start 3D, Q pass/interception, C rearm, Esc pause/resume, V actual history, scrub + drag offense all observed. native-player1.log no errors. User practice data not touched.
Task 5: complete. Final fresh review fixes green (126 checks), all four legacy suites green, rebuilt QA 32 checks / 0 runtime errors. Final release native terminal-edit continuation verified and clean runtime log. Assembly excludes QA bootstrap. 105 original file hashes unchanged. ZIP contains 130 runtime/document files plus hash manifest, CRC verified, no practice recordings, 26.6 MiB. Build source commit c6e2bcd. Package SHA256 38f828811773558bf7161d702d510bb8fc1c954561c01d6b6a3364b54f58a3cc.
Finish: retain local named branch and managed worktree; user requested runnable local build, no remote publication. No further approval requested. Temporary audit ledger retained as evidence; no user data deleted.
