using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CourtX;
using UnityEditor;
using UnityEngine;

public static class ReactiveReview
{
    public static string Label=>Environment.GetCommandLineArgs().SkipWhile(a=>a!="-reactiveLabel").Skip(1).FirstOrDefault()??"rules";
    public static string Folder=>Path.GetFullPath("../Preview/Playtests/Reactive-20261109");
    static readonly List<string> lines=new List<string>();
    static int failures;
    public static void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);Debug.Log(lines.Last());if(!ok)failures++;}
    static Vector3[] Positions()=>new[]{new Vector3(1.5f,0,-8.1f),new Vector3(.35f,0,-8.1f),new Vector3(6,0,-2.3f),new Vector3(-.4f,0,-8),new Vector3(1,0,-6),new Vector3(4.6f,0,-3.5f)};
    static void Step(ReactiveTactics d,Vector3[] p,TacticalPhase phase,int owner=0,int target=-1,int frames=30)
    {for(int i=0;i<frames;i++)d.Step(TacticalDrill.PickAndRoll,phase,owner,target,p,1f/60);}
    public static void RunRules()
    {
        lines.Clear();failures=0;
        foreach(var c in new[]{DefenseCoverage.Drop,DefenseCoverage.Switch,DefenseCoverage.Blitz})
        {
            var d=new ReactiveTactics();var p=Positions();d.Reset(c,4435);
            Step(d,p,TacticalPhase.Assemble,frames:300);
            Check(d.Stage==DefenseStage.Contain&&d.Marks.SequenceEqual(new[]{0,1,2}),c+": waiting never triggers screen coverage");
            Step(d,p,TacticalPhase.ScreenReady);Step(d,p,TacticalPhase.Roll,frames:1);
            Check(d.Stage==DefenseStage.Contain,c+": defender reaction is delayed");
            Step(d,p,TacticalPhase.Roll);
            Check(d.Stage==DefenseStage.Engaged,c+": actual nearby screen use activates coverage");
            if(c==DefenseCoverage.Drop)Check(d.DefenseTargets[1].z>p[0].z+1.5f&&d.DefenseTargets[1].z< -3,"Drop: big protects paint rather than trapping at screen");
            if(c==DefenseCoverage.Switch)
            {
                Check(d.Marks.SequenceEqual(new[]{1,0,2}),"Switch: exchange assignments");
                Step(d,p,TacticalPhase.Roll,frames:300);
                Check(d.Marks.SequenceEqual(new[]{1,0,2}),"Switch: no repeated assignment flipping");
            }
            if(c==DefenseCoverage.Blitz)
            {
                Check(TacticalRules.Distance(d.DefenseTargets[0],p[0])<1.6f&&TacticalRules.Distance(d.DefenseTargets[1],p[0])<1.6f,"Blitz: two defenders close handler");
                Step(d,p,TacticalPhase.Roll,-1,2,80);
                Check(d.Stage==DefenseStage.Recover&&!d.HelpActive,"Blitz: pass out ends trap and weak-side help recovers");
            }
            d.Reset(c,4435);Step(d,p,TacticalPhase.Roll,1,frames:120);
            Check(d.Stage==DefenseStage.Contain,"Early pass bypasses "+c+" without scripted trap");
        }
        var helper=new ReactiveTactics();var hp=Positions();hp[0]=new Vector3(1,0,-4.5f);helper.Reset(DefenseCoverage.Drop,7);
        Step(helper,hp,TacticalPhase.Assemble);
        Check(helper.HelpActive,"Paint penetration draws delayed weak-side help");
        hp[0]=new Vector3(4,0,-8);Step(helper,hp,TacticalPhase.Assemble,frames:90);
        Check(!helper.HelpActive,"Retreat releases help with hysteresis");
        var random=new ReactiveTactics();random.Reset(DefenseCoverage.Random,88);var first=random.Coverage;random.Reset(DefenseCoverage.Random,88);
        Check(first==random.Coverage&&first!=DefenseCoverage.Random,"Seed resolves a concrete repeatable coverage");
        Teammates();
        Recaps();
        Finish();
    }
    sealed class DelayedProvider:ITacticalRecapProvider
    {
        public readonly TaskCompletionSource<string> completion=new TaskCompletionSource<string>();
        public Task<string> GenerateAsync(string json,string local,CancellationToken token)=>completion.Task;
    }
    static void Recaps()
    {
        var episode=new TacticalEpisode{controlled=0,drill="高位挡拆",coverage="Drop",seed=8};
        var journal=new TacticalJournal(episode);
        var decision=new PassDecision{from=0,to=1,time=2,options=new[]{new PassOption{receiver=1,open=true,space=2},new PassOption{receiver=2,open=true,space=3}},outcome=PassOutcome.Missed};
        journal.AddDecision(decision);decision.options[0].open=false;
        Check(episode.decisions[0].options[0].open,"Journal freezes decision-time lane risk instead of retaining mutable input");
        var report=TacticalJournal.Build(episode);
        Check(report.assessment==DecisionAssessment.OpenLane&&report.outcome==PassOutcome.Missed,"Recap separates open-lane choice from missed-catch execution");
        episode.decisions[0].options[0].open=false;episode.decisions[0].outcome=PassOutcome.Caught;
        report=TacticalJournal.Build(episode);
        Check(report.assessment==DecisionAssessment.RiskyLane&&report.outcome==PassOutcome.Caught,"Caught risky pass is not retroactively judged safe");
        Check(report.alternative==2,"Alternative is grounded in another open option recorded at release");
        episode.decisions[0].options[1].open=false;
        Check(TacticalJournal.Build(episode).alternative==-1,"Recap does not invent an unrecorded open alternative");
        var empty=TacticalJournal.Build(new TacticalEpisode());
        Check(empty.assessment==DecisionAssessment.NoDecision&&empty.decisionIndex==-1&&!string.IsNullOrEmpty(empty.text),"No-pass round produces useful feedback without inventing a pass");
        var clone=JsonUtility.FromJson<TacticalEpisode>(JsonUtility.ToJson(episode));
        Check(clone.decisions.Count==1&&clone.decisions[0].outcome==PassOutcome.Caught&&clone.seed==8,"Export roundtrip preserves recorded facts");
        var late=Task.Run(async()=>
        {
            var provider=new DelayedProvider();var gate=new RecapRequestGate();
            var pending=gate.GenerateAsync(provider,"{}","本地");gate.Reset();provider.completion.SetResult("stale");
            return await pending;
        }).GetAwaiter().GetResult();
        Check(late==null,"Reset invalidates a late optional-provider response");
    }
    static void Teammates()
    {
        var p=Positions();var trap=new ReactiveTactics();trap.Reset(DefenseCoverage.Blitz,5);
        Step(trap,p,TacticalPhase.ScreenReady);Step(trap,p,TacticalPhase.Roll);
        var shortRoll=trap.ReceiverTarget(1,TacticalPhase.Roll,0,p);
        Check(shortRoll.z> -6.5f&&shortRoll.z< -4.8f,"Trap response: screener offers a short-roll outlet instead of running to rim");
        Step(trap,p,TacticalPhase.Roll); // Allow the helper's separate reaction delay after trap engagement.
        Check(trap.ReceiverTarget(2,TacticalPhase.Roll,0,p).z< -4,"Weak-side receiver lifts when helper tags the short roll");
        var normal=new ReactiveTactics();normal.Reset(DefenseCoverage.Drop,5);Step(normal,p,TacticalPhase.Assemble);
        Check(normal.ReceiverTarget(2,TacticalPhase.Assemble,0,p).z> -3,"Weak-side holds corner when no help is drawn");
        p[0]=new Vector3(1.5f,0,-4.3f);p[5]=new Vector3(4.5f,0,-2.8f);Step(normal,p,TacticalPhase.Roll);
        Check(normal.ReceiverTarget(2,TacticalPhase.Roll,0,p).x<3,"Overplayed weak side can cut behind a committed helper");
        var origin=new Vector3(0,0,-8);
        Check(PressureModel.Evaluate(origin,new[]{origin+Vector3.forward})>.6f,"Pressure port: nearby front defender creates pressure");
        Check(PressureModel.Evaluate(origin,new[]{origin+Vector3.back})==0,"Pressure port: defender behind basket-relative drive direction is excluded");
        Check(PressureModel.Evaluate(origin,new[]{origin+Vector3.forward*5})==0,"Pressure port: distant defender is excluded");
        float one=PressureModel.Evaluate(origin,new[]{origin+Vector3.forward});
        Check(PressureModel.Evaluate(origin,new[]{origin+Vector3.forward,origin+new Vector3(.4f,0,1)})>one,"Pressure port: two close front defenders increase combined pressure");
        var choices=new[]{new ReceiverOption{Id=1,Open=false,Space=3},new ReceiverOption{Id=2,Open=true,Space=2}};
        Check(ReactiveTactics.ChooseReceiver(1,choices,.8f)==2,"Under pressure AI can use a safe alternate instead of forcing blocked request");
        Check(ReactiveTactics.ChooseReceiver(1,choices,0)==-1,"Without urgency AI waits and changes angle for requested target");
        choices[0].Open=true;choices[1].Open=false;
        Check(ReactiveTactics.ChooseReceiver(2,choices,.8f)==1,"Alternate choice follows geometry, not a fixed jersey");
    }
    static void Finish()
    {
        Directory.CreateDirectory(Folder);lines.Add("Failures: "+failures);File.WriteAllLines(Folder+"/"+Label+"-checks.txt",lines);
        if(failures>0)throw new Exception(failures+" reactive checks failed");
    }
}
