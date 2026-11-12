#if COURTX_READABILITY_REVIEW
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CourtX;
using UnityEngine;

// Deterministic recorded display fixtures. Excluded from the release player.
public sealed class ReadabilityPlayerReview:MonoBehaviour
{
    TacticalSession session;LearningHub hub;string folder;int errors,shots;
    readonly List<string> checks=new List<string>();
    static readonly FieldInfo focus=typeof(LearningHubUI).GetField("focusActor",BindingFlags.Static|BindingFlags.NonPublic);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()=>new GameObject("Readability visual checks").AddComponent<ReadabilityPlayerReview>();
    void Check(bool ok,string name){checks.Add((ok?"PASS ":"FAIL ")+name);if(!ok)errors++;}
    IEnumerator Shot(string name)
    {
        yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
        var pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);pixels.Apply();
        int visible=0;
        for(int y=1;y<10;y++)for(int x=1;x<16;x++)
        {var p=pixels.GetPixel(Screen.width*x/16,Screen.height*y/10);if(p.r+p.g+p.b>.1f)visible++;}
        Check(visible>20,name+": captured a rendered surface rather than a black frame");
        File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());Destroy(pixels);shots++;
    }
    void History(TacticalDrill drill,DefenseCoverage coverage,string id)
    {
        var episode=LearningCatalog.Load(new LearningScenario{drill=drill,coverage=coverage});
        episode.id=id;episode.endReason="界面验证样本";hub.Remember(episode);hub.SelectHistory(0);
    }
    void CheckPicking(string tag)
    {
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        var offset=new Vector2((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2);
        var court=new Rect(Mathf.Round(299*scale+offset.x),Mathf.Round(188*scale+offset.y),Mathf.Round(490*scale),Mathf.Round(361*scale));
        var frame=hub.Cursor.Sample();var p=frame.positions[2];
        var marker=new Vector2(court.x+(112+(p.x+7.5f)*28-48)*court.width/548,court.y+(146-p.z*28-140)*court.height/404);
        Check(RecapCourtView.PickActor(court,frame,marker)==2,tag+": visible 05 badge hit maps correctly");
        Check(RecapCourtView.PickActor(court,frame,new Vector2(court.x-20,court.y))==-1,tag+": outside map does not select an actor");
    }
    IEnumerator Start()
    {
        folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));Directory.CreateDirectory(folder);
        Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=30;
        Application.logMessageReceived+=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message);}};
        yield return null;session=FindObjectOfType<TacticalSession>();session.acceptsInput=false;hub=session.Learning;
        var initialActors=Enumerable.Range(0,6).Select(session.Position).ToArray();
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,576),new Vector2Int(1920,1080),new Vector2Int(1280,800)})
        {
            string tag=size.x+"x"+size.y;Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.6f);
            Check(Screen.width==size.x&&Screen.height==size.y,tag+": actual render resolution matches request");
            hub.Show(LearningPage.Lesson);hub.SelectScenario(new LearningScenario{coverage=DefenseCoverage.Switch});hub.Seek(3.4f);
            yield return Shot(tag+"-lesson");CheckPicking(tag);
            string original=JsonUtility.ToJson(hub.Current);focus.SetValue(null,1);yield return Shot(tag+"-focus04");
            Check(JsonUtility.ToJson(hub.Current)==original,tag+": focus does not mutate recorded facts");
            hub.SelectScenario(new LearningScenario{coverage=DefenseCoverage.Drop});
            var flight=hub.Current.frames.First(f=>f.holder<0);hub.Seek(flight.time);yield return Shot(tag+"-flight");
            Check((int)focus.GetValue(null)==-1,tag+": changing example restores all trails");
            hub.SelectQuestion(5);hub.ResetAnswer();yield return Shot(tag+"-quiz");
            Check(Mathf.Abs(hub.Cursor.Time-hub.Question.time)<.001f,tag+": question remains at its recorded instant");
            hub.ChooseAnswer(hub.Question.correct);
            typeof(LearningHubUI).GetField("rightScroll",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new Vector2(0,100000));
            yield return Shot(tag+"-feedback");
            History(TacticalDrill.HighPostCut,DefenseCoverage.Drop,"qa-review-"+tag);hub.Seek(3.1f);yield return Shot(tag+"-review");
            History(TacticalDrill.PickAndRoll,DefenseCoverage.Drop,"qa-a-"+tag);hub.PinBaseline();
            History(TacticalDrill.PickAndRoll,DefenseCoverage.Switch,"qa-b-"+tag);hub.Show(LearningPage.WhatIf);hub.SetCompareSide(true);hub.Seek(3.5f);
            yield return Shot(tag+"-comparison");focus.SetValue(null,3);yield return Shot(tag+"-focus11");
            hub.SetCompareSide(false);yield return new WaitForSecondsRealtime(.15f);
            Check((int)focus.GetValue(null)==-1,tag+": A/B record switch clears prior trail focus");
            Check(!session.Running&&Enumerable.Range(0,6).All(i=>session.Position(i)==initialActors[i]),tag+": viewing never advances the live court");
        }
        checks.Add("Screenshots: "+shots);checks.Add("Failures: "+errors);
        checks.Add("QA fixtures are visual evidence only, not user practice or learning outcomes.");
        File.WriteAllLines(Path.Combine(folder,"visual-checks.txt"),checks);Application.Quit(errors==0?0:1);
    }
}
#endif
