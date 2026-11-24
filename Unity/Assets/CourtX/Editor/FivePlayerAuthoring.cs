using System;
using System.IO;
using System.Linq;
using CourtX;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class FivePlayerAuthoring
{
    public const string Scene="Assets/CourtX/Scenes/FivePlayerTraining.unity";
    public const string Delivery="E:/study documents/2026 Fall/AIAA 4435 Inter/CourtX/Windows_FivePlayer_1117_Timing";
    public const string Evidence="E:/study documents/2026 Fall/AIAA 4435 Inter/CourtX/Preview/Playtests/FivePlayer-20261117-Timing";
    public static string ZoneDelivery=>Path.Combine(Path.GetDirectoryName(Delivery),"Windows_FivePlayer_1121_Zone");
    public static void Generate()
    {
        var scene=EditorSceneManager.OpenScene(TacticalAuthoring.Scene);
        var legacy=UnityEngine.Object.FindObjectOfType<TacticalSession>();var walk=legacy.player;
        var root=new GameObject("CourtX five-player workbench");var game=root.AddComponent<FiveCourtGame>();game.player=walk;
        game.indicatorMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/CourtX/Materials/TacticalGuide.mat");
        var visual=legacy.ball.visual;game.ball=visual.ball;
        visual.forearm.gameObject.SetActive(false);visual.palm.gameObject.SetActive(false);visual.planBall.gameObject.SetActive(false);
        UnityEngine.Object.DestroyImmediate(visual);walk.basketball=null;walk.enabled=false;walk.acceptsInput=false;
        if(walk.planSymbols){UnityEngine.Object.DestroyImmediate(walk.planSymbols.gameObject);walk.planSymbols=null;}
        walk.mapMarker.SetActive(false);walk.overview.enabled=false;walk.eyes.enabled=true;
        var actorRoot=new GameObject("Ten players").transform;
        for(int i=0;i<10;i++)
        {
            var template=legacy.actors[i<5?i%3:3+(i-5)%3];var obj=UnityEngine.Object.Instantiate(template.gameObject,actorRoot);
            obj.name=FiveRules.Number(i)+(i<5?" offense":" zone defense");obj.SetActive(true);
            var actor=obj.GetComponent<TacticalActor>();actor.Id=i;actor.DefenseStartsAt=5;game.actors[i]=actor;
            foreach(var child in obj.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Jersey number")).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach(int side in new[]{-1,1})
            {
                string labelPath="Assets/CourtX/Materials/Five-"+FiveRules.Number(i)+".mat";var labelMaterial=AssetDatabase.LoadAssetAtPath<Material>(labelPath);
                if(!labelMaterial){labelMaterial=new Material(Shader.Find("Unlit/Transparent Cutout"));AssetDatabase.CreateAsset(labelMaterial,labelPath);}
                labelMaterial.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CourtX/Art/FiveNumbers/"+FiveRules.Number(i)+".png");EditorUtility.SetDirty(labelMaterial);
                var label=GameObject.CreatePrimitive(PrimitiveType.Quad);label.name="Number "+FiveRules.Number(i);label.transform.SetParent(obj.transform,false);
                label.transform.localPosition=new Vector3(0,1.26f,side*.158f);label.transform.localRotation=Quaternion.Euler(0,side==1?180:0,0);label.transform.localScale=new Vector3(.27f,.18f,1);
                UnityEngine.Object.DestroyImmediate(label.GetComponent<Collider>());label.GetComponent<Renderer>().sharedMaterial=labelMaterial;
            }
            foreach(float x in new[]{-.055f,.055f})
            {
                Eye(obj.transform,"Eye",new Vector3(x,1.675f,.131f),new Vector3(.057f,.055f,.028f),Color.white,"EyeWhite");
                Eye(obj.transform,"Pupil",new Vector3(x,1.675f,.148f),new Vector3(.025f,.030f,.012f),new Color(.08f,.13f,.11f),"EyeDark");
            }
        }
        foreach(var actor in legacy.actors)UnityEngine.Object.DestroyImmediate(actor.gameObject);
        if(legacy.guidanceRing)UnityEngine.Object.DestroyImmediate(legacy.guidanceRing.gameObject);
        UnityEngine.Object.DestroyImmediate(legacy.gameObject);
        var start=FiveRules.Create(FivePlay.HighPost,FiveDefense.Balanced,0,0);
        for(int i=0;i<10;i++){game.actors[i].transform.position=FiveRules.World(start.positions[i]);game.actors[i].transform.rotation=Quaternion.Euler(0,i<5?0:180,0);}
        PlayerSettings.productName="CourtX 5v5";PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();Debug.Log("FIVE SCENE GENERATED");
    }
    static void Eye(Transform parent,string name,Vector3 position,Vector3 size,Color color,string materialName)
    {
        string path="Assets/CourtX/Materials/"+materialName+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Standard")){color=color};AssetDatabase.CreateAsset(mat,path);}
        var eye=GameObject.CreatePrimitive(PrimitiveType.Sphere);eye.name=name;eye.transform.SetParent(parent,false);eye.transform.localPosition=position;eye.transform.localScale=size;
        UnityEngine.Object.DestroyImmediate(eye.GetComponent<Collider>());eye.GetComponent<Renderer>().sharedMaterial=mat;
    }
    [MenuItem("CourtX/Build Windows Player")]
    public static void BuildPortable()
    {
        string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
        string buildDirectory = Path.Combine(projectDirectory, "Builds", "Windows");
        string output = Path.Combine(buildDirectory, "CourtX.exe");
        Directory.CreateDirectory(buildDirectory);
        var report = BuildPipeline.BuildPlayer(new[] { Scene }, output,
            BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
            throw new UnityEditor.Build.BuildFailedException("CourtX build failed: " + report.summary.result);
        Debug.Log("CourtX Windows player built at " + output);
    }

    public static void Build()=>BuildTo(Delivery+"/CourtX.exe",false);
    public static void BuildQA()=>BuildTo(Evidence+"/QA/CourtX QA.exe",true);
    public static void BuildGuided()
    {
        FivePlayerReview.RunCoach();
        string path=Path.Combine(ZoneDelivery,"CourtX.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report=BuildPipeline.BuildPlayer(new[]{Scene},path,BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Guided build failed: "+report.summary.result);
        Debug.Log("GUIDED BUILD SUCCEEDED: "+path);
    }
    static void BuildTo(string output,bool qa)
    {
        Generate();Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=qa?new[]{"COURTX_FIVE_REVIEW"}:null});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Five-player build failed: "+report.summary.result);
        Debug.Log("FIVE BUILD SUCCEEDED: "+report.summary.totalSize);
    }
}
