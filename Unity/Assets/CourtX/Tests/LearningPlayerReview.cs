#if COURTX_LEARNING_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CourtX;
using UnityEngine;
using Process=System.Diagnostics.Process;
// Instrumentation is only compiled in the independent QA player.
public sealed class LearningPlayerReview:MonoBehaviour
{
    TacticalSession s;LearningHub h;string folder,round;Process encoder;bool recording;int frames,failures;
    readonly List<string> lines=new List<string>();
    sealed class LongProvider:ITacticalRecapProvider
    {public Task<string> GenerateAsync(string json,string local,CancellationToken token)=>Task.FromResult(string.Concat(Enumerable.Repeat("长段解说测试：按记录确认持球人、队友位置和传球线路。\n",40))+"长文本末尾检查标记");}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){new GameObject("Learning QA recorder").AddComponent<LearningPlayerReview>();}
    void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
    IEnumerator Wait(float seconds){yield return new WaitForSecondsRealtime(seconds);}
    Texture2D Capture(){var tex=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);tex.Apply();return tex;}
    IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var tex=Capture();File.WriteAllBytes(Path.Combine(folder,round+"-"+name+".png"),tex.EncodeToPNG());Destroy(tex);}
    IEnumerator Record()
    {while(recording){yield return new WaitForEndOfFrame();if(!recording)break;var tex=Capture();var bytes=tex.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);Destroy(tex);frames++;}}
    void Drive(Vector3 point)
    {var delta=TacticalRules.Flat(point-s.Position(s.Controlled));var local=s.player.transform.InverseTransformDirection(delta.magnitude>.15f?delta.normalized:Vector3.zero);s.player.Drive(new Vector2(local.x,local.z),Vector2.zero,Time.deltaTime,true,false);}
    IEnumerator Walk(Vector3 point,float seconds=4)
    {float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end&&TacticalRules.Distance(s.Position(s.Controlled),point)>.18f){if(s.ball.Flying&&s.ball.Target==s.Controlled)Drive(s.ball.CatchPoint);else Drive(point);yield return null;}Drive(s.Position(s.Controlled));}
    IEnumerator Until(Func<bool> condition,float seconds)
    {float end=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<end){if(s.ball.Flying&&s.ball.Target==s.Controlled)Drive(s.ball.CatchPoint);yield return null;}}
    IEnumerator Pass()
    {s.ball.BeginCharge();yield return Wait(.18f);int receiver=-1;float best=-1;for(int i=0;i<3;i++)if(i!=s.Controlled&&s.CanPass(s.Controlled,i,out _,s.ball.ReleaseSpeed)&&s.ReceiverSpace(i)>best){receiver=i;best=s.ReceiverSpace(i);}if(receiver<0)receiver=s.Controlled==0?1:0;s.ball.Release(receiver);yield return Until(()=>!s.ball.Flying,3);}
    IEnumerator Practice(TacticalDrill drill,int role,DefenseCoverage defense)
    {
        h.Show(LearningPage.Lesson);h.SelectScenario(new LearningScenario{drill=drill,controlled=role,coverage=defense,seed=4435});h.StartPractice();s.player.SetOverview(true);yield return Wait(.4f);
        if(drill==TacticalDrill.PickAndRoll)
        {
            if(role==1)yield return Walk(new Vector3(.35f,0,-8.1f));yield return Until(()=>s.Phase==TacticalPhase.ScreenReady,5);
            if(role==0)yield return Walk(new Vector3(1.8f,0,-8.5f),3);yield return Until(()=>s.Tactics.Stage==DefenseStage.Engaged,4);
            yield return Wait(.55f);
            if(role==0){yield return Walk(new Vector3(3.2f,0,-6.7f),2);yield return Pass();}
            else{yield return Walk(s.GuidancePoint(),3);s.RequestPass();yield return Until(()=>s.ball.Holder==1||s.Phase==TacticalPhase.Turnover,5);}
        }
        else
        {
            if(role==1)yield return Walk(new Vector3(.1f,0,-5.8f),3);else {yield return Wait(1.3f);yield return Pass();}
            yield return Until(()=>s.Phase==TacticalPhase.Cut||s.Phase==TacticalPhase.Turnover,5);
            if(role==0){yield return Walk(new Vector3(-4.8f,0,-5.7f),3);yield return Walk(new Vector3(-1.6f,0,-3.8f),3);s.RequestPass();yield return Until(()=>s.Phase==TacticalPhase.Complete,3);}
            else if(s.ball.Holder==1){yield return Wait(1);yield return Pass();yield return Until(()=>s.Phase==TacticalPhase.Complete||s.Phase==TacticalPhase.Turnover,5);}
        }
        yield return Wait(.5f);string id=s.Journal.Episode.id;s.OpenReview();
        Check(h.Current?.id==id&&!h.IsExample&&h.Page==LearningPage.Review,"Actual "+drill+" role "+role+" returns exact round");
        Check(h.Current.decisions.Count>0,"Actual "+drill+" role "+role+" records pass");
        yield return Wait(.3f);yield return Shot("practice-"+drill+"-"+role+"-"+defense);yield return Wait(1);
    }
    IEnumerator Start()
    {
        folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-learningRound");round=index>=0&&index+1<args.Length?args[index+1]:"round1";
        Screen.SetResolution(1280,720,false);Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=30;
        if(args.Contains("-learningManual"))yield break;
        Application.logMessageReceived+=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception){failures++;lines.Add("ERROR "+message);}};
        yield return Wait(1);s=FindObjectOfType<TacticalSession>();s.acceptsInput=false;h=s.Learning;
        encoder=new Process{StartInfo=new System.Diagnostics.ProcessStartInfo("ffmpeg.exe","-hide_banner -loglevel error -y -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 20 -pix_fmt yuv420p -movflags +faststart \""+folder+"/"+round+"-learning.mp4\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true}};
        encoder.Start();recording=true;StartCoroutine(Record());
        h.Show(LearningPage.Lesson);h.Cursor.Toggle();yield return Wait(3);yield return Shot("lesson");
        for(int i=0;i<6;i++)
        {h.SelectQuestion(i);yield return Wait(.35f);if(i==5)yield return Shot("challenge-before");h.ChooseAnswer((h.Question.correct+1)%3);Check(h.Answer>=0&&!string.IsNullOrEmpty(h.Feedback),"Question "+i+" gives wrong-choice feedback");yield return Wait(.35f);h.ResetAnswer();h.ChooseAnswer(h.Question.correct);if(i==5)yield return Shot("challenge-after");}
        yield return Practice(TacticalDrill.PickAndRoll,0,DefenseCoverage.Drop);h.PinBaseline();
        yield return Practice(TacticalDrill.PickAndRoll,0,DefenseCoverage.Switch);
        h.Show(LearningPage.WhatIf);Check(LearningHistory.CanCompare(h.Baseline,h.Candidate,out _),"Actual A/B share opening");h.SetCompareSide(false);h.Seek(3.5f);yield return Wait(1);yield return Shot("compare-A");h.SetCompareSide(true);yield return Wait(1);yield return Shot("compare-B");
        yield return Practice(TacticalDrill.HighPostCut,1,DefenseCoverage.Drop);
        if(round!="round1")
        {
            yield return Practice(TacticalDrill.PickAndRoll,1,DefenseCoverage.Blitz);
            h.Show(LearningPage.Lesson);h.StartPractice();yield return Wait(.2f);s.OpenReview();Check(h.Current.decisions.Count==0,"Short no-pass round invents no passing assessment");yield return Shot("no-pass");
            s.RecapProvider=new LongProvider();s.OpenReview();yield return Wait(.4f);
            typeof(LearningHubUI).GetField("rightScroll",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new Vector2(0,100000));yield return Wait(.3f);yield return Shot("long-bottom");s.RecapProvider=null;
        }
        h.Show(LearningPage.Review);yield return Wait(1);yield return Shot("history");
        recording=false;yield return new WaitForEndOfFrame();encoder.StandardInput.Close();encoder.WaitForExit(30000);Check(encoder.ExitCode==0&&frames>300,"Standalone full-UI video encoded");
        Screen.SetResolution(1024,576,false);yield return Wait(.6f);h.SelectQuestion(5);yield return Wait(.4f);yield return Shot("small-challenge");
        h.Show(LearningPage.Lesson);h.SelectScenario(new LearningScenario{drill=TacticalDrill.HighPostCut,controlled=1});h.Seek(3);yield return Wait(.5f);yield return Shot("small-lesson");
        lines.Add("Video frames: "+frames+"; failures: "+failures);File.WriteAllLines(Path.Combine(folder,round+"-player-checks.txt"),lines);Application.Quit(failures==0?0:1);
    }
}
#endif
