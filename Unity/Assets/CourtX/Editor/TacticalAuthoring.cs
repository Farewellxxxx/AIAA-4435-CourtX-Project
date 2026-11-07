using System;
using System.Linq;
using CourtX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class TacticalAuthoring
{
    public const string Scene="Assets/CourtX/Scenes/TacticalTraining.unity";
    public static void Generate()
    {
        if(System.IO.File.Exists(Scene)) {Debug.Log("Tactical scene already exists; preserving authored edits.");return;}
        var scene=EditorSceneManager.OpenScene(CourtAuthoring.Scene);
        var walk=UnityEngine.Object.FindObjectOfType<Walkthrough>();
        var session=new GameObject("Tactical session - replaceable logic").AddComponent<TacticalSession>();
        session.player=walk;
        var existing=UnityEngine.Object.FindObjectsOfType<PreviewActor>().OrderBy(a=>(int)a.role).ToArray();
        var leader=UnityEngine.Object.Instantiate(existing[0].gameObject,existing[0].transform.parent);
        leader.name="BallHandler 01 - AI when off ball";
        UnityEngine.Object.DestroyImmediate(leader.GetComponent<PreviewActor>());
        session.actors[0]=leader.AddComponent<TacticalActor>();session.actors[0].Id=0;
        for(int i=0;i<existing.Length;i++)
        {
            int id=(int)existing[i].role;var root=existing[i].gameObject;
            UnityEngine.Object.DestroyImmediate(existing[i]);
            session.actors[id]=root.AddComponent<TacticalActor>();session.actors[id].Id=id;
        }
        var material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/CourtX/Materials/Jersey04.mat"));
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CourtX/Art/Jersey01.png");
        AssetDatabase.CreateAsset(material,"Assets/CourtX/Materials/Jersey01.mat");
        foreach(var r in leader.GetComponentsInChildren<Renderer>())if(r.name.StartsWith("Jersey number"))r.sharedMaterial=material;
        string[] numbers={"01","04","05","11","12","13"};
        for(int i=0;i<6;i++)
        {
            string path="Assets/CourtX/Materials/Plan-"+numbers[i]+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat)
            {
                mat=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/CourtX/Materials/Plan-04.mat"));
                mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CourtX/Art/Plan/01.png");AssetDatabase.CreateAsset(mat,path);
            }
            session.mapMaterials[i]=mat;
        }
        var ball=session.gameObject.AddComponent<TacticalBall>();session.ball=ball;ball.session=session;ball.visual=walk.basketball;
        walk.basketball.enabled=false;walk.acceptsInput=false;walk.enabled=false;
        var ring=new GameObject("Suggested receiving or screen point");
        var line=ring.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=64;line.widthMultiplier=.045f;
        var guide=new Material(Shader.Find("Unlit/Color"));guide.color=new Color(.25f,.88f,.70f);
        AssetDatabase.CreateAsset(guide,"Assets/CourtX/Materials/TacticalGuide.mat");line.sharedMaterial=guide;
        for(int i=0;i<64;i++){float a=i*2*Mathf.PI/64;line.SetPosition(i,new Vector3(Mathf.Cos(a)*.70f,0,Mathf.Sin(a)*.70f));}
        session.guidanceRing=ring.transform;
        EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
        Debug.Log("Created independent tactical scene.");
    }
    public static void Build()
    {
        if(!System.IO.File.Exists(Scene))Generate();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{Scene},locationPathName="../Windows_Tactics/CourtX Tactics.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Tactical build failed: "+report.summary.result);
        Debug.Log("TACTICAL BUILD SUCCEEDED: "+report.summary.totalSize+" bytes");
    }
}
