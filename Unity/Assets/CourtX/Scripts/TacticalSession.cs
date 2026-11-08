using System;
using UnityEngine;

namespace CourtX
{
    [DefaultExecutionOrder(10)]
    public sealed class TacticalSession : MonoBehaviour
    {
        public Walkthrough player;
        public TacticalBall ball;
        public TacticalActor[] actors=new TacticalActor[6];
        public Material[] mapMaterials=new Material[6];
        public Transform guidanceRing;
        [Range(.5f,4)] public float offenseSpeed=2.65f;
        [Range(.5f,4)] public float defenseSpeed=2.25f;
        [Range(.1f,1)] public float decisionInterval=.22f;
        public TacticalDrill Drill { get; private set; }
        public TacticalPhase Phase { get; private set; }
        public int Controlled { get; private set; }
        public bool Running { get; private set; }
        public bool MenuOpen { get; private set; }=true;
        public bool acceptsInput=true;
        public int SelectedReceiver { get; private set; }=1;
        public string Message { get; private set; }="选择练习与角色，开始一个回合。";
        public string PassReason { get; private set; }="";
        public int PhaseChanges { get; private set; }
        public event Action<int,int> PassStarted;
        public event Action<int> BallReceived;
        public event Action<TacticalPhase> PhaseChanged;
        readonly Vector3[] targets=new Vector3[6];
        float phaseTime,screenHold,think,requestUntil,aiPassCooldown,noticeUntil;
        Vector3 screen;
        Vector3 passStep;
        float passStepUntil;
        int passStepActor=-1;
        int lastPasser=-1;
        bool postReceived,rollReceived,cutReceived;
        int menuDrill,menuRole;
        bool help=true,initialized;
        public string Number(int id)=>new[]{"01","04","05","11","12","13"}[Mathf.Clamp(id,0,5)];
        public Vector3 Position(int id)=>id==Controlled?TacticalRules.Flat(player.transform.position):actors[id].transform.position;
        public Vector3 Facing(int id)=>id==Controlled?player.transform.forward:actors[id].transform.forward;
        void Start()
        {
            player.acceptsInput=false;player.enabled=false;
            if(player.basketball)player.basketball.enabled=false;
            initialized=true;StartDrill(TacticalDrill.PickAndRoll,false);OpenMenu();
        }
        public void StartDrill(TacticalDrill drill,bool offBall)
        {
            if(!initialized)return;
            Drill=drill;Controlled=offBall?1:0;Running=true;MenuOpen=false;
            menuDrill=(int)drill;menuRole=offBall?1:0;
            Phase=drill==TacticalDrill.PickAndRoll?TacticalPhase.Assemble:TacticalPhase.HighPost;
            PhaseChanges=0;phaseTime=screenHold=think=requestUntil=aiPassCooldown=0;
            passStepUntil=0;passStepActor=-1;
            lastPasser=-1;postReceived=rollReceived=cutReceived=false;
            player.basketball=null;player.ResetView();
            var start=new[]{new Vector3(-.9f,0,-10.3f),new Vector3(-3.8f,0,-7f),new Vector3(6.1f,0,-2.3f),
                new Vector3(-.65f,0,-8.15f),new Vector3(-2.25f,0,-5.8f),new Vector3(4.6f,0,-3.5f)};
            if(drill==TacticalDrill.HighPostCut)
            {start[0]=new Vector3(-3.8f,0,-9.3f);start[1]=new Vector3(.6f,0,-3.7f);start[3]=new Vector3(-3.2f,0,-7.5f);start[4]=new Vector3(1.5f,0,-3.5f);}
            for(int i=0;i<6;i++)
            {
                actors[i].gameObject.SetActive(i!=Controlled);actors[i].SetPosition(start[i]);
                actors[i].transform.rotation=Quaternion.LookRotation(i>=3?Vector3.back:Vector3.forward);
                actors[i].Action=i>=3?ActorAction.Slide:ActorAction.Space;targets[i]=start[i];
            }
            TeleportPlayer(start[Controlled],drill==TacticalDrill.HighPostCut&&offBall?205:0);
            screen=new Vector3(.35f,0,-8.1f);SelectedReceiver=Controlled==0?1:0;
            RebindMap();ball.ResetBall(0);PassReason="";
            Notify(offBall?"你是 04：先到地面光圈，观察持球人；E 要球。":"你是 01：观察队友，按住 Q 准备，松开传球。");
            Cursor.lockState=acceptsInput?CursorLockMode.Locked:CursorLockMode.None;
            ComputeTargets();
        }
        public void TeleportPlayer(Vector3 p,float yaw)
        {
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;
            player.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));cc.enabled=true;
            Physics.SyncTransforms();
        }
        void RebindMap()
        {
            var map=player.planSymbols;map.subjects[0]=player.transform;
            int slot=1;
            for(int i=0;i<6;i++)
            {
                if(i==Controlled)continue;
                map.subjects[slot]=actors[i].transform;
                map.badges[slot].GetComponent<Renderer>().sharedMaterial=mapMaterials[i];slot++;
            }
        }
        public void OpenMenu()
        {
            MenuOpen=true;Running=false;ball.CancelCharge();player.SetOverview(false);
            ball.Pose(); // Freeze the displayed ball at the same simulation instant as the menu.
            Cursor.lockState=CursorLockMode.None;
        }
        public void Resume()
        {MenuOpen=false;Running=true;Cursor.lockState=acceptsInput?CursorLockMode.Locked:CursorLockMode.None;}
        public void SelectReceiver(int slot)
        {
            int n=0;for(int i=0;i<3;i++)if(i!=Controlled){if(n++==slot)SelectedReceiver=i;}
        }
        public void RequestPass()
        {
            if(!Running)return;
            if(ball.Holder==Controlled){Notify("球已在你手上。1 / 2 选择队友，松开 Q 传球。");return;}
            if(ball.Holder>=3||Phase==TacticalPhase.Turnover){Notify("本回合结束，按 R 重试。");return;}
            requestUntil=Time.time+3.5f;
            string reason;
            bool possible=CanPass(ball.Holder,Controlled,out reason);
            PassReason=possible?"已举手要球，队友会在传球窗口出球。":reason;
            Notify(PassReason);aiPassCooldown=Mathf.Min(aiPassCooldown,.22f);
        }
        public bool CanPass(int from,int to,out string reason)
        {
            if(from<0||from>=3||to<0||to>=3||from==to){reason="球正在传递，请稍候。";return false;}
            float distance=TacticalRules.Distance(Position(from),Position(to));
            if(distance>11){reason="距离太远，向持球人靠近一些。";return false;}
            if(distance<1.05f){reason="距离太近，先拉开接球空间。";return false;}
            Vector3 start=Position(from)+Vector3.up*1.27f+(Position(to)-Position(from)).normalized*.4f;
            Vector3 end=Position(to)+Vector3.up*1.27f;float duration=Mathf.Clamp(Vector3.Distance(start,end)/8,.25f,1.4f);
            for(int step=1;step<=8;step++)
            {
                float t0=(step-1)/8f,t1=step/8f;
                for(int i=3;i<6;i++)
                {
                    var d0=Position(i)+Vector3.ClampMagnitude(actors[i].Velocity*(duration*t0),.65f);
                    var d1=Position(i)+Vector3.ClampMagnitude(actors[i].Velocity*(duration*t1),.65f);
                    float entry;bool secured;
                    if(TacticalRules.DefensiveContact(TacticalRules.PassPoint(start,end,t0),TacticalRules.PassPoint(start,end,t1),d0,d1,Facing(i),out entry,out secured,.15f))
                    {reason="传球线被 "+Number(i)+" 挡住：横移拉开角度，再按 E。";return false;}
                }
            }
            reason="传球窗口开放";return true;
        }
        public void ReportPass(int from,int to)
        {lastPasser=from;Notify(Number(from)+" → "+Number(to)+"：球在空中，靠近落点接球。");aiPassCooldown=1.1f;PassStarted?.Invoke(from,to);}
        public void ReportCatch(int id)
        {
            PassReason="";
            Notify(Number(id)+" 接球。"+(id==Controlled?"你现在持球。":"你仍控制 "+Number(Controlled)+"，继续跑位。"));
            if(Drill==TacticalDrill.HighPostCut && id==1 && lastPasser==0 && Phase==TacticalPhase.HighPost)
            {
                postReceived=true;
                if(TacticalRules.Distance(Position(1),new Vector3(.1f,0,-5.8f))<1.1f)ChangePhase(TacticalPhase.Cut);
                else Notify("04 已接球：先到高位光圈，01 再开始空切。");
            }
            else if(Drill==TacticalDrill.HighPostCut && id==0 && lastPasser==1 && Phase==TacticalPhase.Cut)
            {cutReceived=true;if(Position(0).z>-5.6f)Complete();}
            else if(Drill==TacticalDrill.PickAndRoll && Phase==TacticalPhase.Roll && id==1 && lastPasser==0)
            {rollReceived=true;if(Position(1).z>-6.7f)Complete();}
            BallReceived?.Invoke(id);
        }
        public void Turnover(string reason){ChangePhase(TacticalPhase.Turnover);Notify(reason);if(guidanceRing)guidanceRing.gameObject.SetActive(false);}
        void Complete(){ChangePhase(TacticalPhase.Complete);Notify("配合完成：已创造接球空间。可以继续传球，或按 R 再练一次。");}
        void Notify(string text){Message=text;noticeUntil=Time.time+4;}
        void ChangePhase(TacticalPhase phase)
        {if(Phase==phase)return;Phase=phase;phaseTime=0;PhaseChanges++;PhaseChanged?.Invoke(phase);}
        void Update()
        {
            if(!initialized)return;
            if(acceptsInput&&Input.GetKeyDown(KeyCode.Escape)){if(MenuOpen)Resume();else OpenMenu();}
            if(!Running)return;
            if(acceptsInput)
            {
                if(Input.GetKeyDown(KeyCode.R)){StartDrill(Drill,Controlled==1);return;}
                if(Input.GetKeyDown(KeyCode.F1))help=!help;
                if(Input.GetMouseButtonDown(0))Cursor.lockState=CursorLockMode.Locked;
                var look=Cursor.lockState==CursorLockMode.Locked||Input.GetMouseButton(1)?new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*2:Vector2.zero;
                player.Drive(new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")),look,Time.deltaTime,
                    Input.GetKey(KeyCode.Tab),Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
                if(Input.GetKeyDown(KeyCode.Alpha1))SelectReceiver(0);
                if(Input.GetKeyDown(KeyCode.Alpha2))SelectReceiver(1);
                if(Input.GetKeyDown(KeyCode.E))RequestPass();
                if(Input.GetKeyDown(KeyCode.Q))ball.BeginCharge();
                if(Input.GetKeyUp(KeyCode.Q)&&ball.Charging)ball.Release(SelectedReceiver);
            }
            if(Phase==TacticalPhase.Turnover)return;
            phaseTime+=Time.deltaTime;aiPassCooldown-=Time.deltaTime;
            AdvancePhase();
            think-=Time.deltaTime;
            if(think<=0){ComputeTargets();DecidePass();think=decisionInterval;}
            MoveActors(Time.deltaTime);UpdateGuidance();
        }
        void AdvancePhase()
        {
            if(Drill==TacticalDrill.PickAndRoll)
            {
                if(Phase==TacticalPhase.Assemble)
                {
                    if(ball.Holder==0&&TacticalRules.Distance(Position(1),screen)<.80f)screenHold+=Time.deltaTime;else screenHold=0;
                    if(screenHold>.65f){ChangePhase(TacticalPhase.ScreenReady);Notify("掩护已站定：01 从掩护右侧绕过，04 再顺下。");}
                }
                if(Phase==TacticalPhase.ScreenReady&&(TacticalRules.Distance(Position(1),screen)>1.05f||ball.Holder!=0))
                {screenHold=0;ChangePhase(TacticalPhase.Assemble);Notify("掩护尚未使用：01 持球，04 回到圈内站定。");}
                if(Phase==TacticalPhase.ScreenReady&&ball.Holder==0&&Position(0).x>.65f&&Position(0).z>-8.9f)
                {ChangePhase(TacticalPhase.Roll);Notify("防守已被牵动：04 顺下，01 观察传球窗口。");}
                if(Phase==TacticalPhase.Roll&&rollReceived&&ball.Holder==1&&Position(1).z>-5.5f)Complete();
            }
            else
            {
                if(Phase==TacticalPhase.HighPost&&postReceived&&ball.Holder==1&&TacticalRules.Distance(Position(1),new Vector3(.1f,0,-5.8f))<1.1f)
                    ChangePhase(TacticalPhase.Cut);
                if(Phase==TacticalPhase.Cut&&cutReceived&&ball.Holder==0&&Position(0).z>-4.9f)Complete();
            }
            if(Phase!=TacticalPhase.Complete&&Phase!=TacticalPhase.Turnover&&phaseTime>18&&Time.time>noticeUntil)
                Notify(Phase==TacticalPhase.Assemble?"队形可自由调整。04 到光圈站定；也可按 R 重新开始。":"暂未出现窗口。横移或回到光圈接应；R 可重新开始。");
        }
        void ComputeTargets()
        {
            for(int i=0;i<6;i++)targets[i]=Position(i);
            targets[2]=new Vector3(Position(0).x>3?-6.0f:6.0f,0,-2.25f);
            if(Drill==TacticalDrill.PickAndRoll)
            {
                targets[1]=(Phase==TacticalPhase.Assemble||Phase==TacticalPhase.ScreenReady)?screen:new Vector3(1.65f,0,-3.9f);
                if(Phase==TacticalPhase.ScreenReady)targets[0]=new Vector3(1.8f,0,-7.7f);
                if(Phase==TacticalPhase.Roll)targets[0]=new Vector3(3.65f,0,-6.5f);
            }
            else
            {
                targets[1]=new Vector3(.1f,0,-5.8f);
                if(Phase==TacticalPhase.Cut)targets[0]=Position(0).z<-6.3f?new Vector3(-4.7f,0,-5.7f):new Vector3(-1.6f,0,-3.8f);
            }
            for(int i=0;i<3;i++)actors[i].Action=ball.Holder==i?ActorAction.Dribble:ActorAction.Space;
            if(Phase==TacticalPhase.ScreenReady)actors[1].Action=ActorAction.Screen;
            if(Phase==TacticalPhase.Roll)actors[1].Action=ActorAction.Roll;
            if(Phase==TacticalPhase.Cut)actors[0].Action=ActorAction.Cut;
            if(ball.Flying)actors[ball.Target].Action=ActorAction.Receive;
            if(requestUntil>Time.time&&Controlled!=ball.Holder)actors[Controlled].Action=ActorAction.Receive;
            Vector3 rim=new Vector3(0,0,-1.575f);
            int owner=ball.Holder>=0?ball.Holder:ball.Target;
            for(int i=3;i<6;i++)
            {
                int mark=i-3;Vector3 pos=Position(mark);Vector3 toward=(rim-pos).normalized;
                float gap=mark==owner?1.25f:1.45f;
                targets[i]=pos+toward*gap;actors[i].Action=mark==owner?ActorAction.HandsUp:ActorAction.Slide;
                if(i==4&&Phase==TacticalPhase.Roll&&phaseTime<1.25f)
                {targets[i]=Position(0)+(rim-Position(0)).normalized*1.5f;actors[i].Action=ActorAction.Help;}
                if(i==5&&owner>=0&&owner<3&&Position(owner).z>-5.6f&&Mathf.Abs(Position(owner).x)<3.5f)
                {targets[i]=Vector3.Lerp(pos,Position(owner),.55f)+Vector3.forward*.45f;actors[i].Action=ActorAction.Help;}
            }
            if(Phase==TacticalPhase.Complete)for(int i=0;i<3;i++)targets[i]=Position(i);
            if(passStepActor>=0&&Time.time<passStepUntil&&ball.Holder==passStepActor)
                targets[passStepActor]=passStep;
        }
        void DecidePass()
        {
            if(ball.Holder<0||ball.Holder>=3||ball.Holder==Controlled||aiPassCooldown>0)return;
            int to=-1;
            if(requestUntil>Time.time)to=Controlled;
            else if(Drill==TacticalDrill.HighPostCut&&Phase==TacticalPhase.HighPost&&ball.Holder==0&&TacticalRules.Distance(Position(1),new Vector3(.1f,0,-5.8f))<.8f)to=1;
            else if(Drill==TacticalDrill.HighPostCut&&Phase==TacticalPhase.Cut&&ball.Holder==1&&Position(0).z>-5.8f)to=0;
            else if(Drill==TacticalDrill.PickAndRoll&&Phase==TacticalPhase.Roll&&ball.Holder==0&&Position(1).z>-6.8f)to=1;
            if(to<0)return;
            string reason;
            if(CanPass(ball.Holder,to,out reason)){ball.Launch(to);requestUntil=0;PassReason="";}
            else
            {
                if(requestUntil>Time.time)PassReason=reason;
                // An AI passer may change its angle, but does not move the human receiver or gift possession.
                if(Time.time>=passStepUntil)
                {
                    Vector3 origin=Position(ball.Holder),toward=(Position(to)-origin).normalized;
                    Vector3 side=Vector3.Cross(Vector3.up,toward);
                    float best=-1;Vector3 choice=origin;
                    foreach(float sign in new[]{-1f,1f})
                    {
                        Vector3 candidate=TacticalRules.ClampCourt(origin+side*(sign*1.6f));
                        float clearance=10;
                        for(int i=3;i<6;i++)
                        {float t;clearance=Mathf.Min(clearance,TacticalRules.SegmentDistance(Position(i),candidate,Position(to),out t));}
                        if(clearance>best){best=clearance;choice=candidate;}
                    }
                    passStep=choice;passStepActor=ball.Holder;passStepUntil=Time.time+1.1f;
                    targets[passStepActor]=passStep;
                }
            }
        }
        void MoveActors(float dt)
        {
            for(int i=0;i<6;i++)
            {
                if(i==Controlled)continue;
                var root=actors[i].transform;Vector3 from=root.position;
                bool receiving=ball.Flying&&ball.Target==i;
                Vector3 to=TacticalRules.ClampCourt(receiving?from:targets[i]);
                Vector3 direction=to-from;direction.y=0;
                float speed=i>=3?defenseSpeed:offenseSpeed;
                // A planted screener is a real obstacle; defenders route around it instead of passing through.
                Vector3 step=direction.sqrMagnitude>.015f?Vector3.ClampMagnitude(direction,speed*dt):Vector3.zero;
                for(int j=0;j<6;j++)
                {
                    if(j==i)continue;
                    var delta=TacticalRules.Flat(from+step-Position(j));float d=delta.magnitude;
                    if(d<.66f&&d>.001f)
                    {
                        Vector3 normal=delta/d;
                        if(Vector3.Dot(step,normal)<0)step-=Vector3.Dot(step,normal)*normal;
                        step+=normal*(.66f-d)*Mathf.Min(dt*8,1);
                        if(direction.magnitude>.5f)step+=Vector3.Cross(Vector3.up,normal)*dt*.3f;
                    }
                }
                root.position=TacticalRules.ClampCourt(from+step);
                Vector3 face=i>=3?Position(i-3)-root.position:(ball.Holder==i?new Vector3(0,0,-1.6f)-root.position:direction);
                if(receiving&&ball.Holder<0)face=ball.Position-root.position;
                face.y=0;if(face.sqrMagnitude>.01f)root.rotation=Quaternion.Slerp(root.rotation,Quaternion.LookRotation(face),dt*8);
            }
        }
        public Vector3 GuidancePoint()
        {
            if(Drill==TacticalDrill.PickAndRoll)
                return Controlled==1?(Phase==TacticalPhase.Assemble||Phase==TacticalPhase.ScreenReady?screen:new Vector3(1.65f,0,-3.9f)):
                    (Phase==TacticalPhase.Assemble?new Vector3(-.8f,0,-9.5f):new Vector3(2.5f,0,-7f));
            return Controlled==1?new Vector3(.1f,0,-5.8f):(Phase==TacticalPhase.Cut?new Vector3(-1.6f,0,-3.8f):new Vector3(-3.8f,0,-9.3f));
        }
        void UpdateGuidance()
        {
            if(!guidanceRing)return;guidanceRing.gameObject.SetActive(Phase!=TacticalPhase.Complete&&Phase!=TacticalPhase.Turnover);
            guidanceRing.position=GuidancePoint()+Vector3.up*.035f;
        }
        void OnApplicationFocus(bool focused){if(!focused&&initialized&&acceptsInput)OpenMenu();}
        void OnDisable(){Cursor.lockState=CursorLockMode.None;}

        void OnGUI()
        {
            if(!initialized)return;
            TacticalUI.Draw(this,ref menuDrill,ref menuRole,help);
        }
    }
}
