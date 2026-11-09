using UnityEngine;

namespace CourtX
{
    public enum TacticalDrill { PickAndRoll, HighPostCut }
    public enum TacticalPhase { Assemble, ScreenReady, Roll, HighPost, Cut, Complete, Turnover }
    public enum ActorAction { Space, Dribble, Screen, Roll, Cut, Receive, Slide, HandsUp, Help, Signal }
    public struct TacticalPassPlan
    {
        public Vector3 Origin,Destination;
        public float Duration;
    }

    // Pure geometry shared by intent evaluation and actual ball-flight interception.
    public static class TacticalRules
    {
        public static Vector3 Flat(Vector3 p) { p.y=0; return p; }
        public static Vector3 PassPoint(Vector3 from,Vector3 to,float t)=>Vector3.Lerp(from,to,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.34f);
        public static float Distance(Vector3 a,Vector3 b) => Vector3.Distance(Flat(a),Flat(b));
        public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b,out float along)
        {
            p=Flat(p);a=Flat(a);b=Flat(b);var d=b-a;
            along=d.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);
            return Vector3.Distance(p,a+d*along);
        }
        public static bool LaneBlocked(Vector3 from,Vector3 to,Vector3 defender,float clearance=.67f)
        {
            float t;float d=SegmentDistance(defender,from,to,out t);
            return d<clearance && t>.08f && t<1f;
        }
        // Relative motion turns a moving ball / moving defender into one continuous sweep.
        // Return the entry time, not the closest point, so actor ordering cannot decide possession.
        public static bool SweepCylinder(Vector3 from,Vector3 to,Vector3 rootFrom,Vector3 rootTo,
            float radius,float low,float high,out float entry)
        {
            Vector3 a=from-rootFrom,d=(to-rootTo)-a;
            float begin=0,end=1,aa=d.x*d.x+d.z*d.z,cc=a.x*a.x+a.z*a.z-radius*radius;
            entry=0;
            if(aa<.000001f){if(cc>0)return false;}
            else
            {
                float bb=2*(a.x*d.x+a.z*d.z),disc=bb*bb-4*aa*cc;
                if(disc<0)return false;
                float sq=Mathf.Sqrt(disc);begin=Mathf.Max(begin,(-bb-sq)/(2*aa));end=Mathf.Min(end,(-bb+sq)/(2*aa));
            }
            if(Mathf.Abs(d.y)<.000001f){if(a.y<low||a.y>high)return false;}
            else
            {
                float t0=(low-a.y)/d.y,t1=(high-a.y)/d.y;
                begin=Mathf.Max(begin,Mathf.Min(t0,t1));end=Mathf.Min(end,Mathf.Max(t0,t1));
            }
            entry=begin;return begin<=end && begin<=1 && end>=0;
        }
        public static bool DefensiveContact(Vector3 from,Vector3 to,Vector3 rootFrom,Vector3 rootTo,
            Vector3 facing,out float entry,out bool secured,float margin=0)
        {
            secured=false;entry=float.PositiveInfinity;
            float body;
            bool hit=SweepCylinder(from,to,rootFrom,rootTo,.36f+margin,.25f,1.94f,out body);
            if(hit)entry=body;
            Vector3 arrival=Flat(from-to).normalized,forward=Flat(facing).normalized;
            // Seeing the incoming ball matters: a blind body collision is a deflection, not a catch.
            if(Vector3.Dot(forward,arrival)>.45f)
            {
                float hand;Vector3 offset=forward*.20f;
                if(SweepCylinder(from,to,rootFrom+offset,rootTo+offset,.45f+margin,.90f,2.14f,out hand)&&hand<=entry)
                {entry=hand;secured=true;hit=true;}
            }
            return hit;
        }
        public static Vector3 ClampCourt(Vector3 p)
        {
            p.x=Mathf.Clamp(p.x,-7.1f,7.1f);p.z=Mathf.Clamp(p.z,-13.5f,-.55f);p.y=0;return p;
        }
    }
}
