using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
namespace CourtX
{
    [Serializable] public sealed class FiveRecording
    {
        public string schema="courtx.five.v1",id=Guid.NewGuid().ToString("N"),name="",kind="actual",created="",endReason="";
        public List<FiveState> frames=new List<FiveState>();
        public float Start=>frames.Count>0?frames[0].time:0;
        public float End=>frames.Count>0?frames[frames.Count-1].time:0;
        public string PlayTitle=>!string.IsNullOrWhiteSpace(name)?name:
            frames.Count==0?"练习":frames[0].playbookVersion==2?FiveZoneBook.PlayName(frames[0].play):FiveRules.Plays[(int)frames[0].play];
        [NonSerialized] DateTime? legacyDateReference;
        static readonly string[] DateFormats={"yyyy-MM-dd HH:mm:ss","yyyy-MM-dd HH:mm"};
        public DateTime? CreatedAt(DateTime reference)
        {
            if(DateTime.TryParseExact(created,DateFormats,CultureInfo.InvariantCulture,DateTimeStyles.None,out var full))return full;
            // Older recordings omit the year. Resolve at most one calendar rollover.
            reference=legacyDateReference??reference;
            for(int year=reference.Year;year>=Math.Max(1,reference.Year-1);year--)
                if(DateTime.TryParseExact(year.ToString("0000",CultureInfo.InvariantCulture)+"-"+created,
                    DateFormats,CultureInfo.InvariantCulture,DateTimeStyles.None,out var legacy)&&legacy.Date<=reference.Date)return legacy;
            return null;
        }
        public string CreatedClock(DateTime reference)
        {
            bool seconds=created!=null&&created.IndexOf(':')!=created.LastIndexOf(':');
            return CreatedAt(reference)?.ToString(seconds?"HH:mm:ss":"HH:mm",CultureInfo.InvariantCulture)??"时间未知";
        }
        public string CooperationOutcome
        {
            get
            {
                var final=frames.Count==0?null:frames[frames.Count-1];
                if(final?.madeBasket==true)
                {
                    var shot=frames.FindLast(frame=>frame.flight?.shot==true)?.flight;
                    return shot==null?"投篮命中":FiveRules.Number(shot.from)+" 命中";
                }
                if(endReason=="已完成一次战术决策"||final?.success==true)return "协作达成";
                if(endReason=="传球被封堵"||final?.turnover==true)return "协作中断";
                return "练习结束";
            }
        }
        public const int MaxFrames=6000;
        public void Add(FiveState s,bool force=false)
        {
            if(s==null||frames.Count>=MaxFrames)return;
            if(frames.Count>0)
            {
                var last=frames[frames.Count-1];
                if(s.time<last.time-.0001f)throw new InvalidOperationException("Record time cannot move backward");
                if(Mathf.Abs(s.time-last.time)<.0001f){if(force)frames[frames.Count-1]=s.Copy();return;}
                if(!force&&s.time-last.time<.099f&&s.eventSerial==last.eventSerial)return;
            }
            frames.Add(s.Copy());
        }
        public FiveRecording Copy()
        {var r=(FiveRecording)MemberwiseClone();r.frames=frames.Select(f=>f.Copy()).ToList();return r;}
        static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        static bool Point(Vector2 p)=>Finite(p.x)&&Finite(p.y)&&Mathf.Abs(p.x)<20&&Mathf.Abs(p.y)<30;
        public static bool Valid(FiveRecording r)
        {
            if(r==null||r.schema!="courtx.five.v1"||r.frames==null||r.frames.Count==0||r.frames.Count>MaxFrames||string.IsNullOrEmpty(r.id)||r.id.Length>80||r.id.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'))return false;
            if(r.kind!="actual"&&r.kind!="lesson"&&r.kind!="whatif")return false;
            float prior=-1;
            foreach(var s in r.frames)
            {
                if(s==null||!Finite(s.time)||s.time<prior||s.time<0||s.time>3600||!Finite(s.phaseAge)||!Finite(s.passCooldown)||!Finite(s.requestUntil))return false;prior=s.time;
                if((int)s.play<0||(int)s.play>2||(int)s.defense<0||(int)s.defense>2||s.controlled<0||s.controlled>4||s.holder< -1||s.holder>9||s.phase<0||s.screener>4||s.request< -1||s.request>4)return false;
                if(s.playbookVersion==2)
                {
                    if(s.branch<0||s.branch>2||s.phase>=FiveZoneBook.Count(s.play,s.branch)||s.screener< -1||s.routePoint<0||!Finite(s.settledAge)||s.settledAge<0)return false;
                }
                else if(s.playbookVersion!=0||s.phase>4||s.screener<0)return false;
                if(s.positions==null||s.positions.Length!=10||s.velocities==null||s.velocities.Length!=10||s.targets==null||s.targets.Length!=10||s.facing==null||s.facing.Length!=10||s.pinned==null||s.pinned.Length!=10||s.tasks==null||s.tasks.Length!=10)return false;
                if(s.positions.Any(p=>!Point(p))||s.velocities.Any(p=>!Point(p))||s.targets.Any(p=>!Point(p))||s.facing.Any(p=>!Point(p))||!Point(s.looseBall)||!Point(s.screenPoint))return false;
                if(s.tasks.Any(t=>t!=null&&t.Length>2000))return false;
                var f=s.flight;
                if(f==null)continue;
                bool validTarget=f.shot?f.to==-1&&Vector2.Distance(f.destination,FiveFlight.Rim)<.01f:f.to>=0&&f.to<5&&f.from!=f.to;
                if(s.holder!=-1||f.from<0||f.from>4||!validTarget||!Point(f.origin)||!Point(f.destination)||!Finite(f.elapsed)||!Finite(f.duration)||f.duration<=0||f.duration>10||f.elapsed<0||f.elapsed>f.duration+.06f)return false;
            }
            return true;
        }
        public void Save(string folder)
        {
            if(!Valid(this))throw new InvalidDataException("回放状态不完整，未写入文件。");
            Directory.CreateDirectory(folder);string path=Path.Combine(folder,id+".json"),temp=path+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(this));
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }
        public static FiveRecording Read(string path)
        {
            if(new FileInfo(path).Length>45*1024*1024)throw new InvalidDataException("回放文件过大。");
            var r=JsonUtility.FromJson<FiveRecording>(File.ReadAllText(path));
            if(!Valid(r))throw new InvalidDataException("回放文件包含无效状态。");
            r.legacyDateReference=File.GetLastWriteTime(path);return r;
        }
        public static FiveRecording Lesson(FivePlay play,FiveDefense defense,int role,int holder)
            => RecordLesson(FiveRules.Create(play,defense,role,holder));
        public static FiveRecording GuidedLesson(FivePlay play,int role,int branch=0)
            => RecordLesson(FiveCoach.Create(play,role,branch));

        static FiveRecording RecordLesson(FiveState initial)
        {
            const int rate=60;
            float deadline=initial.playbookVersion==2?90:25;
            var state=initial.Copy();
            state.demo=true;
            var lesson=new FiveRecording{name=state.playbookVersion==2?FiveZoneBook.PlayName(state.play):FiveRules.Plays[(int)state.play],kind="lesson",created="教学演练"};
            lesson.Add(state,true);

            float start=state.time;
            int ticks=0;
            int deadlineTick=Mathf.Max(0,Mathf.RoundToInt((deadline-start)*rate));
            void Tick(int heldActor=-1)
            {
                FiveRules.Step(state,1f/rate,heldActor);
                state.time=start+(++ticks)/(float)rate;
                lesson.Add(state);
            }

            // A pass can be marked successful at release and still be intercepted.
            // After the deadline, only an existing flight can keep the lesson running.
            while(!state.turnover && (!state.success || state.flight!=null) &&
                  (ticks<deadlineTick || state.flight!=null))
            {
                // Step advances the clock before deciding whether to release a pass.
                bool atDeadline=ticks+1==deadlineTick&&state.flight==null;
                Tick(atDeadline?state.holder:-1);
            }

            int tailTicks=0;
            if(state.turnover)
            {
                lesson.endReason="演练结束 · 配合被截断";
                tailTicks=72;
            }
            else if(state.success)
            {
                lesson.endReason=state.madeBasket?"演练结束 · 投篮命中":"演练结束 · 配合达成";
                tailTicks=90;
            }
            else lesson.endReason="演练结束 · 未形成机会";

            // Let the settled pose develop while reserving possession and blocking another pass.
            int holder=state.holder;
            for(int i=0;i<tailTicks;i++)Tick(holder);
            lesson.Add(state,true);
            return lesson;
        }
        public FiveState Sample(float time)
        {
            time=Mathf.Clamp(time,Start,End);int lo=0,hi=frames.Count-1;
            while(lo<hi){int mid=(lo+hi+1)/2;if(frames[mid].time<=time)lo=mid;else hi=mid-1;}
            var a=frames[lo];var s=a.Copy();float elapsed=time-a.time;
            if(lo+1<frames.Count)
            {
                var b=frames[lo+1];float u=Mathf.InverseLerp(a.time,b.time,time);
                for(int i=0;i<10;i++){s.positions[i]=Vector2.Lerp(a.positions[i],b.positions[i],u);s.velocities[i]=Vector2.Lerp(a.velocities[i],b.velocities[i],u);s.facing[i]=Vector2.Lerp(a.facing[i],b.facing[i],u).normalized;}
            }
            s.time=time;s.phaseAge+=elapsed;s.passCooldown-=elapsed;
            if(s.flight!=null)s.flight.elapsed=Mathf.Min(s.flight.duration,s.flight.elapsed+elapsed);
            return s;
        }
        public float[] Nodes()
        {
            var result=new List<float>{Start};int serial=frames[0].eventSerial,phase=frames[0].phase;
            for(int i=1;i<frames.Count;i++)if(frames[i].eventSerial!=serial||frames[i].phase!=phase)
            {result.Add(frames[i].time);serial=frames[i].eventSerial;phase=frames[i].phase;}
            if(End>result[result.Count-1]+.1f)result.Add(End);return result.ToArray();
        }
    }
}
