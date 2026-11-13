using System;
using UnityEngine;
namespace CourtX
{
    public enum FivePlay { HighPost, Screen, Horns }
    public enum FiveDefense { Balanced, Rim, Perimeter }
    [Serializable] public sealed class FiveFlight
    {
        public int from,to;public Vector2 origin,destination;public float elapsed,duration;
        public FiveFlight Copy()=>(FiveFlight)MemberwiseClone();
        public Vector2 Position=>Vector2.Lerp(origin,destination,Mathf.Clamp01(elapsed/Mathf.Max(.01f,duration)));
    }
    [Serializable] public sealed class FiveState
    {
        public FivePlay play;public FiveDefense defense;
        public int controlled,holder,phase,eventSerial,request=-1,lastPassFrom=-1,lastPassTo=-1;
        public float time,phaseAge,requestUntil,passCooldown;
        public bool success,turnover,demo,screenActive;
        public Vector2[] positions=new Vector2[10],velocities=new Vector2[10],targets=new Vector2[10],facing=new Vector2[10];
        public bool[] pinned=new bool[10];public string[] tasks=new string[10];
        public FiveFlight flight;public Vector2 looseBall,screenPoint;
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
