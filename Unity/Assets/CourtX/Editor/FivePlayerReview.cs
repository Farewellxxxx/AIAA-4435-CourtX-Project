using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourtX;
using UnityEngine;

public static class FivePlayerReview
{
    public static void RunAll(){FiveBaselineReview.Run();Run();}
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
        FiveBoardReview.Run();FiveViewReview.Run();FivePracticeReview.Run();FiveFrameReview.Run();Finish();
    }
    public static void Finish()
    {
        lines.Add("Failures: "+failures);Directory.CreateDirectory(ReactiveReview.Folder);
        File.WriteAllLines(Path.Combine(ReactiveReview.Folder,ReactiveReview.Label+"-checks.txt"),lines);
        if(failures>0)throw new Exception(failures+" five-player checks failed");
        Debug.Log("FIVE CHECKS PASSED: "+(lines.Count-1));
    }
}
