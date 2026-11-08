using UnityEngine;

namespace CourtX
{
    [DefaultExecutionOrder(80)]
    public sealed class TacticalBall : MonoBehaviour
    {
        public TacticalSession session;
        public PreviewBasketball visual;
        public int Holder { get; private set; } = 0;
        public int Target { get; private set; } = -1;
        public bool Flying => Holder<0 && Target>=0;
        public bool Charging { get; private set; }
        public float Charge { get; private set; }
        public int CompletedPasses { get; private set; }
        public int Interceptions { get; private set; }
        public int Deflections { get; private set; }
        public int LastContactActor { get; private set; }=-1;
        public bool Deflected { get; private set; }
        public Vector3 CatchPoint=>TacticalRules.Flat(end);
        public float ReleaseSpeed=>Mathf.Lerp(6.8f,10.5f,Charge);
        Vector3 start,end;
        Vector3 catchOrigin;
        float catchBlend=1;
        Vector3 dropPosition,dropVelocity;
        bool dropping;
        readonly Vector3[] previousDefenders=new Vector3[6];
        Vector3 previousReceiver;
        float elapsed,duration,heldTime,bounce;
        public Vector3 Position=>dropping?dropPosition:Flying?FlightPosition():visual.ball.transform.position;
        public void ResetBall(int holder)
        {
            Holder=holder;Target=-1;Charging=false;Charge=0;heldTime=0;bounce=0;
            CompletedPasses=Interceptions=Deflections=0;LastContactActor=-1;Deflected=false;dropping=false;catchBlend=1;
            var body=visual.ball;
            if(!body.isKinematic) {body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
            body.isKinematic=true;body.detectCollisions=false;body.interpolation=RigidbodyInterpolation.None;Pose();
        }
        public void BeginCharge()
        {if(Holder==session.Controlled && session.Running){Charging=true;Charge=0;}}
        public void CancelCharge() {Charging=false;Charge=0;}
        public bool Release(int target)
        {
            if(!Charging||Holder!=session.Controlled||!session.Running){CancelCharge();return false;}
            float speed=ReleaseSpeed;CancelCharge();
            return Launch(target,speed);
        }
        public bool Launch(int target,float speed=8f)
        {
            if(!session.Running||Holder<0||Holder>=3||target<0||target>=3||target==Holder)return false;
            int passer=Holder;
            var plan=PlanPass(passer,target,speed);start=plan.Origin;end=plan.Destination;duration=plan.Duration;
            elapsed=0;Target=target;Holder=-1;Charging=false;
            dropping=false;Deflected=false;LastContactActor=-1;
            for(int i=3;i<6;i++)previousDefenders[i]=session.Position(i);
            previousReceiver=session.Position(target);
            session.ReportPass(passer,target);Pose();return true;
        }
        public TacticalPassPlan PlanPass(int from,int to,float speed=8f)
        {
            var origin=session.Position(from);var receiver=session.Position(to);
            var destination=receiver;float travel=0;
            // A fixed, bounded lead rather than a homing ball. Recompute only before release.
            for(int i=0;i<4;i++)
            {
                var release=origin+(destination-origin).normalized*.4f;
                travel=Mathf.Clamp(TacticalRules.Distance(release,destination)/Mathf.Max(4,speed),.25f,1.4f);
                destination=TacticalRules.ClampCourt(receiver+Vector3.ClampMagnitude(session.Velocity(to)*travel,2.5f));
            }
            var startPoint=origin+(destination-origin).normalized*.4f+Vector3.up*1.27f;
            var endPoint=destination+Vector3.up*1.27f;
            return new TacticalPassPlan{Origin=startPoint,Destination=endPoint,Duration=Mathf.Clamp(Vector3.Distance(startPoint,endPoint)/Mathf.Max(4,speed),.25f,1.4f)};
        }
        void Update()
        {
            if(!session.Running)return;
            if(dropping){AdvanceDrop(Time.deltaTime);return;}
            catchBlend=Mathf.Min(1,catchBlend+Time.deltaTime/.16f);
            if(Charging)Charge=Mathf.Clamp01(Charge+Time.deltaTime/1.1f);
            if(!Flying){heldTime+=Time.deltaTime;bounce+=Time.deltaTime*(7.5f+session.player.Speed*.6f);return;}
            Vector3 previous=FlightPosition();
            float flightStep=Mathf.Min(Time.deltaTime,duration-elapsed);
            float frameFraction=flightStep/Mathf.Max(Time.deltaTime,.000001f);
            elapsed=Mathf.Min(duration,elapsed+Time.deltaTime);
            Vector3 next=FlightPosition();
            // Ball and players must share the same arrival instant on a partial final frame.
            Vector3 receiverAtArrival=Vector3.Lerp(previousReceiver,session.Position(Target),frameFraction);
            previousReceiver=session.Position(Target);
            Vector3 contactRoot=Vector3.zero;
            float earliest=float.PositiveInfinity;int contact=-1;bool secured=false;
            for(int i=3;i<6;i++)
            {
                float entry;bool caught;
                var defenderAtArrival=Vector3.Lerp(previousDefenders[i],session.Position(i),frameFraction);
                if(TacticalRules.DefensiveContact(previous,next,previousDefenders[i],defenderAtArrival,session.Facing(i),out entry,out caught)&&entry<earliest)
                {earliest=entry;contact=i;secured=caught;contactRoot=Vector3.Lerp(previousDefenders[i],defenderAtArrival,entry);}
                previousDefenders[i]=session.Position(i);
            }
            if(contact>=0)
            {
                LastContactActor=contact;var point=Vector3.Lerp(previous,next,earliest);
                if(secured)
                {
                    Holder=contact;Target=-1;Interceptions++;heldTime=0;
                    catchOrigin=point;catchBlend=0;
                    session.actors[contact].Action=ActorAction.Receive;
                    session.Turnover(session.Number(contact)+" 看到了传球并接稳了。横移改变角度，R 重试。");
                }
                else
                {
                    Deflected=true;Deflections++;
                    Vector3 normal=TacticalRules.Flat(point-contactRoot).normalized;
                    Vector3 velocity=Vector3.Reflect((next-previous)/Mathf.Max(flightStep,.001f),normal)*.28f+Vector3.up;
                    Drop(point,velocity,"球碰到 "+session.Number(contact)+" 被弹开，防守人没有接稳。R 重试。");
                }
                return;
            }
            if(elapsed<duration)return;
            int receiver=Target;
            // The target point is fixed at release; running away can genuinely miss the catch.
            if(TacticalRules.Distance(receiverAtArrival,end)>.78f)
            {
                Drop(end,(next-previous)/Mathf.Max(flightStep,.001f)*.22f,"传球落空。接球时靠近落点，按 R 重试。");return;
            }
            Holder=receiver;Target=-1;heldTime=0;catchOrigin=end;catchBlend=0;CompletedPasses++;session.ReportCatch(receiver);
        }
        Vector3 FlightPosition()
        {
            float t=Mathf.Clamp01(elapsed/Mathf.Max(duration,.01f));
            return TacticalRules.PassPoint(start,end,t);
        }
        void Drop(Vector3 point,Vector3 velocity,string reason)
        {
            Holder=-1;Target=-1;Charging=false;dropping=true;dropPosition=point;dropVelocity=velocity;
            visual.ball.position=point;session.Turnover(reason);
        }
        void AdvanceDrop(float dt)
        {
            dropPosition+=dropVelocity*dt+Vector3.down*(4.905f*dt*dt);dropVelocity+=Vector3.down*(9.81f*dt);
            if(dropPosition.y<.122f)
            {
                dropPosition.y=.122f;dropVelocity.y=Mathf.Abs(dropVelocity.y)<.7f?0:-dropVelocity.y*.24f;
                dropVelocity.x*=.72f;dropVelocity.z*=.72f;
            }
            dropVelocity.x*=Mathf.Exp(-dt);dropVelocity.z*=Mathf.Exp(-dt);
            if(Mathf.Abs(dropPosition.x)>7.25f){dropPosition.x=Mathf.Clamp(dropPosition.x,-7.25f,7.25f);dropVelocity.x*=-.3f;}
            if(dropPosition.z< -13.7f||dropPosition.z>-.3f){dropPosition.z=Mathf.Clamp(dropPosition.z,-13.7f,-.3f);dropVelocity.z*=-.3f;}
        }
        void LateUpdate()=>Pose();
        public void Pose()
        {
            if(!visual||!session.player)return;
            var ball=visual.ball.transform;
            if(dropping){ball.position=dropPosition;if(session.Running)ball.Rotate(dropVelocity.z*40*Time.deltaTime,0,-dropVelocity.x*40*Time.deltaTime,Space.World);}
            else if(Flying)ball.position=FlightPosition();
            else if(Holder>=0)
            {
                bool mine=Holder==session.Controlled;
                var root=mine?session.player.transform:session.actors[Holder].transform;
                if(Charging&&mine)ball.position=session.player.eyes.transform.TransformPoint(.12f,-.28f,.67f);
                else if(Holder>=3||heldTime<.3f)ball.position=root.TransformPoint(.12f,1.27f,.48f);
                else ball.position=root.TransformPoint(.43f,.14f+Mathf.Abs(Mathf.Sin(bounce))*1.10f,mine?1.02f:.52f);
                if(catchBlend<1)ball.position=Vector3.Lerp(catchOrigin,ball.position,Mathf.SmoothStep(0,1,catchBlend));
                if(session.Running)ball.Rotate(40*Time.deltaTime,0,55*Time.deltaTime,Space.World);
            }
            bool showHand=session.Running&&Holder==session.Controlled&&!session.player.overview.enabled;
            visual.forearm.gameObject.SetActive(showHand);visual.palm.gameObject.SetActive(showHand);
            if(showHand)
            {
                var root=session.player.transform;
                Vector3 hand=Charging?ball.position+new Vector3(.13f,-.04f,0):root.TransformPoint(.44f,Mathf.Max(.97f,ball.position.y+.14f),.93f);
                Vector3 elbow=root.TransformPoint(.50f,1.15f,.34f),wrist=hand-root.forward*.09f;
                visual.forearm.position=(elbow+wrist)*.5f;visual.forearm.rotation=Quaternion.FromToRotation(Vector3.up,wrist-elbow);
                visual.forearm.localScale=new Vector3(.085f,(elbow-wrist).magnitude*.5f,.085f);
                visual.palm.position=hand;visual.palm.rotation=root.rotation*Quaternion.Euler(Charging?-35:12,0,0);
            }
            visual.planBall.gameObject.SetActive(session.player.overview.enabled);
            visual.planBall.position=new Vector3(ball.position.x,4.6f,ball.position.z);
        }
    }
}
