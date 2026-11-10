using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourtX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad] public static class LearningPlaytest
{
    static LearningPlaytest(){EditorApplication.playModeStateChanged+=state=>
    {if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("CourtX.LearningPlay",false))return;
        SessionState.SetBool("CourtX.LearningPlay",false);new GameObject("Learning lifecycle checks").AddComponent<LearningPlayChecks>();};}
    public static void Run(){EditorSceneManager.OpenScene(TacticalAuthoring.Scene);SessionState.SetBool("CourtX.LearningPlay",true);EditorApplication.isPlaying=true;}
}
public sealed class LearningPlayChecks:MonoBehaviour
{
    readonly List<string> lines=new List<string>();int failures;TacticalSession s;
    void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);Debug.Log(lines.Last());if(!ok)failures++;}
    IEnumerator Wait(int frames){for(int i=0;i<frames;i++)yield return null;}
    IEnumerator Start()
    {
        Time.captureFramerate=30;s=FindObjectOfType<TacticalSession>();s.acceptsInput=false;
        Application.logMessageReceived+=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception){failures++;lines.Add("ERROR "+message);}};
        yield return null;var h=s.Learning;
        Check(h.IsOpen&&h.Page==LearningPage.Lesson&&h.Current!=null&&!s.Running,"Startup opens recorded lesson with live simulation paused");
        h.SelectScenario(new LearningScenario{drill=TacticalDrill.HighPostCut,controlled=1,coverage=DefenseCoverage.Blitz,seed=765});
        h.StartPractice();yield return Wait(10);
        Check(s.Running&&!h.IsOpen&&s.Drill==TacticalDrill.HighPostCut&&s.Controlled==1&&s.Seed==765&&s.CoverageChoice==DefenseCoverage.Drop,"Lesson configuration transfers into actual practice");
        Check(s.Journal.Episode.hasScenarioConfig&&s.Journal.Episode.coverageChoice==0,"Live journal preserves explicit scenario metadata for historical retry");
        float active=s.ActiveTime;var actors=Enumerable.Range(0,6).Select(s.Position).ToArray();s.OpenLearning();
        h.Cursor.Toggle();yield return new WaitForSecondsRealtime(.2f);
        Check(!s.Running&&s.ActiveTime==active&&Enumerable.Range(0,6).All(i=>s.Position(i)==actors[i]),"Teaching playback never advances a paused practice or its actors");
        Check(h.IsOpen&&h.Cursor.Time>h.Cursor.StartTime,"Teaching replay advances independently");
        s.Resume();yield return Wait(3);Check(s.Running&&!h.IsOpen&&s.ActiveTime>active,"Resume returns to the same unfinished practice");
        string id=s.Journal.Episode.id;s.OpenReview();yield return Wait(2);
        Check(h.IsOpen&&h.Page==LearningPage.Review&&h.Current?.id==id&&h.History.Entries.Any(e=>e.id==id),"V review displays and archives this exact actual round");
        Check(s.ReviewOpen&&!s.Running,"Legacy review state remains valid for integrations");
        int count=h.History.Entries.Count;s.OpenReview();yield return null;Check(h.History.Entries.Count==count,"Opening review twice does not duplicate history");
        h.PinBaseline();Check(h.Baseline?.id==id,"Current completed round can become comparison A");
        h.UseBaselineStart();h.StartPractice();yield return Wait(20);s.OpenReview();yield return Wait(2);
        string reason="";Check(h.Candidate!=null&&LearningHistory.CanCompare(h.Baseline,h.Candidate,out reason),"Second actual practice with shared start becomes comparison B");
        if(h.Baseline!=null&&h.Candidate!=null)
        {
            string snapshot=JsonUtility.ToJson(h.Baseline);h.Show(LearningPage.WhatIf);h.SetCompareSide(true);h.Seek(.5f);float time=h.Cursor.Time;
            h.SetCompareSide(false);Check(Mathf.Abs(h.Cursor.Time-Mathf.Min(time,h.Baseline.frames.Last().time))<.001f,"A/B view changes retain elapsed time and clamp ended round");
            Check(JsonUtility.ToJson(h.Baseline)==snapshot,"Comparison never mutates baseline facts");
        }
        else {Check(false,"A/B view changes retain elapsed time and clamp ended round");Check(false,"Comparison never mutates baseline facts");}
        h.SelectQuestion(2);Check(h.Page==LearningPage.Challenge&&h.Scenario.controlled==1&&h.Scenario.coverage==DefenseCoverage.Blitz,"Question binds its role, coverage and scene");
        h.Cursor.Toggle();h.Tick(3);Check(Mathf.Abs(h.Cursor.Time-h.Question.time)<.001f,"Question snapshot stays fixed while its options and feedback are displayed");
        h.ChooseAnswer(0);Check(h.Answer==0&&!string.IsNullOrWhiteSpace(h.Feedback),"Wrong answer receives specific feedback");
        h.ShowQuestionLesson();Check(h.Page==LearningPage.Lesson&&Mathf.Abs(h.Cursor.Time-h.Question.time)<.001f,"Question returns to its corresponding lesson instant");
        h.SelectQuestion(2);Check(h.Answer==0,"Answer persists across module navigation");
        h.ResetAnswer();Check(h.Answer==-1,"Explicit retry clears this question answer");
        h.ChooseAnswer(h.Question.correct);Check(h.Answer==h.Question.correct,"Question can be retried after reflection");
        h.StartPractice();yield return Wait(2);Check(s.Drill==TacticalDrill.PickAndRoll&&s.Controlled==1&&s.CoverageChoice==DefenseCoverage.Blitz&&s.Journal.Episode.learningObjective.Contains(h.Question.title),"Challenge launches its actual matching scene with learning objective");
        h.Show(LearningPage.WhatIf);Check(h.Scenario.Key==LearningScenario.FromEpisode(h.Baseline).Key,"Returning from another drill restores the pinned comparison opening");
        s.Resume();
        s.OpenReview();yield return null;h.SelectHistory(h.History.Entries.Count-1);
        Check(h.Page==LearningPage.Review&&h.Current!=null&&h.Current.id!=s.Journal.Episode.id,"History can review an older round without replacing current journal");
        h.StartPractice();yield return null;Check(s.Running&&!s.Journal.Closed&&s.Journal.Episode.id!=id,"Historical retry starts a fresh simulation rather than resuming recorded frames");
        yield return Wait(3);string failedId=s.Journal.Episode.id;
        string blockPath=Path.Combine(s.ReviewFolder,failedId+".json");Directory.CreateDirectory(blockPath);s.OpenReview();
        Check(h.SaveFailed&&!string.IsNullOrWhiteSpace(h.RecordWarning),"Disk write failure is visible on the displayed actual episode");
        h.Show(LearningPage.Lesson);h.StartPractice();yield return Wait(3);s.OpenReview();
        h.SelectHistory(h.History.Entries.ToList().FindIndex(e=>e.id==failedId));
        Check(h.SaveFailed&&h.RecordWarning.Contains("保存"),"Save failure follows its history entry after another round");
        Directory.Delete(blockPath,false);h.RetrySaveCurrent();
        Check(!h.SaveFailed&&File.Exists(blockPath),"Retry persists the selected failed history episode and clears its warning");
        Directory.CreateDirectory(ReactiveReview.Folder);lines.Add("Failures: "+failures);File.WriteAllLines(Path.Combine(ReactiveReview.Folder,ReactiveReview.Label+"-checks.txt"),lines);
        EditorApplication.Exit(failures==0?0:1);
    }
}
