using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace CourtX
{
    public sealed class LearningMetrics
    {public int open,risky,caught,missed,pending;}
    public sealed class LearningHistory
    {
        public IReadOnlyList<TacticalEpisode> Entries=>entries;
        readonly List<TacticalEpisode> entries=new List<TacticalEpisode>();
        public int SkippedFiles {get;private set;}
        public bool Add(TacticalEpisode episode)
        {
            if(!Valid(episode)||entries.Any(e=>e.id==episode.id))return false;
            entries.Insert(0,JsonUtility.FromJson<TacticalEpisode>(JsonUtility.ToJson(episode)));
            if(entries.Count>30)entries.RemoveAt(entries.Count-1);return true;
        }
        public void Load(string folder)
        {
            SkippedFiles=0;if(!Directory.Exists(folder))return;
            try
            {
                // Process oldest to newest so the newest ends up first; bound startup work.
                var files=new DirectoryInfo(folder).GetFiles("*.json").OrderByDescending(f=>f.LastWriteTimeUtc).Take(60).Reverse();
                foreach(var file in files)
                {
                    try
                    {
                        if(file.Length>4*1024*1024){SkippedFiles++;continue;}
                        var episode=JsonUtility.FromJson<TacticalEpisode>(File.ReadAllText(file.FullName));
                        if(!Valid(episode)){SkippedFiles++;continue;}Add(episode);
                    }
                    catch(Exception error) when(error is IOException||error is UnauthorizedAccessException||error is ArgumentException){SkippedFiles++;}
                }
            }
            catch(Exception error) when(error is IOException||error is UnauthorizedAccessException){SkippedFiles++;}
        }
        static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
        // A halfcourt recording cannot legitimately contain positions hundreds of metres away.
        // Bound coordinates before line lengths are computed, including overflow-prone corrupt files.
        static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z)&&Mathf.Abs(v.x)<=100&&Mathf.Abs(v.y)<=100&&Mathf.Abs(v.z)<=100;
        public static bool Valid(TacticalEpisode e)
        {
            if(e==null||string.IsNullOrEmpty(e.id)||e.controlled<0||e.controlled>1||!Finite(e.duration)||e.duration<0||
                e.frames==null||e.frames.Count==0||e.frames.Count>1800||e.decisions==null||e.decisions.Count>500||e.events==null||e.events.Count>1000)return false;
            float prior=-1;
            foreach(var f in e.frames)
            {
                if(f==null||!Finite(f.time)||f.time<prior||f.time<0||f.positions==null||f.positions.Length!=6||!f.positions.All(Finite)||
                    f.marks==null||f.marks.Length!=3||f.marks.Any(x=>x<0||x>2)||f.holder< -1||f.holder>5||!Finite(f.ball))return false;
                prior=f.time;
            }
            if(e.duration+.001f<e.frames[e.frames.Count-1].time)return false;
            prior=-1;
            foreach(var d in e.decisions)
            {
                if(d==null||!Finite(d.time)||d.time<0||d.time>e.duration+.001f||d.time<prior||d.from<0||d.from>2||d.to<0||d.to>2||d.from==d.to||
                    d.options==null||d.options.Length!=2||d.options.Any(o=>o==null||o.receiver<0||o.receiver>2||o.receiver==d.from||!Finite(o.space)||!Finite(o.distance))||
                    d.options.Select(o=>o.receiver).Distinct().Count()!=2||!Finite(d.origin)||!Finite(d.destination)||!Enum.IsDefined(typeof(PassOutcome),d.outcome))return false;
                prior=d.time;
            }
            return e.events.All(x=>x!=null&&Finite(x.time));
        }
        public static bool CanCompare(TacticalEpisode a,TacticalEpisode b,out string reason)
        {
            reason="先保存两次实际练习，再选择 A / B。";
            if(!Valid(a)||!Valid(b))return false;
            if(a.id==b.id){reason="A 和 B 是同一次练习，请再练一轮。";return false;}
            if(a.truncated||b.truncated){reason="记录触及上限，不能作为完整回合对比。";return false;}
            if(a.drill!=b.drill||a.controlled!=b.controlled||a.seed!=b.seed){reason="需要相同战术、角色和种子；可以改变防守或自己的操作。";return false;}
            var x=a.frames[0];var y=b.frames[0];
            if(Mathf.Abs(x.time-y.time)>.001f||x.holder!=y.holder||Vector3.Distance(x.ball,y.ball)>.01f||
                Enumerable.Range(0,6).Any(i=>Vector3.Distance(x.positions[i],y.positions[i])>.01f))
            {reason="初始站位或球权不同，请从同一个开局重练。";return false;}
            reason="相同开局的两次实际练习；人的操作可能不同，结果不只由防守方式决定。";return true;
        }
        public static LearningMetrics Summary(TacticalEpisode episode)
        {
            var result=new LearningMetrics();if(episode?.decisions==null)return result;
            foreach(var d in episode.decisions)
            {
                if(d.options.Any(o=>o.receiver==d.to&&o.open))result.open++;else result.risky++;
                if(d.outcome==PassOutcome.Caught)result.caught++;else if(d.outcome==PassOutcome.Pending)result.pending++;else result.missed++;
            }
            return result;
        }
    }
}
