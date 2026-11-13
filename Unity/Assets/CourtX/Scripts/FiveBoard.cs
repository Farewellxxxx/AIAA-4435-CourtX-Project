using System;
using System.Collections.Generic;
using UnityEngine;
namespace CourtX
{
    public sealed class FiveBoard
    {
        public FiveState State{get;private set;}=FiveRules.Create(FivePlay.HighPost,FiveDefense.Balanced,0,0);
        FiveRecording source,branch;
        bool playing,editing,editSaved,reopened;
        float sourceTime,branchStart;
        readonly List<Snapshot> undo=new List<Snapshot>();
        sealed class Snapshot{public FiveRecording branch;public FiveState state;public float sourceTime,branchStart;}
        public FiveRecording Record=>branch??source;
        public bool IsBranch=>branch!=null;
        public bool CanUndo=>undo.Count>0;
        public string Notice{get;private set;}="暂停后可拖动任何球员，试试另一种站位。";
        public float Speed=1;
        public float Start=>Record?.Start??0;
        public float End=>Mathf.Max(Record?.End??0,IsBranch?Time:0);
        public bool Playing
        {
            get=>playing;
            set{if(!value)Commit();playing=value;}
        }
        public float Time=>State.time;
        public void Load(FiveRecording r)
        {
            if(!FiveRecording.Valid(r))throw new ArgumentException("无法打开不完整的五人制回放。");
            source=r.Copy();branch=null;undo.Clear();playing=editing=editSaved=false;sourceTime=source.Start;State=source.Sample(source.Start);
            Notice="暂停后可拖动任何球员，试试另一种站位。";
        }
        void Commit(){if(branch!=null&&State.time>=branch.End)branch.Add(State,true);}
        public void Seek(float time)
        {playing=false;Commit();State=Record.Sample(time);editing=editSaved=false;}
        public void Tick(float seconds)
        {
            if(!playing||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            float remaining=Mathf.Min(seconds*Speed,5);
            if(branch==null){State=source.Sample(Time+remaining);if(Time>=source.End-.0001f)playing=false;return;}
            if(Time<branch.End-.0001f)
            {
                float used=Mathf.Min(remaining,branch.End-Time);State=branch.Sample(Time+used);remaining-=used;
            }
            while(remaining>.00001f)
            {
                if(Time-branchStart>=180||branch.frames.Count>=FiveRecording.MaxFrames-1){playing=false;Notice="这一条假设已推演 3 分钟；可恢复原片段再探索。";break;}
                float dt=Mathf.Min(remaining,1f/60);FiveRules.Step(State,dt);branch.Add(State);remaining-=dt;
            }
        }
        void PrepareEdit()
        {
            playing=false;
            if(editing&&editSaved)return;
            Commit();undo.Add(new Snapshot{branch=branch?.Copy(),state=State.Copy(),sourceTime=sourceTime,branchStart=branchStart});
            if(undo.Count>20)undo.RemoveAt(0);editSaved=true;
            if(branch==null)
            {
                sourceTime=Time;branchStart=Time;
                branch=new FiveRecording{name=source.name,kind="whatif",created="假设推演"};State=State.Copy();State.demo=false;
            }
            else
            {
                // Two states at one timestamp encode an instantaneous edit. The first
                // closes the old interpolation; exact-time sampling chooses the last.
                // Repeated edits retain the original left-hand pose, not the last edit.
                var left=State.Copy();foreach(var f in branch.frames)if(f.time==Time){left=f.Copy();break;}
                branch.frames.RemoveAll(f=>f.time>=Time);
                branch.frames.Add(left);branch.frames.Add(State.Copy());
            }
            reopened=State.success||State.turnover;
            if(reopened)
            {
                State.success=State.turnover=false;State.phase=0;State.phaseAge=0;State.request=-1;State.passCooldown=.5f;
                if(State.flight==null&&(State.holder<0||State.holder>=5))State.holder=State.lastPassFrom>=0&&State.lastPassFrom<5?State.lastPassFrom:State.controlled;
                FiveRules.Event(State,"假设重新组织：保留当前站位，恢复进攻方配合。");
                Notice="假设重新组织：保留站位"+(State.flight!=null?"和空中传球。":"，由 "+FiveRules.Number(State.holder)+" 持球。");
            }
            if(!editing)editSaved=false;
        }
        public void BeginEdit(){playing=false;editing=true;editSaved=false;}
        public void EndEdit(){editing=editSaved=false;Commit();}
        public void Move(int actor,Vector2 position)
        {
            if(actor<0||actor>=10||float.IsNaN(position.x)||float.IsNaN(position.y)||float.IsInfinity(position.x)||float.IsInfinity(position.y))return;
            PrepareEdit();State.positions[actor]=FiveRules.Clamp(position);State.velocities[actor]=Vector2.zero;
            FiveRules.Refresh(State);branch.Add(State,true);if(!reopened)Notice="假设推演：继续播放，观察其他人的反应。原片段保持不变。";
        }
        public void SetPinned(int actor,bool value)
        {if(actor<0||actor>=10)return;PrepareEdit();State.pinned[actor]=value;FiveRules.Refresh(State);branch.Add(State,true);Notice=value?"已固定此人，其余球员继续按规则反应。":"已释放此人，继续后参与自动跑位。";}
        public void SetHolder(int actor)
        {if(actor<0||actor>=5)return;PrepareEdit();State.holder=actor;State.flight=null;State.turnover=false;FiveRules.Event(State,"假设设置："+FiveRules.Number(actor)+" 持球。");FiveRules.Refresh(State);branch.Add(State,true);}
        public bool Pass(int actor)
        {
            if(actor<0||actor>=5||State.flight!=null||State.turnover||State.holder==actor)return false;
            PrepareEdit();bool result=FiveRules.Pass(State,actor);branch.Add(State,true);return result;
        }
        public void StepOnce(){playing=true;float speed=Speed;Speed=1;Tick(.5f);Speed=speed;Playing=false;}
        public void Jump(int direction)
        {
            float target=direction<0?Start:End;foreach(float node in Record.Nodes())
            {if(direction<0&&node<Time-.04f)target=node;if(direction>0&&node>Time+.04f){target=node;break;}}
            Seek(target);
        }
        public void Undo()
        {
            if(undo.Count==0)return;var s=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);branch=s.branch;State=s.state;sourceTime=s.sourceTime;branchStart=s.branchStart;
            playing=editing=editSaved=false;Notice="已撤销这次编辑，回到修改前的时刻。";
        }
        public void Restore()
        {branch=null;undo.Clear();State=source.Sample(sourceTime);playing=editing=editSaved=false;Notice="已返回原片段，假设修改已清除。";}
    }
}
