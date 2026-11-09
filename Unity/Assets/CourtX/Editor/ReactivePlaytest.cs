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
public static class ReactivePlaytest
{
    static ReactivePlaytest()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("CourtX.ReactivePlay",false))return;
            SessionState.SetBool("CourtX.ReactivePlay",false);new GameObject("Reactive scenario reviewer").AddComponent<ReactivePlayChecks>();
        };
    }
    public static void Run(){EditorSceneManager.OpenScene(TacticalAuthoring.Scene);SessionState.SetBool("CourtX.ReactivePlay",true);EditorApplication.isPlaying=true;}
}
[DefaultExecutionOrder(9500)]
public sealed class ReactivePlayChecks:MonoBehaviour
{
    TacticalSession s;int failures,frames;bool record;string label;
    readonly List<string> lines=new List<string>();
    Process encoder;RenderTexture rt;Texture2D pixels;
    void Check(bool ok,string text){lines.Add((ok?"PASS ":"FAIL ")+text);Debug.Log(lines.Last());if(!ok)failures++;}
    void Error(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error){lines.Add("ERROR "+message);failures++;}}
    void Drive(Vector3 point,bool map=true,bool sprint=false)
    {
        var delta=TacticalRules.Flat(point-s.Position(s.Controlled));
        var local=s.player.transform.InverseTransformDirection(delta.magnitude>.15f?delta.normalized:Vector3.zero);
        s.player.Drive(new Vector2(local.x,local.z),Vector2.zero,1f/30,map,sprint);
    }
    void FollowCatch(){if(s.ball.Flying&&s.ball.Target==s.Controlled)Drive(s.ball.CatchPoint);}
    IEnumerator Wait(float seconds){for(int i=0;i<seconds*30;i++){FollowCatch();yield return null;}}
    IEnumerator Until(Func<bool> predicate,float seconds)
    {for(int i=0;i<seconds*30&&!predicate();i++){FollowCatch();yield return null;}}
    IEnumerator Walk(Vector3 point,float seconds=4)
    {
        for(int i=0;i<seconds*30&&TacticalRules.Distance(s.Position(s.Controlled),point)>.18f;i++)
        {if(s.ball.Flying){FollowCatch();yield return null;continue;}Drive(point);yield return null;}
        Drive(s.Position(s.Controlled));
    }
    IEnumerator HumanPass()
    {
        s.ball.BeginCharge();yield return Wait(.15f);
        int receiver=-1;
        for(int frame=0;frame<120&&s.ball.Holder==s.Controlled;frame++)
        {
            receiver=-1;float best=-1;
            for(int i=0;i<3;i++)if(i!=s.Controlled)
            {string reason;if(s.CanPass(s.Controlled,i,out reason,s.ball.ReleaseSpeed)&&s.ReceiverSpace(i)>best){receiver=i;best=s.ReceiverSpace(i);}}
            if(receiver>=0)break;
            Drive(s.Position(s.Controlled)+Vector3.right*.6f+Vector3.back*.25f);yield return null;
        }
        if(receiver<0)receiver=s.Controlled==0?1:0;
        s.ball.Release(receiver);yield return Until(()=>!s.ball.Flying,3);
    }
    IEnumerator Start()
    {
        label=ReactiveReview.Label;Directory.CreateDirectory(ReactiveReview.Folder);
        Application.logMessageReceived+=Error;Time.captureFramerate=30;QualitySettings.vSyncCount=0;
        s=FindObjectOfType<TacticalSession>();s.acceptsInput=false;
        rt=new RenderTexture(1280,720,24){antiAliasing=4};pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        encoder=new Process{StartInfo=new System.Diagnostics.ProcessStartInfo("ffmpeg.exe","-hide_banner -loglevel error -y -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 20 -pix_fmt yuv420p -movflags +faststart \""+ReactiveReview.Folder+"/"+label+".mp4\"")
            {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true}};
        encoder.Start();yield return null;record=true;
        for(int scenario=0;scenario<4;scenario++)for(int role=0;role<2;role++)
        {
            var drill=scenario==3?TacticalDrill.HighPostCut:TacticalDrill.PickAndRoll;
            s.StartScenario(drill,role==1,(DefenseCoverage)Mathf.Min(scenario,2),4435);s.player.SetOverview(true);
            string name=scenario+"/"+role;lines.Add("SCENARIO "+name+" at video frame "+frames);
            yield return Wait(.4f);
            if(drill==TacticalDrill.PickAndRoll)
            {
                if(role==1)yield return Walk(new Vector3(.35f,0,-8.1f));
                yield return Until(()=>s.Phase==TacticalPhase.ScreenReady,5);
                Check(s.Phase==TacticalPhase.ScreenReady,name+" screen is planted");
                if(role==0)yield return Walk(new Vector3(1.8f,0,-8.5f),3);
                yield return Until(()=>s.Tactics.Stage==DefenseStage.Engaged,4);
                Check(s.Tactics.Stage==DefenseStage.Engaged,name+" chosen defense reacts to real screen use");
                yield return Wait(.55f);
                if(role==0){yield return Walk(new Vector3(3.2f,0,-6.7f),2);yield return HumanPass();}
                else
                {
                    yield return Walk(s.GuidancePoint(),3);s.RequestPass();yield return Until(()=>s.ball.Holder==1||s.Phase==TacticalPhase.Turnover,4);
                    if(s.ball.Holder!=1&&s.Phase!=TacticalPhase.Turnover){yield return Walk(new Vector3(3.6f,0,-4.6f),2);s.RequestPass();yield return Until(()=>s.ball.Holder==1||s.Phase==TacticalPhase.Turnover,4);}
                }
            }
            else
            {
                if(role==1)yield return Walk(new Vector3(.1f,0,-5.8f),3);
                else {yield return Wait(1.3f);yield return HumanPass();}
                yield return Until(()=>s.Phase==TacticalPhase.Cut||s.Phase==TacticalPhase.Turnover,5);
                if(role==0){yield return Walk(new Vector3(-4.8f,0,-5.7f),3);yield return Walk(new Vector3(-1.6f,0,-3.8f),3);s.RequestPass();yield return Wait(3);}
                else if(s.ball.Holder==1){yield return Wait(1);yield return HumanPass();yield return Until(()=>s.Phase==TacticalPhase.Complete||s.Phase==TacticalPhase.Turnover,5);}
            }
            yield return Wait(.8f);s.player.SetOverview(false);yield return Wait(.35f);
            Check(s.Controlled==role&&s.actors.Count(a=>a.gameObject.activeSelf)==5,name+" fixed identity and five NPCs");
            Check(s.Journal.Episode.decisions.Count>0,name+" actual pass decision occurred");
            Check(s.Journal.Episode.decisions.All(d=>d.options.Length==2),name+" both alternatives recorded at release");
            Check(s.Phase==TacticalPhase.Complete,name+" a spatial advantage completes via direct or relay passing");
            if(drill==TacticalDrill.PickAndRoll&&s.ball.Holder==2&&s.ReceiverSpace(2)>1.2f)
                Check(s.Phase==TacticalPhase.Complete,name+" weak-side advantage counts as a valid PNR outcome");
            var id=s.Journal.Episode.id;s.OpenReview();
            var paused=s.Position(4);float time=s.ActiveTime;yield return Wait(.3f);
            Check(s.ReviewOpen&&!s.Running&&s.ActiveTime==time&&s.Position(4)==paused,name+" review freezes the same episode");
            var exported=JsonUtility.FromJson<TacticalEpisode>(File.ReadAllText(s.SavedReview));
            Check(exported.id==id&&exported.decisions.Count==s.Journal.Episode.decisions.Count,name+" saved JSON matches the displayed review");
            Check(s.Review.text==File.ReadAllText(Path.ChangeExtension(s.SavedReview,".txt")),name+" saved text matches local review");
            lines.Add("OUTCOME "+name+" "+s.Phase+" passes="+s.ball.CompletedPasses+" holder="+s.ball.Holder);
            s.RetryScenario();Check(s.Seed==4435&&s.Journal.Episode.id!=id&&!s.ReviewOpen&&s.Journal.Episode.decisions.Count==0,name+" retry keeps seed and clears old facts");
        }
        string signature=null;
        for(int repeat=0;repeat<2;repeat++)
        {
            s.StartScenario(TacticalDrill.PickAndRoll,false,DefenseCoverage.Random,99);yield return Wait(2);
            for(int i=0;i<90;i++){Drive(new Vector3(1.8f,0,-8.5f));yield return null;}
            string current=s.Tactics.Coverage+"/"+s.Phase+"/"+string.Join(";",Enumerable.Range(0,6).Select(i=>s.Position(i).ToString("F2")))+"/"+string.Join(";",s.Journal.Episode.events.Select(e=>e.kind+":"+e.text));
            if(repeat==0)signature=current;else Check(signature==current,"Same seed and scripted input reproduce positions and key events");
        }
        s.StartScenario(TacticalDrill.PickAndRoll,false,DefenseCoverage.Blitz,99);yield return Wait(2);
        yield return Walk(new Vector3(-4.5f,0,-11),2);
        Check(s.Tactics.Stage==DefenseStage.Contain&&s.ball.Holder==0,"Rejecting the screen leaves defense in containment");
        float clock=s.ActiveTime;int sampleCount=s.Journal.Episode.frames.Count;s.OpenMenu();yield return Wait(.5f);
        Check(s.ActiveTime==clock&&s.Journal.Episode.frames.Count==sampleCount,"Pause freezes active-time clocks and journal sampling");
        s.Resume();s.OpenReview();s.acceptsInput=true;s.SendMessage("OnApplicationFocus",false,SendMessageOptions.RequireReceiver);
        Check(s.ReviewOpen,"Losing window focus does not discard an open review");s.acceptsInput=false;
        record=false;encoder.StandardInput.Close();if(!encoder.WaitForExit(30000)||encoder.ExitCode!=0)Check(false,"video encoding");
        lines.Add("Frames: "+frames+" @30fps; failures: "+failures);File.WriteAllLines(ReactiveReview.Folder+"/"+label+"-checks.txt",lines);
        EditorApplication.Exit(failures==0?0:1);
    }
    void LateUpdate()
    {
        if(!record)return;
        try
        {
            s.ball.Pose();if(s.player.overview.enabled)s.player.planSymbols.Refresh();var camera=s.player.overview.enabled?s.player.overview:s.player.eyes;
            var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();camera.targetTexture=null;RenderTexture.active=old;
            var bytes=pixels.GetRawTextureData();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);frames++;
        }
        catch(Exception e){Debug.LogException(e);record=false;EditorApplication.Exit(1);}
    }
}
