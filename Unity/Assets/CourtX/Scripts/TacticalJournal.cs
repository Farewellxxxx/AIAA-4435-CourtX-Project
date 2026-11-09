using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace CourtX
{
    public enum PassOutcome { Pending, Caught, Missed, Intercepted, Deflected }
    public enum DecisionAssessment { NoDecision, OpenLane, RiskyLane }
    [Serializable] public sealed class PassOption
    {public int receiver;public bool open;public float space,distance;public string reason;}
    [Serializable] public sealed class TacticalFrame
    {public float time;public Vector3[] positions;public int[] marks;public int holder;public Vector3 ball;public string phase,defense;public bool help;}
    [Serializable] public sealed class PassDecision
    {public float time,windowAge,pressure,passSpeed;public int from,to,frame;public Vector3 origin,destination;public PassOption[] options;public PassOutcome outcome;}
    [Serializable] public sealed class TacticalEvent
    {public float time;public string kind,text;}
    [Serializable] public sealed class TacticalEpisode
    {
        public string id,drill,coverage,endReason;public int seed,controlled;public float duration;public bool truncated;
        public List<TacticalFrame> frames=new List<TacticalFrame>();
        public List<PassDecision> decisions=new List<PassDecision>();
        public List<TacticalEvent> events=new List<TacticalEvent>();
    }
    public sealed class RecapReport
    {public DecisionAssessment assessment;public PassOutcome outcome;public int decisionIndex=-1,alternative=-1;public string text="";}
    public sealed class TacticalJournal
    {
        public TacticalEpisode Episode { get; private set; }
        public bool Closed { get; private set; }
        float nextSample;
        int sampledHolder=-2;
        float sampledSpeed;
        readonly float[] windowStart={-1,-1,-1};
        string priorDefense="";
        public TacticalJournal(TacticalEpisode episode){Episode=episode;}
        public void AddDecision(PassDecision decision)
        {
            if(Closed)return;
            if(Episode.decisions.Count>=500){Episode.truncated=true;return;}
            Episode.decisions.Add(JsonUtility.FromJson<PassDecision>(JsonUtility.ToJson(decision)));
        }
        public void Event(float time,string kind,string text)
        {
            if(Closed)return;
            if(Episode.events.Count>=1000){Episode.truncated=true;return;}
            Episode.events.Add(new TacticalEvent{time=time,kind=kind,text=text});
        }
        public TacticalFrame Capture(TacticalSession s)
        {
            return new TacticalFrame{time=s.ActiveTime,positions=Enumerable.Range(0,6).Select(s.Position).ToArray(),
                marks=(int[])s.Tactics.Marks.Clone(),holder=s.ball.Holder,ball=s.ball.Position,phase=s.Phase.ToString(),
                defense=s.Tactics.Stage.ToString(),help=s.Tactics.HelpActive};
        }
        int AddFrame(TacticalSession s)
        {
            if(Episode.frames.Count>=1800){Episode.truncated=true;return -1;}
            Episode.frames.Add(Capture(s));return Episode.frames.Count-1;
        }
        public void Sample(TacticalSession s)
        {
            if(Closed)return;
            Episode.duration=s.ActiveTime;
            string state=s.Tactics.Stage+"/"+s.Tactics.HelpActive+"/"+string.Join(",",s.Tactics.Marks);
            if(state!=priorDefense)
            {
                if(priorDefense!="")Event(s.ActiveTime,"defense",s.CoverageName+"："+
                    (s.Tactics.Stage==DefenseStage.Engaged?"掩护防守启动":s.Tactics.Stage==DefenseStage.Recover?"防守回位":"保持盯防")+
                    (s.Tactics.HelpActive?"，13 进入协防":"，13 负责弱侧区域")+
                    "；11→"+s.Number(s.Tactics.Marks[0])+"，12→"+s.Number(s.Tactics.Marks[1]));
                priorDefense=state;
            }
            if(s.ActiveTime<nextSample)return;nextSample=s.ActiveTime+.2f;
            AddFrame(s);
            float speed=s.ball.Holder==s.Controlled?s.ball.ReleaseSpeed:8f;
            if(sampledHolder!=s.ball.Holder||Mathf.Abs(sampledSpeed-speed)>.001f)
                for(int i=0;i<3;i++)windowStart[i]=-1;
            sampledHolder=s.ball.Holder;sampledSpeed=speed;
            for(int i=0;i<3;i++)
            {
                string reason;bool open=s.CanPass(s.ball.Holder,i,out reason,speed);
                if(!open)windowStart[i]=-1;else if(windowStart[i]<0)windowStart[i]=s.ActiveTime;
            }
        }
        public void RecordPass(TacticalSession s,int from,int to,float speed)
        {
            if(Closed)return;
            var options=new List<PassOption>();
            for(int i=0;i<3;i++)if(i!=from)
            {
                string why;bool open=s.CanPass(from,i,out why,speed);
                options.Add(new PassOption{receiver=i,open=open,reason=why,space=s.ReceiverSpace(i),distance=TacticalRules.Distance(s.Position(from),s.Position(i))});
            }
            var plan=s.ball.PlanPass(from,to,speed);
            bool currentOpen=options.Any(o=>o.receiver==to&&o.open);
            AddDecision(new PassDecision{time=s.ActiveTime,from=from,to=to,frame=AddFrame(s),options=options.ToArray(),
                origin=plan.Origin,destination=plan.Destination,pressure=s.HolderPressure(from),passSpeed=speed,
                windowAge=currentOpen&&s.ball.Holder==from&&sampledHolder==from&&Mathf.Abs(sampledSpeed-speed)<=.001f&&windowStart[to]>=0?s.ActiveTime-windowStart[to]:-1});
            Event(s.ActiveTime,"pass",s.Number(from)+" → "+s.Number(to)+" 出球");
        }
        public void Outcome(float time,PassOutcome outcome,int actor=-1)
        {
            if(Closed||Episode.decisions.Count==0)return;
            var decision=Episode.decisions.Last();
            if(decision.outcome!=PassOutcome.Pending)return;
            decision.outcome=outcome;
            Event(time,"outcome",OutcomeText(outcome)+(actor>=0?"（"+Number(actor)+"）":""));
        }
        public void Finish(float time,string reason)
        {if(Closed)return;Episode.duration=time;Episode.endReason=reason;Closed=true;}
        public string Save(string folder)
        {
            Directory.CreateDirectory(folder);
            string name=Path.Combine(folder,Episode.id);
            File.WriteAllText(name+".json",JsonUtility.ToJson(Episode,true));
            File.WriteAllText(name+".txt",Build(Episode).text);
            return name+".json";
        }
        static string Number(int id)=>new[]{"01","04","05","11","12","13"}[Mathf.Clamp(id,0,5)];
        public static string OutcomeText(PassOutcome value)=>new[]{"尚未完成接球","接稳了传球","传球落空","防守人接住了球","球碰到防守人后弹开"}[(int)value];
        public static RecapReport Build(TacticalEpisode episode)
        {
            int index=episode.decisions.FindLastIndex(d=>d.from==episode.controlled);
            if(index<0)index=episode.decisions.Count-1;
            return Build(episode,index);
        }
        public static RecapReport Build(TacticalEpisode episode,int decisionIndex)
        {
            var result=new RecapReport();
            string heading="本地规则复盘 · "+episode.coverage+" · 种子 "+episode.seed+"\n";
            bool selected=decisionIndex>=0&&decisionIndex<episode.decisions.Count;
            float releaseTime=selected?episode.decisions[decisionIndex].time:episode.decisions.Count==0?episode.duration:0;
            var change=episode.events.LastOrDefault(e=>e.kind=="defense"&&e.time<=releaseTime);
            string defense=change==null?"本段没有记录到协防或掩护防守切换。":change.time.ToString("F1")+" 秒："+change.text+"。";
            if(!selected)
            {
                result.text=heading+(episode.decisions.Count==0?"本回合尚未发生传球。":"当前回放时刻尚未出手；播放到出手时会显示对应评语。")+"\n"+defense+"\n可以先用横移和掩护观察防守，再选择传球；无球时可按 E 要球。\n未出球的选择没有被判为失误。";
                if(episode.truncated)result.text+="\n较长回合已达到记录上限，部分后续片段未记录。";
                return result;
            }
            int index=decisionIndex;
            var d=episode.decisions[index];var chosen=d.options.FirstOrDefault(o=>o.receiver==d.to);
            result.decisionIndex=index;result.outcome=d.outcome;
            result.assessment=chosen!=null&&chosen.open?DecisionAssessment.OpenLane:DecisionAssessment.RiskyLane;
            var alternate=d.options.Where(o=>o.receiver!=d.to&&o.open&&o.space>=1).OrderByDescending(o=>o.space).FirstOrDefault();
            if(alternate!=null)result.alternative=alternate.receiver;
            string actor=d.from==episode.controlled?"你":"队友 "+Number(d.from);
            string choice=result.assessment==DecisionAssessment.OpenLane?"出手时预测传球线较清楚；接球结果单独记录。":"出手时预测传球存在风险"+(chosen==null?"。":"："+chosen.reason+"。");
            string alternative=alternate!=null?"另一种选择：当时 "+Number(alternate.receiver)+" 的预测线路也开放，可再比较接球空间。":"下一次可先横移改变角度，再观察新的接球窗口。";
            result.text=heading+"出手前防守："+defense+"\n\n"+d.time.ToString("F1")+" 秒："+actor+" → "+Number(d.to)+"。\n"+choice+"\n该次传球最终结果："+OutcomeText(d.outcome)+"。"+
                (result.assessment==DecisionAssessment.OpenLane&&d.windowAge>=0?" 出手前窗口已开放约 "+d.windowAge.ToString("F1")+" 秒（同球速、0.2 秒采样）。":"")+
                "\n\n"+alternative+"\n备选是规则估计，未模拟结果；本原型不评估投篮得分。";
            if(episode.truncated)result.text+="\n较长回合已达到记录上限，部分后续片段未记录。";
            return result;
        }
    }
    // Implementations receive an immutable serialized fact record; no Unity object or API key is stored here.
    public interface ITacticalRecapProvider
    {Task<string> GenerateAsync(string episodeJson,string localReview,CancellationToken cancellation);}
    public sealed class RecapRequestGate
    {
        volatile int version;
        CancellationTokenSource source=new CancellationTokenSource();
        public void Reset(){version++;source.Cancel();source.Dispose();source=new CancellationTokenSource();}
        public async Task<string> GenerateAsync(ITacticalRecapProvider provider,string json,string local)
        {
            int request=version;var token=source.Token;
            if(provider==null)return local;
            try
            {
                string response=await provider.GenerateAsync(json,local,token).ConfigureAwait(false);
                if(request!=version||token.IsCancellationRequested)return null;
                return string.IsNullOrWhiteSpace(response)?local:response.Substring(0,Math.Min(response.Length,2400));
            }
            catch(OperationCanceledException){return null;}
        }
    }
}
