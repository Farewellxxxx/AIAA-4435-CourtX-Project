using UnityEngine;

namespace CourtX
{
    public enum TacticalDrill { PickAndRoll, HighPostCut }
    public enum TacticalPhase { Assemble, ScreenReady, Roll, HighPost, Cut, Complete, Turnover }
    public enum ActorAction { Space, Dribble, Screen, Roll, Cut, Receive, Slide, HandsUp, Help }

    // Pure geometry shared by intent evaluation and actual ball-flight interception.
    public static class TacticalRules
    {
        public static Vector3 Flat(Vector3 p) { p.y=0; return p; }
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
        public static Vector3 ClampCourt(Vector3 p)
        {
            p.x=Mathf.Clamp(p.x,-7.1f,7.1f);p.z=Mathf.Clamp(p.z,-13.5f,-.55f);p.y=0;return p;
        }
    }
}
