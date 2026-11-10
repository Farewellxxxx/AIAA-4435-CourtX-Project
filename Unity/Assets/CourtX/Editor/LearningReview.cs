using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourtX;
using UnityEngine;

public static class LearningReview
{
    static readonly List<string> lines=new List<string>();static int failures;
    static void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);Debug.Log(lines.Last());if(!ok)failures++;}
    static TacticalEpisode Clone(TacticalEpisode e)=>JsonUtility.FromJson<TacticalEpisode>(JsonUtility.ToJson(e));
    public static void RunRules()
    {
        lines.Clear();failures=0;
        foreach(var drill in new[]{TacticalDrill.PickAndRoll,TacticalDrill.HighPostCut})
        foreach(int role in new[]{0,1})
        foreach(var coverage in drill==TacticalDrill.PickAndRoll?new[]{DefenseCoverage.Drop,DefenseCoverage.Switch,DefenseCoverage.Blitz}:new[]{DefenseCoverage.Drop})
        {
            var config=new LearningScenario{drill=drill,controlled=role,coverage=coverage,seed=4435};var e=LearningCatalog.Load(config);
            Check(e!=null&&e.controlled==role&&e.drill==drill.ToString()&&e.decisions.Count>0,"Example matches requested drill / coverage / role: "+drill+" "+coverage+" "+role);
            if(e==null)continue;
            Check(LearningHistory.Valid(e),"Recorded example satisfies playback schema");
            var steps=LearningCatalog.Steps(e);
            Check(steps.Length>=3&&steps[0].time==e.frames[0].time&&steps.Last().time<=e.frames.Last().time,"Lesson nodes follow actual recorded interval");
            Check(steps.All(s=>s.roles!=null&&s.roles.Length==6&&s.roles.All(x=>!string.IsNullOrWhiteSpace(x))),"Every lesson node explains all six roles");
            var reread=LearningCatalog.Load(config);e.frames[0].positions[0]=Vector3.one*99;
            Check(reread.frames[0].positions[0].x!=99,"Example consumers cannot modify another lesson instance");
        }
        var normalize=new LearningScenario{drill=TacticalDrill.HighPostCut,controlled=1,coverage=DefenseCoverage.Blitz,seed=9}.Normalize();
        Check(normalize.coverage==DefenseCoverage.Drop&&normalize.controlled==1&&normalize.seed==9,"High-post normalizes irrelevant coverage without changing role or seed");
        var random=new LearningScenario{coverage=DefenseCoverage.Random,seed=99};var expected=new ReactiveTactics();expected.Reset(DefenseCoverage.Random,99);
        Check(random.ResolveCoverage()==expected.Coverage,"Random example selection resolves through the same seeded defense policy");
        var questions=LearningCatalog.Questions();Check(questions.Length>=6,"At least six role-aware defensive reading questions");
        Check(questions.Select(q=>q.id).Distinct().Count()==questions.Length,"Question identifiers remain unique for saved answers");
        foreach(var q in questions)
        {
            var e=LearningCatalog.Load(q.scenario);Check(e!=null&&q.time>=e.frames[0].time&&q.time<=e.frames.Last().time,"Question anchor exists in its displayed record: "+q.id);
            Check(q.options.Length==3&&q.feedback.Length==3&&q.feedback.All(x=>!string.IsNullOrWhiteSpace(x))&&q.correct>=0&&q.correct<3&&!string.IsNullOrWhiteSpace(q.condition),"Every choice has conditional explanatory feedback: "+q.id);
        }
        var switchQuestion=questions.FirstOrDefault(q=>q.id=="switch");
        if(switchQuestion!=null){var p=new RecapPlayback();p.Reset(LearningCatalog.Load(switchQuestion.scenario),-1);p.Seek(switchQuestion.time);var f=p.Sample();Check(f.marks[0]==1&&f.marks[1]==0,"Switch question actually shows exchanged assignments");}
        var lane=questions.FirstOrDefault(q=>q.id=="lane");
        if(lane!=null){var d=LearningCatalog.Load(lane.scenario).decisions.FirstOrDefault(x=>Mathf.Abs(x.time-lane.time)<.001f);Check(d!=null&&d.from==1&&!d.options.First(o=>o.receiver==0).open&&d.options.First(o=>o.receiver==2).open,"Lane question is grounded in recorded open 05 / blocked 01 options");}
        var a=JsonUtility.FromJson<TacticalEpisode>(Resources.Load<TextAsset>("CourtXLearning/PickAndRoll-Drop-role0").text);
        var b=JsonUtility.FromJson<TacticalEpisode>(Resources.Load<TextAsset>("CourtXLearning/PickAndRoll-Switch-role0").text);
        string reason;Check(LearningHistory.CanCompare(a,b,out reason),"Same start and role can compare different defensive variants");
        var bad=Clone(b);bad.seed++;Check(!LearningHistory.CanCompare(a,bad,out reason)&&!string.IsNullOrEmpty(reason),"Different seed comparison is rejected with explanation");
        bad=Clone(b);bad.controlled=1;Check(!LearningHistory.CanCompare(a,bad,out reason),"Different controlled role is not a same-start comparison");
        bad=Clone(b);bad.frames[0].positions[2]+=Vector3.right;Check(!LearningHistory.CanCompare(a,bad,out reason),"Initial geometry mismatch is rejected even if labels agree");
        bad=Clone(b);bad.truncated=true;Check(!LearningHistory.CanCompare(a,bad,out reason),"Truncated record does not claim a complete comparison");
        var hist=new LearningHistory();Check(hist.Add(a)&&hist.Entries.Count==1,"Actual history stores a completed valid episode");
        Check(!hist.Add(a)&&hist.Entries.Count==1,"Repeated review does not duplicate an episode");
        a.frames[0].positions[0]=Vector3.one*99;Check(hist.Entries.Count==1&&hist.Entries[0].frames[0].positions[0].x!=99,"History snapshots do not retain live mutable arrays");
        var metrics=LearningHistory.Summary(b);Check(metrics.open==1&&metrics.risky==0&&metrics.caught==1,"Summary separates release quality and actual caught count");
        var noPass=Clone(b);noPass.decisions.Clear();metrics=LearningHistory.Summary(noPass);Check(metrics.open==0&&metrics.risky==0&&metrics.missed==0,"No-pass practice is never graded as a failed pass");
        Check(LearningScenario.FromEpisode(b)?.coverage==DefenseCoverage.Switch,"Legacy episode resolves its recorded scenario for retry");
        var unknown=Clone(b);unknown.coverage="Unknown";Check(LearningScenario.FromEpisode(unknown)==null,"Unknown legacy coverage is not silently called Drop");
        var malformed=Clone(b);malformed.frames[0].marks[0]=99;Check(!LearningHistory.Valid(malformed),"Invalid mark arrays rejected before court rendering");
        var huge=Clone(b);huge.decisions[0].destination=Vector3.one*float.MaxValue;Check(!LearningHistory.Valid(huge),"Corrupt giant pass coordinates cannot enter the dashed-line renderer");
        var clock=Clone(b);clock.duration=0;Check(!LearningHistory.Valid(clock),"History duration cannot end before its recorded frames");
        var tail=Clone(b);tail.id+="-missing-tail";tail.frames=tail.frames.Take(1).ToList();tail.decisions.Clear();tail.events.Clear();
        Check(LearningHistory.Valid(tail)&&!LearningHistory.Complete(tail),"Missing-tail record remains viewable but is not complete evidence");
        Check(!LearningHistory.CanCompare(b,tail,out reason),"Missing-tail comparison is rejected even without the truncated flag");
        var middle=Clone(b);middle.frames=new List<TacticalFrame>{middle.frames.First(),middle.frames.Last()};Check(!LearningHistory.Complete(middle),"Missing interior timeline is not complete evidence");
        var unfinished=Clone(b);unfinished.endReason="";Check(!LearningHistory.Complete(unfinished),"Record without completion metadata is not comparable evidence");
        var shortRound=Clone(tail);shortRound.duration=.12f;shortRound.endReason="主动结束练习";Check(LearningHistory.Complete(shortRound),"Genuine short zero-pass rounds retain normal sampling tolerance");
        string folder=Path.Combine(ReactiveReview.Folder,"history-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"valid.json"),JsonUtility.ToJson(b));File.WriteAllText(Path.Combine(folder,"broken.json"),"{broken");File.WriteAllText(Path.Combine(folder,"invalid.json"),JsonUtility.ToJson(malformed));
        var loaded=new LearningHistory();loaded.Load(folder);Check(loaded.Entries.Count==1&&loaded.SkippedFiles==2,"History load keeps valid files and reports bad files without crashing");
        Directory.CreateDirectory(ReactiveReview.Folder);lines.Add("Failures: "+failures);File.WriteAllLines(Path.Combine(ReactiveReview.Folder,ReactiveReview.Label+"-checks.txt"),lines);
        if(failures>0)throw new Exception(failures+" learning checks failed");
    }
}
