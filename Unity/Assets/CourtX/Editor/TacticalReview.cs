using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourtX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Process=System.Diagnostics.Process;

[InitializeOnLoad]
public static class TacticalReview
{
    static TacticalReview()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("CourtX.TacticalReview",false))return;
            SessionState.SetBool("CourtX.TacticalReview",false);
            new GameObject("Tactical playtest and recorder").AddComponent<TacticalChecks>();
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(TacticalAuthoring.Scene);
        SessionState.SetBool("CourtX.TacticalReview",true);EditorApplication.isPlaying=true;
    }
}

[DefaultExecutionOrder(9500)]
public sealed class TacticalChecks:MonoBehaviour
{
    TacticalSession s;
    readonly List<string> results=new List<string>();
    int failures,frame;
    string round,path;
    bool record;
    Process encoder;
    RenderTexture rt;
    Texture2D readback;
    void Check(bool ok,string message)
    {results.Add((ok?"PASS ":"FAIL ")+message);Debug.Log(results[results.Count-1]);if(!ok)failures++;}
    void Error(string message,string trace,LogType type)
    {if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert){results.Add("ERROR "+message);failures++;}}
    IEnumerator Wait(float seconds)
    {for(int i=0;i<Mathf.CeilToInt(seconds*30);i++){s.player.Drive(Vector2.zero,Vector2.zero,1f/30,s.player.overview.enabled,false);yield return null;}}
    IEnumerator Until(Func<bool> predicate,float max)
    {for(int i=0;i<max*30&&!predicate();i++)yield return null;}
    IEnumerator WalkTo(Vector3 point,float max=5,bool sprint=false,bool map=true,bool stopForBall=false)
    {
        for(int i=0;i<max*30;i++)
        {
            if(stopForBall&&s.ball.Flying)break;
            Vector3 delta=TacticalRules.Flat(point-s.player.transform.position);
            if(delta.magnitude<.18f)break;
            var local=s.player.transform.InverseTransformDirection(delta.normalized);
            s.player.Drive(new Vector2(local.x,local.z),Vector2.zero,1f/30,map,sprint);
            yield return null;
        }
        s.player.Drive(Vector2.zero,Vector2.zero,1f/30,map,false);
    }
    void Snapshot(string name)
    {
        var cam=s.player.overview.enabled?s.player.overview:s.player.eyes;
        var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
        readback.ReadPixels(new Rect(0,0,1280,720),0,0);readback.Apply();
        File.WriteAllBytes(path+"/"+round+"-"+name+".png",readback.EncodeToPNG());
        cam.targetTexture=null;RenderTexture.active=old;
    }
    IEnumerator PassWhenOpen(int receiver,float limit=5)
    {
        string reason;float until=Time.time+limit;
        s.ball.BeginCharge();yield return Wait(.1f);
        while(Time.time<until&&s.ball.Holder==s.Controlled&&!s.CanPass(s.Controlled,receiver,out reason))
        {
            // A player-like lateral adjustment, through the same CharacterController as WASD.
            var across=Vector3.Cross(Vector3.up,(s.Position(receiver)-s.Position(s.Controlled)).normalized);
            var move=s.player.transform.InverseTransformDirection(across);
            s.player.Drive(new Vector2(move.x,move.z),Vector2.zero,1f/30,true,false);yield return null;
        }
        if(s.ball.Holder!=s.Controlled)yield break;
        s.ball.Release(receiver);
    }
    IEnumerator Start()
    {
        Application.logMessageReceived+=Error;
        round=Environment.GetCommandLineArgs().SkipWhile(a=>a!="-tacticsRound").Skip(1).FirstOrDefault()??"round1";
        if(!new[]{"round1","round2","final"}.Contains(round))throw new ArgumentException("Unknown review round");
        path=Path.GetFullPath("../Preview/Playtests/Tactics"+(Environment.GetCommandLineArgs().Contains("-refinement")?"/Refinement-20261108":""));Directory.CreateDirectory(path);
        Time.captureFramerate=30;QualitySettings.vSyncCount=0;
        s=FindObjectOfType<TacticalSession>();s.acceptsInput=false;
        s.PassStarted+=(from,to)=>
        {
            string reason;bool open=s.CanPass(from,to,out reason);
            results.Add("TRACE frame "+frame+" pass "+from+" > "+to+" open="+open+" "+reason+" positions "+string.Join(";",Enumerable.Range(0,6).Select(i=>s.Position(i).ToString("F2"))));
        };
        rt=new RenderTexture(1280,720,24){antiAliasing=4};readback=new Texture2D(1280,720,TextureFormat.RGB24,false);
        encoder=new Process{StartInfo=new System.Diagnostics.ProcessStartInfo("ffmpeg.exe",
            "-hide_banner -loglevel error -y -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 19 -pix_fmt yuv420p -movflags +faststart \""+path+"/"+round+".mp4\"")
            {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true}};
        encoder.Start();yield return null;record=true;
        Check(s.MenuOpen&&!s.Running,"boot opens mode menu");
        Check(TacticalRules.LaneBlocked(new Vector3(0,0,0),new Vector3(0,0,6),new Vector3(.3f,0,3)),"defender in corridor denies pass");
        Check(!TacticalRules.LaneBlocked(Vector3.zero,new Vector3(0,0,6),new Vector3(2,0,3)),"lateral separation opens corridor");
        Check(!TacticalRules.LaneBlocked(Vector3.zero,new Vector3(0,0,6),new Vector3(0,0,-2)),"defender behind passer does not block");

        for(int drill=0;drill<2;drill++)for(int mode=0;mode<2;mode++)
        {
            s.StartDrill((TacticalDrill)drill,mode==1);yield return null;
            string label=((TacticalDrill)drill)+" / "+(mode==0?"handler":"off-ball");
            results.Add("ROUTE "+label+" starts at frame "+frame);
            Check(s.Controlled==mode&&s.ball.Holder==0,label+" identity and possession");
            Check(s.actors.Count(a=>a.gameObject.activeSelf)==5,label+" exactly five visible NPCs");
            Snapshot(drill+"-"+mode+"-start");
            yield return Wait(.6f);
            if(drill==0)
            {
                if(mode==1)yield return WalkTo(new Vector3(.35f,0,-8.1f),4,false,true);
                yield return Until(()=>s.Phase==TacticalPhase.ScreenReady,5);
                Check(s.Phase==TacticalPhase.ScreenReady,label+" screener plants before action");
                if(mode==0)
                {
                    yield return WalkTo(new Vector3(1.8f,0,-8.5f),3,false,true);
                    yield return WalkTo(new Vector3(3.65f,0,-6.5f),3,false,true);
                }
                yield return Until(()=>s.Phase==TacticalPhase.Roll,4);
                Check(s.Phase==TacticalPhase.Roll,label+" handler uses screen, triggers roll");
                if(mode==1)
                {
                    yield return WalkTo(new Vector3(1.65f,0,-5.6f),4,false,true,true);
                    s.RequestPass();
                    yield return Until(()=>s.ball.Flying||s.ball.Holder==1,1);
                    if(!s.ball.Flying&&s.ball.Holder!=1)
                    {
                        yield return WalkTo(new Vector3(3.8f,0,-4.6f),3,false,true,true);
                        s.RequestPass();
                    }
                }
                else
                {
                    yield return Until(()=>s.Position(1).z>-6f,4);
                    s.SelectReceiver(0);yield return PassWhenOpen(s.SelectedReceiver);
                }
                yield return Until(()=>s.ball.Holder==1||s.Phase==TacticalPhase.Turnover,4);
                Check(s.ball.Holder==1,label+" roll receiver catches real pass; "+s.Message);
                if(mode==1&&s.ball.Holder==1)yield return WalkTo(new Vector3(1.65f,0,-4.2f),2,false,false);
                yield return Wait(1);
            }
            else
            {
                if(mode==1)yield return WalkTo(new Vector3(.1f,0,-5.8f),3,false,true,true);
                else
                {
                    yield return Wait(1.3f);s.SelectReceiver(0);yield return PassWhenOpen(s.SelectedReceiver);
                }
                yield return Until(()=>s.Phase==TacticalPhase.Cut||s.Phase==TacticalPhase.Turnover,5);
                Check(s.ball.Holder==1&&s.Phase==TacticalPhase.Cut,label+" high post receives and triggers cut; "+s.Message);
                if(mode==0)
                {
                    yield return WalkTo(new Vector3(-4.8f,0,-5.7f),3,false,true,true);
                    yield return WalkTo(new Vector3(-1.6f,0,-3.8f),3,false,true,true);
                    s.RequestPass();
                }
                else
                {
                    yield return Until(()=>s.Position(0).z>-5.2f,5);
                    s.SelectReceiver(0);yield return PassWhenOpen(s.SelectedReceiver);
                }
                yield return Until(()=>s.ball.Holder==0||s.Phase==TacticalPhase.Turnover,4);
                yield return Wait(.5f);
                Check(s.ball.Holder==0&&s.ball.CompletedPasses>=2,label+" cutter catches return pass; "+s.Message);
            }
            Check(s.Controlled==mode,label+" possession never switches player identity");
            Check(s.Phase==TacticalPhase.Complete,label+" drill objective completes (actual "+s.Phase+")");
            s.player.SetOverview(true);Snapshot(drill+"-"+mode+"-result");yield return Wait(1.2f);
        }

        results.Add("ADVERSARIAL CHECKS start at frame "+frame);
        yield return Wait(19);
        Check(s.Phase==TacticalPhase.Complete&&s.Message.Contains("配合完成"),"completed drill keeps its success feedback while the player takes a break");
        s.StartDrill(TacticalDrill.PickAndRoll,true);
        s.TeleportPlayer(new Vector3(.35f,0,-8.1f),0);s.ball.ResetBall(1);yield return Wait(2);
        Check(s.Phase==TacticalPhase.Assemble,"a ball-holding screener cannot unlock the pick-and-roll");
        s.StartDrill(TacticalDrill.PickAndRoll,true);
        s.TeleportPlayer(new Vector3(.35f,0,-8.1f),0);s.offenseSpeed=0;yield return Wait(.8f);
        s.TeleportPlayer(new Vector3(-4,0,-10),0);yield return Wait(.2f);
        Check(s.Phase==TacticalPhase.Assemble,"leaving an unused screen returns to assembly");
        s.StartDrill(TacticalDrill.HighPostCut,true);s.offenseSpeed=s.defenseSpeed=0;
        s.TeleportPlayer(new Vector3(4,0,-10),0);s.actors[0].SetPosition(new Vector3(-3,0,-10));
        for(int i=3;i<6;i++)s.actors[i].SetPosition(new Vector3(i-4,0,-2));
        s.ball.Launch(1);yield return Wait(1.4f);
        Check(s.ball.Holder==1&&s.Phase==TacticalPhase.HighPost,"catching on the perimeter does not bypass high-post arrival");
        s.TeleportPlayer(new Vector3(.1f,0,-5.8f),0);yield return Wait(.2f);
        Check(s.Phase==TacticalPhase.Cut,"an early receiver can recover by carrying the ball to the high post");
        s.StartDrill(TacticalDrill.PickAndRoll,true);s.offenseSpeed=s.defenseSpeed=0;
        s.actors[0].SetPosition(new Vector3(-3,0,-10));s.TeleportPlayer(new Vector3(3,0,-10),0);
        s.actors[3].SetPosition(new Vector3(-2.1f,0,-10));
        s.actors[3].transform.rotation=Quaternion.LookRotation(Vector3.left);
        for(int i=4;i<6;i++)s.actors[i].SetPosition(new Vector3(i-4,0,-2));
        s.ball.Launch(1,10.5f);yield return Wait(.8f);
        Check(s.ball.Holder==3&&s.ball.Interceptions==1,"a fast pass cannot tunnel through a close defender during initial flight");
        // Adversarial fixture: no NPC movement, exact defensive corridor; force pass through it.
        s.StartDrill(TacticalDrill.PickAndRoll,true);s.offenseSpeed=s.defenseSpeed=0;
        s.actors[0].SetPosition(new Vector3(-3,0,-10));s.TeleportPlayer(new Vector3(3,0,-10),0);
        s.actors[3].SetPosition(new Vector3(0,0,-10));s.actors[4].SetPosition(new Vector3(-4,0,-2));s.actors[5].SetPosition(new Vector3(4,0,-2));
        s.actors[3].transform.rotation=Quaternion.LookRotation(Vector3.left);
        s.RequestPass();yield return Wait(.4f);
        Check(s.ball.Holder==0&&s.PassReason.Contains("挡住"),"blocked request is declined with actionable reason");
        s.ball.Launch(1);yield return Wait(1.4f);
        Check(s.Phase==TacticalPhase.Turnover&&s.ball.Holder==3&&s.ball.Interceptions==1,"forced pass is intercepted once, with defender ownership");

        s.StartDrill(TacticalDrill.PickAndRoll,true);s.offenseSpeed=s.defenseSpeed=0;
        s.actors[0].SetPosition(new Vector3(-3,0,-10));s.TeleportPlayer(new Vector3(3,0,-10),0);
        for(int i=3;i<6;i++)s.actors[i].SetPosition(new Vector3(i-4,0,-2));
        s.ball.Launch(1);yield return WalkTo(new Vector3(3,0,-6),1,true,true);yield return Wait(.5f);
        Check(s.Phase==TacticalPhase.Turnover&&s.ball.Holder==-1,"running away from fixed catch point produces a missed pass");
        s.offenseSpeed=2.65f;s.defenseSpeed=2.25f;
        s.StartDrill(TacticalDrill.HighPostCut,false);
        var before=s.Position(1);s.player.SetOverview(true);yield return Wait(.5f);
        Check(s.player.overview.enabled&&TacticalRules.Distance(before,s.Position(1))>.4f,"hold Tab keeps NPC simulation moving");
        var p=s.player.transform.position;s.player.SetOverview(false);
        Check(TacticalRules.Distance(p,s.player.transform.position)<.001f&&s.player.eyes.enabled,"releasing Tab restores first person without teleport");
        s.TeleportPlayer(new Vector3(-6,0,-12),0);p=s.player.transform.position;
        for(int i=0;i<15;i++){s.player.Drive(Vector2.up,Vector2.zero,1f/30,i%2==0,true);yield return null;}
        float ran=TacticalRules.Distance(p,s.player.transform.position);p=s.player.transform.position;
        for(int i=0;i<15;i++){s.player.Drive(Vector2.up,Vector2.zero,1f/30,false,false);yield return null;}
        Check(ran>2.6f&&TacticalRules.Distance(p,s.player.transform.position)<1.6f,"Shift sprint and immediate walking recovery during repeated Tab");
        for(int i=0;i<12;i++)s.StartDrill((TacticalDrill)(i%2),i%2==1);
        Check(s.Controlled==1&&s.ball.Holder==0&&!s.ball.Flying&&!s.ball.Charging&&s.actors.Count(a=>a.gameObject.activeSelf)==5,"twelve mixed-mode resets leave one owner and five NPCs");
        s.ball.Launch(1);var held=s.ball.Position;var pausedActors=s.actors.Select(a=>a.transform.position).ToArray();s.OpenMenu();yield return Wait(.5f);
        Check(Vector3.Distance(held,s.ball.Position)<.01f&&!s.Running&&s.actors.Select((a,i)=>Vector3.Distance(a.transform.position,pausedActors[i])).All(d=>d<.001f),"menu pauses in-flight ball and all actors");
        s.StartDrill(TacticalDrill.PickAndRoll,false);yield return Wait(.2f);
        Check(s.ball.Holder==0&&!s.ball.Flying,"new round cancels previous flight");
        record=false;encoder.StandardInput.Close();
        if(!encoder.WaitForExit(30000)||encoder.ExitCode!=0)Check(false,"video encoding failed");
        results.Add("Frames: "+frame+" @ 30 fps; failures: "+failures);
        File.WriteAllLines(path+"/"+round+"-checks.txt",results);
        EditorApplication.Exit(failures==0?0:1);
    }
    void LateUpdate()
    {
        if(!record)return;
        try
        {
            s.ball.Pose();if(s.player.overview.enabled)s.player.planSymbols.Refresh();
            var cam=s.player.overview.enabled?s.player.overview:s.player.eyes;var old=RenderTexture.active;
            cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            readback.ReadPixels(new Rect(0,0,1280,720),0,0);readback.Apply();cam.targetTexture=null;RenderTexture.active=old;
            var data=readback.GetRawTextureData();encoder.StandardInput.BaseStream.Write(data,0,data.Length);frame++;
        }
        catch(Exception e){Debug.LogException(e);record=false;File.WriteAllLines(path+"/"+round+"-checks.txt",results);EditorApplication.Exit(1);}
    }
}
