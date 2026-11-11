#if COURTX_POLISH_REVIEW
using System.Collections;
using System.IO;
using System.Reflection;
using CourtX;
using UnityEngine;

// Display fixtures only; never included in the distributable player or user's replay folder.
public sealed class PolishPlayerReview:MonoBehaviour
{
    TacticalSession s;LearningHub h;string folder;int errors;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()=>new GameObject("Polish visual checks").AddComponent<PolishPlayerReview>();
    IEnumerator Shot(string name)
    {
        yield return new WaitForSecondsRealtime(.35f);yield return new WaitForEndOfFrame();
        var pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);pixels.Apply();
        File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());Destroy(pixels);
    }
    void ExampleHistory(TacticalDrill drill,DefenseCoverage defense,string id)
    {var e=LearningCatalog.Load(new LearningScenario{drill=drill,coverage=defense});e.id=id;e.endReason="界面验证样本";h.Remember(e);h.SelectHistory(0);}
    IEnumerator Start()
    {
        folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));Application.runInBackground=true;
        QualitySettings.vSyncCount=0;Application.targetFrameRate=30;
        Application.logMessageReceived+=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception){errors++;File.AppendAllText(Path.Combine(folder,"visual-errors.txt"),message+"\n");}};
        yield return null;s=FindObjectOfType<TacticalSession>();s.acceptsInput=false;h=s.Learning;
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,576),new Vector2Int(1920,1080),new Vector2Int(1280,800)})
        {
            string tag=size.x+"x"+size.y;Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.6f);
            h.Show(LearningPage.Lesson);h.SelectScenario(new LearningScenario{coverage=DefenseCoverage.Switch});h.Seek(3.4f);yield return Shot(tag+"-lesson");
            h.SelectQuestion(5);h.ResetAnswer();yield return Shot(tag+"-quiz");h.ChooseAnswer(h.Question.correct);
            typeof(LearningHubUI).GetField("rightScroll",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new Vector2(0,100000));yield return Shot(tag+"-feedback");
            ExampleHistory(TacticalDrill.HighPostCut,DefenseCoverage.Drop,"qa-review-"+tag);h.Seek(3.1f);yield return Shot(tag+"-review");
            ExampleHistory(TacticalDrill.PickAndRoll,DefenseCoverage.Drop,"qa-a-"+tag);h.PinBaseline();
            ExampleHistory(TacticalDrill.PickAndRoll,DefenseCoverage.Switch,"qa-b-"+tag);h.Show(LearningPage.WhatIf);h.SetCompareSide(true);h.Seek(3.5f);yield return Shot(tag+"-comparison");
            if(s.Running)errors++;
        }
        File.WriteAllText(Path.Combine(folder,"visual-player-result.txt"),"20 screenshots across 4 viewport sizes; errors="+errors+"\nQA records are rendering fixtures, not user practice.\n");Application.Quit(errors==0?0:1);
    }
}
#endif
