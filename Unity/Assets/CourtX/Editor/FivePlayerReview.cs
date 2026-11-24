using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourtX;
using UnityEngine;

public static class FivePlayerReview
{
    public static void RunAll(){FiveBaselineReview.Run();Run();RunCoach();}
    public static void RunView(){lines.Clear();failures=0;FiveViewReview.Run();Finish();}
    public static void RunTiming(){lines.Clear();failures=0;FiveBoardReview.RunLessonTiming();Finish();}
    public static void RunCoach()
    {
        lines.Clear(); failures=0;
        // CourtLab holder changes, including options that finish without another pass.
        string[,] routes={
            {"01>02 02>05","01>02 02>05 05>04","01>02 02>05 05>03"},
            {"01>02","01>05",""},
            {"01>02 02>01","01>02 02>05","01>02 02>01 01>02 02>03"}
        };
        int[,] scorers={{4,3,2},{1,4,0},{0,4,2}};
        foreach(FivePlay play in Enum.GetValues(typeof(FivePlay)))
        for(int branch=0;branch<3;branch++)
        {
            string line=FiveZoneBook.PlayName(play)+" / "+FiveZoneBook.BranchName(play,branch);
            string expected=routes[(int)play,branch];
            int shooter=scorers[(int)play,branch];
            var film=FiveRecording.GuidedLesson(play,FiveCoach.Recommended(play),branch);
            string route=string.Join(" ",film.frames.Where(f=>f.flight!=null&&!f.flight.shot)
                .GroupBy(f=>f.eventSerial).Select(g=>PassText(g.First().flight)));
            Check(FiveRecording.Valid(film)&&film.frames.Last().madeBasket&&route==expected&&
                film.CooperationOutcome==FiveRules.Number(shooter)+" 命中",line+" | demo "+route+" | "+film.End.ToString("F2")+"s | "+film.CooperationOutcome);
            CheckTacticalMovement(film,play,branch,line);
            for(int role=0;role<5;role++)
            {
                var s=FiveCoach.Create(play,role,branch);
                var received=new List<string>();
                float thinkingTime=0;
                bool instructions=true;
                int practicedShooter=-1;
                for(int tick=0;tick<60*90&&!s.turnover&&!s.success;tick++)
                {
                    instructions&=!string.IsNullOrWhiteSpace(s.tasks[role]);
                    if(s.phaseAge>1.4f)s.positions[role]=Vector2.MoveTowards(s.positions[role],s.targets[role],3f/60);
                    if(s.holder==role&&FiveCoach.Ready(s))
                    {
                        thinkingTime+=1f/60;
                        if(thinkingTime>2)
                        {
                            if(FiveCoach.AtFinish(s))FiveRules.Shoot(s);
                            else if(FiveCoach.CanPass(s,FiveCoach.Receiver(s)))FiveRules.Pass(s,FiveCoach.Receiver(s),true);
                        }
                    }
                    else thinkingTime=0;
                    var airborne=s.flight;
                    if(airborne?.shot==true)practicedShooter=airborne.from;
                    FiveRules.Step(s,1f/60,role);
                    if(airborne!=null&&!airborne.shot&&s.flight==null&&s.holder==airborne.to)received.Add(PassText(airborne));
                }
                Check(instructions&&s.madeBasket&&practicedShooter==shooter&&string.Join(" ",received)==expected,
                    line+" | role "+FiveRules.Number(role)+" | "+string.Join(" ",received)+" | "+s.lastEvent+" phase "+s.phase);
            }
        }
        var final=FiveRecording.GuidedLesson(FivePlay.HighPost,4,2).frames.First(s=>s.flight!=null&&s.flight.from==4&&s.flight.to==2).Copy();
        final.positions[5]=Vector2.Lerp(final.flight.origin,final.flight.destination,.5f);
        final.pinned[5]=true;
        for(int i=0;i<120&&final.flight!=null;i++)FiveRules.Step(final,1f/60,4);
        Check(final.turnover&&!final.success&&final.lastEvent.Contains("X1"),"Blocked final pass gives the interceptor, never a successful catch");
        var ready=FiveCoach.Create(FivePlay.HighPost,0);
        for(int i=0;i<5;i++)ready.positions[i]=ready.targets[i];
        for(int i=0;i<600;i++)FiveRules.Step(ready,1f/60,0);
        Check(ready.phase==0&&ready.holder==0&&!FiveRules.Pass(ready,2)&&FiveRules.Pass(ready,1),
            "The guide waits for Q and accepts the demonstrated receiver");
        FiveRules.Call(ready);
        Check(ready.phase==0&&ready.holder==0&&ready.flight==null,"Re-call restores the designated opener");
        var early=FiveCoach.Create(FivePlay.HighPost,0);
        Check(FiveRules.Shoot(early)&&!FiveRules.Shoot(early)&&!FiveRules.Pass(early,1,true),"Early rim shot is a single release");
        var arc=early.flight.Copy();arc.elapsed=arc.duration*.84f;
        Check(arc.Position==FiveFlight.Rim&&Mathf.Abs(arc.Height-FiveFlight.RimHeight)<.001f,"Shot crosses the center of the rim at official height");
        for(int i=0;i<200;i++)FiveRules.Step(early,1f/60,0);
        Check(early.madeBasket&&early.success&&early.flight==null&&early.holder==-1,"Guaranteed basket ends the early attempt");
        var off=FiveCoach.Create(FivePlay.HighPost,0);
        Check(FiveRules.Pass(off,3,true)&&off.flight.to==3,"Human can pass to an unplanned teammate");
        var rushed=FiveCoach.Create(FivePlay.HighPost,0);
        FiveRules.Pass(rushed,1,true);
        Check(rushed.flight!=null&&!rushed.flight.advancesBeat,"Early release is a real pass without advancing the taught sequence");
        for(int i=0;i<150&&rushed.flight!=null;i++)FiveRules.Step(rushed,1f/60,1);
        Check(rushed.pendingGuidedCatch&&rushed.holder==1,"Correct early receiver holds the ball while the team settles");
        FiveRules.Pass(rushed,3,true);
        Check(!rushed.pendingGuidedCatch&&rushed.flight?.to==3,"A second free pass cancels pending progress for the first receiver");
        var camRoot=new GameObject("aim test");var camera=camRoot.AddComponent<Camera>();
        try
        {
            camera.pixelRect=new Rect(32,24,800,600);
            camera.transform.position=new Vector3(0,1.6f,-22);
            camera.transform.LookAt(FiveRules.World(FiveFlight.Rim,3.05f));
            var aim=FiveCoach.Create(FivePlay.HighPost,0);
            Check(FiveActors.Crosshair(camera)==camera.pixelRect.center&&FiveActors.PickTarget(camera,aim)==FiveActors.RimTarget,
                "Centered crosshair targets the hoop in an offset viewport");
            camera.transform.LookAt(FiveRules.World(aim.positions[1],1.15f));
            Check(FiveActors.PickTarget(camera,aim)==1,"Turning toward the wing selects that teammate at the same crosshair");
            camera.transform.rotation=Quaternion.LookRotation(Vector3.up,Vector3.forward);
            Check(FiveActors.PickTarget(camera,aim)==-1,"Looking at the ceiling clears Q targeting");
            camera.nearClipPlane=.1f;
            camera.transform.position=new Vector3(0,1.6f,-5);
            camera.transform.rotation=Quaternion.Euler(-60,0,0);
            for(int i=1;i<5;i++)aim.positions[i]=new Vector2(0,i==1?4:9);
            Check(FiveActors.PickTarget(camera,aim)==-1,"A nearby player's legs behind the eye cannot create a pass target on the ceiling");
            camera.transform.LookAt(FiveRules.World(aim.positions[1],1.15f));
            Check(FiveActors.PickTarget(camera,aim)==1,"Aiming back at the nearby player's chest restores the pass target");
        }
        finally{UnityEngine.Object.DestroyImmediate(camRoot);}
        Finish();
    }
    static string PassText(FiveFlight f)=>FiveRules.Number(f.from)+">"+FiveRules.Number(f.to);
    static void CheckTacticalMovement(FiveRecording film,FivePlay play,int branch,string line)
    {
        for(int phase=0;phase<FiveZoneBook.Count(play,branch);phase++)
        {
            var target=FiveZoneBook.Frame(play,branch,phase);
            var poses=film.frames.Where(f=>f.phase==phase&&f.holder==target.holder);
            bool reached=poses.Any(f=>Enumerable.Range(0,5).All(i=>
                Vector2.Distance(f.positions[i],target.offense[i])<.65f&&
                Vector2.Distance(f.positions[5+i],target.defense[i])<.90f));
            Check(reached,line+" | ten players and holder reach reference frame "+target.id);
            if(target.screener>=0)Check(poses.Any(f=>f.screenActive&&f.screener==target.screener),
                line+" | screen by "+FiveRules.Number(target.screener)+" against X"+(target.screened+1)+" in "+target.id);
        }
    }
    static readonly List<string> lines=new List<string>();
    static int failures;
    public static void Check(bool value,string name)
    {lines.Add((value?"PASS ":"FAIL ")+name);Debug.Log(lines.Last());if(!value)failures++;}
    static void Advance(FiveState s,float seconds,int human=0)
    {for(int i=0;i<Mathf.RoundToInt(seconds*60);i++)FiveRules.Step(s,1f/60,human);}
    public static FiveState OpenPass()
    {
        var s=FiveRules.Create(FivePlay.HighPost,FiveDefense.Balanced,0,0);
        s.positions[0]=new Vector2(-4,10);s.positions[1]=new Vector2(4,10);
        for(int i=5;i<10;i++){s.positions[i]=new Vector2(i-7,2);s.pinned[i]=true;}
        s.pinned[1]=true;FiveRules.Refresh(s);return s;
    }
    public static void Run()
    {
        lines.Clear();failures=0;
        foreach(FivePlay play in Enum.GetValues(typeof(FivePlay)))foreach(FiveDefense defense in Enum.GetValues(typeof(FiveDefense)))
        {
            var s=FiveRules.Create(play,defense,4,0);
            Check(s.positions.Length==10&&s.controlled==4&&s.holder==0,play+" / "+defense+": ten actors and separate role / possession");
            Check(s.positions.Take(10).Distinct().Count()==10&&s.positions[5].y>5&&s.positions[6].y>5&&s.positions.Skip(7).All(p=>p.y<4),"A complete upper-two / lower-three zone is initialized");
            var original=s.Copy();Advance(s,12,4);
            Check(s.time>11.9f&&s.positions.All(p=>!float.IsNaN(p.x)&&Mathf.Abs(p.x)<=7.1f&&p.y>=.4f&&p.y<=13.6f),"Twelve seconds stays finite and on court");
            Check(s.positions.Skip(5).Where((p,i)=>(p-original.positions[i+5]).magnitude>.25f).Count()>=2,"Upper zone defenders react while low defenders can retain valid positions");
            var again=original.Copy();Advance(again,12,4);
            Check(JsonUtility.ToJson(s)==JsonUtility.ToJson(again),"Same full state yields deterministic continuation");
            Check(s.tasks.Length==10&&s.tasks.All(t=>!string.IsNullOrEmpty(t)),"All ten actors have current contextual tasks");
            var low=FiveRules.Create(play,defense,0,0);low.positions[0]=new Vector2(6,2);Advance(low,2,0);
            Check(Vector2.Distance(low.positions[9],low.positions[0])<1.55f,"Ball at the right baseline activates the right low defender");
        }
        var pass=OpenPass();Check(FiveRules.Pass(pass,1)&&pass.flight!=null,"Single pass command immediately releases without a charge phase");
        Check(!FiveRules.Pass(pass,2),"Second command cannot replace an airborne ball");
        Advance(pass,1.4f);
        Check(pass.flight==null&&pass.holder==1&&pass.controlled==0,"Assisted catch transfers possession, never human role");
        Check(!FiveRules.Pass(pass,1)&&!FiveRules.Pass(pass,7),"Cannot pass to self or a defender");
        var request=OpenPass();request.holder=1;request.pinned[0]=true;FiveRules.Request(request,0);Advance(request,1.4f);
        Check(request.holder==0&&request.controlled==0,"A valid request makes AI pass through the same ball lifecycle");
        var recalled=OpenPass();recalled.turnover=true;recalled.success=true;recalled.phase=4;FiveRules.Call(recalled);Advance(recalled,2);
        Check(!recalled.turnover&&!recalled.success&&recalled.phase<4&&recalled.holder>=0,"Calling again rearms a failed/completed sequence");
        var copy=OpenPass();var clone=copy.Copy();clone.positions[1]=Vector2.zero;clone.pinned[3]=true;
        Check(copy.positions[1]==new Vector2(4,10)&&!copy.pinned[3],"State copy owns its player and pin arrays");
        FiveBoardReview.Run();FiveBoardReview.RunLessonTiming();FiveViewReview.Run();FivePracticeReview.Run();FiveFrameReview.Run();Finish();
    }
    public static void Finish()
    {
        lines.Add("Failures: "+failures);Directory.CreateDirectory(ReactiveReview.Folder);
        File.WriteAllLines(Path.Combine(ReactiveReview.Folder,ReactiveReview.Label+"-checks.txt"),lines);
        if(failures>0)throw new Exception(failures+" five-player checks failed");
        Debug.Log("FIVE CHECKS PASSED: "+(lines.Count-1));
    }
}
