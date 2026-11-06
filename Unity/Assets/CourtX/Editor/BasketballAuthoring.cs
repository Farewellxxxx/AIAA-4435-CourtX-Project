using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using CourtX;

public static class BasketballAuthoring
{
    static Material Material(string name,Color color)
    {
        string path=CourtAuthoring.Root+"Materials/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material) return material;
        material=new Material(Shader.Find("Standard")){color=color};
        material.SetFloat("_Glossiness",.18f);
        AssetDatabase.CreateAsset(material,path);
        return material;
    }
    static Transform Shape(Transform parent,string name,PrimitiveType shape,Vector3 scale,Material material,bool collider=false)
    {
        var obj=GameObject.CreatePrimitive(shape);
        obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localScale=scale;
        obj.GetComponent<Renderer>().sharedMaterial=material;
        if(!collider) Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj.transform;
    }
    public static void Install(Walkthrough walk)
    {
        if(walk.basketball) return;
        var root=new GameObject("Preview basketball and right hand").transform;
        root.SetParent(walk.transform.parent,false);
        var demo=root.gameObject.AddComponent<PreviewBasketball>();
        demo.player=walk;walk.basketball=demo;
        var rubber=Material("Basketball rubber",new Color(.85f,.33f,.045f));
        var skin=Material("Preview hand",new Color(.74f,.49f,.30f));
        var seams=Material("Basketball seams",new Color(.085f,.055f,.035f));
        var ball=Shape(root,"Basketball - 24.4 cm",PrimitiveType.Sphere,Vector3.one*.244f,rubber,true);
        demo.ball=ball.gameObject.AddComponent<Rigidbody>();
        demo.ball.mass=.62f;
        demo.ball.interpolation=RigidbodyInterpolation.Interpolate;
        demo.ball.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        demo.ball.drag=.06f;demo.ball.angularDrag=.1f;
        string physicsPath=CourtAuthoring.Root+"Materials/Basketball bounce.physicMaterial";
        var physics=AssetDatabase.LoadAssetAtPath<PhysicMaterial>(physicsPath);
        if(!physics)
        {
            physics=new PhysicMaterial("Basketball bounce"){bounciness=.72f,dynamicFriction=.55f,staticFriction=.55f,
                bounceCombine=PhysicMaterialCombine.Maximum};
            AssetDatabase.CreateAsset(physics,physicsPath);
        }
        ball.GetComponent<Collider>().sharedMaterial=physics;
        for(int panel=0;panel<3;panel++)
        {
            var seam=new GameObject("Inset seam").AddComponent<LineRenderer>();
            seam.transform.SetParent(ball,false);seam.useWorldSpace=false;seam.loop=true;seam.positionCount=96;
            seam.sharedMaterial=seams;seam.widthMultiplier=.003f;
            seam.shadowCastingMode=ShadowCastingMode.Off;seam.receiveShadows=false;
            for(int i=0;i<96;i++)
            {
                float a=i*Mathf.PI*2/96;
                Vector3 point=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                if(panel==1)point=new Vector3(0,Mathf.Cos(a),Mathf.Sin(a));
                if(panel==2)point=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                seam.SetPosition(i,point*.502f);
            }
        }
        demo.forearm=Shape(root,"Right forearm",PrimitiveType.Cylinder,Vector3.one,skin);
        demo.palm=Shape(root,"Right palm",PrimitiveType.Sphere,new Vector3(.12f,.047f,.18f),skin);
        for(int i=0;i<4;i++)
        {
            var finger=Shape(demo.palm,"Finger",PrimitiveType.Capsule,new Vector3(.14f,.55f,.15f),skin);
            finger.localPosition=new Vector3(-.32f+i*.21f,-.08f,.48f);
            finger.localRotation=Quaternion.Euler(90,0,0);
        }
        demo.planBall=Shape(root,"Ball position on live map",PrimitiveType.Sphere,new Vector3(.24f,.02f,.24f),rubber);
        demo.planBall.gameObject.SetActive(false);
        var oldBall=walk.mapMarker.transform.Find("Ball");
        if(oldBall)oldBall.gameObject.SetActive(false);
        EditorUtility.SetDirty(walk);
        demo.ball.isKinematic=true;
        demo.Pose();
    }
    public static void InstallAndVerify()
    {
        var scene=EditorSceneManager.OpenScene(CourtAuthoring.Scene);
        Install(Object.FindObjectOfType<Walkthrough>());
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        CourtControlsReview.Run();
    }
    public static void BuildAndVerify()
    {
        CourtReview.BuildWindows();
        CourtControlsReview.Run();
    }
}
