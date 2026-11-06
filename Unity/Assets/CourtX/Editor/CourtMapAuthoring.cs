using CourtX;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CourtMapAuthoring
{
    public static void Install(Walkthrough walk)
    {
        if(walk.planSymbols) return;
        var root=new GameObject("Plan view - live role badges");
        root.transform.SetParent(walk.transform.parent,false);
        var map=root.AddComponent<PlanSymbols>();
        map.subjects=new Transform[6];map.badges=new Transform[6];
        map.subjects[0]=walk.transform;
        foreach (var actor in walk.transform.parent.GetComponentsInChildren<PreviewActor>(true))
            map.subjects[(int)actor.role]=actor.transform;
        string[] labels={"YOU","04","05","11","12","13"};
        for(int i=0;i<labels.Length;i++)map.badges[i]=Quad(root.transform,labels[i],.86f);
        map.heading=Quad(root.transform,"Heading",.70f);
        walk.planSymbols=map;
        root.SetActive(false);
    }
    static Transform Quad(Transform root,string label,float span)
    {
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name=label;quad.transform.SetParent(root,false);
        quad.transform.localRotation=Quaternion.Euler(90,0,0);
        quad.transform.localScale=new Vector3(span,span,1);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        string path=CourtAuthoring.Root+"Materials/Plan-"+label+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat)
        {
            mat=new Material(Shader.Find("Unlit/Transparent"));
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(CourtAuthoring.Root+"Art/Plan/"+label+".png");
            AssetDatabase.CreateAsset(mat,path);
        }
        var renderer=quad.GetComponent<Renderer>();renderer.sharedMaterial=mat;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        return quad.transform;
    }
    public static void InstallAndReview()
    {
        var scene=EditorSceneManager.OpenScene(CourtAuthoring.Scene);
        Install(Object.FindObjectOfType<Walkthrough>());
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        RoutePlaytest.Run();
    }
}
