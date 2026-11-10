using System;
using System.IO;
using UnityEngine;
namespace CourtX
{
    public sealed partial class TacticalSession
    {
        public TacticalJournal Journal { get; private set; }
        public RecapReport Review { get; private set; }
        public RecapPlayback Replay { get; } = new RecapPlayback();
        public bool ReviewOpen { get; private set; }
        public string SavedReview { get; private set; }="";
        public string OptionalNarration { get; private set; }="";
        public ITacticalRecapProvider RecapProvider { get; set; }
        readonly RecapRequestGate recapGate=new RecapRequestGate();
        public string ReviewFolder=>Application.isEditor?Path.GetFullPath(Path.Combine(Application.dataPath,"../../Preview/Playtests/Learning-20261111/Sessions")):
            Path.Combine(Path.GetDirectoryName(Application.dataPath),"Replays");
        void ArchivePriorRound()
        {
            recapGate.Reset();ReviewOpen=false;OptionalNarration="";Replay.Reset(null,-1);
            if(Journal!=null&&!Journal.Closed&&ActiveTime>.05f)
            {Journal.Finish(ActiveTime,"重开前的练习片段");SaveJournal();}
        }
        void BeginJournal()
        {
            Journal=new TacticalJournal(new TacticalEpisode{id=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8),
                drill=Drill.ToString(),coverage=CoverageName,seed=Seed,controlled=Controlled,hasScenarioConfig=true,coverageChoice=(int)CoverageChoice});
            SavedReview="";Review=null;Journal.Sample(this);
        }
        void LateUpdate()
        {
            if(initialized&&Running)Journal?.Sample(this);
            if(ReviewOpen){Replay.Tick(Time.unscaledDeltaTime);RefreshReviewSelection();}
            learning?.Tick(Time.unscaledDeltaTime);
        }
        public void RefreshReviewSelection()
        {
            if(ReviewOpen&&Journal!=null&&(Review==null||Review.decisionIndex!=Replay.DecisionIndex))
                Review=TacticalJournal.Build(Journal.Episode,Replay.DecisionIndex);
        }
        void SaveJournal()
        {
            try{SavedReview=Journal.Save(ReviewFolder);}
            catch(Exception error){SavedReview="保存失败："+error.Message;Debug.LogWarning(SavedReview);}
            learning?.Remember(Journal.Episode,SavedReview);
        }
        public void OpenReview()
        {
            if(!initialized||Journal==null)return;
            Journal.Sample(this);Journal.Finish(ActiveTime,Phase==TacticalPhase.Complete?"配合完成":Phase==TacticalPhase.Turnover?Message:"主动结束练习");
            Review=TacticalJournal.Build(Journal.Episode);SaveJournal();
            OpenMenu();MenuOpen=false;ReviewOpen=true;
            Replay.Reset(Journal.Episode,Review.decisionIndex);TacticalRecapUI.Reset();
            RequestOptionalNarration();
            learning?.ReviewCurrent(Journal.Episode);
        }
        async void RequestOptionalNarration()
        {
            if(RecapProvider==null)return;
            string id=Journal.Episode.id;OptionalNarration="拓展解说生成中；本地复盘已可使用。";
            try
            {
                var response=await recapGate.GenerateAsync(RecapProvider,JsonUtility.ToJson(Journal.Episode),Review.text);
                if(response!=null&&this&&ReviewOpen&&Journal.Episode.id==id)OptionalNarration=response;
            }
            catch(Exception){if(this&&ReviewOpen&&Journal.Episode.id==id)OptionalNarration="拓展解说暂不可用；保留本地事实复盘。";}
        }
        void OnDestroy(){recapGate.Reset();}
    }
}
