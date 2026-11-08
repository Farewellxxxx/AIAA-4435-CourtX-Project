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
public static class TacticalRefinementReview
{
    static TacticalRefinementReview()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("CourtX.Refinement",false))return;
            SessionState.SetBool("CourtX.Refinement",false);
            new GameObject("Interaction regression").AddComponent<TacticalInteractionChecks>();
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(TacticalAuthoring.Scene);
        SessionState.SetBool("CourtX.Refinement",true);EditorApplication.isPlaying=true;
    }
}

public sealed class TacticalInteractionChecks:MonoBehaviour
{
    TacticalSession s;
    readonly List<string> lines=new List<string>();
    int failures,fps=60;
    string path,label;
    static string Arg(string name,string fallback)=>Environment.GetCommandLineArgs().SkipWhile(a=>a!=name).Skip(1).FirstOrDefault()??fallback;
    void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);Debug.Log(lines.Last());if(!ok)failures++;}
    void Error(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error){lines.Add("ERROR "+message);failures++;}}
    IEnumerator Wait(float seconds){for(int i=0;i<Mathf.CeilToInt(seconds*fps);i++)yield return null;}
    IEnumerator Walk(float seconds,bool reverse=false)
    {
        for(int i=0;i<Mathf.CeilToInt(seconds*fps);i++)
        {s.player.Drive(reverse?Vector2.down:Vector2.up,Vector2.zero,1f/fps,true,false);yield return null;}
    }
    void Fixture(bool live=false)
    {
        s.enabled=true;s.StartDrill(TacticalDrill.PickAndRoll,true);s.enabled=live;
        s.actors[0].SetPosition(new Vector3(-3,0,-10));s.TeleportPlayer(new Vector3(3,0,-10),270);
        for(int i=2;i<6;i++){s.actors[i].SetPosition(new Vector3(i-4,0,-2));s.actors[i].transform.rotation=Quaternion.LookRotation(Vector3.left);}
        s.ball.ResetBall(0);Physics.SyncTransforms();
    }
    void Snapshot(string name)
    {
        s.player.SetOverview(true);s.ball.Pose();s.player.planSymbols.Refresh();
        var cam=s.player.overview;var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
        image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path+"/"+label+"-"+name+".png",image.EncodeToPNG());
        cam.targetTexture=null;RenderTexture.active=old;Destroy(rt);Destroy(image);
    }
    IEnumerator Start()
    {
        Application.logMessageReceived+=Error;s=FindObjectOfType<TacticalSession>();s.acceptsInput=false;
        label=Arg("-refineLabel","interaction");int stage=int.Parse(Arg("-refineStage","1"));
        path=Path.GetFullPath("../Preview/Playtests/Tactics/Refinement-20261108");Directory.CreateDirectory(path);
        yield return null;
        foreach(int rate in new[]{15,30,60})
        {
            fps=rate;Time.captureFramerate=fps;
            Fixture();s.actors[3].SetPosition(new Vector3(0,0,-10));s.actors[3].transform.rotation=Quaternion.LookRotation(Vector3.right);
            s.ball.Launch(1,8);yield return Wait(.42f);
            Check(s.ball.Holder==-1&&s.Phase==TacticalPhase.Turnover,rate+" fps: blind rear contact deflects without gifting defender possession");
            var hit=s.ball.Position;Snapshot(rate+"-deflection");yield return Wait(1.5f);
            Check(s.ball.Position.y<.25f&&TacticalRules.Distance(hit,s.ball.Position)>.1f,rate+" fps: blocked ball visibly falls instead of sticking to a hand");

            Fixture();s.actors[3].SetPosition(new Vector3(0,0,-10));
            s.ball.Launch(1,8);yield return Wait(.5f);
            Check(s.ball.Holder==3&&s.ball.Interceptions==1,rate+" fps: a prepared defender facing the pass can secure it");

            Fixture();s.actors[3].SetPosition(new Vector3(-1.5f,0,-10));s.actors[4].SetPosition(new Vector3(-1.85f,0,-10));
            s.ball.Launch(1,10.5f);yield return Wait(.3f);
            Check(s.ball.Holder==4,rate+" fps: nearer defender wins contact independent of actor-array order");
        }
        fps=60;Time.captureFramerate=fps;
        Fixture();s.actors[3].SetPosition(new Vector3(0,0,-10));s.actors[3].transform.rotation=Quaternion.LookRotation(Vector3.right);
        s.ball.Launch(1);yield return Wait(.4f);s.OpenMenu();var paused=s.ball.Position;yield return Wait(.4f);
        Check(Vector3.Distance(paused,s.ball.Position)<.001f,"pause freezes a deflected ball");
        s.enabled=true;s.StartDrill(TacticalDrill.PickAndRoll,false);yield return Wait(.15f);
        Check(s.ball.Holder==0&&!s.ball.Flying&&s.ball.Position.y>=.12f,"reset clears failed-flight state and restores one owner");
        if(stage>=2)
        {
            Fixture(true);s.offenseSpeed=s.defenseSpeed=0;s.TeleportPlayer(new Vector3(3,0,-10),0);
            yield return Walk(.25f);s.ball.Launch(1);yield return Walk(1);
            Check(s.ball.Holder==1&&s.ball.CompletedPasses==1,"steady moving human catches a leading pass");
            Fixture(true);s.TeleportPlayer(new Vector3(3,0,-10),0);
            yield return Walk(.25f);s.ball.Launch(1);yield return Walk(1,true);
            Check(s.ball.Holder==-1&&s.Phase==TacticalPhase.Turnover,"reversing after release can miss the fixed receiving point");
            s.StartDrill(TacticalDrill.PickAndRoll,false);s.offenseSpeed=2.65f;s.defenseSpeed=0;
            s.TeleportPlayer(new Vector3(-3,0,-12),0);s.actors[1].SetPosition(new Vector3(2,0,-10));
            for(int i=3;i<6;i++)s.actors[i].SetPosition(new Vector3(i-4,0,-2));
            yield return Wait(.2f);s.ball.Launch(1);var receiving=s.Position(1);yield return Wait(.25f);
            Check(TacticalRules.Distance(receiving,s.Position(1))>.3f,"AI receiver continues toward the catch point during flight");
            yield return Wait(1);Check(s.ball.Holder==1,"moving AI receiver completes the catch");
            Fixture();s.ball.Launch(1);float jump=0;
            for(int i=0;i<90&&s.ball.Flying;i++){var previous=s.ball.Position;yield return null;if(!s.ball.Flying)jump=Vector3.Distance(previous,s.ball.Position);}
            Check(s.ball.Holder==1&&jump<.3f,"catch starts at contact without a visual teleport into the hands");
            s.OpenMenu();var catchPaused=s.ball.Position;yield return Wait(.3f);
            Check(Vector3.Distance(catchPaused,s.ball.Position)<.001f,"menu freezes the catch blend");
            s.Resume();yield return Wait(.25f);Check(s.ball.Holder==1,"resuming catch blend preserves singular possession");
        }
        lines.Add("Stage "+stage+": "+failures+" failures");
        File.WriteAllLines(path+"/"+label+"-checks.txt",lines);EditorApplication.Exit(failures==0?0:1);
    }
}
