using System;
using UnityEngine;
namespace CourtX
{
    public enum FivePlay { HighPost, Screen, Horns }
    public enum FiveDefense { Balanced, Rim, Perimeter }
    [Serializable] public sealed class FiveFlight
    {
        public int from,to;public Vector2 origin,destination;public float elapsed,duration;
        public bool shot,advancesBeat;
        public const float RimHeight=3.05f;
        public static readonly Vector2 Rim=new Vector2(0,1.575f);
        public float Progress=>Mathf.Clamp01(elapsed/Mathf.Max(.01f,duration));
        // The final part of a shot drops vertically through the net.
        float RimProgress=>Mathf.Clamp01(Progress/.84f);
        public Vector2 Position=>Vector2.Lerp(origin,destination,shot?RimProgress:Progress);
        public float Height
        {
            get
            {
                if(!shot)return 1.25f+Mathf.Sin(Progress*Mathf.PI)*.35f;
                if(Progress>.84f)return Mathf.Lerp(RimHeight,2.50f,(Progress-.84f)/.16f);
                float u=RimProgress;
                float lift=1.1f+.14f*Vector2.Distance(origin,destination);
                return Mathf.Lerp(1.9f,RimHeight,u)+4*lift*u*(1-u);
            }
        }
        public FiveFlight Copy()=>(FiveFlight)MemberwiseClone();
    }
    [Serializable] public sealed class FiveState : ISerializationCallbackReceiver
    {
        public FivePlay play;public FiveDefense defense;
        // Older recordings omit these fields and retain their original phase numbering.
        public int playbookVersion,branch,routePoint;
        public float settledAge;
        public bool pendingGuidedCatch;
        public int controlled,holder,phase,eventSerial,request=-1,lastPassFrom=-1,lastPassTo=-1;
        public float time,phaseAge,requestUntil,passCooldown;
        public bool success,turnover,demo,screenActive,guided,madeBasket,usedScreen;
        public Vector2[] positions=new Vector2[10],velocities=new Vector2[10],targets=new Vector2[10],facing=new Vector2[10];
        public bool[] pinned=new bool[10];public string[] tasks=new string[10];
        public FiveFlight flight;public Vector2 looseBall,screenPoint;
        [SerializeField] bool hasFlight;
        public void OnBeforeSerialize(){hasFlight=flight!=null;}
        public void OnAfterDeserialize(){if(!hasFlight)flight=null;}
        public int screener=4;
        public string attackIntent="",defenseIntent="",lastEvent="",phaseName="";
        public Vector2 Ball=>flight!=null?flight.Position:holder>=0&&holder<10?positions[holder]:looseBall;
        public FiveState Copy()
        {
            var s=(FiveState)MemberwiseClone();s.positions=(Vector2[])positions.Clone();s.velocities=(Vector2[])velocities.Clone();
            s.targets=(Vector2[])targets.Clone();s.facing=(Vector2[])facing.Clone();s.pinned=(bool[])pinned.Clone();s.tasks=(string[])tasks.Clone();s.flight=flight?.Copy();return s;
        }
    }
}
