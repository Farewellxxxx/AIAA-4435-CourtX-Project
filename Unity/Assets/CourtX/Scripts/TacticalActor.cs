using System.Linq;
using UnityEngine;

namespace CourtX
{
    // Replace this visual adapter with an Animator; gameplay depends only on the root and Action.
    public sealed class TacticalActor : MonoBehaviour
    {
        public int Id;
        public ActorAction Action;
        public Vector3 Velocity { get; private set; }
        Transform[] upper,lower,thigh,leg,shin,shoe;
        Vector3 previous;
        float gait;
        void Awake()
        {
            upper=Parts("Upper arm");lower=Parts("Forearm");thigh=Parts("Shorts");
            leg=Parts("Leg");shin=Parts("Lower leg");shoe=Parts("Shoe");previous=transform.position;
        }
        Transform[] Parts(string name)=>GetComponentsInChildren<Transform>().Where(t=>t.name==name).OrderBy(t=>t.localPosition.x).ToArray();
        public static void Rod(Transform t,Vector3 a,Vector3 b,float width)
        {
            t.localPosition=(a+b)*.5f;t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
            t.localScale=new Vector3(width,(a-b).magnitude*.5f,width);
        }
        public void SetPosition(Vector3 p) {transform.position=p;previous=p;Velocity=Vector3.zero;}
        void LateUpdate()
        {
            Velocity=Time.deltaTime>0?(transform.position-previous)/Time.deltaTime:Vector3.zero;
            previous=transform.position;float speed=Velocity.magnitude;
            gait+=Time.deltaTime*Mathf.Min(speed*3.5f,11);
            if(upper.Length!=2)return;
            bool defense=Id>=3;bool raised=Action==ActorAction.HandsUp||Action==ActorAction.Help;
            for(int i=0;i<2;i++)
            {
                float s=i==0?-1:1;
                float swing=Mathf.Sin(gait+(i==0?0:Mathf.PI))*Mathf.Min(speed*.065f,.21f);
                Vector3 shoulder=new Vector3(s*.205f,1.39f,0),elbow,wrist;
                if(defense)
                {
                    float wave=Mathf.Sin(Time.time*3+Id)*.06f;
                    elbow=new Vector3(s*.40f,raised?1.68f:1.26f,.08f);
                    wrist=new Vector3(s*(raised?.38f:.64f),raised?2.04f+wave:1.30f+wave,.16f);
                }
                else if(Action==ActorAction.Receive||Action==ActorAction.Screen)
                {elbow=new Vector3(s*.32f,1.22f,.18f);wrist=new Vector3(s*.20f,1.38f,.48f);}
                else if(Action==ActorAction.Dribble && i==1)
                {elbow=new Vector3(.32f,1.2f,.12f);wrist=new Vector3(.43f,1.03f+Mathf.Abs(Mathf.Sin(Time.time*6))* .25f,.53f);}
                else {elbow=new Vector3(s*.28f,1.12f,swing*.5f);wrist=new Vector3(s*.28f,.99f,.16f+swing);}
                Rod(upper[i],shoulder,elbow,.107f);Rod(lower[i],elbow,wrist,.085f);
                if(thigh.Length==2&&leg.Length==2&&shin.Length==2&&shoe.Length==2)
                {
                    float strafe=defense?swing*.3f:0;
                    var hip=new Vector3(s*.095f,.95f,0);
                    var knee=new Vector3(s*.17f+strafe,.50f,.06f+swing*.6f);
                    var ankle=new Vector3(s*.22f+strafe,.13f+Mathf.Max(0,swing)*.25f,-.01f+swing);
                    Rod(thigh[i],hip,Vector3.Lerp(hip,knee,.64f),.195f);
                    Rod(leg[i],Vector3.Lerp(hip,knee,.60f),knee,.13f);Rod(shin[i],knee,ankle,.105f);
                    shoe[i].localPosition=ankle+new Vector3(0,-.053f,.06f);
                }
            }
        }
    }
}
