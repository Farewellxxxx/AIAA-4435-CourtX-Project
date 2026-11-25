using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourtX;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Exercise the actual random start, player release, physical catch and saved replay paths.
public static class FiveDrillReview
{
    const float Step = 1f/60;
    const int Shot = -1;
    static readonly List<string> report = new List<string>();

    public static void RunCorner() => Run(true);
    public static void RunAll() => Run(false);
    public static void RunAcceptance()
    {
        RunShooterOpening();
        RunEarlyPass();
        RunFeedback();
        RunAll();
        RunScene();
        FiveWaypointReview.Run();
        FivePlayerReview.RunAll();
    }

    public static void RunShooterOpening()
    {
        for(int branch=0;branch<3;branch++)
        for(int variant=0;variant<2;variant++)
        {
            var s=FiveDrill.Create(FivePlay.Horns,branch,variant!=0);
            var wing=s.positions[1];
            // The reported corner-trap round: O2 was 1.28 m off the mark at 1.15 s.
            var path=new[]{wing,new Vector2(4.17f,8.05f),new Vector2(3.95f,8.44f),
                new Vector2(3.79f,8.99f),new Vector2(3.83f,9.13f),new Vector2(3.61f,8.58f)};
            var times=new[]{.6f,1f,1.15f,1.35f,1.45f,1.65f};
            for(int i=0;i<180&&s.holder!=1;i++)
            {
                float t=s.time+Step;
                s.positions[1]=path[path.Length-1];
                for(int j=1;j<times.Length;j++)if(t<=times[j])
                {
                    s.positions[1]=Vector2.Lerp(path[j-1],path[j],Mathf.InverseLerp(times[j-1],times[j],t));
                    break;
                }
                Tick(s);
                Expect(s.phase==0||s.holder==1,"O1 started the cut before O2 caught the opening pass");
            }
            Expect(s.phase==1&&s.holder==1&&s.lastPassFrom==0&&FiveDrill.CanPassDuringCut(s),
                "The moving wing should receive in time for the early pass");
            Debug.Log($"OPENING CATCH branch {branch}/{variant}: O2 received at {s.time:F2}s");
        }
        var denied=FiveDrill.Create(FivePlay.Horns,2,false);
        TickUntilFirstRelease(denied);
        var missed=denied.Copy();
        missed.positions[1]+=Vector2.left*3;
        for(int i=0;i<180&&missed.flight!=null;i++)Tick(missed);
        Expect(missed.drill.recoveringPrefix&&missed.phase==0,"Running away from the flight must leave the opening incomplete");
        missed.positions[1]=new Vector2(4.7f,9.6f);
        for(int i=0;i<1200&&missed.holder!=1;i++)
        {
            Tick(missed);
            Expect(missed.phase==0||missed.holder==1,"Recovered loose ball incorrectly started the cut");
        }
        Expect(missed.holder==1&&missed.phase==1,"O1 should recover the missed pass and feed O2 again");
        denied.positions[5]=Vector2.Lerp(denied.flight.origin,denied.flight.destination,.55f);
        denied.pinned[5]=true;
        for(int i=0;i<120&&!FiveDrill.IsFinished(denied);i++)Tick(denied);
        Expect(denied.turnover&&denied.drill.interceptedBy==5&&denied.phase==0,"The opening feed must remain interceptable");
        Debug.Log("SHOOTER OPENING PASSED: moving wing in all six conditions");
        var args=Environment.GetCommandLineArgs();
        int replay=Array.IndexOf(args,"-openingReplay");
        if(replay>=0)
        {
            var original=FiveRecording.Read(args[replay+1]);
            var first=original.frames[0];
            Expect(first.drill!=null&&first.play==FivePlay.Horns&&first.phase==0,"Expected a recorded shooter drill");
            var live=FiveDrill.Create(first.play,first.branch,first.drill.variant);
            for(int i=0;i<600&&live.holder!=1&&live.time<original.End;i++)
            {
                live.positions[1]=original.Sample(live.time+Step).positions[1];
                Tick(live);
                Expect(live.phase==0||live.holder==1,"Recorded wing motion skipped the first pass");
            }
            Expect(live.lastPassFrom==0&&live.holder==1&&FiveDrill.CanPassDuringCut(live),"Recorded wing motion did not produce the catch");
            Debug.Log($"RECORDED OPENING PASSED: O2 caught O1's opening pass at {live.time:F2}s");
        }
    }

    static void TickUntilFirstRelease(FiveState s)
    {
        // Park O2 on a defender as the first read opens. Re-open the lane later.
        var wing=s.positions[1];
        for(int i=0;i<180;i++)
        {
            s.positions[1]=s.positions[6]+Vector2.left*.35f;
            Tick(s);
            Expect(s.phase==0&&s.holder==0&&s.flight==null,"A denied opener must keep the ball with O1");
        }
        s.positions[1]=wing;
        for(int i=0;i<120&&s.flight==null;i++)Tick(s);
        Expect(s.flight!=null&&s.flight.from==0&&s.flight.to==1,"O1 must retry after O2 gets free");
    }

    public static void RunEarlyPass()
    {
        var root=new GameObject("Early pass input review");
        try
        {
            var game=root.AddComponent<FiveCourtGame>();
            game.SaveFolder=Path.Combine(ReactiveReview.Folder,"early-pass-"+Guid.NewGuid().ToString("N"));
            CheckCutTiming();
            for(int branch=0;branch<3;branch++)
            for(int variant=0;variant<2;variant++)
            {
                var start=FiveDrill.Create(FivePlay.Horns,branch,variant!=0);
                for(int i=0;i<1200&&!(start.phase==2&&start.holder==1);i++)Tick(start);
                Expect(start.phase==2&&start.holder==1,"02 must have the actual first catch during the cut");
                for(int path=0;path<4;path++)
                {
                    var s=start.Copy();
                    int receiver=path==0?3:0;
                    for(int i=0;i<10;i++)s.pinned[i]=true;
                    s.positions[1]=new Vector2(4,7);
                    s.positions[receiver]=new Vector2(0,3);
                    for(int i=5;i<10;i++)s.positions[i]=new Vector2(-5,10);
                    if(path==1||path==3)s.positions[9]=new Vector2(-.55f,3);
                    if(path==2)s.positions[5]=new Vector2(2,5);
                    game.SendMessage("BeginRound",s);
                    Expect(!game.ReleaseAt(FiveActors.RimTarget)&&!game.ReleaseAt(1),"Early Q is pass-only and never self-targets");
                    Expect(game.ReleaseAt(receiver),"Q should release an early pass from the wing");
                    for(int i=0;i<600;i++)
                    {
                        if(path==3&&s.holder==receiver)s.positions[5]=new Vector2(2,5);
                        game.StepPractice(Step);
                        if(FiveDrill.IsFinished(s)||s.holder==1&&s.flight==null)break;
                    }
                    Expect(s.drill.chosenTarget==FiveDrill.Unchosen,"An early release must not count as the corner answer");
                    if(path==0)Expect(s.madeBasket&&s.drill.shooter==receiver,"An open teammate should shoot");
                    else if(path==1)
                    {
                        Expect(s.holder==1&&s.phase==2&&!s.turnover&&!game.ReleaseAt(2),"A covered receiver should return the ball, with no second early Q");
                        for(int i=0;i<10;i++)s.pinned[i]=false;
                        for(int i=0;i<4800&&!FiveDrill.CanChoose(s)&&!FiveDrill.IsFinished(s);i++)
                        {
                            if(game.ShowRunTarget)s.positions[1]=Vector2.MoveTowards(s.positions[1],FiveCoach.RunTarget(s),Step*3.1f);
                            game.StepPractice(Step);
                        }
                        Expect(FiveDrill.CanChoose(s),"After a return the original branch must reach its decision");
                    }
                    else Expect(s.turnover&&s.drill.interceptedBy==5,"A real defender should intercept the "+(path==2?"outbound":"return")+" pass");
                }
            }
            Debug.Log("EARLY PASS PASSED: all six Horns conditions, open catch, covered return, both interceptions and resumed corner decision");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }

    static void CheckCutTiming()
    {
        var running=FiveDrill.Create(FivePlay.Horns,0,false);
        int shots=0,returns=0,steals=0;
        for(int frame=0;frame<2100&&running.phase<4;frame++)
        {
            Tick(running);
            if(frame%10!=0||!FiveDrill.CanPassDuringCut(running))continue;
            var attempt=running.Copy();
            Expect(FiveRules.Pass(attempt,0,true),"The cutter should be selectable at different times");
            for(int t=0;t<360&&!FiveDrill.IsFinished(attempt)&&attempt.drill.earlyPass!=FiveEarlyPass.Returned;t++)Tick(attempt);
            if(attempt.madeBasket)shots++;
            else if(attempt.drill.earlyPass==FiveEarlyPass.Returned)returns++;
            else if(attempt.turnover)steals++;
            else throw new InvalidOperationException("An unedited cut stalled after the pass");
        }
        Debug.Log($"EARLY PASS TIMING: unchanged defense, O2 to cutting O1 at different instants: {shots} baskets, {returns} returns, {steals} turnovers");
        Expect(shots>0,"An unedited shooter route must actually offer at least one early scoring window");
    }

    public static void RunFeedback()
    {
        // Freeze the other players to test an open and a blocked version of the same pass.
        var guided=FiveCoach.Create(FivePlay.HighPost,1);
        guided.phase=1;guided.holder=1;
        Expect(FiveCoach.Receiver(guided)==4,"Teaching fixture must have 02 passing to 05");
        for(int i=0;i<10;i++)guided.pinned[i]=true;
        guided.positions[1]=new Vector2(3,8);guided.positions[2]=new Vector2(-3,8);
        for(int i=5;i<10;i++)guided.positions[i]=new Vector2(5,2);
        var blocked=guided.Copy();
        blocked.positions[5]=new Vector2(0,8);
        Expect(FiveRules.Pass(guided,2,true),"The wrong open receiver must be selectable in teaching");
        for(int i=0;i<120&&guided.flight!=null;i++)FiveRules.Step(guided,Step,1);
        string cue=FiveCoach.Cue(guided,1);
        Expect(!guided.turnover&&guided.holder==2&&cue.Contains("03")&&cue.Contains("05"),
            "After 03 catches safely, tell the player that 05 should receive this beat");
        for(int i=0;i<180&&guided.holder!=1;i++)FiveRules.Step(guided,Step,1);
        Expect(guided.holder==1&&guided.phase==1,"Teaching must give the ball back on the same beat");
        Expect(FiveRules.Pass(blocked,2,true),"Teaching blocked pass did not release");
        for(int i=0;i<120&&blocked.flight!=null;i++)FiveRules.Step(blocked,Step,1);
        string reason=FiveCoach.Cue(blocked,1);
        Expect(blocked.turnover&&reason.Contains("X1")&&reason.Contains("03")&&reason.Contains("C"),
            "Teaching interception should identify the defender, the attempted receiver and how to restart");
        FiveRules.Call(blocked);
        Expect(!blocked.turnover&&!FiveCoach.Cue(blocked,1).Contains("截断"),"Restart must clear the old teaching feedback");
        Debug.Log("TEACHING FEEDBACK PASSED: wrong catch, return pass, actual interception and restart");
    }

    public static void RunScene()
    {
        var scene=EditorSceneManager.OpenScene(FivePlayerAuthoring.Scene,OpenSceneMode.Additive);
        try
        {
            var game=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FiveCourtGame>(true)).Single();
            game.SaveFolder=Path.Combine(ReactiveReview.Folder,"scene-rounds-"+Guid.NewGuid().ToString("N"));
            game.player.SendMessage("Awake");
            game.SendMessage("Start");
            CheckMovingCutter(game);
            for(int task=0;task<9;task++)
            for(int condition=0;condition<2;condition++)
            {
                var s=FiveDrill.Create((FivePlay)(task/3),task%3,condition!=0);
                game.SendMessage("BeginRound",s);
                bool previousPrompt=false;
                for(int i=0;i<60*80&&!game.CanActWithBall&&!FiveDrill.IsFinished(s);i++)
                {
                    bool prompt=game.ShowRunTarget;
                    game.DrivePractice(Vector2.zero,Vector2.zero,Step,false,prompt&&!previousPrompt);
                    previousPrompt=prompt;
                    game.StepPractice(Step);
                    game.Visuals.Apply();
                }
                Expect(game.CanActWithBall,"3D scene approach stalled, task "+task+"/"+condition+" phase "+s.phase+
                    ", controlled position "+s.positions[s.controlled]+", waypoint "+s.targets[s.controlled]);
                int action=s.drill.expectedTarget;
                int target=action==Shot?FiveActors.RimTarget:action;
                var point=action==Shot?FiveRules.World(FiveFlight.Rim,FiveFlight.RimHeight):FiveRules.World(s.positions[action],1.15f);
                var eyes=game.player.eyes.transform;
                var direction=(point-eyes.position).normalized;
                float yaw=Vector3.SignedAngle(game.player.transform.forward,new Vector3(direction.x,0,direction.z),Vector3.up);
                float pitch=(Mathf.Asin(direction.y)-Mathf.Asin(eyes.forward.y))*Mathf.Rad2Deg;
                game.DrivePractice(Vector2.zero,new Vector2(yaw,pitch),Step,false);
                Expect(game.Target==target,"The assessment camera must keep the live reticle usable with the minimap visible");
                game.DrivePractice(Vector2.zero,Vector2.zero,Step,false);
                Expect(game.Target==target&&game.ReleaseAt(game.Target),"3D reticle could not select the visible option, task "+task+"/"+condition+" got "+game.Target);
                for(int i=0;i<60*30&&!FiveDrill.IsFinished(s);i++)game.StepPractice(Step);
                Expect(FiveDrill.IsFinished(s)&&s.drill.chosenTarget==action,"3D scene resolution failed");
                game.FinishPractice();
            }
            Debug.Log("DRILL SCENE PASSED: all 18 rounds, one assist press per approach and first-person camera aim");
        }
        finally { EditorSceneManager.CloseScene(scene,true); }
    }

    static void CheckMovingCutter(FiveCourtGame game)
    {
        var s=FiveDrill.Create(FivePlay.Horns,2,false);
        game.SendMessage("BeginRound",s);
        for(int i=0;i<1600;i++)
        {
            game.DrivePractice(Vector2.zero,Vector2.zero,Step,false);
            game.StepPractice(Step);
            if(s.phase==2&&s.velocities[0].magnitude>1f&&FiveDrill.CanPassDuringCut(s))break;
        }
        Expect(s.phase==2&&s.velocities[0].magnitude>1f,"O1 must be moving through the live scene");
        var eyes=game.player.eyes.transform;
        var direction=(FiveRules.World(s.positions[0],1.15f)-eyes.position).normalized;
        float yaw=Vector3.SignedAngle(game.player.transform.forward,new Vector3(direction.x,0,direction.z),Vector3.up);
        float pitch=(Mathf.Asin(direction.y)-Mathf.Asin(eyes.forward.y))*Mathf.Rad2Deg;
        game.DrivePractice(Vector2.zero,new Vector2(yaw,pitch),Step,false);
        Expect(game.Target==0&&game.ReleaseAt(game.Target),"The first-person reticle must pass to the moving cutter");
        Expect(s.flight.to==0&&s.drill.earlyReceiver==0&&s.drill.earlyReleaseTime>0,
            "Record the early pass separately from the final read");
        game.PausePractice();game.ResumePractice();
        for(int i=0;i<600&&s.drill.earlyPass!=FiveEarlyPass.Returned&&!FiveDrill.IsFinished(s);i++)game.StepPractice(Step);
        Expect(s.drill.earlyPass==FiveEarlyPass.Returned||FiveDrill.IsFinished(s),"The live cutter did not resolve the pass");
        Debug.Log("EARLY PASS SCENE PASSED: live 02 aimed at cutting 01, resolution "+s.drill.earlyPass+" / "+s.drill.outcome);
    }

    static void Expect(bool ok, string detail)
    {
        if (!ok) throw new InvalidOperationException(detail);
    }

    static void Run(bool cornerOnly)
    {
        report.Clear();
        var root = new GameObject("CourtX assessment acceptance");
        var game = root.AddComponent<FiveCourtGame>();
        var previousRandom = UnityEngine.Random.state;
        var errors = new List<string>();
        UnityEngine.Random.InitState(320519);
        string output = ReactiveReview.Folder;
        Directory.CreateDirectory(output);
        game.SaveFolder = Path.Combine(output,"review-rounds-" + Guid.NewGuid().ToString("N"));
        try
        {
            if (!cornerOnly)
            {
                var samples = new HashSet<int>();
                for (int i = 0; i < 360; ++i)
                {
                    game.StartDrill();
                    samples.Add(((int)game.Live.play * 3 + game.Live.branch) * 2 + (game.Live.drill.variant ? 1 : 0));
                }
                Expect(samples.Count==18,"Random entry must reach all nine tasks in both conditions");
            }
            foreach (FivePlay play in Enum.GetValues(typeof(FivePlay)))
            for (int branch = 0; branch < 3; branch++)
            {
                if (cornerOnly && (play!=FivePlay.Horns || branch!=2)) continue;
                try
                {
                    CheckLateWindow(play,branch);
                    if(play==FivePlay.Horns&&branch<2)CheckHornsHandler(branch);
                }
                catch(Exception error) { Failure(errors,play+"-"+branch+" handoff",error); }
                try
                {
                    var original = CheckRound(game,play,branch,false);
                    var variant = CheckRound(game,play,branch,true);
                    Expect(original.phase==variant.phase && original.controlled==variant.controlled,
                        "The paired rounds must ask the same player at the same beat");
                    Expect(original.drill.expectedTarget!=variant.drill.expectedTarget,
                        "Both conditions expect the same action");
                    Expect(Enumerable.Range(5,5).Max(i=>Vector2.Distance(original.positions[i],variant.positions[i]))>.7f,
                        "The two coverages are visually indistinguishable");
                    CheckOtherChoice(original,variant.drill.expectedTarget);
                    CheckOtherChoice(variant,original.drill.expectedTarget);
                    if(play==FivePlay.Horns&&branch==2)
                    {
                        CheckLateInterception(game,original);
                        CheckOpenAlternative(original);
                    }
                }
                catch (Exception error)
                {
                    Failure(errors,play+"-"+branch,error);
                }
            }
            if(errors.Count>0)throw new InvalidOperationException(string.Join(" | ",errors));
            Debug.Log("DRILL CHECKS PASSED: " + report.Count + " complete rounds");
        }
        finally
        {
            File.WriteAllLines(Path.Combine(output,cornerOnly?"corner-cases.txt":"drill-cases.txt"),report);
            UnityEngine.Random.state = previousRandom;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void Failure(List<string> errors,string label,Exception error)
    {
        string problem=label+": "+error.Message;
        errors.Add(problem);
        report.Add("FAIL "+problem);
        Debug.LogError("DRILL FAILURE "+problem);
    }

    static void Tick(FiveState s) => FiveRules.Step(s,Step,s.controlled);

    static void CheckLateWindow(FivePlay play,int branch)
    {
        var s=FiveDrill.Create(play,branch,false);
        int actor=s.controlled;
        string label=play+"-"+branch;
        bool incoming=play==FivePlay.HighPost||play==FivePlay.Horns&&branch==2;
        var away=play==FivePlay.HighPost?Vector2.left:Vector2.right;
        var offSpot=FiveRules.Clamp(s.positions[actor]+away*1.65f);
        var prefix=new List<int>();
        for(int i=0;i<60*40&&!FiveCoach.NeedsRunTarget(s)&&!FiveDrill.IsFinished(s)&&!FiveDrill.CanChoose(s);i++)
        {
            s.positions[actor]=Vector2.MoveTowards(s.positions[actor],offSpot,3.1f*Step);
            var flight=s.flight;
            Tick(s);
            if(flight!=null&&s.flight==null&&s.holder==flight.to)prefix.Add(flight.to);
        }
        Expect(FiveCoach.NeedsRunTarget(s)&&s.positions[actor]==offSpot,
            label+" did not reach a late run target with the human parked off spot, phase "+s.phase+" "+s.lastEvent);
        var expectedPasses=play==FivePlay.Screen?Array.Empty<int>():
            play==FivePlay.Horns&&branch==2?new[]{1,0}:new[]{1};
        Expect(prefix.SequenceEqual(expectedPasses),label+" prefix before the missing position: "+string.Join(",",prefix));
        Expect(s.phase==(play==FivePlay.HighPost?1:incoming?s.drill.decisionPhase-1:s.drill.decisionPhase),
            label+" asked for the human at the wrong beat: "+s.phase);

        int phase=s.phase;
        var spot=FiveCoach.RunTarget(s);
        var early=FiveDrill.Create(play,branch,false);
        bool sawEarlyPrompt=false;
        for(int i=0;i<60*45&&!FiveDrill.CanChoose(early)&&!FiveDrill.IsFinished(early);i++)
        {
            early.positions[actor]=Vector2.MoveTowards(early.positions[actor],spot,4.5f*Step);
            sawEarlyPrompt|=FiveCoach.NeedsRunTarget(early);
            Tick(early);
        }
        Expect(FiveDrill.CanChoose(early)&&!sawEarlyPrompt,
            label+" a player already at the key spot should receive control without a redundant prompt");

        // Let the actors reach their waiting formation before observing movement.
        for(int i=0;i<240;i++)Tick(s);
        int holder=s.holder;
        Expect(holder==(incoming?(play==FivePlay.Horns?0:1):actor)&&s.flight==null&&s.phase==phase&&!FiveDrill.CanChoose(s),
            label+" the ball did not wait with the correct passer/handler");
        var standing=(Vector2[])s.positions.Clone();
        float attackMotion=0,defenseMotion=0;
        for(int i=0;i<180;i++)
        {
            Tick(s);
            for(int id=0;id<10;id++)
            {
                float travel=Vector2.Distance(s.positions[id],standing[id]);
                if(id>=5)defenseMotion=Mathf.Max(defenseMotion,travel);
                else if(id!=actor)attackMotion=Mathf.Max(attackMotion,travel);
            }
        }
        Expect(s.holder==holder&&s.flight==null&&s.phase==phase&&FiveCoach.NeedsRunTarget(s)&&
            !FiveDrill.CanChoose(s),label+" the unattended gate advanced");
        Expect(attackMotion>.04f&&defenseMotion>.04f,label+" attack/defense stopped moving while waiting ("+
            attackMotion.ToString("F3")+" / "+defenseMotion.ToString("F3")+" m)");
        if(play==FivePlay.Horns)
            Expect(s.positions[0].x>5f&&s.positions[0].y<4f,label+" O1 did not reach the corner after feeding O2");
        Expect(!FiveRules.Shoot(s)&&!FiveRules.Pass(s,(holder+1)%5,true),
            label+" accepted a human decision before handoff");
        if(incoming)
        {
            Expect(!FiveRules.Pass(s,actor),
                label+" allowed an AI pass to an absent receiver");
            var blocked=s.Copy();
            blocked.positions[actor]=spot;
            blocked.positions[5]=Vector2.Lerp(blocked.positions[holder],spot,.5f);
            Expect(!FiveRules.Pass(blocked,actor),label+" threw the key feed through a closed lane");
            // The cooldown gives this real passer time to observe the receiver enter
            // from one side of the boundary and retreat before the release.
            var left=s.Copy();
            left.positions[actor]=spot+away*(FiveCoach.PlayerArrival-.03f);
            left.passCooldown=.5f;
            Tick(left);
            Expect(!FiveCoach.NeedsRunTarget(left)&&left.flight==null,label+" did not recognize the brief arrival");
            left.positions[actor]+=away*(3.1f*Step);
            for(int i=0;i<100;i++)Tick(left);
            Expect(left.holder==holder&&left.phase==phase&&left.flight==null&&FiveCoach.NeedsRunTarget(left)&&
                !FiveRules.Pass(left,actor),label+" remembered a receiver who had already left before release");
        }

        var nearEdge=spot+away*(FiveCoach.PlayerArrival-.18f);
        FiveFlight feed=null;
        bool caught=false;
        for(int i=0;i<60*35&&!FiveDrill.CanChoose(s)&&!FiveDrill.IsFinished(s);i++)
        {
            if(feed==null)s.positions[actor]=Vector2.MoveTowards(s.positions[actor],nearEdge,3.1f*Step);
            if(caught)s.positions[actor]=Vector2.MoveTowards(s.positions[actor],spot+away*1.6f,3.1f*Step);
            var previous=s.flight;
            Tick(s);
            if(incoming&&feed==null&&s.flight!=null&&s.flight.to==actor)
            {
                feed=s.flight;
                float gap=Vector2.Distance(feed.destination,spot);
                Expect(feed.from==holder&&Vector2.Distance(feed.destination,s.positions[actor])<.05f&&
                    gap<=FiveCoach.PlayerArrival+.01f,label+" did not feed the real receiver inside the zone");
            }
            if(feed!=null&&previous==feed&&s.flight==null&&s.holder==actor)caught=true;
            if(caught)Expect(!FiveCoach.NeedsRunTarget(s),label+" repeated the receive prompt after a physical catch");
        }
        Expect((!incoming||caught)&&FiveDrill.CanChoose(s)&&!FiveCoach.NeedsRunTarget(s),
            label+" could not play the late arrival through to the read, phase "+s.phase+" "+s.lastEvent);
        var held=s.Copy();
        var start=s.positions[actor];
        var after=FiveRules.Clamp(start+Vector2.up*1.35f);
        for(int i=0;i<60;i++)
        {
            s.positions[actor]=Vector2.MoveTowards(s.positions[actor],after,3.1f*Step);
            Tick(s);
            Tick(held);
        }
        Expect(Vector2.Distance(s.positions[actor],start)>1f&&FiveDrill.CanChoose(s)&&!FiveCoach.NeedsRunTarget(s),
            label+" lost player control after dribbling away");
        Expect(Enumerable.Range(5,5).Any(id=>Vector2.Distance(held.positions[id],s.positions[id])>.05f),
            label+" defenders did not respond to the live ballhandler's change of position");
        int choice=s.drill.expectedTarget;
        bool released=choice==Shot?FiveRules.Shoot(s):FiveRules.Pass(s,choice,true);
        Expect(released&&Vector2.Distance(s.drill.ballAtRelease,s.positions[actor])<.001f,
            label+" read/release did not use the live dribble position");
        Debug.Log("HANDOFF PASS "+label+" | phase "+phase+" | "+
            (feed!=null?FiveRules.Number(feed.from)+" -> "+FiveRules.Number(feed.to):"holding position")+
            " | waiting attack/defense travel "+attackMotion.ToString("F2")+" / "+defenseMotion.ToString("F2")+" m");
    }

    static void CheckHornsHandler(int branch)
    {
        var s=FiveDrill.Create(FivePlay.Horns,branch,false);
        int actor=s.controlled;
        for(int i=0;i<60*10&&s.holder!=actor&&!s.turnover;i++)Tick(s);
        Expect(s.holder==actor&&s.lastPassFrom==0&&s.lastPassTo==actor,"Horns "+branch+" missed the opening catch");
        var spot=FiveZoneBook.Frame(s.play,s.branch,s.drill.decisionPhase).offense[actor];
        var displaced=spot+Vector2.right*1.6f;
        for(int i=0;i<60*40&&!FiveCoach.NeedsRunTarget(s)&&!FiveDrill.CanChoose(s)&&!s.turnover;i++)
        {
            s.positions[actor]=Vector2.MoveTowards(s.positions[actor],displaced,3.1f*Step);
            Tick(s);
        }
        Expect(s.phase==s.drill.decisionPhase&&s.holder==actor&&FiveCoach.NeedsRunTarget(s),
            "Horns "+branch+" should ask the ballhandler for just the final holding position");
        for(int i=0;i<60*10&&!FiveDrill.CanChoose(s);i++)
        {
            if(FiveCoach.NeedsRunTarget(s))s.positions[actor]=Vector2.MoveTowards(s.positions[actor],spot,3.1f*Step);
            Tick(s);
        }
        Expect(FiveDrill.CanChoose(s)&&s.lastPassFrom==0&&s.lastPassTo==actor&&
            Vector2.Distance(s.positions[actor],spot)>.8f,"Horns "+branch+" failed to unlock at the edge of the holding region");
    }

    static FiveState CheckRound(FiveCourtGame game, FivePlay play, int branch, bool variant)
    {
        game.OpenDrill();
        game.SendMessage("BeginRound",FiveDrill.Create(play,branch,variant));
        var s=game.Live;
        string label=FiveZoneBook.PlayName(play)+" / "+FiveZoneBook.BranchName(play,branch)+(variant?" / 变体":" / 原防守");
        Expect(s.play==play&&s.branch==branch&&s.drill.variant==variant,label+" was never drawn");
        Expect(game.Page==2 && game.IsDrill && s.holder==0 && !game.CanActWithBall,label+" blind entry");
        Expect(s.positions.SequenceEqual(FiveCoach.Create(play,s.controlled,branch).positions),label+" starting positions changed");
        var passes=new List<int>();
        for(int i=0;i<60*100&&!FiveDrill.CanChoose(s)&&!s.turnover&&!s.madeBasket;i++)
        {
            var flight=s.flight;
            if(game.ShowRunTarget)
                s.positions[s.controlled]=Vector2.MoveTowards(s.positions[s.controlled],FiveCoach.RunTarget(s),3.1f*Step);
            game.StepPractice(Step);
            if(flight!=null&&!flight.shot&&s.flight==null&&s.holder==flight.to)passes.Add(flight.to);
        }
        Expect(game.CanActWithBall&&!game.ShowRunTarget,label+" never reached a playable decision; phase "+s.phase+" "+s.lastEvent);
        int last=0;var sourcePasses=new List<int>();
        for(int phase=1;phase<=s.phase;phase++)
        {
            int next=FiveZoneBook.Frame(play,branch,phase).holder;
            if(next!=last)sourcePasses.Add(next);
            last=next;
        }
        Expect(passes.SequenceEqual(sourcePasses),label+" taught prefix changed: "+string.Join(",",passes));
        int decisionPhase=s.phase;
        game.PausePractice();
        Expect(game.HasPausedPractice&&game.Page==2&&!game.Practicing,label+" pause did not preserve assessment");
        game.ResumePractice();
        for(int i=0;i<90;i++)game.StepPractice(Step);
        game.CallPlay();
        Expect(s.phase==decisionPhase&&game.CanActWithBall&&s.flight==null,label+" answered or restarted without the player");
        var pose=s.Copy();
        int expected=s.drill.expectedTarget;
        int[,] normal={{Shot,3,2},{1,4,Shot},{0,4,2}};
        int[,] changed={{3,1,3},{4,3,4},{3,3,3}};
        int actor=play==FivePlay.HighPost?4:play==FivePlay.Screen?0:1;
        Expect(s.controlled==actor&&expected==(variant?changed:normal)[(int)play,branch],
            label+" role or available option does not match the intended read");
        Expect(game.ReleaseAt(expected==Shot?FiveActors.RimTarget:expected),label+" input was rejected");
        Expect(!game.ReleaseAt(expected==Shot?FiveActors.RimTarget:expected),label+" double release");
        Expect(s.drill.chosenTarget==expected&&s.drill.decisionCorrect,label+" choice metadata");
        Expect(pose.drill.chosenTarget==-2,label+" recording snapshots share mutable decision data");
        for(int i=0;i<60*30&&!s.madeBasket&&!s.turnover;i++)game.StepPractice(Step);
        Expect(s.madeBasket&&!s.turnover,label+" reasonable option did not finish: "+s.lastEvent);
        game.FinishPractice();
        Expect(game.Page==2&&!game.Practicing&&!game.HasPausedPractice,label+" completed drill should stay on its result");
        game.ShowPage(1);
        Expect(game.Board.Record.IsDrill&&game.Board.Record.id==game.LiveRecord.id,label+" saved result could not be opened manually");
        var stored=FiveRecording.Read(Path.Combine(game.SaveFolder,game.LiveRecord.id+".json"));
        Expect(stored.IsDrill&&stored.frames.Last().madeBasket&&stored.frames.Last().drill.chosenTarget==expected,
            label+" JSON round trip lost decision or finish");
        Expect(stored.frames[0].drill.chosenTarget==-2,label+" future choice leaked into opening frame");
        Expect(stored.DrillSummary=="投篮命中，本轮完成。",label+" result should only report what happened");
        var beforeEdit=JsonUtility.ToJson(game.Board.TimelineRecord);
        game.Board.Seek(pose.time);
        game.Board.PlaceFrame(5,FiveRules.Clamp(pose.positions[5]+Vector2.left));
        game.Board.Restore();
        Expect(JsonUtility.ToJson(game.Board.TimelineRecord)==beforeEdit,label+" pause or What-if mutated the test recording");
        if(variant&&play==FivePlay.Horns&&branch==2)
            Expect(s.drill.received&&s.drill.nextPassFrom==3&&s.drill.nextPassTo==2&&s.drill.shooter==2,
                "Corner trap: the free high post must continue through a live 04 to 03 pass");
        string line=label+" | "+FiveRules.Number(s.controlled)+" | "+(expected==Shot?"投篮":"传 "+FiveRules.Number(expected))
            +" | "+stored.DrillSummary;
        report.Add(line);
        Debug.Log("DRILL PASS "+line);
        return pose;
    }

    static void CheckOtherChoice(FiveState pose,int other)
    {
        var s=pose.Copy();
        bool released=other==Shot?FiveRules.Shoot(s):FiveRules.Pass(s,other,true);
        Expect(released&&s.drill.chosenTarget==other,"An alternative live choice was refused or overwritten");
        for(int i=0;i<60*30&&!FiveDrill.IsFinished(s);i++)FiveRules.Step(s,Step,s.controlled);
        Expect(FiveDrill.IsFinished(s),"The alternative choice silently stalled");
        if(s.play==FivePlay.Horns&&s.branch==2)
            Expect(!s.drill.decisionCorrect&&s.turnover,"Corner trap: the closed passing lane must allow an interception");
    }

    static void CheckOpenAlternative(FiveState pose)
    {
        var s=pose.Copy();
        const int outlet=0;
        s.positions[outlet]=new Vector2(-2,4);
        for(int i=0;i<10;i++)s.pinned[i]=true;
        for(int i=5;i<10;i++)s.positions[i]=new Vector2(6,1);
        Expect(FiveRules.Pass(s,outlet,true)&&!s.drill.decisionCorrect,"A third open receiver must be allowed");
        for(int i=0;i<160&&!s.drill.received;i++)FiveRules.Step(s,Step,s.controlled);
        Expect(s.drill.received&&!FiveDrill.IsFinished(s),"The other receiver must play on after a clean catch");
        for(int i=0;i<360&&!FiveDrill.IsFinished(s);i++)FiveRules.Step(s,Step,s.controlled);
        Expect(s.madeBasket&&s.drill.shooter==outlet&&FiveDrill.Summary(s)=="投篮命中，本轮完成。",
            "A basket through another open receiver should have the same result");
    }

    static void CheckLateInterception(FiveCourtGame game,FiveState pose)
    {
        game.SendMessage("BeginRound",pose.Copy());
        var s=game.Live;
        Expect(FiveRules.Pass(s,s.drill.expectedTarget,true)&&s.drill.decisionCorrect,"A free lane should be recognised at release");
        var record=new FiveRecording{name="QA",kind="actual"};
        record.Add(s,true);
        s.positions[5]=Vector2.Lerp(s.flight.origin,s.flight.destination,.55f);
        s.pinned[5]=true;
        for (int i=0;i<120&&!FiveDrill.IsFinished(s);i++){FiveRules.Step(s,Step,s.controlled);record.Add(s);}
        record.Add(s,true);
        Expect(s.turnover&&s.drill.interceptedBy==5&&s.drill.decisionCorrect&&!s.drill.received,
            "A defender entering after release must not overwrite the original read");
        var reread=JsonUtility.FromJson<FiveRecording>(JsonUtility.ToJson(record));
        Expect(FiveRecording.Valid(reread)&&reread.frames.Last().drill.interceptedBy==5,"Failed play lost in JSON");
        Expect(reread.DrillSummary=="传球被 X1 截断，本轮失败。","Late interception must show just the actual defender and failure");
        game.FinishPractice();
        Expect(game.Page==2&&!game.Practicing&&game.LiveRecord.id==game.SelectedReplayId,
            "Failed drill must stay on the result screen and save its recording");
        game.StartDrill();
        Expect(game.Practicing&&!FiveDrill.IsFinished(game.Live)&&game.Live.drill.chosenTarget==FiveDrill.Unchosen,
            "The next random round must clear the previous result");
        var alreadyClosed=pose.Copy();
        int target=alreadyClosed.drill.expectedTarget;
        alreadyClosed.positions[7]=Vector2.Lerp(alreadyClosed.positions[alreadyClosed.controlled],alreadyClosed.positions[target],.5f);
        Expect(FiveRules.Pass(alreadyClosed,target,true)&&!alreadyClosed.drill.decisionCorrect,
            "The assigned target should not earn credit when its actual passing lane is already blocked");
        Debug.Log("DRILL PASS: open at release, late X1 interception, both facts preserved");
    }
}
