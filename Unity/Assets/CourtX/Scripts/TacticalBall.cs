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
        Vector3 start,end;
        float elapsed,duration,heldTime,bounce;
        public Vector3 Position=>visual.ball.position;
        public void ResetBall(int holder)
        {
            Holder=holder;Target=-1;Charging=false;Charge=0;heldTime=0;bounce=0;
            CompletedPasses=Interceptions=0;
            var body=visual.ball;
            if(!body.isKinematic) {body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
            body.isKinematic=true;body.detectCollisions=false;Pose();
        }
        public void BeginCharge()
        {if(Holder==session.Controlled && session.Running){Charging=true;Charge=0;}}
        public void CancelCharge() {Charging=false;Charge=0;}
        public bool Release(int target)
        {
            float speed=Mathf.Lerp(6.8f,10.5f,Charge);CancelCharge();
            return Launch(target,speed);
        }
        public bool Launch(int target,float speed=8f)
        {
            if(!session.Running||Holder<0||Holder>=3||target<0||target>=3||target==Holder)return false;
            int passer=Holder;
            var toward=(session.Position(target)-session.Position(passer)).normalized;
            start=session.Position(passer)+Vector3.up*1.27f+toward*.4f;
            end=session.Position(target)+Vector3.up*1.27f;
            duration=Mathf.Clamp(Vector3.Distance(start,end)/Mathf.Max(4,speed),.25f,1.4f);
            elapsed=0;Target=target;Holder=-1;Charging=false;
            session.ReportPass(passer,target);Pose();return true;
        }
        void Update()
        {
            if(!session.Running)return;
            if(Charging)Charge=Mathf.Clamp01(Charge+Time.deltaTime/1.1f);
            if(!Flying){heldTime+=Time.deltaTime;bounce+=Time.deltaTime*(7.5f+session.player.Speed*.6f);return;}
            Vector3 previous=FlightPosition();elapsed+=Time.deltaTime;
            Vector3 next=FlightPosition();
            // Swept segment prevents fast passes tunnelling through a defender.
            for(int i=3;i<6;i++)
                {
                    float along;float d=TacticalRules.SegmentDistance(session.Position(i),previous,next,out along);
                    if(d<.43f && next.y<2.15f)
                    {
                        Holder=i;Target=-1;Interceptions++;heldTime=0;
                        session.Turnover("被 "+session.Number(i)+" 截断了。观察传球线，按 R 重试。");return;
                    }
                }
            if(elapsed<duration)return;
            int receiver=Target;
            // The target point is fixed at release; running away can genuinely miss the catch.
            if(TacticalRules.Distance(session.Position(receiver),end)>1.05f)
            {
                Holder=-1;Target=-1;visual.ball.position=end;
                session.Turnover("传球落空。接球时靠近落点，按 R 重试。");return;
            }
            Holder=receiver;Target=-1;heldTime=0;CompletedPasses++;session.ReportCatch(receiver);
        }
        Vector3 FlightPosition()
        {
            float t=Mathf.Clamp01(elapsed/Mathf.Max(duration,.01f));
            return Vector3.Lerp(start,end,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.34f);
        }
        void LateUpdate()=>Pose();
        public void Pose()
        {
            if(!visual||!session.player)return;
            var ball=visual.ball.transform;
            if(Flying)ball.position=FlightPosition();
            else if(Holder>=0)
            {
                bool mine=Holder==session.Controlled;
                var root=mine?session.player.transform:session.actors[Holder].transform;
                if(Charging&&mine)ball.position=session.player.eyes.transform.TransformPoint(.12f,-.28f,.67f);
                else if(Holder>=3||heldTime<.3f)ball.position=root.TransformPoint(.12f,1.27f,.48f);
                else ball.position=root.TransformPoint(.43f,.14f+Mathf.Abs(Mathf.Sin(bounce))*1.10f,mine?1.02f:.52f);
                ball.Rotate(40*Time.deltaTime,0,55*Time.deltaTime,Space.World);
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
