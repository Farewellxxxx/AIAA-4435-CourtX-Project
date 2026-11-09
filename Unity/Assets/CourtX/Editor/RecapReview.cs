using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourtX;
using UnityEngine;

public static class RecapReview
{
    static readonly List<string> lines = new List<string>();
    static int failures;
    static void Check(bool ok, string name)
    { lines.Add((ok ? "PASS " : "FAIL ") + name); Debug.Log(lines.Last()); if (!ok) failures++; }
    public static TacticalEpisode Fixture()
    {
        var e = new TacticalEpisode { controlled = 0, coverage = "Drop", duration = 4 };
        for (int t = 0; t <= 4; t++)
            e.frames.Add(new TacticalFrame { time = t, positions = Enumerable.Range(0,6).Select(i => new Vector3(i+t,0,-t)).ToArray(),
                ball = new Vector3(t*2,1,-t), holder = t < 2 ? 0 : t < 3 ? -1 : 1,
                marks = t < 3 ? new[]{0,1,2} : new[]{1,0,2}, phase = "Roll", defense = "Contain" });
        e.decisions.Add(new PassDecision { time=1, frame=1, from=0, to=1, outcome=PassOutcome.Missed,
            options=new[]{new PassOption { receiver=1, open=true, space=2 },new PassOption { receiver=2, open=false }} });
        e.decisions.Add(new PassDecision { time=3, frame=3, from=0, to=2, outcome=PassOutcome.Caught,
            options=new[]{new PassOption { receiver=1, open=true, space=2 },new PassOption { receiver=2, open=false, reason="blocked" }} });
        e.events.Add(new TacticalEvent { time=.5f, kind="defense", text="EARLY" });
        e.events.Add(new TacticalEvent { time=2.5f, kind="defense", text="LATER" });
        return e;
    }
    public static void RunRules()
    {
        lines.Clear(); failures=0; var e=Fixture(); string original=JsonUtility.ToJson(e);
        var first=TacticalJournal.Build(e,0);
        Check(first.decisionIndex==0 && first.assessment==DecisionAssessment.OpenLane && first.outcome==PassOutcome.Missed,"Selected first pass retains its own risk and outcome");
        Check(first.text.Contains("EARLY")&&!first.text.Contains("LATER"),"Selected pass never cites later defense as release context");
        Check(TacticalJournal.Build(e,1).decisionIndex==1 && TacticalJournal.Build(e).decisionIndex==1,"Explicit last and default human pass selection agree");
        Check(TacticalJournal.Build(e,-1).assessment==DecisionAssessment.NoDecision,"Before first release has no invented pass assessment");
        var p=new RecapPlayback();p.Reset(e,0);
        Check(p.Time==1 && p.DecisionIndex==0 && !p.Playing,"Open paused on selected release");
        p.Seek(.5f);Check(p.DecisionIndex==-1,"Scrub before first pass clears selected decision");
        p.Seek(2.5f); var f=p.Sample();
        Check(f!=null && Enumerable.Range(0,6).All(i=>Mathf.Abs(f.positions[i].x-(i+2.5f))<.001f),"All six actors interpolate by timestamp");
        Check(f!=null && f.ball==new Vector3(5,1,-2.5f),"Ball follows its own recorded positions");
        Check(f!=null && f.holder==-1 && f.marks[0]==0,"No future possession or mark transfer during interpolation");
        Check(p.DecisionIndex==0,"Between releases retains the preceding decision");
        p.Jump(1);Check(p.Time==3 && p.DecisionIndex==1 && !p.Playing,"Next pass selects exact release and pauses");
        p.Jump(-1);Check(p.Time==1 && p.DecisionIndex==0,"Previous pass selects earlier release");
        p.Seek(-10);Check(p.Time==0,"Scrubbing clamps before recording start");
        p.Toggle();p.Speed=.5f;p.Tick(1);Check(p.Playing && p.Time==.5f,"Half speed advances half a recorded second");
        p.Toggle();p.Tick(1);Check(p.Time==.5f,"Paused playback does not advance");
        p.Seek(3.5f);p.Speed=1;p.Toggle();p.Tick(2);Check(p.Time==4 && !p.Playing,"Playback stops exactly at last recorded frame");
        p.Toggle();Check(p.Playing && p.Time==0,"Play at end restarts the episode");
        Check(JsonUtility.ToJson(e)==original,"Replay and report building do not mutate archived facts");
        p.Seek(4);f=p.Sample();if(f!=null){f.positions[0]=Vector3.one*99;f.marks[0]=99;}
        Check(e.frames[4].positions[0].x==4 && e.frames[4].marks[0]==1,"Sample arrays cannot mutate recording even at endpoint");
        e.frames.Insert(2,new TacticalFrame { time=1,positions=e.frames[1].positions,ball=new Vector3(9,1,-1),holder=-1,marks=new[]{0,1,2} });
        p.Reset(e,-1);p.Seek(1);f=p.Sample();
        Check(f!=null && f.ball.x==9 && f.holder==-1,"Duplicate timestamps choose the latest exact discrete state");
        var empty=new TacticalEpisode();p.Reset(empty,-1);p.Toggle();p.Tick(3);
        Check(p.Sample()==null && !p.Playing && p.DecisionIndex==-1,"Empty episode stays usable and cannot play");
        empty.frames.Add(new TacticalFrame { time=7,positions=new Vector3[6],marks=new[]{0,1,2},holder=-1 });
        p.Reset(empty,-1);p.Toggle();Check(p.Time==7 && !p.Playing && p.Sample()!=null,"Single-frame and nonzero-start recording remains stationary");
        Directory.CreateDirectory(ReactiveReview.Folder);File.WriteAllLines(Path.Combine(ReactiveReview.Folder,ReactiveReview.Label+"-checks.txt"),lines.Concat(new[]{"Failures: "+failures}));
        if(failures>0)throw new Exception(failures+" recap checks failed");
    }
}
