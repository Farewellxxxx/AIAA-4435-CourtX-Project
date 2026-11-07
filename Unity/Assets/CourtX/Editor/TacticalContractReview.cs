using System;
using System.IO;
using UnityEditor;

public static class TacticalContractReview
{
    public static void Run()
    {
        var type=Type.GetType("CourtX.TacticalSession, Assembly-CSharp");
        bool ready=type!=null && type.GetMethod("StartDrill")!=null && type.GetMethod("RequestPass")!=null;
        string result=ready?"PASS tactical session exposes drill and request interfaces":"FAIL tactical session cannot start drills or request a teammate pass (feature absent)";
        Directory.CreateDirectory("../Preview/Playtests");
        File.WriteAllText("../Preview/Playtests/tactical-contract-red.txt",result);
        UnityEngine.Debug.Log(result);
        EditorApplication.Exit(ready?0:1);
    }
}
