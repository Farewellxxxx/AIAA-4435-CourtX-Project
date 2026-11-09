using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CourtX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CourtControlsReview
{
    static CourtControlsReview()
    {
        EditorApplication.playModeStateChanged += change =>
        {
            if(change!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("CourtX.ControlsReview",false))return;
            SessionState.SetBool("CourtX.ControlsReview",false);
            new GameObject("Editor controls regression").AddComponent<CourtControlsChecks>();
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(CourtAuthoring.Scene);
        SessionState.SetBool("CourtX.ControlsReview",true);EditorApplication.isPlaying=true;
    }
}

public class CourtControlsChecks : MonoBehaviour
{
    readonly List<string> results=new List<string>();
    int failures;
    Walkthrough walk;
    void Check(bool ok,string what)
    {
        results.Add((ok?"PASS ":"FAIL ")+what);if(!ok)failures++;
    }
    void OnLog(string message,string trace,LogType type)
    {
        if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)
        {results.Add("ERROR "+message);failures++;}
    }
    IEnumerator Frames(int count,Vector2 motion,bool map,bool sprint)
    {
        for(int i=0;i<count;i++)
        {
            walk.Drive(motion,Vector2.zero,1f/60,map,sprint);
            yield return null;
        }
    }
    IEnumerator Start()
    {
        Application.logMessageReceived+=OnLog;
        Time.captureFramerate=60;
        walk=FindObjectOfType<Walkthrough>();walk.acceptsInput=false;
        var basket=walk.basketball;
        var layout=walk.layout;
        yield return null;
        foreach(CourtRole role in Enum.GetValues(typeof(CourtRole)))
            Check(layout.GetSpawn(role)!=null && Mathf.Abs(layout.GetSpawn(role).position.y)<.001f,"foot-level role interface "+role);
        Check(layout.ScreenPoint && layout.RollTarget && layout.RimTarget && layout.WeakSideCorner,"four tactical anchors retained");
        var start=walk.transform.position;
        walk.Drive(Vector2.zero,Vector2.zero,1f/60,true,false);
        Check(walk.overview.enabled&&!walk.eyes.enabled,"Tab held selects overview");
        yield return Frames(30,Vector2.left,true,false);
        var mapPosition=walk.transform.position;
        float mapDistance=Vector3.Distance(start,mapPosition);
        Check(mapDistance>1.45f&&mapDistance<1.55f,"walk continues 1.5 m while Tab held");
        var badge=walk.planSymbols.badges[0].position;
        Check(new Vector2(badge.x-mapPosition.x,badge.z-mapPosition.z).magnitude<.005f,"YOU follows movement without reopening map");
        var actor=FindObjectOfType<PreviewActor>();
        var originalActor=actor.transform.position;
        actor.transform.position+=Vector3.right*.7f;
        yield return null;
        badge=walk.planSymbols.badges[(int)actor.role].position;
        Check(Mathf.Abs(badge.x-actor.transform.position.x)<.005f,"moving NPC badge refreshes every frame");
        actor.transform.position=originalActor;
        var rotation=walk.transform.rotation;
        walk.Drive(Vector2.left,Vector2.zero,1f/60,false,false);
        Check(walk.eyes.enabled&&!walk.overview.enabled,"Tab release immediately returns first person");
        Check(Vector3.Distance(walk.transform.position,mapPosition)<.055f && Quaternion.Angle(rotation,walk.transform.rotation)<.01f,"release keeps position and heading; movement continuous");
        walk.ResetView();
        yield return Frames(30,Vector2.left,false,false);
        float walkDistance=Vector3.Distance(start,walk.transform.position);
        walk.ResetView();
        yield return Frames(30,Vector2.left,true,true);
        float runDistance=Vector3.Distance(start,walk.transform.position);
        Check(Mathf.Abs(runDistance-2.75f)<.06f && runDistance>walkDistance*1.8f,"Shift runs at 5.5 m/s in map view");
        var beforeRelease=walk.transform.position;
        yield return Frames(20,Vector2.right,false,false);
        Check(Mathf.Abs(Vector3.Distance(beforeRelease,walk.transform.position)-1f)<.05f,"Shift release restores 3 m/s");
        walk.ResetView();
        yield return Frames(15,new Vector2(-1,-1),false,true);
        Check(Vector3.Distance(start,walk.transform.position)<1.43f,"diagonal sprint is normalized");
        walk.ResetView();
        for(int i=0;i<20;i++)walk.Drive(Vector2.left,Vector2.zero,1f/60,i%2==0,true);
        Check(Vector3.Distance(start,walk.transform.position)>1.7f,"repeated Tab press/release does not interrupt running");
        walk.ResetView();
        yield return Frames(120,Vector2.down,true,true);
        Check(walk.transform.position.z>-16 && Mathf.Abs(walk.transform.position.y)<.08f,"sprinting respects wall and floor while map open");
        walk.ResetView();
        float min=100,max=-100;
        for(int i=0;i<80;i++)
        {
            yield return null;
            min=Mathf.Min(min,basket.ball.position.y);max=Mathf.Max(max,basket.ball.position.y);
        }
        Check(min>=.12f&&max-min>.95f,"dribble travels between hand height and floor without penetrating floor");
        basket.BeginCharge();
        yield return null;
        Check(basket.IsCharging&&basket.ball.isKinematic,"Q gathers ball in front of camera");
        basket.Release();
        float shortSpeed=basket.LastLaunchSpeed;
        Check(basket.IsAirborne&&!basket.ball.isKinematic,"Q release hands ball to physics");
        yield return Frames(15,Vector2.zero,true,false);
        Check(basket.ball.position.z>start.z+1f,"ball flies while Tab held");
        basket.Recall();
        Check(!basket.IsAirborne&&!basket.IsCharging&&basket.ball.isKinematic,"E recalls and resets ball");
        basket.BeginCharge();
        yield return Frames(80,Vector2.zero,false,false);
        Check(basket.Charge01>=.99f,"charge clamps at full power");
        basket.Release();
        Check(basket.LastLaunchSpeed>shortSpeed+5f,"longer Q hold produces a faster throw");
        yield return Frames(150,Vector2.zero,true,false);
        Check(basket.ball.position.y>.08f&&basket.ball.position.z<2.1f,"thrown ball collides with court and baseline wall");
        yield return Frames(220,Vector2.zero,true,false);
        Check(!basket.IsAirborne,"ball automatically returns after six seconds");
        basket.BeginCharge();
        walk.ResetView();
        Check(!basket.IsCharging&&!basket.IsAirborne && !walk.overview.enabled,"R resets player camera and ball together");
        Time.captureFramerate=0;
        Directory.CreateDirectory("../Preview/Playtests");
        string report=Array.IndexOf(Environment.GetCommandLineArgs(),"-reactiveLabel")>=0?ReactiveReview.Folder+"/free-practice-checks.txt":"../Preview/Playtests/Controls-20261106-checks.txt";
        Directory.CreateDirectory(Path.GetDirectoryName(report));File.WriteAllLines(report,results);
        EditorApplication.Exit(failures==0?0:1);
    }
}
