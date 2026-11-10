using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace CourtX
{
    public enum LearningPage {Lesson,WhatIf,Challenge,Review}
    public sealed class LearningHub
    {
        readonly TacticalSession session;
        readonly Dictionary<string,int> answers=new Dictionary<string,int>();
        readonly Dictionary<string,string> saveResults=new Dictionary<string,string>();
        public LearningScenario Scenario {get;private set;}=new LearningScenario();
        public LearningPage Page {get;private set;}
        public bool IsOpen {get;private set;}
        public LearningHistory History {get;}=new LearningHistory();
        public RecapPlayback Cursor {get;}=new RecapPlayback();
        public TacticalEpisode Current {get;private set;}
        public TacticalEpisode Baseline {get;private set;}
        public TacticalEpisode Candidate {get;private set;}
        public LearningQuestion[] Questions {get;}=LearningCatalog.Questions();
        public int QuestionIndex {get;private set;}
        public LearningQuestion Question=>Questions[QuestionIndex];
        public int Answer=>answers.TryGetValue(Question.id,out int value)?value:-1;
        public string Feedback=>Answer<0?"先观察站位、持球人和防守，再做选择。":Question.feedback[Answer];
        public bool IsExample {get;private set;}=true;
        public bool CanPractice=>Page!=LearningPage.Review||LearningScenario.FromEpisode(Current)!=null;
        public LearningStep[] Steps=>LearningCatalog.Steps(Current);
        public string Notice {get;private set;}="";
        public bool SaveFailed=>!IsExample&&Current!=null&&saveResults.TryGetValue(Current.id,out string result)&&result.StartsWith("保存失败");
        public string RecordWarning=>IsExample||Current==null?"":
            (SaveFailed?"保存未完成，退出前请重试；文件可能未写完整。\n":"")+
            (!LearningHistory.Complete(Current)?"记录不完整：仅展示已有片段，不能用于 A/B 对比。":"");
        public void RetrySaveCurrent()
        {
            if(!SaveFailed)return;
            try
            {
                if(Path.GetFileName(Current.id)!=Current.id||Current.id.IndexOfAny(Path.GetInvalidFileNameChars())>=0)throw new IOException("无效的记录文件名");
                saveResults[Current.id]=new TacticalJournal(Current).Save(session.ReviewFolder);
                Notice="本轮已完整保存到 Replays。";
            }
            catch(Exception error){saveResults[Current.id]="保存失败："+error.Message;Notice="保存仍未完成，请检查文件夹是否可写。";}
        }
        public LearningHub(TacticalSession owner)
        {session=owner;History.Load(owner.ReviewFolder);IsOpen=true;LoadLesson();}
        void SetCurrent(TacticalEpisode episode,bool example,float time=0)
        {Current=episode;IsExample=example;Cursor.Reset(episode,-1);Cursor.Seek(time);}
        void LoadLesson()=>SetCurrent(LearningCatalog.Load(Scenario),true);
        public void Show(LearningPage page)
        {
            session.OpenMenu();Page=page;Notice="";
            if(page==LearningPage.Lesson)LoadLesson();
            else if(page==LearningPage.Challenge)SelectQuestion(QuestionIndex);
            else if(page==LearningPage.Review){if(History.Entries.Count>0)SelectHistory(0);else SetCurrent(null,false);}
            else {if(Baseline!=null)Scenario=LearningScenario.FromEpisode(Baseline);SetCompareSide(Candidate!=null);}
        }
        public void SelectScenario(LearningScenario scenario)
        {Scenario=scenario.Copy().Normalize();if(Page==LearningPage.Lesson)LoadLesson();}
        public void StartPractice()
        {
            if(!CanPractice){Notice="这份旧记录缺少可重开的配置，请从战术演练选择新练习。";return;}
            var config=Scenario.Copy().Normalize();
            string objective=Page==LearningPage.Challenge?Question.title:LearningCatalog.Goal(config.drill);
            session.StartScenario(config.drill,config.controlled==1,config.coverage,config.seed);
            session.Journal.Episode.learningObjective=objective;
        }
        public void PracticeStarted()
        {IsOpen=false;Cursor.Seek(Cursor.Time);Scenario=new LearningScenario{drill=session.Drill,controlled=session.Controlled,coverage=session.CoverageChoice,seed=session.Seed}.Normalize();}
        public void MenuOpened(){IsOpen=true;Cursor.Seek(Cursor.Time);}
        public void ResumePractice(){IsOpen=false;Cursor.Seek(Cursor.Time);}
        public void Remember(TacticalEpisode episode,string saveResult=null)
        {
            if(episode!=null&&!string.IsNullOrEmpty(episode.id)&&saveResult!=null)saveResults[episode.id]=saveResult;
            if(!History.Add(episode))return;
            if(Baseline!=null&&LearningHistory.CanCompare(Baseline,episode,out _))Candidate=History.Entries[0];
        }
        public void ReviewCurrent(TacticalEpisode episode)
        {
            Remember(episode);int index=History.Entries.ToList().FindIndex(e=>e.id==episode.id);
            if(index>=0)SelectHistory(index);
        }
        public void SelectHistory(int index)
        {
            if(index<0||index>=History.Entries.Count)return;
            IsOpen=true;Page=LearningPage.Review;var e=History.Entries[index];
            SetCurrent(e,false);int decision=TacticalJournal.Build(e).decisionIndex;Cursor.Reset(e,decision);
            var config=LearningScenario.FromEpisode(e);if(config!=null)Scenario=config;
            Notice=config==null?"旧记录可观看，但缺少可重开的场景配置。":"实际练习记录；时间轴与传球分析同步。";
        }
        public void SelectQuestion(int index)
        {
            QuestionIndex=Mathf.Clamp(index,0,Questions.Length-1);Page=LearningPage.Challenge;IsOpen=true;
            Scenario=Question.scenario.Copy().Normalize();SetCurrent(LearningCatalog.Load(Scenario),true,Question.time);Notice="";
        }
        public void ChooseAnswer(int index){if(index>=0&&index<3&&Answer<0)answers[Question.id]=index;}
        public void ResetAnswer(){answers.Remove(Question.id);}
        public void ShowQuestionLesson(){Page=LearningPage.Lesson;Scenario=Question.scenario.Copy().Normalize();LoadLesson();Seek(Question.time);}
        public void PinBaseline()
        {
            if(IsExample||!LearningHistory.Valid(Current)){Notice="请先完成一次实际练习，并在复盘中设为 A。";return;}
            if(!LearningHistory.Complete(Current)||LearningScenario.FromEpisode(Current)==null){Notice="这份记录不完整或缺少配置，不能作为对比开局。";return;}
            Baseline=Current;Candidate=null;Notice="已设为 A；使用相同开局再练一次，可改变防守或自己的操作。";
        }
        public void UseBaselineStart()
        {
            if(Baseline==null){Notice="请先在实战复盘中设定 A。";return;}
            Scenario=LearningScenario.FromEpisode(Baseline);Page=LearningPage.WhatIf;SetCompareSide(false);
        }
        public void SetCompareSide(bool candidate)
        {
            float time=Cursor.Time;SetCurrent(candidate?Candidate:Baseline,false,time);
            LearningHistory.CanCompare(Baseline,Candidate,out string reason);Notice=reason;
        }
        public void Seek(float time)=>Cursor.Seek(time);
        public void Tick(float seconds){if(IsOpen&&Page!=LearningPage.Challenge)Cursor.Tick(seconds);}
    }
}
