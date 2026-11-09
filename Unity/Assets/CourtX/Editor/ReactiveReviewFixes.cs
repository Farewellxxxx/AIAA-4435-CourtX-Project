using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourtX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ReactiveReviewFixes
{
    static ReactiveReviewFixes()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("CourtX.ReviewFixes",false))return;
            SessionState.SetBool("CourtX.ReviewFixes",false);new GameObject("Fresh review regressions").AddComponent<ReactiveReviewFixChecks>();
        };
    }
    public static void Run(){EditorSceneManager.OpenScene(TacticalAuthoring.Scene);SessionState.SetBool("CourtX.ReviewFixes",true);EditorApplication.isPlaying=true;}
}
public sealed class ReactiveReviewFixChecks:MonoBehaviour
{
    TacticalSession s;int failures;readonly List<string> lines=new List<string>();
    void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);Debug.Log(lines.Last());if(!ok)failures++;}
    IEnumerator Wait(float seconds){for(int i=0;i<seconds*30;i++)yield return null;}
    void Place(int id,Vector3 point){if(id==s.Controlled)s.TeleportPlayer(point,0);else s.actors[id].SetPosition(point);}
    void Fixture(bool offBall,bool live=false)
    {
        s.enabled=true;s.StartScenario(TacticalDrill.HighPostCut,offBall,DefenseCoverage.Drop,4435);s.enabled=live;
        s.offenseSpeed=s.defenseSpeed=0;
        Place(0,new Vector3(-4,0,-9));Place(1,new Vector3(0,0,-5.8f));Place(2,new Vector3(3,0,-8));
        for(int i=3;i<6;i++)Place(i,new Vector3(-6+(i-3)*6,0,-1));s.ball.ResetBall(0);Physics.SyncTransforms();
    }
    IEnumerator Start()
    {
        Time.captureFramerate=30;s=FindObjectOfType<TacticalSession>();s.acceptsInput=false;
        Application.logMessageReceived+=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception){failures++;lines.Add("ERROR "+message);}};
        yield return null;
        foreach(bool offBall in new[]{false,true})
        {
            Fixture(offBall);s.ball.Launch(2);yield return Wait(1.4f);
            Check(s.ball.Holder==2,"First-leg relay "+offBall+": 05 actually catches from 01");
            s.ball.Launch(1);yield return Wait(1);
            Check(s.ball.Holder==1&&s.Phase==TacticalPhase.Cut,"First-leg relay "+offBall+": 04 reception from 05 at high post starts cut");
        }
        Fixture(false,true);s.ball.ResetBall(2);yield return Wait(1.3f);
        Check(s.ball.Holder==1&&s.Phase==TacticalPhase.Cut,"AI 05 continues initial high-post relay without requiring a human request");

        Fixture(false,true);
        var speedJournal=new TacticalJournal(new TacticalEpisode());speedJournal.Sample(s);yield return Wait(.35f);
        speedJournal.RecordPass(s,0,1,10.5f);
        Check(speedJournal.Episode.decisions[0].options.First(o=>o.receiver==1).open&&speedJournal.Episode.decisions[0].windowAge<0,"Changed release speed does not inherit a differently sampled window age");
        Fixture(true,true); // AI holder uses the same 8 m/s preview as its actual launch.
        Place(1,new Vector3(0,0,-7)); // Stay outside the high-post trigger while sampling this fixture.
        var sameSpeed=new TacticalJournal(new TacticalEpisode());sameSpeed.Sample(s);yield return Wait(.35f);
        Check(s.ball.Holder==0,"Window timing fixture retains the sampled passer");
        sameSpeed.RecordPass(s,0,2,8);
        Check(sameSpeed.Episode.decisions[0].windowAge>=.3f,"An unchanged open AI preview retains its sampled duration");
        var closed=new TacticalJournal(new TacticalEpisode());closed.Sample(s);
        Place(3,Vector3.Lerp(s.Position(0),s.Position(2),.5f));s.actors[3].transform.rotation=Quaternion.LookRotation(Vector3.left);
        closed.RecordPass(s,0,2,8);var decision=closed.Episode.decisions[0];
        Check(!decision.options.First(o=>o.receiver==2).open&&decision.windowAge<0,"A defender closing the lane between samples invalidates the open-window claim");
        lines.Add("Failures: "+failures);Directory.CreateDirectory(ReactiveReview.Folder);File.WriteAllLines(ReactiveReview.Folder+"/"+ReactiveReview.Label+"-checks.txt",lines);
        EditorApplication.Exit(failures==0?0:1);
    }
}
