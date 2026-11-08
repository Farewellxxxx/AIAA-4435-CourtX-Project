# Tactical integration

## Scene boundary

`HalfCourt.unity`, its prefab and original six runtime scripts remain unchanged. `TacticalTraining.unity` is a separate authored scene. Its `Tactical session - replaceable logic` object references the existing cameras, player controller, basketball visual and six actor roots. The controlled actor's mannequin is hidden so there are always five visible NPCs. The fixed controlled identity is independent of possession.

The tactical scene disables `PreviewActor`, `Walkthrough` input/HUD and the old `PreviewBasketball` update loop. It calls the original Walkthrough movement functions directly, preserving CharacterController, camera, walk/run and overview behavior. `TacticalBall` is the sole possession authority. Do not enable both ball controllers in the same scene.

When replacing the whole preview layer with the team's controller, also disable the separate TacticalSession object. To reuse the tactical logic instead, reconnect its player/actor/ball references to the new rig and keep only one movement/possession authority. Disabling PreviewOnly alone is the old free-practice handoff and is not sufficient for the new scene's external session object.

## Public seam for teammate code

| Component | Interface | Purpose |
| --- | --- | --- |
| TacticalSession | `StartDrill(TacticalDrill, bool offBall)` | Reset scene state, choose drill and player identity |
| TacticalSession | `Controlled`, `Position(id)`, `Facing(id)` | Identity and world-space root data |
| TacticalSession | `Phase`, `PhaseChanged` | Observe tactical transitions |
| TacticalSession | `SelectReceiver(slot)`, `RequestPass()`, `CanPass(from,to,out reason)` | Human or future AI/XR intent adapter |
| TacticalSession | `PassStarted`, `BallReceived` | Hook animation, audio or research instrumentation |
| TacticalSession | `actors`, `offenseSpeed`, `defenseSpeed`, `decisionInterval` | Inspector references and adjustable prototype parameters |
| TacticalBall | `Holder`, `Target`, `Flying`, `Position` | Authoritative ownership and in-flight state |
| TacticalBall | `BeginCharge()`, `Release(target)`, `CancelCharge()` | Assisted human pass intent; Q adapter can be replaced |
| TacticalBall | `Launch(target,speed)` | AI pass through the same flight/catch/interception logic |
| TacticalBall | `PlanPass(from,to,speed)`, `CatchPoint`, `ReleaseSpeed` | Shared bounded lead and fixed receiving destination; preview using the intended speed |
| TacticalBall | `Deflected`, `Deflections`, `LastContactActor` | Distinguish body contact from a secured interception |
| TacticalSession | `Velocity(id)`, `RequestSeconds` | Sampled planar motion and remaining active request time |
| TacticalActor | `Id`, `Action`, `Velocity` | Replace primitive procedural pose with an Animator |
| CourtLayout | existing spawns and four anchors | Original team integration API preserved |

Actor IDs are 0=01, 1=04, 2=05, 3=11, 4=12, 5=13. IDs 0–2 are offense. `Holder=-1` means no player owns the ball; `Target>=0` distinguishes a flight from a missed pass. A defender owns intercepted balls, and the rep ends until R. No off-ball request or focus change teleports the ball to the human.

NPC motion uses bounded court targets, local separation and collision-aware human movement. A screen requires reaching the designated point and holding for 0.65 seconds. Roll/cut transitions depend on spatial progress and catches, not a pre-rendered timeline. An AI passer can step sideways if its lane is blocked. The human can diverge; guidance remains available and R provides explicit recovery.

## Deliberate prototype limits

- Three defenders use individual assignments with a short hedge and weak-side help/recovery. No complete five-man zone, trained model or full 2K simulation.
- Assisted passes predict receiver movement, capped at 2.5 m lead, then lock the destination at release. NPC receivers approach that point; the human receives a ground-ring cue and retains movement control. Catch radius is .78 m, followed by a .16 s visual blend into the hand. `CanPass` accepts an optional speed and evaluates the same planned arc and defensive volumes; it is a risk estimate, not a guarantee against a defender changing direction.
- Contact is swept against moving defensive body/hand volumes, and the earliest contact wins. A defender facing the arrival can secure the ball; a blind body hit deflects it without awarding ownership. Deflected/missed balls fall and bounce visually, while the exercise ends for R recovery. This is a bounded analytic animation, not a full rebound simulation.
- No shooting, score, rebounds, steals from a held dribble, possession clock, fouls, or end-to-end match loop. A turnover ends this exercise; it does not start an AI fast break.
- Simple generated character poses, not motion capture. Independent joint animation can replace TacticalActor while retaining root/collider ownership.
- Timing and positions are C# prototype settings; initial layout is in StartDrill / ComputeTargets. The old CourtLayout anchors remain available, but do not yet author all tactical paths. A later ScriptableObject playbook can replace these constants.
- Chinese UI requests an installed Microsoft YaHei font. The distributed target is Windows. No font binary is embedded.

## Reproduce verification

Run Unity with `-batchmode -projectPath <Unity path> -executeMethod TacticalReview.Run -tacticsRound final -logFile <log path>` (do not add `-quit`; the coroutine exits itself). This uses actual scene actions and saves a 30fps camera recording, snapshots and assertion output in `Preview/Playtests/Tactics`. FFmpeg must be available on PATH. Run `CourtControlsReview.Run` separately to exercise the unchanged free-practice scene. `TacticalAuthoring.Build` builds only the tactical scene to `Windows_Tactics` and leaves `Windows` untouched.

Do not run multiple Unity instances on the same project. The review first checks ordinary routes, then uses explicit adversarial fixtures for lane denial, interception, missed catches, reset and pause; those fixtures are not presented as normal player gameplay.

The Nov 8 interaction suite is `TacticalRefinementReview.Run -refineStage 3 -refineLabel <label>`. `Tools/run_unity_review.ps1` wraps editor invocation on this machine. Full routes with `-refinement` save to `Preview/Playtests/Tactics/Refinement-20261108`. Source tests are retained, while recordings/builds remain local. E requests bind to the actual holder and count down only during active simulation; release requires an owned active charge. `Launch` remains the explicit AI/integration entry point.
