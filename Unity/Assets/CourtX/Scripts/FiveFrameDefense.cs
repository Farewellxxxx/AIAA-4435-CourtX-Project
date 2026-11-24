using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CourtX
{
    // Single-frame layout solver. It never steps the game clock or changes offense.
    // Inspired by CourtLab's priority jobs + pinned coverage + minimum-cost assignment;
    // these five jobs are for a 2-3 zone, not its Box-and-1/chaser defense.
    public static class FiveFrameDefense
    {
        sealed class Job { public int home,priority;public Vector2 point;public string name; }
        static Vector2 TowardRim(Vector2 p,float amount)=>p+(new Vector2(0,1.575f)-p).normalized*amount;
        public static string Arrange(FiveState s,FiveState basis)
        {
            var template=s.Copy();for(int i=5;i<10;i++)template.pinned[i]=false;
            // Keep the recorded rotation as the starting point for a v2 frame study.
            // The edited ball and offensive positions then determine fresh coverage jobs.
            if(s.playbookVersion>=2)
            {
                for(int id=5;id<10;id++)template.targets[id]=basis.positions[id];
            }
            else FiveRules.Refresh(template);
            var ball=s.Ball;int side=ball.x<0?-1:1;
            int centerId=s.playbookVersion>=2?9:8,rightId=s.playbookVersion>=2?8:9;
            int primary=ball.y<4.4f&&Mathf.Abs(ball.x)>3.1f?(side<0?7:rightId):ball.y<FiveRules.HelpDepth(s.defense)&&Mathf.Abs(ball.x)<3.9f?centerId:(side<0?5:6);
            int rim=primary==centerId?(side<0?rightId:7):centerId;
            var jobs=new List<Job>();
            for(int id=5;id<10;id++)
            {
                bool upper=id<7;int flank=id==5||id==7?-1:1;
                var point=template.targets[id];string name=upper?"兼顾外线接应与高位":"保护短角与底线接应";
                int priority=upper?4:3;
                if(id==primary)
                {
                    priority=0;name="接管持球侧，限制进入中路";
                    if(s.playbookVersion>=2)point=TowardRim(ball,s.defense==FiveDefense.Perimeter?.95f:1.35f);
                }
                else if(id==rim){priority=1;point=new Vector2(-side*.55f,2.05f);name="补篮下，保护同伴身后的空间";}
                else
                {
                    int threat=-1;float best=float.MaxValue;
                    for(int a=0;a<5;a++)
                    {
                        if(a==s.holder||s.positions[a].x*flank<.1f||upper!=(s.positions[a].y>=4.8f))continue;
                        float score=Vector2.Distance(s.positions[a],template.targets[id]);
                        if(score<best){best=score;threat=a;}
                    }
                    if(threat>=0)
                    {
                        point=TowardRim(s.positions[threat],s.defense==FiveDefense.Rim?2.1f:s.defense==FiveDefense.Perimeter?1.05f:1.5f);
                        point.y=Mathf.Clamp(point.y,upper?4.8f:1.4f,upper?9:4.5f);
                        point.x=Mathf.Clamp(point.x,-5.8f,5.8f);
                        name="照应 "+FiveRules.Number(threat)+" 的接球点，兼顾相邻区域";
                    }
                    if(id==centerId){priority=2;name="补中路连接，兼顾篮下";}
                    else if(upper&&flank==side)priority=2;
                }
                jobs.Add(new Job{home=id,priority=priority,point=FiveRules.Clamp(point),name=name});
            }
            jobs=jobs.OrderBy(j=>j.priority).ThenBy(j=>j.home).ToList();
            var assigned=new Dictionary<int,Job>();var remaining=new List<Job>(jobs);
            for(int id=5;id<10;id++)if(s.pinned[id]||s.holder==id)
            {
                var match=remaining.Where(j=>Vector2.Distance(s.positions[id],j.point)<1.65f).OrderBy(j=>j.priority).ThenBy(j=>Vector2.Distance(s.positions[id],j.point)).FirstOrDefault();
                if(match!=null){assigned[id]=match;remaining.Remove(match);}
                s.targets[id]=s.positions[id];s.velocities[id]=Vector2.zero;
                s.tasks[id]=s.holder==id?"当前已截到球：保持这一帧的持球位置。":"保留你摆放的位置；"+(match!=null?match.name:"离开原责任区，其余防守人优先补球侧与篮下。");
            }
            var free=Enumerable.Range(5,5).Where(i=>!s.pinned[i]&&s.holder!=i).ToArray();var chosen=remaining.Take(free.Length).ToArray();
            int[] bestIds=null,current=new int[free.Length];float bestCost=float.MaxValue;
            void Search(int depth,int mask,float cost)
            {
                if(cost>=bestCost)return;
                if(depth==chosen.Length){bestCost=cost;bestIds=(int[])current.Clone();return;}
                var job=chosen[depth];
                for(int i=0;i<free.Length;i++)if((mask&(1<<i))==0)
                {
                    int id=free[i];float next=Vector2.Distance(basis.positions[id],job.point);
                    if((id<7)!=(job.home<7))next+=1.4f;
                    if(id!=job.home)next+=.4f;
                    current[depth]=id;Search(depth+1,mask|(1<<i),cost+next);
                }
            }
            Search(0,0,0);
            for(int i=0;i<chosen.Length;i++)
            {
                int id=bestIds[i];var job=chosen[i];assigned[id]=job;s.positions[id]=job.point;s.targets[id]=job.point;
                s.velocities[id]=Vector2.zero;s.tasks[id]=job.name+"。这是当前暂停帧的布防调整。";
            }
            // Resolve very close targets while keeping all manual assumptions and offense fixed.
            for(int pass=0;pass<4;pass++)foreach(int id in free)for(int other=0;other<10;other++)if(other!=id)
            {
                var delta=s.positions[id]-s.positions[other];float distance=delta.magnitude;
                if(distance>=.7f)continue;var direction=distance>.001f?delta/distance:new Vector2(id<other?-1:1,0);
                s.positions[id]=FiveRules.Clamp(s.positions[id]+direction*(.7f-distance));
            }
            for(int id=5;id<10;id++){s.targets[id]=s.positions[id];var facing=ball-s.positions[id];if(facing.sqrMagnitude>.01f)s.facing[id]=facing.normalized;}
            string who(int home)=>assigned.FirstOrDefault(p=>p.Value.home==home).Value!=null?FiveRules.Number(assigned.First(p=>p.Value.home==home).Key):"空缺";
            string text=who(primary)+" 接管球侧，"+who(rim)+" 补篮下。";
            if(remaining.Count>free.Length)text+="固定站位留下弱侧缺口。";else text+="其余防守人照应邻近接应点。";
            s.defenseIntent=text;return text;
        }
    }
}
