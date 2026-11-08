# Pass, defense and reception refinement plan

> **For agentic workers:** Use superpowers:executing-plans, completing and reviewing each round before the next.

**Goal:** Make passing, defensive contact and moving reception believable and readable in the existing two 3v3 drills.

**Architecture:** Keep the current Unity/C# finite-state prototype, scene and interfaces. Share deterministic pass geometry between the ball and decision feedback. Add only bounded blocked-ball animation, not an entire rebound/match system.

**Tech Stack:** Unity 2022.3.62f3c1, C#, existing procedural actors, existing FFmpeg recorder.

**Spec / delegation:** On 2026-11-08 the user asked the agent to independently identify improvements, implement one per round, review for new problems and repeat. They delegated design choices; no new design-approval checkpoint is needed. The chosen approach refines rules and feedback. A trained policy would not repair these collision/ownership faults; a complete physics-only ball would unnecessarily replace the working tactical framework.

## Constraints

- Preserve free practice, both tactical drills, fixed player identity, WASD/Shift/live hold-Tab and existing team integration seams.
- Start from a976158; keep a rollback tag and the previous packaged executable.
- Continue on the already authorized feature branch, with one commit per verified round, then push results.
- No full scoring/rebounding game, no external animation/model imports, no fabricated manual test claims.
- Record actual engine routes and inspect them. Keep original adversarial checks distinct from natural play.

## Round 1 — fair defensive contact

Files: TacticalRules.cs, TacticalBall.cs, TacticalActor.cs, TacticalRefinementReview.cs.

- [ ] Reproduce rear/side contact magically changing ownership, and defender array order winning simultaneous contact.
- [ ] Sweep the ball against actual defensive volumes, resolve earliest contact, distinguish a facing hand catch from a body deflection.
- [ ] Keep deflected/missed balls visible falling to the court, with no invented owner or automatic recall; the exercise can still end with R recovery.
- [ ] Run contact regression at multiple frame rates, review normal drill routes, record observations and commit.

## Round 2 — moving reception

Files: TacticalRules.cs, TacticalBall.cs, TacticalSession.cs, TacticalRefinementReview.cs, TacticalReview.cs.

- [ ] Reproduce a steady moving receiver being passed to their old location and NPCs freezing during flight.
- [ ] Predict a bounded fixed receiving point at release; have NPCs approach it. Show that point to a human receiver without steering their body.
- [ ] Catch within a bounded radius and blend the visual into the hands; sudden direction changes must still be able to miss.
- [ ] Verify moving catches, deliberate misses, reset/pause during ball animation and all four actual drill routes; review and commit.

## Round 3 — decisions and understandable requests

Files: TacticalSession.cs, TacticalBall.cs, TacticalUI.cs, TacticalRefinementReview.cs.

- [ ] Reproduce stale/expired requests and mismatches between proposed trajectory and passing-window feedback.
- [ ] Use the same planned trajectory and geometry for risk preview and AI; bind requests to the actual passer, expire clearly, freeze request time in menus, cancel on possession change.
- [ ] Validate normal and blocked requests, menu pause, possession changes and invalid charge/release intents; review and commit.

## Final gate

- [ ] Fresh independent whole-change review; fix material findings and rerun affected tests.
- [ ] Full tactical and old-practice regressions, final Windows build, inspected recordings, concise iteration notes and updated integration guide.
- [ ] Push commits and rollback tag, verify remote hashes and saved files.

## Review focus

1. Ownership must stay singular across deflection, catch blending, pause and repeated resets.
2. Earliest physical contact wins regardless of actor-array order and frame rate.
3. A fixed prediction must not become a homing ball or magnetically catch a player who ran away.
4. Old E intent must not trigger a surprise pass by a different future holder.
5. New contact/receiving behavior must not quietly bypass tactical completion guards.

## Ledger

- Baseline inspected: clean a976158. Existing ball has 2D unconditional interception and only endpoint catching; NPC receiver freezes. These are concrete improvement targets, not speculative rewrites.
- Existing feature checkout used in place, consistent with prior user-approved single-editor workflow. No additional worktree is requested.
- Round 1 red: 7/14 contact checks failed, including the array-order issue at 15 fps. Review then exposed stale Rigidbody position during paused drop and an existing lane-preview mismatch in the high-post route. Unified simulated/rendered position and brought shared defensive-volume preview forward from round 3 so round 1 remains independently playable.
- Round 1 complete: run_unity_review.ps1 TacticalRefinementReview.Run stage 1 -> 14/14; TacticalReview.Run round1 -> 45/45, four natural routes recorded. Release trace proved the high-post failure was a stale test-driver decision after charging; the driver now charges before checking the release window. Runtime collision rules remain intact. Inspected round1 recording frame.
- Round 2 complete: moving-human catch and AI flight motion reproduced RED (2 failures); fixed lead point, receiver movement, shared preview plan, .78 m catch radius and .16 s hand blend. Stage 2 complete -> 21/21; four full routes -> 45/45. Catch pause/continuity verified. The route driver now walks toward the visible fixed receiving point, as a human must; deliberate reversal still fails. Inspected recorded first-person frame.
- Round 3 complete: five request/release failures reproduced RED; all fixed with holder-bound active-time requests, explicit expiry and owned-charge guard. Stage 3 -> 26/26; full tactical routes -> 45/45. UI preview uses current charge speed and shared lead geometry. Remaining gate: independent review, stable regression, build, visual checks and delivery.
