using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        Finish();
    }
    static void Finish()
    {
        Directory.CreateDirectory(Folder);lines.Add("Failures: "+failures);File.WriteAllLines(Folder+"/"+Label+"-checks.txt",lines);
        if(failures>0)throw new Exception(failures+" reactive checks failed");
    }
}
