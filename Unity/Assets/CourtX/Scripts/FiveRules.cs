using System;
using UnityEngine;
namespace CourtX
{
    public static class FiveRules
    {
        public static readonly string[] Plays={"中锋提中","后卫挡拆发起","牛角 · 射手战术"};
        public static readonly string[] Defenses={"均衡轮转","收缩护筐","外线施压"};
        public static readonly string[] Roles={"控球后卫","侧翼射手","弱侧射手","锋线接应","中锋"};
        public static string Number(int id)=>id<5?"0"+(id+1):"X"+(id-4);
        public static Vector2 Clamp(Vector2 p)=>new Vector2(Mathf.Clamp(p.x,-7.05f,7.05f),Mathf.Clamp(p.y,.45f,13.55f));
        public static Vector3 World(Vector2 p,float height=0)=>new Vector3(p.x,height,-p.y);
        public static Vector2 Flat(Vector3 p)=>new Vector2(p.x,-p.z);
        static readonly Vector2 rim=new Vector2(0,1.575f);
        public static float HelpDepth(FiveDefense defense)=>defense==FiveDefense.Rim?7.1f:defense==FiveDefense.Perimeter?4.5f:5.9f;
        static Vector2 Toward(Vector2 from,Vector2 to,float distance)=>from+(to-from).normalized*distance;
        public static void Event(FiveState s,string message){s.lastEvent=message;s.eventSerial++;}
        public static FiveState Create(FivePlay play,FiveDefense defense,int role,int holder)
        {
            var s=new FiveState{play=(FivePlay)Mathf.Clamp((int)play,0,2),defense=(FiveDefense)Mathf.Clamp((int)defense,0,2),controlled=Mathf.Clamp(role,0,4),holder=Mathf.Clamp(holder,0,4)};
            s.positions=new[]{new Vector2(0,10.8f),new Vector2(-5.8f,7.8f),new Vector2(6,7.5f),new Vector2(-5.9f,2),new Vector2(2.3f,3.1f),
                new Vector2(-2.5f,6.8f),new Vector2(2.5f,6.8f),new Vector2(-4.1f,2.7f),new Vector2(0,2.6f),new Vector2(4.1f,2.7f)};
            if(play==FivePlay.Screen){s.positions[0]=new Vector2(-1.5f,10.5f);s.positions[2]=new Vector2(6.2f,2);s.positions[4]=new Vector2(-2.5f,6.1f);}
            if(play==FivePlay.Horns){s.positions[1]=new Vector2(-6,2);s.positions[2]=new Vector2(6,2);s.positions[3]=new Vector2(-2.3f,6.3f);s.positions[4]=new Vector2(2.3f,6.3f);}
            for(int i=0;i<10;i++)s.facing[i]=i<5?Vector2.down:Vector2.up;
            Event(s,"选择站位，观察 2–3 联防。");Refresh(s);return s;
        }
        static void Set(FiveState s,int id,Vector2 target,string task){s.targets[id]=Clamp(target);s.tasks[id]=task;}
        public static void Refresh(FiveState s)
        {
            var p=s.positions;var ball=s.Ball;float side=ball.x<0?-1:1;
            s.phaseName=new[]{"组织落位","建立连接","牵动防守","寻找出口","机会形成"}[Mathf.Clamp(s.phase,0,4)];
            s.screenActive=false;s.screener=s.play==FivePlay.Horns?3:4;
            Set(s,0,new Vector2(-1.3f,10.2f),"保持持球与回传空间，观察高位接应。");
            Set(s,1,new Vector2(-6.1f,7.7f),"拉开左侧宽度，准备接应转移球。");
            Set(s,2,new Vector2(6.1f,2.1f),"保持弱侧底角，观察防守是否向球侧收缩。");
            Set(s,3,new Vector2(-5.8f,2.1f),"在短角显露接球点，不与中锋挤在一起。");
            Set(s,4,new Vector2(0,5.8f),"提到罚球线附近接应，牵动中路防守。");
            if(s.play==FivePlay.HighPost)
            {
                Set(s,0,new Vector2(-5.3f,9),"向侧翼调整传球角度，避开上沿正面的封堵。");
                s.attackIntent=s.phase<2?"中锋提中接应，牵动联防中路；两侧保持宽度。":"让中路防守选择上提或守篮下，寻找其身后的接应空间。";
                if(s.phase>=2){Set(s,0,new Vector2(-2.2f,4.1f),"传后向空隙切入，再观察回传线路。");Set(s,3,new Vector2(-3.4f,1.8f),"沿底线接应中路协防身后，留出传球角度。");}
            }
            else if(s.play==FivePlay.Screen)
            {
                s.screenPoint=new Vector2(.5f,8.2f);
                Set(s,4,s.phase<2?s.screenPoint:new Vector2(1.8f,4),s.phase<2?"在持球人侧前方站定掩护，等待后卫使用。":"向中路短顺下，提供被夹击后的传球出口。");
                Set(s,0,s.phase==0?new Vector2(-1.3f,10):new Vector2(2.4f,7),s.phase<2?"靠近并使用中锋掩护，观察上沿如何交接。":"吸引第一线后，比较短顺下与弱侧接应。");
                s.screenActive=s.phase==1&&(p[4]-s.screenPoint).magnitude<1.2f;
                s.attackIntent=s.phase<2?"用掩护改变上沿防守角度，让持球人进入联防间隙。":"吸引中路协防，再寻找顺下或弱侧出口。";
            }
            else
            {
                s.screenPoint=new Vector2(-2.3f,6.3f);
                Set(s,4,new Vector2(3.6f,6.1f),"从右高位向外显露接球点，观察射手绕掩护后的窗口。");
                Set(s,3,s.phase<3?s.screenPoint:new Vector2(-1.8f,3.1f),s.phase<3?"在左高位为射手做无球掩护，先站定。":"掩护后切入，提供射手之外的第二出口。");
                Set(s,1,s.phase==0?new Vector2(-5.8f,2.3f):s.phase==1?new Vector2(-3.1f,6.9f):new Vector2(-6,7.8f),s.phase==0?"从左侧准备绕过高位掩护。":"借高位掩护兜到外线，保持可见接球角度。");
                s.screenActive=s.phase>=1&&s.phase<3&&(p[3]-s.screenPoint).magnitude<1.1f;
                s.attackIntent=s.phase<2?"双高位连接球与无球掩护，为射手创造接球角度。":"观察追防与外扑，选择射手接球或掩护后的切入。";
            }
            float front=s.defense==FiveDefense.Perimeter?.8f:s.defense==FiveDefense.Rim?-.65f:0;
            Set(s,5,new Vector2(-2.7f+ball.x*.18f,6.6f+front),"守住左上沿，兼顾持球压力与高位传球线。");
            Set(s,6,new Vector2(2.7f+ball.x*.18f,6.6f+front),"守住右上沿，随球移动并准备接管持球者。");
            Set(s,7,new Vector2(-4.1f+ball.x*.1f,2.8f),"守左侧低位，兼顾底角与沿底线切入。");
            Set(s,8,new Vector2(ball.x*.22f,2.7f),"保持中路护筐，观察高位接应与身后切入。");
            Set(s,9,new Vector2(4.1f+ball.x*.1f,2.8f),"守右侧低位，准备接应上沿被突破后的协防。");
            int upper=side<0?5:6,weakUpper=side<0?6:5,lower=side<0?7:9,weakLower=side<0?9:7;
            bool low=ball.y<4.4f&&Mathf.Abs(ball.x)>3.1f;
            float helpDepth=HelpDepth(s.defense);
            bool middle=ball.y<helpDepth&&Mathf.Abs(ball.x)<3.9f;
            if(low)
            {
                Set(s,lower,Toward(ball,rim,1.05f),"球到低处，接管底线持球者，限制直达篮下。");
                Set(s,8,new Vector2(side*.7f,2.6f),"补球侧低位身后，保护中路接应窗口。");
                Set(s,weakLower,new Vector2(-side*1.6f,2),"弱侧收回篮下，补上同伴外出的空间。");
                Set(s,upper,new Vector2(ball.x-side*1.3f,Mathf.Max(5,ball.y+2)),"上沿下沉，限制回传和高位连接。");
                s.defenseIntent="低位接球，另外两名低位补篮下；上沿封回传。";
            }
            else if(middle)
            {
                Set(s,8,Toward(ball,rim,1.1f),"中路上提限制持球者，要求两翼补身后。");
                Set(s,upper,new Vector2(ball.x+side*1.1f,ball.y+1.15f),"从侧后方压缩回传，不穿过持球者瞬移防守。");
                Set(s,weakLower,new Vector2(-side*1.9f,2),"弱侧低位收窄，保护中路协防后的篮下。");
                Set(s,lower,new Vector2(side*3.4f,2.4f),"兼顾短角与中路协防身后的接应者。");
                s.defenseIntent="中路协防限制突破，低位补身后，弱侧保持轮转余地。";
            }
            else
            {
                Set(s,upper,Toward(ball,rim,s.defense==FiveDefense.Perimeter?.95f:1.7f),"上沿向持球者施压，保持球与篮筐之间的角度。");
                s.defenseIntent=s.defense==FiveDefense.Rim?"保持收缩，优先保护中路和篮下；接受较大的外线接球空间。":s.defense==FiveDefense.Perimeter?"上沿扩大外线压力，低位准备补位，留意中路空隙。":"上二下三随球轮转，兼顾高位连接与底角接应。";
            }
            Set(s,weakUpper,new Vector2(-side*2.2f,6.3f+front),"弱侧上沿收窄，兼顾高位与外线转移。");
            // Current ball ownership always wins over the nominal play route.
            if(s.holder>=0&&s.holder<5)
            {
                int h=s.holder;
                if(s.phase>=2||h!=0)s.targets[h]=p[h];
                s.tasks[h]="当前持球：观察防守和接应角度，再决定传球或调整位置。";
            }
            if(s.flight!=null){s.targets[s.flight.to]=s.flight.destination;s.tasks[s.flight.to]="向来球位置接应，准备接球后的下一步选择。";}
            for(int i=0;i<10;i++)
            {
                if(s.success&&i<5)s.targets[i]=p[i];
                if(s.pinned[i]){s.targets[i]=p[i];s.tasks[i]="保持你设定的站位；当前假设固定此人。";}
                s.targets[i]=Clamp(s.targets[i]);
            }
        }
        public static float Space(FiveState s,int id)
        {float d=30;for(int i=5;i<10;i++)d=Mathf.Min(d,Vector2.Distance(s.positions[id],s.positions[i]));return d;}
        public static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b,out float along)
        {var delta=b-a;along=delta.sqrMagnitude>.0001f?Mathf.Clamp01(Vector2.Dot(p-a,delta)/delta.sqrMagnitude):0;return Vector2.Distance(p,a+delta*along);}
        public static int Risk(FiveState s,int to)
        {
            if(s.holder<0||s.holder>=5||to<0||to>=5||s.holder==to)return 2;
            float clearance=30;
            for(int i=5;i<10;i++){float u;float d=SegmentDistance(s.positions[i],s.positions[s.holder],s.positions[to],out u);if(u>.08f&&u<.94f)clearance=Mathf.Min(clearance,d);}
            float space=Space(s,to);return clearance<.55f||space<.8f?2:clearance<1.1f||space<1.65f?1:0;
        }
        public static bool Pass(FiveState s,int to)
        {
            if(s.turnover||s.flight!=null||s.holder<0||s.holder>=5||to<0||to>=5||to==s.holder)return false;
            int from=s.holder,risk=Risk(s,to);float distance=Vector2.Distance(s.positions[from],s.positions[to]);
            s.flight=new FiveFlight{from=from,to=to,origin=s.positions[from],destination=s.positions[to],duration=Mathf.Clamp(distance/10.5f,.3f,1.5f)};
            s.lastPassFrom=from;s.lastPassTo=to;s.holder=-1;s.request=-1;s.passCooldown=1.2f;
            Event(s,Number(from)+" → "+Number(to)+" 出球"+(risk==2?"：这条线路受到防守压力。":"：已选择可用的接应线路。"));
            bool outlet=s.play==FivePlay.HighPost?(to==3||to==2||to==0):s.play==FivePlay.Screen?(to==4||to==2):(to==1||to==3);
            if(s.phase>=2&&outlet&&risk<2){s.success=true;s.phase=4;Event(s,"决策完成：利用防守移动选择了接应窗口。C 再 call 战术，或 V 复盘。");}
            Refresh(s);return true;
        }
        public static void Request(FiveState s,int receiver)
        {
            if(receiver<0||receiver>=5||s.turnover)return;
            if(s.holder==receiver){Event(s,"你已经持球，指向队友后按 Q 直接传球。");return;}
            s.request=receiver;s.requestUntil=s.time+4;
            Event(s,s.flight!=null?"已要球：等待这次传球落定。":"已要球：队友会在出现传球角度后回应。");
        }
        public static void Call(FiveState s)
        {
            s.phase=0;s.phaseAge=0;s.success=false;s.turnover=false;s.request=-1;s.passCooldown=.5f;
            if(s.holder<0||s.holder>=5){s.holder=s.controlled;s.flight=null;}
            Event(s,"重新组织："+Plays[(int)s.play]+"。队友按当前站位重新落位。");Refresh(s);
        }
        static void Change(FiveState s,int phase){s.phase=phase;s.phaseAge=0;Event(s,"进入下一阶段："+new[]{"组织落位","建立连接","牵动防守","寻找出口","机会形成"}[phase]);}
        static void AdvancePhase(FiveState s)
        {
            if(s.success||s.turnover)return;
            if(s.phase==0&&s.phaseAge>.65f)
            {
                Vector2 spot=s.play==FivePlay.Screen?new Vector2(.5f,8.2f):s.play==FivePlay.Horns?new Vector2(3.6f,6.1f):new Vector2(0,5.8f);
                if(Vector2.Distance(s.positions[4],spot)<1.2f)Change(s,1);
            }
            else if(s.phase==1)
            {
                bool ready=s.play==FivePlay.Screen?s.positions[0].x>.3f&&s.positions[0].y<9:s.holder==4;
                if(ready&&s.phaseAge>.4f)Change(s,2);
            }
            else if(s.phase==2&&s.phaseAge>2.8f)Change(s,3);
        }
        static void Decide(FiveState s,int human)
        {
            if(s.flight!=null||s.holder<0||s.holder>=5||s.holder==human||s.turnover||s.passCooldown>0)return;
            if(s.request>=0)
            {
                if(s.time>s.requestUntil){s.request=-1;Event(s,"要球窗口暂未形成，调整角度后可再次按 E。");}
                else if(s.request!=s.holder&&Risk(s,s.request)<2){Pass(s,s.request);return;}
            }
            if(s.success||s.phaseAge<.85f)return;
            int to=-1;
            if(s.phase==1&&s.play!=FivePlay.Screen&&s.holder!=4)to=4;
            if(s.phase>=2)
            {
                int[] options=s.play==FivePlay.Horns?new[]{1,3,2,0,4}:s.play==FivePlay.Screen?new[]{4,2,1,3,0}:new[]{3,2,0,1,4};
                float score=-100;foreach(int option in options)if(option!=s.holder){float v=Space(s,option)-Risk(s,option)*3;if(v>score){score=v;to=option;}}
            }
            // Flexible initial ball ownership can connect to the normal initiating pair.
            if(s.phase==0&&s.phaseAge>1.4f&&s.holder!=0&&s.holder!=4)to=0;
            if(s.phase==1&&to>=0&&Risk(s,to)==2)
            {
                // When the high-post entry is denied, reverse the ball before trying again.
                int outlet=-1;float best=-100;
                for(int i=0;i<5;i++)if(i!=s.holder&&i!=to&&i!=s.lastPassFrom&&Risk(s,i)<2)
                {float value=Space(s,i)-Risk(s,i)*2;if(value>best){best=value;outlet=i;}}
                if(outlet>=0)to=outlet;
            }
            if(to>=0&&Risk(s,to)<2)Pass(s,to);
        }
        public static void Step(FiveState s,float seconds,int human=-1)
        {
            if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            float dt=Mathf.Min(seconds,.05f);s.time+=dt;s.phaseAge+=dt;s.passCooldown-=dt;
            if(s.turnover){Refresh(s);return;}
            AdvancePhase(s);Refresh(s);Decide(s,human);
            for(int i=0;i<10;i++)
            {
                var p=s.positions[i];var delta=s.targets[i]-p;
                if(i==human||s.pinned[i]){s.velocities[i]=Vector2.zero;continue;}
                Vector2 desired=Vector2.ClampMagnitude(delta*3, i<5?2.65f:2.45f);
                for(int j=0;j<10;j++)if(j!=i){var away=p-s.positions[j];float d=away.magnitude;if(d<.8f&&d>.01f)desired+=away/d*(.8f-d)*3;}
                desired=Vector2.ClampMagnitude(desired,i<5?2.65f:2.45f);
                s.velocities[i]=Vector2.MoveTowards(s.velocities[i],desired,8*dt);
                s.positions[i]=Clamp(p+s.velocities[i]*dt);
                Vector2 face=i>=5?s.Ball-s.positions[i]:s.velocities[i].sqrMagnitude>.1f?s.velocities[i]:s.Ball-s.positions[i];
                if(face.sqrMagnitude>.01f)s.facing[i]=face.normalized;
            }
            if(s.flight!=null)
            {
                var f=s.flight;var before=f.Position;f.elapsed+=dt;var now=f.Position;int blocker=-1;
                if(f.elapsed>.09f)for(int i=5;i<10;i++){float u;if(SegmentDistance(s.positions[i],before,now,out u)<.28f){blocker=i;break;}}
                if(blocker>=0){s.looseBall=now;s.flight=null;s.holder=blocker;s.turnover=true;Event(s,"传球线路被 "+Number(blocker)+" 封住。C 重新组织，或 V 看回放。");}
                else if(f.elapsed>=f.duration)
                {
                    if(Vector2.Distance(s.positions[f.to],f.destination)<3){s.holder=f.to;Event(s,Number(f.to)+" 接到球，继续观察下一步。");}
                    else{s.looseBall=f.destination;s.holder=-1;s.turnover=true;Event(s,"接应者离开了来球区域，C 可重新组织。");}
                    s.flight=null;
                }
            }
            Refresh(s);
        }
    }
}
