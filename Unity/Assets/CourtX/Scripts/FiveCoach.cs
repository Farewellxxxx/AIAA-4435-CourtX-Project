using UnityEngine;

namespace CourtX
{
    // A phase is one authored CourtLab frame. The current frame owns the ball
    // until a pass to the next frame's holder has physically arrived.
    public static class FiveCoach
    {
        public const float Arrival = .48f;
        const float ReadSeconds = 1.15f;
        const float SettledSeconds = .72f;

        static FiveZoneFrame Current(FiveState s) => FiveZoneBook.Frame(s.play,s.branch,s.phase);
        public static int Recommended(FivePlay play) => FiveZoneBook.Recommended(play);
        public static bool AtFinish(FiveState s) => s.phase >= FiveZoneBook.Count(s.play,s.branch)-1;

        public static FiveState Create(FivePlay play,int role,int branch=0)
        {
            var s=FiveRules.Create(play,FiveDefense.Balanced,role,0);
            s.guided=true;
            s.playbookVersion=2;
            s.branch=Mathf.Clamp(branch,0,2);
            var first=Current(s);
            s.holder=first.holder;
            for(int i=0;i<5;i++)
            {
                s.positions[i]=first.offense[i];
                s.positions[i+5]=first.defense[i];
            }
            FiveRules.Event(s,first.title);
            FiveRules.Refresh(s);
            return s;
        }

        public static int Receiver(FiveState s)
        {
            if(AtFinish(s))return -1;
            int next=FiveZoneBook.Frame(s.play,s.branch,s.phase+1).holder;
            return next==Current(s).holder?-1:next;
        }

        static FiveZoneArrow Route(FiveZoneFrame frame)
        {
            foreach(var arrow in frame.arrows)
                if(arrow.player>=0&&arrow.via!=null&&arrow.via.Length>0)return arrow;
            return null;
        }

        static Vector2 Destination(FiveState s,FiveZoneFrame frame,int id)
        {
            var route=Route(frame);
            if(route!=null&&s.routePoint<route.via.Length)
            {
                if(id==route.player)return route.via[s.routePoint];
                // Keep the ball screen set until the dribbler clears 05's shoulder.
                if(frame.id=="gd-gap"&&id==4)
                    return FiveZoneBook.Frame(s.play,s.branch,s.phase-1).offense[id];
            }
            return frame.offense[id];
        }

        public static bool InPlace(FiveState s,int id) =>
            Vector2.Distance(s.positions[id],Destination(s,Current(s),id))<=Arrival;

        static bool Set(FiveState s,FiveZoneFrame frame)
        {
            var route=Route(frame);
            if(route!=null&&s.routePoint<route.via.Length)return false;
            for(int i=0;i<5;i++)
                if(Vector2.Distance(s.positions[i],frame.offense[i])>Arrival ||
                   Vector2.Distance(s.positions[5+i],frame.defense[i])>.72f)return false;
            return true;
        }

        public static bool Ready(FiveState s) =>
            !s.success&&!s.turnover&&s.flight==null&&s.phaseAge>=ReadSeconds&&s.settledAge>=SettledSeconds&&Set(s,Current(s));

        public static bool CanPass(FiveState s,int to) =>
            to>=0&&s.holder==Current(s).holder&&to==Receiver(s)&&Ready(s);

        public static string Cue(FiveState s,int id)
        {
            var frame=Current(s);
            if(s.turnover||s.madeBasket)return s.lastEvent;
            if(s.flight?.shot==true)return FiveRules.Number(s.flight.from)+" 已出手，观察投篮。";
            if(s.flight!=null&&s.flight.to==id)return "保持接球位置，准备接 "+FiveRules.Number(s.flight.from)+" 的传球。";
            if(s.pendingGuidedCatch&&s.holder==id)return InPlace(s,id)?"已提前接球，保持球权，等队友完成这次落位。":"已提前接球，带球到高亮圈完成落位。";
            if(s.holder==id&&s.holder!=frame.holder)return "瞄准 "+FiveRules.Number(frame.holder)+" 按 Q 回传，重新衔接当前配合。";
            if(!InPlace(s,id))return (s.holder==id?"带球到高亮圈。":"跑到高亮圈。")+frame.tasks[id];
            if(s.holder==id)
            {
                if(!Ready(s))return "保持球权，等队友落位并观察防守。"+frame.tasks[id];
                if(AtFinish(s))return "站稳，瞄准篮筐，按 Q 投篮。";
                int to=Receiver(s);
                if(to>=0)return "瞄准 "+FiveRules.Number(to)+"，按 Q 传球。"+frame.tasks[id];
            }
            return frame.tasks[id];
        }

        public static void Describe(FiveState s)
        {
            var frame=Current(s);
            bool stopped=s.success||s.turnover||s.flight?.shot==true;
            s.phaseName=s.madeBasket?"投篮命中":s.flight?.shot==true?"投篮出手":frame.title;
            s.attackIntent=frame.offenseIntent;
            s.defenseIntent=frame.defenseIntent;
            var screen=Screen(s,frame);
            s.screener=screen.screener;
            s.screenActive=!stopped&&s.screener>=0&&
                Vector2.Distance(s.positions[s.screener],screen.offense[s.screener])<=Arrival;
            if(s.screener>=0)s.screenPoint=s.positions[s.screener];
            for(int i=0;i<5;i++)
            {
                s.targets[i]=stopped?s.positions[i]:Destination(s,frame,i);
                s.targets[5+i]=stopped?s.positions[5+i]:frame.defense[i];
                s.tasks[i]=Cue(s,i);
                s.tasks[5+i]=frame.tasks[5+i];
            }
        }

        public static int ActiveScreened(FiveState s) =>
            s.screenActive&&s.guided&&s.playbookVersion>=2?Screen(s,Current(s)).screened:-1;

        static FiveZoneFrame Screen(FiveState s,FiveZoneFrame frame)
        {
            if(frame.id=="gd-gap"&&s.routePoint==0)
                return FiveZoneBook.Frame(s.play,s.branch,s.phase-1);
            return frame;
        }

        public static Vector2 RouteDefender(FiveState s,int actor,Vector2 from,Vector2 next)
        {
            if(!s.guided||s.playbookVersion<2||!s.screenActive||actor<5)return next;
            var screen=Screen(s,Current(s));
            if(actor!=5+screen.screened)return next;
            const float gap=.64f;
            if((next-s.screenPoint).sqrMagnitude>=gap*gap)return next;
            var normal=(from-s.screenPoint).normalized;
            if(normal.sqrMagnitude<.01f)normal=Vector2.left;
            var tangent=new Vector2(-normal.y,normal.x);
            if(Vector2.Dot(tangent,s.targets[actor]-from)<0)tangent=-tangent;
            return s.screenPoint+(normal*gap+tangent*(next-from).magnitude).normalized*gap;
        }

        static void Next(FiveState s)
        {
            s.phase++;
            s.phaseAge=0;
            s.settledAge=0;
            s.pendingGuidedCatch=false;
            s.routePoint=0;
            FiveRules.Event(s,Current(s).title);
        }

        public static void Catch(FiveState s,FiveFlight flight)
        {
            if(flight.from!=Current(s).holder||flight.to!=Receiver(s))return;
            if(flight.advancesBeat)Next(s);
            else s.pendingGuidedCatch=true;
        }

        public static void Tick(FiveState s,int human,float dt)
        {
            if(s.success||s.turnover||s.flight!=null||s.holder<0)return;
            var frame=Current(s);
            var route=Route(frame);
            if(route!=null&&s.routePoint<route.via.Length&&InPlace(s,route.player))s.routePoint++;
            if(s.screenActive)s.usedScreen=true;
            s.settledAge=Set(s,frame)?s.settledAge+dt:0;
            if(s.pendingGuidedCatch)
            {
                if(Ready(s))Next(s);
                return;
            }
            if(s.holder!=frame.holder)
            {
                if(s.holder!=human&&s.passCooldown<=0)FiveRules.Pass(s,frame.holder,true);
                return;
            }
            if(!Ready(s))return;
            if(AtFinish(s))
            {
                if(s.holder!=human&&s.passCooldown<=0)FiveRules.Shoot(s);
                return;
            }
            int receiver=Receiver(s);
            if(receiver<0)Next(s);
            else if(s.holder!=human&&s.passCooldown<=0)FiveRules.Pass(s,receiver);
        }
    }
}
