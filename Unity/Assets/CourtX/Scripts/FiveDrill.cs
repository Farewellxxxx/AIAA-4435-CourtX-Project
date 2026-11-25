using System;
using UnityEngine;

namespace CourtX
{
    public enum FiveDrillStage { Prefix, Decision, Resolve, Complete }
    public enum FiveEarlyPass { Unused, Outbound, Gather, Returning, Returned, Shot }

    [Serializable]
    public sealed class FiveDrillRound
    {
        public FiveDrillStage stage;
        public FiveEarlyPass earlyPass;
        public int earlyReceiver=-1;
        public float earlyReleaseTime,earlyCatchTime;
        public bool variant,decisionCorrect,reasonableChoice,received;
        public bool entrySet,entered,recoveringPrefix;
        public int recoveryActor;
        public int actor,decisionPhase,assignedTarget,expectedTarget,chosenTarget=FiveDrill.Unchosen;
        public int readDefender,interceptedBy=-1,nextPassFrom=-1,nextPassTo=-1,shooter=-1;
        public float decisionOpened,decisionTime,releaseTime,catchTime,finishTime;
        public float laneClearance,receiverSpace,shooterSpace,shotDistance;
        public Vector2 defenderAtRelease,ballAtRelease,targetAtRelease;
        [NonSerialized] internal Vector2 previousWingPosition,wingVelocity;
        public string outcome="";
        public FiveDrillRound Copy()=>(FiveDrillRound)MemberwiseClone();
    }

    // A round borrows the lesson's actual passing, cutting and screening timeline.
    // The last read is played by the human. The source frames remain shared and read-only.
    public static class FiveDrill
    {
        public const int Unchosen=-2,Shot=-1;

        sealed class Read
        {
            public readonly int phase,actor,original,alternate,defender,extra;
            public Read(int phase,int actor,int original,int alternate,int defender,int extra=-1)
            {
                this.phase=phase;this.actor=actor;this.original=original;this.alternate=alternate;
                this.defender=defender;this.extra=extra;
            }
        }

        // Roles/targets are O1..O5 = 0..4; defensive reads are X1..X5 = 5..9.
        static readonly Read[,] reads={
            {new Read(3,4,Shot,3,9),new Read(3,4,3,1,8,2),new Read(3,4,2,3,7)},
            {new Read(3,0,1,4,8,3),new Read(3,0,4,3,7),new Read(3,0,Shot,4,6)},
            {new Read(4,1,0,3,8),new Read(4,1,4,3,9,2),new Read(5,1,2,3,5,2)}
        };
        static Read Situation(FivePlay play,int branch)
        {
            if((uint)play>=3||branch<0||branch>=3)throw new ArgumentOutOfRangeException(nameof(branch));
            return reads[(int)play,branch];
        }

        public static FiveState Create(FivePlay play,int branch,bool variant)
        {
            var read=Situation(play,branch);
            var s=FiveCoach.Create(play,read.actor,branch);
            s.drill=new FiveDrillRound{actor=read.actor,decisionPhase=read.phase,variant=variant,
                readDefender=read.defender,assignedTarget=variant?read.alternate:read.original,
                expectedTarget=variant?read.alternate:read.original,previousWingPosition=s.positions[read.actor]};
            FiveRules.Event(s,"本轮开始。");FiveRules.Refresh(s);
            return s;
        }

        public static FiveState RandomRound()
        {
            int index=UnityEngine.Random.Range(0,9);
            return Create((FivePlay)(index/3),index%3,UnityEngine.Random.value<.5f);
        }

        public static bool CanChoose(FiveState s)=>s?.drill!=null&&s.drill.stage==FiveDrillStage.Decision&&
            s.holder==s.drill.actor&&s.flight==null;
        public static bool CanPassDuringCut(FiveState s)
        {
            if(s?.drill==null||s.play!=FivePlay.Horns||s.drill.stage!=FiveDrillStage.Prefix||
                s.drill.earlyPass!=FiveEarlyPass.Unused||s.holder!=s.drill.actor||s.flight!=null||
                s.turnover||s.phase<1||s.phase>3)return false;
            // The three shooter branches share the cut through the paint to the right corner.
            return s.phase<3||!AtMark(s,0,FiveZoneBook.Frame(s.play,s.branch,3).offense[0],FiveCoach.Arrival);
        }
        public static bool IsFinished(FiveState s)=>s?.drill!=null&&s.drill.stage==FiveDrillStage.Complete;
        internal static Vector2 EntrySpot(FiveState s) =>
            FiveZoneBook.Frame(s.play,s.branch,s.drill.decisionPhase).offense[s.drill.actor];

        // The opening wing feed starts the cut. Only the later decision
        // handoff requires the player inside its designated region.
        static bool EntryPass(FiveState s)
        {
            var d=s.drill;
            return s.phase>0&&s.phase<d.decisionPhase&&s.holder!=d.actor&&
                (FiveCoach.Receiver(s)==d.actor||s.phase+1==d.decisionPhase);
        }

        static bool CompletedEntryCatch(FiveState s)
        {
            // Earlier recordings have the pass and phase, but no entry latch.
            return s.holder==s.drill.actor&&s.lastPassTo==s.drill.actor&&
                (s.play==FivePlay.HighPost&&s.phase>=2||
                 s.play==FivePlay.Horns&&s.branch==2&&s.phase>=s.drill.decisionPhase);
        }

        internal static bool AwaitingPlayer(FiveState s)
        {
            var d=s.drill;
            return d.stage==FiveDrillStage.Prefix&&d.earlyPass!=FiveEarlyPass.Gather&&!d.entered&&!CompletedEntryCatch(s)&&s.flight==null&&!d.recoveringPrefix&&
                (s.phase==d.decisionPhase||EntryPass(s));
        }

        static bool AtEntry(FiveState s) =>
            Vector2.Distance(s.positions[s.controlled],EntrySpot(s))<=FiveCoach.PlayerArrival;

        internal static bool AtMark(FiveState s,int id,Vector2 mark,float tolerance)
        {
            float distance=Vector2.Distance(s.positions[id],mark);
            if(distance<=tolerance)return true;
            // The user may stand on another player's assigned spot. Accept the
            // adjacent position only once that teammate actually gets there.
            return distance<=.86f&&Vector2.Distance(s.positions[s.controlled],mark)<.8f&&
                Vector2.Distance(s.positions[id],s.positions[s.controlled])<1.1f;
        }

        internal static Vector2 AvoidPlayer(FiveState s,int id,Vector2 desired)
        {
            if(s.drill.stage!=FiveDrillStage.Prefix||id==s.controlled)return desired;
            var route=s.targets[id]-s.positions[id];
            var obstacle=s.positions[s.controlled]-s.positions[id];
            if(route.sqrMagnitude<1f||obstacle.sqrMagnitude>2.25f)return desired;
            var direction=route.normalized;
            float ahead=Vector2.Dot(direction,obstacle);
            var side=new Vector2(-direction.y,direction.x);
            float lateral=Vector2.Dot(side,obstacle);
            if(ahead<=0||ahead>=route.magnitude||Mathf.Abs(lateral)>=.8f)return desired;
            // A collinear obstruction needs a sideways step; separation alone
            // can leave a cutter stuck in front of a stationary pair.
            float sign=Mathf.Abs(lateral)>.01f?-Mathf.Sign(lateral):id%2==0?1f:-1f;
            return desired+side*(sign*(.8f-Mathf.Abs(lateral))*2.6f);
        }

        static bool OpenPass(FiveState s,int to) =>
            Clearance(s,s.positions[s.holder],s.positions[to])>.48f&&FiveRules.Space(s,to)>.7f;

        static bool OpeningWingFeed(FiveState s) => s.play==FivePlay.Horns&&s.phase==0;

        static Vector2 WingCatchPoint(FiveState s)
        {
            var wing=s.positions[s.drill.actor];
            float flight=Mathf.Clamp(Vector2.Distance(s.positions[s.holder],wing)/10.5f,.3f,1.5f);
            return FiveRules.Clamp(wing+s.drill.wingVelocity*(flight*.75f));
        }

        internal static bool AdvancesPrefix(FiveState s,int to)
        {
            var d=s.drill;
            if(d.stage!=FiveDrillStage.Prefix||s.phase>=d.decisionPhase)return false;
            if(EntryPass(s))return d.entrySet&&to==d.actor&&AtEntry(s)&&OpenPass(s,to);
            if((!d.entrySet&&!FiveCoach.Ready(s))||to!=FiveCoach.Receiver(s)||!OpenPass(s,to))return false;
            return !OpeningWingFeed(s)||Clearance(s,s.positions[s.holder],WingCatchPoint(s))>.48f;
        }
        public static string Summary(FiveState s)
        {
            var d=s.drill;
            if(!IsFinished(s))return "本轮进行中。";
            if(s.madeBasket)return "投篮命中，本轮完成。";
            if(s.turnover)
            {
                int blocker=d.interceptedBy>=5&&d.interceptedBy<10?d.interceptedBy:s.holder;
                return blocker>=5&&blocker<10?"传球被 "+FiveRules.Number(blocker)+" 截断，本轮失败。":"未能接到传球，本轮失败。";
            }
            if(d.outcome=="投篮未中")return "投篮未中，本轮结束。";
            return d.outcome=="回合超时"?"本轮超时。":"本轮已结束。";
        }

        static Vector2 Mark(FiveState s,FiveZoneFrame frame,int id,bool variant)
        {
            if(!variant&&s.play==FivePlay.Horns&&s.branch==2&&id==5)
            {
                // Finish the source frame's stunt with one extra step into the
                // elbow passing lane. This is the last read after the return.
                return Vector2.Lerp(frame.offense[1],frame.offense[3],.51f);
            }
            if(variant)switch(s.play)
            {
                case FivePlay.HighPost:
                    switch(s.branch)
                    {
                        case 0: // X5 closes the free-throw-line jumper, leaving a cutter behind.
                            if(id==9)return new Vector2(-.58f,5.03f);
                            if(id==3)return new Vector2(1.6f,2.06f);
                            break;
                        case 1: // X4 tags the interior cutter; X2 digs down, vacating the wing.
                            if(id==8)return Vector2.Lerp(frame.offense[4],frame.offense[3],.72f);
                            if(id==6)return new Vector2(3.8f,5.05f);
                            if(id==5)return new Vector2(-.9f,8.85f);
                            if(id==1)return new Vector2(5.65f,8.1f);
                            if(id==2)return new Vector2(-6.1f,5.55f);
                            break;
                        case 2: // X3 stays out with the weak corner; nobody tags the baseline cut.
                            if(id==7)return Vector2.Lerp(frame.offense[4],frame.offense[2],.79f);
                            if(id==9)return new Vector2(-.50f,4.65f);
                            break;
                    }
                    break;
                case FivePlay.Screen:
                    switch(s.branch)
                    {
                        case 0: // X4 recovers to the corner and X5 helps on the ball. The roller slips inside.
                            if(id==8)return Vector2.Lerp(frame.offense[0],frame.offense[1],.80f);
                            if(id==9)return new Vector2(3.05f,4.58f);
                            if(id==4)return new Vector2(.55f,4.75f);
                            if(id==7)return new Vector2(-2.55f,2.62f);
                            break;
                        case 1: // X3 comes up to take the short roll; the weak baseline is unguarded.
                            if(id==7)return new Vector2(1.12f,5.84f);
                            if(id==4)return new Vector2(.08f,5.91f);
                            if(id==3)return new Vector2(-2.25f,1.67f);
                            if(id==9)return new Vector2(3.65f,4.60f);
                            break;
                        case 2: // X2 recovers to the guard's shoulder as X5 stays deep.
                            if(id==6)return new Vector2(3.35f,6.80f);
                            if(id==4)return new Vector2(.55f,4.48f);
                            break;
                    }
                    break;
                case FivePlay.Horns:
                    switch(s.branch)
                    {
                        case 0: // X4 beats the shooter to the corner lane and X2 sinks with the cut.
                            if(id==8)return Vector2.Lerp(frame.offense[1],frame.offense[0],.72f);
                            if(id==6)return new Vector2(4.88f,6.78f);
                            if(id==3)return new Vector2(.82f,5.86f);
                            break;
                        case 1: // X5 steps onto the screener's release; O4 flashes to the open elbow.
                            if(id==9)return Vector2.Lerp(frame.offense[1],frame.offense[4],.77f);
                            if(id==6)return new Vector2(4.65f,6.88f);
                            if(id==5)return new Vector2(-1.24f,8.3f);
                            if(id==3)return new Vector2(-.65f,6.35f);
                            break;
                        case 2: // X1 jumps the long reversal instead of stunting toward the right wing.
                            if(id==5)return new Vector2(1.45f,8.62f);
                            break;
                    }
                    break;
            }
            return id<5?frame.offense[id]:frame.defense[id-5];
        }

        static Vector2 Recovery(FiveState s,int id,Vector2 mark)
        {
            var d=s.drill;
            if(!d.received)return mark;
            var ball=s.positions[d.chosenTarget];
            if(d.variant&&d.decisionCorrect)
            {
                // The first receiver draws help. An extra pass is available only
                // after the low defender/upper guard has physically rotated.
                if(s.play==FivePlay.Screen&&s.branch==0&&id==7)return ball+new Vector2(-.65f,1.05f);
                if(s.play==FivePlay.Horns&&s.branch>=1&&id==5)return ball+new Vector2(1.06f,.68f);
                if(s.play==FivePlay.HighPost&&s.branch==1&&id==6)return ball+new Vector2(-.38f,-1.08f);
            }
            if(id!=d.readDefender)return mark;
            return Vector2.MoveTowards(mark,ball,Mathf.Clamp(Vector2.Distance(mark,ball)-1.20f,0,1.8f));
        }

        static Vector2 ReadMark(FiveState s,FiveZoneFrame frame,int id,bool variant)
        {
            var d=s.drill;
            var mark=Mark(s,frame,id,variant);
            if(!d.entered&&d.stage==FiveDrillStage.Prefix)return mark;
            // A closeout must meet the real catch. Preserve its angle and
            // distance even when the receiver is on the rim of the 1 m area.
            // More distant outlets and weak-side defenders keep their width.
            var center=frame.offense[d.actor];
            var ball=d.stage==FiveDrillStage.Resolve?d.ballAtRelease:s.positions[d.actor];
            float influence=1f-Mathf.InverseLerp(1.5f,6f,Vector2.Distance(mark,center));
            return FiveRules.Clamp(mark+Vector2.ClampMagnitude(ball-center,FiveCoach.PlayerArrival)*influence);
        }

        static int ReadTarget(FiveState s)
        {
            var read=Situation(s.play,s.branch);
            var frame=FiveZoneBook.Frame(s.play,s.branch,read.phase);
            var p=s.positions[read.defender];
            float original=(p-ReadMark(s,frame,read.defender,false)).sqrMagnitude;
            float alternate=(p-ReadMark(s,frame,read.defender,true)).sqrMagnitude;
            return original<=alternate?read.original:read.alternate;
        }

        static void SupportMovement(FiveState s)
        {
            var anchor=EntrySpot(s);
            // Keep the outlets spaced along the arc. The handler adjusts the
            // passing angle, and the zone shades with the ball on its own side.
            for(int id=0;id<10;id++)
            {
                if(id==s.controlled)continue;
                var mark=s.targets[id];
                var axis=id==s.holder?anchor-mark:mark-FiveFlight.Rim;
                var slide=new Vector2(-axis.y,axis.x).normalized;
                float stride=Mathf.Sin(s.phaseAge*1.04f+id*.87f);
                float length=id==s.holder?.88f:id<5?.74f:.45f;
                s.targets[id]+=slide*(length*stride);
                if(id>=5)s.targets[id]+=Vector2.ClampMagnitude(s.Ball-mark,1f)*
                    (.24f*Mathf.Sin(s.phaseAge*.79f+id));
            }
        }

        internal static void Describe(FiveState s)
        {
            var d=s.drill;
            if(s.phase<d.decisionPhase)FiveCoach.Describe(s);
            else
            {
                var frame=FiveZoneBook.Frame(s.play,s.branch,d.decisionPhase);
                s.screener=-1;s.screenActive=false;
                for(int id=0;id<10;id++)
                {
                    var mark=ReadMark(s,frame,id,d.variant);
                    if(d.stage==FiveDrillStage.Complete)mark=s.positions[id];
                    else if(d.stage!=FiveDrillStage.Prefix)
                    {
                        if(id==s.holder||id==s.controlled)mark=s.positions[id];
                        else if(id<5)
                        {
                            // A receiver can lift or slide a step along its own spacing line.
                            float t=s.time-d.decisionOpened;
                            mark+=new Vector2(.35f*Mathf.Sin(t*.86f+id),.22f*Mathf.Sin(t*.61f+id*2));
                        }
                        else if(d.stage==FiveDrillStage.Resolve)mark=Recovery(s,id,mark);
                        else if(id>=5)
                        {
                            // If the handler dribbles away from the catch spot,
                            // the nearer zone defenders shade with the ball.
                            var displacement=s.positions[d.actor]-frame.offense[d.actor];
                            float travel=Mathf.Max(0,displacement.magnitude-FiveCoach.PlayerArrival);
                            float shade=Vector2.Distance(mark,s.positions[d.actor])<4f?.65f:.28f;
                            mark+=displacement.normalized*Mathf.Min(1.1f,travel*shade);
                        }
                    }
                    s.targets[id]=mark;
                }
            }
            if(d.stage==FiveDrillStage.Prefix)
            {
                if(d.earlyPass==FiveEarlyPass.Gather)s.targets[d.earlyReceiver]=s.positions[d.earlyReceiver];
                else if(AwaitingPlayer(s))
                {
                    if(d.entrySet||s.phase==d.decisionPhase)SupportMovement(s);
                    s.targets[s.controlled]=EntrySpot(s);
                }
                else if(d.recoveringPrefix)s.targets[d.recoveryActor]=s.looseBall;
                else if(d.entrySet&&s.flight==null)SupportMovement(s);
            }
            s.phaseName=d.stage==FiveDrillStage.Prefix?"战术展开":d.stage==FiveDrillStage.Decision?"持球决策":
                d.stage==FiveDrillStage.Resolve?"回合进行中":"回合结束";
            s.attackIntent="";s.defenseIntent="";
            for(int i=0;i<10;i++)s.tasks[i]="";
        }

        static void Next(FiveState s)
        {
            s.drill.entrySet=false;
            FiveCoach.Next(s);
        }

        static void PlayPrefix(FiveState s,float dt)
        {
            var d=s.drill;
            if(!d.entrySet)
            {
                FiveCoach.SettleFrame(s,dt);
                if(!FiveCoach.Ready(s))return;
            }
            int to=FiveCoach.Receiver(s);
            if(EntryPass(s))
            {
                d.entrySet=true;
                if(s.passCooldown<=0)FiveRules.Pass(s,d.actor);
                return;
            }
            if(to<0||s.holder==to){Next(s);return;}
            if(s.passCooldown>0)return;
            if(FiveRules.Pass(s,to))return;
            d.entrySet=true;
            // A handler who dribbled away may have lost the direct return.
            // Use an open safety outlet, then let that teammate feed O1.
            if(s.holder==d.actor)
                for(int id=0;id<5;id++)
                    if(id!=s.holder&&id!=to&&OpenPass(s,id)&&FiveRules.Pass(s,id))return;
        }

        internal static void Tick(FiveState s,float dt)
        {
            var d=s.drill;
            if(OpeningWingFeed(s))
            {
                var position=s.positions[d.actor];
                var velocity=Vector2.ClampMagnitude((position-d.previousWingPosition)/dt,6f);
                d.wingVelocity=Vector2.Lerp(d.wingVelocity,velocity,1f-Mathf.Exp(-dt*12f));
                d.previousWingPosition=position;
            }
            if(d.recoveringPrefix)
            {
                if(Vector2.Distance(s.positions[d.recoveryActor],s.looseBall)<.65f)
                {
                    s.holder=d.recoveryActor;d.recoveringPrefix=false;
                    d.entrySet=true;
                    FiveRules.Event(s,FiveRules.Number(s.holder)+" 控制住球。");
                }
                return;
            }
            if(s.flight!=null||s.turnover||s.holder<0)return;
            if(d.stage==FiveDrillStage.Prefix)
            {
                if(d.earlyPass==FiveEarlyPass.Gather)
                {
                    if(s.time-d.earlyCatchTime<.32f)return;
                    float distance=Vector2.Distance(s.positions[s.holder],FiveFlight.Rim);
                    if(OpenShot(distance,FiveRules.Space(s,s.holder)))FiveRules.ReleaseShot(s,true);
                    else FiveRules.Pass(s,d.actor);
                    return;
                }
                if(s.phase<d.decisionPhase){PlayPrefix(s,dt);return;}
                var frame=FiveZoneBook.Frame(s.play,s.branch,d.decisionPhase);
                if(s.holder==d.actor&&(AtEntry(s)||CompletedEntryCatch(s)))d.entered=true;
                bool ready=s.holder==d.actor&&d.entered;
                for(int i=0;i<10&&ready;i++)
                    if(i!=d.actor)ready=AtMark(s,i,ReadMark(s,frame,i,d.variant),.46f);
                s.settledAge=ready?s.settledAge+dt:0;
                if(s.settledAge>=.28f)
                {
                    d.stage=FiveDrillStage.Decision;d.decisionOpened=s.time;
                    d.expectedTarget=ReadTarget(s);
                    FiveRules.Event(s,"轮到你持球。");
                }
                return;
            }
            if(d.stage!=FiveDrillStage.Resolve||!d.received)return;
            var read=Situation(s.play,s.branch);
            if(d.variant&&d.decisionCorrect&&read.extra>=0&&d.nextPassFrom<0&&s.holder==read.alternate)
            {
                if(s.time-d.catchTime<.90f||s.passCooldown>0)return;
                if(Clearance(s,s.positions[s.holder],s.positions[read.extra])>.58f&&FiveRules.Space(s,read.extra)>1.05f)
                {
                    if(FiveRules.Pass(s,read.extra))return;
                }
                else if(s.time-d.catchTime<2.5f)return;
            }
            // A clean catch-and-shoot takes only the gather; allowing a full
            // second for the defense to recover would erase the passing advantage.
            if(s.time-d.catchTime<.32f)return;
            FiveRules.ReleaseShot(s,true);
        }

        internal static bool MayPass(FiveState s,int to,bool playerChoice,bool advances)
        {
            var d=s.drill;
            if(d.stage==FiveDrillStage.Prefix)
            {
                if(playerChoice)return CanPassDuringCut(s);
                if(d.earlyPass==FiveEarlyPass.Gather)return s.holder==d.earlyReceiver&&to==d.actor;
                return advances||d.entrySet&&!EntryPass(s)&&s.phase<d.decisionPhase&&
                    s.holder==d.actor&&to!=FiveCoach.Receiver(s)&&OpenPass(s,to);
            }
            if(CanChoose(s))return true;
            var read=Situation(s.play,s.branch);
            return !playerChoice&&d.stage==FiveDrillStage.Resolve&&d.received&&d.variant&&
                d.decisionCorrect&&d.nextPassFrom<0&&s.holder==read.alternate&&to==read.extra;
        }

        internal static bool MayShoot(FiveState s,bool followUp)=>CanChoose(s)||
            followUp&&(s.drill.stage==FiveDrillStage.Resolve&&s.drill.received||
                s.drill.stage==FiveDrillStage.Prefix&&s.drill.earlyPass==FiveEarlyPass.Gather&&s.holder==s.drill.earlyReceiver);

        static float Clearance(FiveState s,Vector2 from,Vector2 to)
        {
            float clearance=float.MaxValue;
            for(int i=5;i<10;i++)
            {
                float along;
                clearance=Mathf.Min(clearance,FiveRules.SegmentDistance(s.positions[i],from,to,out along));
            }
            return clearance;
        }

        static void RecordChoice(FiveState s,int to)
        {
            var d=s.drill;
            d.expectedTarget=ReadTarget(s);d.chosenTarget=to;
            d.defenderAtRelease=s.positions[d.readDefender];d.ballAtRelease=s.positions[d.actor];
            d.targetAtRelease=to==Shot?FiveFlight.Rim:s.positions[to];
            d.releaseTime=s.time;d.decisionTime=s.time-d.decisionOpened;
            d.receiverSpace=FiveRules.Space(s,to==Shot?d.actor:to);
            d.laneClearance=Clearance(s,d.ballAtRelease,d.targetAtRelease);
            // Preserve a separate grade for a viable alternative: it must create
            // a genuine scoring opportunity at release, not simply reach a body.
            float basketDistance=Vector2.Distance(to==Shot?d.ballAtRelease:d.targetAtRelease,FiveFlight.Rim);
            bool available=to==Shot?OpenShot(basketDistance,d.receiverSpace):
                d.laneClearance>.48f&&d.receiverSpace>.85f;
            d.decisionCorrect=to==d.expectedTarget&&available;
            bool safeOutlet=FiveRules.Space(s,d.actor)<1.45f&&d.receiverSpace>=2.0f;
            d.reasonableChoice=d.decisionCorrect||available&&(to==Shot?basketDistance<=7.5f:
                d.laneClearance>=.75f&&d.receiverSpace>=1.6f&&(basketDistance<=8.0f||safeOutlet));
            d.stage=FiveDrillStage.Resolve;
        }

        internal static void PassReleased(FiveState s,int from,int to,bool playerChoice)
        {
            var d=s.drill;
            if(d.stage==FiveDrillStage.Prefix&&OpeningWingFeed(s)&&s.flight.advancesBeat)
            {
                s.flight.destination=WingCatchPoint(s);
                s.flight.duration=Mathf.Clamp(Vector2.Distance(s.flight.origin,s.flight.destination)/10.5f,.3f,1.5f);
            }
            else if(d.stage==FiveDrillStage.Prefix&&playerChoice)
            {
                d.earlyPass=FiveEarlyPass.Outbound;
                d.earlyReceiver=to;
                d.earlyReleaseTime=s.time;
                s.flight.advancesBeat=false;
                // Lead a moving cutter towards their current mark, never past it.
                var toward=s.targets[to]-s.positions[to];
                float speed=Mathf.Max(0,Vector2.Dot(s.velocities[to],toward.normalized));
                s.flight.destination=Vector2.MoveTowards(s.positions[to],s.targets[to],speed*s.flight.duration);
                s.flight.duration=Mathf.Clamp(Vector2.Distance(s.flight.origin,s.flight.destination)/10.5f,.3f,1.5f);
            }
            else if(d.stage==FiveDrillStage.Prefix&&d.earlyPass==FiveEarlyPass.Gather)
            {
                d.earlyPass=FiveEarlyPass.Returning;
                s.flight.advancesBeat=false;
            }
            else if(d.stage==FiveDrillStage.Decision)RecordChoice(s,to);
            else if(d.stage==FiveDrillStage.Resolve){d.nextPassFrom=from;d.nextPassTo=to;}
        }

        internal static void ShotReleased(FiveState s,int shooter)
        {
            var d=s.drill;
            if(d.earlyPass==FiveEarlyPass.Gather)d.earlyPass=FiveEarlyPass.Shot;
            else if(d.stage==FiveDrillStage.Decision)RecordChoice(s,Shot);
            d.shooter=shooter;d.shooterSpace=FiveRules.Space(s,shooter);
            d.shotDistance=Vector2.Distance(s.positions[shooter],FiveFlight.Rim);
            // Deterministic finish model. A close defender or an unusually long
            // attempt can cause a miss even after a good preceding tactical read.
            s.flight.misses=!OpenShot(d.shotDistance,d.shooterSpace);
            if(s.flight.misses)
            {
                var ray=(FiveFlight.Rim-s.positions[shooter]).normalized;
                s.flight.destination=FiveFlight.Rim+ray*.48f+new Vector2(ray.y,-ray.x)*.44f;
            }
        }

        static bool OpenShot(float distance,float space)=>distance<=9.2f&&space>=.85f&&(distance<=3f||space>=1.65f);

        internal static void Catch(FiveState s,FiveFlight flight)
        {
            var d=s.drill;
            if(d.stage==FiveDrillStage.Prefix)
            {
                if(d.earlyPass==FiveEarlyPass.Outbound)
                {
                    d.earlyPass=FiveEarlyPass.Gather;
                    d.earlyCatchTime=s.time;
                }
                else if(d.earlyPass==FiveEarlyPass.Returning)d.earlyPass=FiveEarlyPass.Returned;
                else if(flight.advancesBeat)
                {
                    if(flight.to==d.actor&&s.phase>0)d.entered=true;
                    Next(s);
                }
                return;
            }
            if(d.stage==FiveDrillStage.Resolve){d.received=true;d.catchTime=s.time;}
        }

        internal static bool RecoverOpeningFeed(FiveState s,FiveFlight flight)
        {
            var d=s.drill;
            if(d.stage!=FiveDrillStage.Prefix||!OpeningWingFeed(s)||flight.to!=d.actor)return false;
            s.looseBall=flight.destination;s.holder=-1;d.recoveringPrefix=true;
            d.recoveryActor=flight.from;
            FiveRules.Event(s,"队友正在接应落球。");
            return true;
        }

        internal static void Finish(FiveState s,string outcome)
        {
            s.drill.outcome=outcome;s.drill.finishTime=s.time;s.drill.stage=FiveDrillStage.Complete;
        }
    }
}
