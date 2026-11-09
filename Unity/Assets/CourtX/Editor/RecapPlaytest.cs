using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CourtX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RecapPlaytest
{
    static RecapPlaytest()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("CourtX.RecapPlay",false))return;
            SessionState.SetBool("CourtX.RecapPlay",false);new GameObject("Recap integration checks").AddComponent<RecapPlayChecks>();
        };
    }
    public static void Run(){EditorSceneManager.OpenScene(TacticalAuthoring.Scene);SessionState.SetBool("CourtX.RecapPlay",true);EditorApplication.isPlaying=true;}
}
public sealed class RecapPlayChecks:MonoBehaviour
{
    TacticalSession s;int failures;readonly List<string> lines=new List<string>();
    void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);Debug.Log(lines.Last());if(!ok)failures++;}
    IEnumerator Wait(int frames){for(int i=0;i<frames;i++)yield return null;}
    void LoadRecordedExample()
    {
        var files=Directory.GetFiles(Path.GetFullPath("../docs/verification/reactive-20261109/examples"),"*.json",SearchOption.AllDirectories);
        var e=files.Select(path=>JsonUtility.FromJson<TacticalEpisode>(File.ReadAllText(path))).OrderByDescending(x=>x.decisions.Count).First();
        var live=s.Journal.Episode;live.frames=e.frames;live.decisions=e.decisions;live.events=e.events;
        live.controlled=e.controlled;live.coverage=e.coverage;live.drill=e.drill;
        s.Journal.Finish(e.duration,e.endReason); // Recorded real play, no synthetic replacement trajectories.
    }
    sealed class LongProvider:ITacticalRecapProvider
    {
        public Task<string> GenerateAsync(string json,string local,CancellationToken cancellation)
            =>Task.FromResult(string.Concat(Enumerable.Repeat("长文本显示测试：观察队友与篮球的相对位置，再判断传球窗口。\n",50))+"长文本末尾检查标记");
    }
    IEnumerator Start()
    {
        Directory.CreateDirectory(ReactiveReview.Folder);s=FindObjectOfType<TacticalSession>();s.acceptsInput=false;
        Time.captureFramerate=30;
        Application.logMessageReceived+=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception){failures++;lines.Add("ERROR "+message);}};
        yield return null;s.StartScenario(TacticalDrill.HighPostCut,false,DefenseCoverage.Drop,4435);
        yield return Wait(10);LoadRecordedExample();s.OpenReview();yield return Wait(2);
        int expected=s.Journal.Episode.decisions.FindLastIndex(d=>d.from==s.Controlled);
        if(expected<0)expected=s.Journal.Episode.decisions.Count-1;
        Check(s.Replay.DecisionIndex==expected && s.Replay.Sample()!=null,"OpenReview binds replay to recorded episode and default pass");
        float active=s.ActiveTime;var positions=Enumerable.Range(0,6).Select(s.Position).ToArray();string facts=JsonUtility.ToJson(s.Journal.Episode);
        s.Replay.Seek(s.Replay.StartTime);s.Replay.Toggle();yield return new WaitForSecondsRealtime(.2f);
        Check(s.Replay.Time>s.Replay.StartTime+.1f,"Session advances replay while live simulation is paused");
        Check(s.ActiveTime==active && Enumerable.Range(0,6).All(i=>s.Position(i)==positions[i]),"Replay never moves live actors or active-time clock");
        Check(JsonUtility.ToJson(s.Journal.Episode)==facts,"Playing recap never mutates journal");
        s.Replay.Seek(s.Journal.Episode.decisions[0].time);yield return Wait(2);
        Check(s.Review.decisionIndex==0,"Scrubbing first release updates session report");
        s.Replay.Jump(1);yield return Wait(2);
        Check(s.Review.decisionIndex==s.Replay.DecisionIndex && s.Review.decisionIndex>0,"Next pass updates report and diagram together");
        s.Replay.Seek(s.Replay.StartTime);yield return Wait(2);
        Check(s.Review.decisionIndex==-1 && s.Review.assessment==DecisionAssessment.NoDecision,"Before first pass clears old assessment");
        s.RecapProvider=new LongProvider();s.OpenReview();yield return Wait(4);
        Check(s.OptionalNarration.EndsWith("长文本末尾检查标记"),"Long provider fixture reaches UI intact");
        // Actual rendered layout is inspected in the separate RecapPlayerReview player.
        s.RetryScenario();yield return Wait(3);
        Check(!s.ReviewOpen && !s.Journal.Closed && s.Review==null,"Retry leaves recap and starts a fresh journal");
        s.RecapProvider=null;s.OpenReview();yield return Wait(2);
        Check(s.Review.assessment==DecisionAssessment.NoDecision && s.Replay.Sample()!=null,"No-pass round opens without invented pass");
        lines.Add("Failures: "+failures);File.WriteAllLines(Path.Combine(ReactiveReview.Folder,ReactiveReview.Label+"-checks.txt"),lines);
        EditorApplication.Exit(failures==0?0:1);
    }
}
