using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using CourtX;

public static class CourtReview
{
    const string Pending = "CourtX.ReviewPending";
    static string Output => Path.GetFullPath("../Preview");

    public static void Verify()
    {
        EditorSceneManager.OpenScene(CourtAuthoring.Scene);
        SessionState.SetBool(Pending,true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    static void Attach()
    {
        EditorApplication.playModeStateChanged += change =>
        {
            if(change==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
                EditorApplication.delayCall += Check;
        };
    }

    static void Capture(Camera camera,string filename,int width,int height)
    {
        var old=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4);
        var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;
            camera.Render();
            RenderTexture.active=rt;
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            File.WriteAllBytes(Path.Combine(Output,filename),pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=null;RenderTexture.active=old;
            RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
    static void Check()
    {
        SessionState.SetBool(Pending,false);
        var checks=new List<string>();
        int exit=0;
        void Require(bool ok,string label)
        {
            if(!ok) throw new InvalidOperationException(label);
            checks.Add("PASS  "+label);
        }
        try
        {
            var walk=UnityEngine.Object.FindObjectOfType<Walkthrough>();
            var layout=walk.layout;
            var roles=(CourtRole[])Enum.GetValues(typeof(CourtRole));
            var points=new HashSet<Vector3>();
            foreach(var role in roles)
            {
                var spawn=layout.GetSpawn(role);
                Require(spawn && points.Add(spawn.position) && Mathf.Abs(spawn.position.y)<.001f,role+" has a distinct foot-level spawn");
            }
            Require(layout.ScreenPoint && layout.RollTarget && layout.WeakSideCorner && layout.RimTarget,"all four tactical anchors resolve");
            var actors=UnityEngine.Object.FindObjectsOfType<PreviewActor>();
            Require(actors.Length==5,"five replaceable NPC stand-ins");
            foreach(var actor in actors)
                Require(Vector3.Distance(actor.transform.position,layout.GetSpawn(actor.role).position)<.01f,"NPC at assigned spawn: "+actor.role);
            walk.ResetView();
            var initial=walk.transform.position;
            Require(Vector3.Distance(initial,layout.GetSpawn(CourtRole.BallHandler).position)<.01f,"first person starts at ball handler");
            Capture(walk.eyes,"FirstPerson.png",1440,810);
            foreach(var axis in new[]{Vector2.up,Vector2.down,Vector2.right,Vector2.left})
            {
                walk.ResetView();
                var direction=walk.transform.right*axis.x+walk.transform.forward*axis.y;
                for(int i=0;i<25;i++) walk.Step(axis,Vector2.zero,.012f);
                Require(Vector3.Dot(walk.transform.position-initial,direction)>.78f,"walk on court "+axis);
            }
            walk.Step(Vector2.zero,new Vector2(37,14),.016f);
            Require(Quaternion.Angle(walk.transform.rotation,layout.GetSpawn(CourtRole.BallHandler).rotation)>35
                && Quaternion.Angle(walk.eyes.transform.localRotation,Quaternion.identity)>13,"yaw and pitch");
            walk.ResetView();
            for(int i=0;i<400;i++) walk.Step(Vector2.down,Vector2.zero,.016f);
            Require(walk.transform.position.z>-16 && walk.transform.position.z<-15.5f && Mathf.Abs(walk.transform.position.y)<.08f,"back wall blocks travel; floor supports player");
            walk.ResetView();walk.SetOverview(true);
            Require(walk.overview.enabled && !walk.eyes.enabled && walk.mapMarker.activeInHierarchy,"overview switches camera and displays ball handler");
            var symbols=walk.planSymbols;
            Require(symbols && symbols.gameObject.activeInHierarchy && symbols.badges.Length==6,"plan contains all six role badges");
            walk.SetOverview(false);
            for(int i=0;i<25;i++)walk.Step(Vector2.left,new Vector2(1f,0f),.012f);
            walk.SetOverview(true);
            foreach(var role in roles)
            {
                int i=(int)role;
                Vector3 subject=symbols.subjects[i].position;
                var badge=symbols.badges[i].position;
                Require(Mathf.Abs(subject.x-badge.x)<.001f && Mathf.Abs(subject.z-badge.z)<.001f,"live map position: "+role);
            }
            Vector3 arrowDirection=symbols.heading.position-symbols.badges[0].position;
            arrowDirection.y=0;
            Require(Vector3.Dot(arrowDirection.normalized,walk.transform.forward)>.999f,"map heading after movement and turn");
            walk.ResetView();walk.SetOverview(true);
            Capture(walk.overview,"Positions.png",960,900);
            walk.ResetView();
            Require(walk.eyes.enabled && !walk.overview.enabled && !walk.mapMarker.activeSelf && Vector3.Distance(walk.transform.position,initial)<.01f,"reset restores start and first-person view");
            Require(!symbols.gameObject.activeInHierarchy,"first-person view hides plan badges");
        }
        catch(Exception ex) { checks.Add("FAIL "+ex);Debug.LogException(ex);exit=1; }
        File.WriteAllLines(Path.Combine(Output,"PlayMode-checks.txt"),checks);
        EditorApplication.Exit(exit);
    }
    public static void BuildWindows()
    {
        Directory.CreateDirectory("../Windows");
        var report=BuildPipeline.BuildPlayer(new[]{CourtAuthoring.Scene},"../Windows/CourtX.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception(report.summary.result.ToString());
    }
}
