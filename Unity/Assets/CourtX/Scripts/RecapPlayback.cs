using UnityEngine;
namespace CourtX
{
    public sealed class RecapPlayback
    {
        TacticalEpisode episode;
        public float Time { get; private set; }
        public float StartTime => HasFrames ? episode.frames[0].time : 0;
        public float EndTime => HasFrames ? episode.frames[episode.frames.Count-1].time : 0;
        bool HasFrames => episode!=null && episode.frames.Count>0;
        public float Speed { get; set; } = 1;
        public bool Playing { get; private set; }
        public int DecisionIndex { get; private set; } = -1;
        public int FrameIndex { get; private set; } = -1;
        public void Reset(TacticalEpisode value, int decisionIndex)
        {
            episode=value;Playing=false;Speed=1;
            SetTime(value!=null && decisionIndex>=0 && decisionIndex<value.decisions.Count ? value.decisions[decisionIndex].time : StartTime);
        }
        public void Seek(float time) { Playing=false;SetTime(time); }
        void SetTime(float time)
        {
            Time=HasFrames ? Mathf.Clamp(time,StartTime,EndTime) : 0;
            FrameIndex=-1;DecisionIndex=-1;
            if(!HasFrames)return;
            // Exact duplicates may contain a release state after the regular sample.
            for(int i=0;i<episode.frames.Count && episode.frames[i].time<=Time;i++)FrameIndex=i;
            for(int i=0;i<episode.decisions.Count && episode.decisions[i].time<=Time;i++)DecisionIndex=i;
        }
        public void Toggle()
        {
            if(!HasFrames || EndTime<=StartTime){Playing=false;return;}
            if(!Playing && Time>=EndTime)SetTime(StartTime);
            Playing=!Playing;
        }
        public void Tick(float seconds)
        {
            if(!Playing)return;
            SetTime(Time+Mathf.Max(0,seconds)*Mathf.Clamp(Speed,.25f,2));
            if(Time>=EndTime)Playing=false;
        }
        public void Jump(int direction)
        {
            Playing=false;if(episode==null)return;
            if(direction<0)
            {
                for(int i=episode.decisions.Count-1;i>=0;i--)
                    if(episode.decisions[i].time<Time && episode.decisions[i].time>=StartTime)
                    {SetTime(episode.decisions[i].time);return;}
            }
            else foreach(var d in episode.decisions)
                if(d.time>Time && d.time<=EndTime){SetTime(d.time);return;}
        }
        public TacticalFrame Sample()
        {
            if(FrameIndex<0)return null;
            var a=episode.frames[FrameIndex];var b=episode.frames[Mathf.Min(FrameIndex+1,episode.frames.Count-1)];
            float blend=b.time>a.time ? Mathf.Clamp01((Time-a.time)/(b.time-a.time)) : 0;
            var positions=(Vector3[])a.positions.Clone();
            for(int i=0;i<positions.Length && i<b.positions.Length;i++)positions[i]=Vector3.Lerp(a.positions[i],b.positions[i],blend);
            return new TacticalFrame {time=Time,positions=positions,ball=Vector3.Lerp(a.ball,b.ball,blend),
                holder=a.holder,marks=(int[])a.marks.Clone(),phase=a.phase,defense=a.defense,help=a.help};
        }
    }
}
