using System;
using System.Collections.Generic;
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
                if((int)s.play<0||(int)s.play>2||(int)s.defense<0||(int)s.defense>2||s.controlled<0||s.controlled>4||s.holder< -1||s.holder>9||s.phase<0||s.phase>4||s.screener<0||s.screener>4||s.request< -1||s.request>4)return false;
                if(s.positions==null||s.positions.Length!=10||s.velocities==null||s.velocities.Length!=10||s.targets==null||s.targets.Length!=10||s.facing==null||s.facing.Length!=10||s.pinned==null||s.pinned.Length!=10||s.tasks==null||s.tasks.Length!=10)return false;
                if(s.positions.Any(p=>!Point(p))||s.velocities.Any(p=>!Point(p))||s.targets.Any(p=>!Point(p))||s.facing.Any(p=>!Point(p))||!Point(s.looseBall)||!Point(s.screenPoint))return false;
                if(s.tasks.Any(t=>t!=null&&t.Length>2000))return false;
                var f=s.flight;if(f!=null&&(s.holder!=-1||f.from<0||f.from>4||f.to<0||f.to>4||f.from==f.to||!Point(f.origin)||!Point(f.destination)||!Finite(f.elapsed)||!Finite(f.duration)||f.duration<=0||f.duration>10||f.elapsed<0||f.elapsed>f.duration+.06f))return false;
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
            if(!Valid(r))throw new InvalidDataException("回放文件包含无效状态。");return r;
        }
        public static FiveRecording Lesson(FivePlay play,FiveDefense defense,int role,int holder)
        {
            var r=new FiveRecording{name=FiveRules.Plays[(int)play],kind="lesson",created="教学演练",endReason="教学演练结束"};
            var s=FiveRules.Create(play,defense,role,holder);s.demo=true;r.Add(s,true);
            for(int i=0;i<1500;i++){FiveRules.Step(s,1f/60);r.Add(s);}
            r.Add(s,true);return r;
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
