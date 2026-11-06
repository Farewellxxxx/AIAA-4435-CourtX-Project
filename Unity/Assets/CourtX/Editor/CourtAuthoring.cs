using System;
using System.Collections.Generic;
using System.IO;
using CourtX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Author once into ordinary scene/prefab assets. Runtime does not depend on this builder.
public static class CourtAuthoring
{
    public const string Root = "Assets/CourtX/";
    public const string Scene = Root + "Scenes/HalfCourt.unity";
    static readonly Dictionary<string,Material> palette = new Dictionary<string,Material>();
    static readonly Vector3[] positions = {
        new Vector3(-.7f,0,-10.7f), new Vector3(-2.55f,0,-7.65f), new Vector3(6.85f,0,-1.27f),
        new Vector3(-.1f,0,-7.55f), new Vector3(-2.15f,0,-4.65f), new Vector3(4.85f,0,-2.48f) };
    static readonly float[] facing = {4,78,-102,198,190,119};
    const string Navy="183B43", Teal="117F82", Bone="F4EBCF", Rust="C34D27", Wall="DFE0CE", Steel="465D60";
    static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);

    static Material Mat(string hex)
    {
        if (palette.TryGetValue(hex,out var m) && m) return m;
        string path=Root+"Materials/"+hex+".mat";
        m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            m=new Material(Shader.Find("Standard"));
            ColorUtility.TryParseHtmlString("#"+hex,out var color);
            m.color=color;
            m.SetFloat("_Glossiness",.22f);
            AssetDatabase.CreateAsset(m,path);
        }
        return palette[hex]=m;
    }
    static Transform Node(Transform parent,string name,Vector3 position)
    {
        var obj=new GameObject(name).transform;
        obj.SetParent(parent,false); obj.localPosition=position;
        return obj;
    }
    static Transform Shape(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,string color,bool solid=false)
    {
        var obj=GameObject.CreatePrimitive(type);
        obj.name=name; obj.transform.SetParent(parent,false);
        obj.transform.localPosition=pos; obj.transform.localScale=scale;
        obj.GetComponent<Renderer>().sharedMaterial=Mat(color);
        if (!solid) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj.transform;
    }
    static Transform Box(Transform p,string name,Vector3 center,Vector3 size,string color,bool solid=false)
        => Shape(p,name,PrimitiveType.Cube,center,size,color,solid);
    static void Rod(Transform p,string name,Vector3 a,Vector3 b,float diameter,string color)
    {
        var t=Shape(p,name,PrimitiveType.Cylinder,(a+b)*.5f,V(diameter,(b-a).magnitude*.5f,diameter),color);
        t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    static void Sign(Transform p,string value,Vector3 pos,float height,string color,float yaw=0)
    {
        var t=Node(p,value,pos);
        t.localRotation=Quaternion.Euler(0,yaw,0);
        var label=t.gameObject.AddComponent<TextMesh>();
        label.text=value; label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center;
        label.fontSize=96; label.characterSize=height/8f; label.fontStyle=FontStyle.Bold;
        ColorUtility.TryParseHtmlString("#"+color,out var tint); label.color=tint;
    }
    static void Ground(Transform p)
    {
        Box(p,"Floor collision",V(0,-.105f,-7),V(18,.2f,18),Navy,true);
        var surface=Node(p,"Maple parquet and painted court",Vector3.zero).gameObject;
        string path=Root+"Art/Floor.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!mesh)
        {
            mesh=new Mesh{name="18m floor"};
            mesh.vertices=new[]{V(-9,0,-16),V(-9,0,2),V(9,0,2),V(9,0,-16)};
            mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};
            mesh.triangles=new[]{0,1,2,0,2,3}; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path);
        }
        var m=Mat("FFFFFF");
        var tex=(TextureImporter)AssetImporter.GetAtPath(Root+"Art/CourtSurface.png");
        if(tex.anisoLevel!=8 || tex.maxTextureSize!=2048)
        {tex.anisoLevel=8;tex.maxTextureSize=2048;tex.SaveAndReimport();}
        m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(tex.assetPath); EditorUtility.SetDirty(m);
        surface.AddComponent<MeshFilter>().sharedMesh=mesh;
        surface.AddComponent<MeshRenderer>().sharedMaterial=m;
    }
    static void Basket(Transform p)
    {
        var h=Node(p,"Goal assembly",Vector3.zero);
        Box(h,"Support base",V(0,.25f,1.35f),V(1,.5f,1.05f),Navy,true);
        Box(h,"Padded support",V(0,1.8f,1.43f),V(.55f,3.5f,.55f),Teal,true);
        for(int side=-1;side<=1;side+=2)
        {
            Rod(h,"Cantilever",V(0,3.66f,1.43f),V(side*.62f,3.64f,-1.16f),.1f,Steel);
            Rod(h,"Brace",V(0,2.5f,1.43f),V(side*.62f,3.64f,-1.16f),.07f,Steel);
        }
        Box(h,"Backboard",V(0,3.475f,-1.20f),V(1.80f,1.05f,.05f),"B6D7CF",true);
        foreach(float y in new[]{2.96f,3.99f})
            Box(h,"Board trim",V(0,y,-1.233f),V(1.81f,.038f,.046f),Navy);
        foreach(float x in new[]{-.884f,.884f})
            Box(h,"Board trim",V(x,3.475f,-1.233f),V(.036f,1.05f,.046f),Navy);
        foreach(float y in new[]{3.05f,3.50f}) Box(h,"Target square",V(0,y,-1.24f),V(.59f,.038f,.016f),Bone);
        foreach(float x in new[]{-.277f,.277f}) Box(h,"Target square",V(x,3.275f,-1.24f),V(.036f,.45f,.016f),Bone);
        Box(h,"Rim mount",V(0,3.03f,-1.31f),V(.14f,.09f,.17f),Rust);
        Vector3 ring(float a,float y,float r)=> V(r*Mathf.Cos(a),y,-1.575f+r*Mathf.Sin(a));
        for(int i=0;i<32;i++) Rod(h,"Rim",ring(i*Mathf.PI/16,3.04f,.234f),ring((i+1)*Mathf.PI/16,3.04f,.234f),.02f,"F16524");
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;
            foreach(int s in new[]{-1,1})
            {
                var mid=ring(a+s*Mathf.PI/12,2.84f,.17f);
                Rod(h,"Net",ring(a,3.035f,.225f),mid,.01f,Bone);
                Rod(h,"Net",mid,ring(a+s*Mathf.PI/6,2.64f,.12f),.009f,Bone);
            }
        }
    }
    static void Hall(Transform p)
    {
        var room=Node(p,"Training hall",Vector3.zero);
        Box(room,"North wall",V(0,3.65f,2.1f),V(18.3f,7.3f,.2f),Wall,true);
        Box(room,"South wall",V(0,3.65f,-16.1f),V(18.3f,7.3f,.2f),Wall,true);
        foreach(float x in new[]{-9.1f,9.1f}) Box(room,"Side wall",V(x,3.65f,-7),V(.2f,7.3f,18),Wall,true);
        for(int i=0;i<18;i++)
        {
            Box(room,"Baseline padding",V(-8.5f+i,1.04f,1.92f),V(.98f,2.06f,.14f),Navy);
            for(int side=-1;side<=1;side+=2)
                Box(room,"Side padding",V(side*8.92f,1.04f,1.5f-i),V(.14f,2.06f,.98f),Navy);
        }
        Box(room,"Accent rail",V(0,2.17f,1.96f),V(18,.09f,.06f),"E09B56");
        Sign(room,"COURT X",V(-5.1f,3.32f,1.94f),.73f,Navy);
        Sign(room,"B A S K E T B A L L    /    T A C T I C A L   L A B",V(-5.1f,2.72f,1.94f),.17f,"467578");
        Sign(room,"01",V(5.27f,3.42f,1.94f),.98f,Teal);
        Sign(room,"P R A C T I C E    C O U R T",V(5.27f,2.68f,1.94f),.17f,Navy);
        Sign(room,"C O U R T X",V(0,3.35f,-15.97f),.65f,Navy,180);
        for(int i=0;i<9;i++)
        {
            float x=-8+2*i;
            Box(room,"Clerestory frame",V(x,5.40f,1.963f),V(1.91f,1.75f,.045f),Steel);
            Box(room,"Clerestory pane",V(x,5.40f,1.93f),V(1.82f,1.63f,.025f),"C0DCD7");
            Box(room,"Window mullion",V(x,5.36f,1.906f),V(1.82f,.034f,.015f),Steel);
        }
        var rafters=Node(room,"Roof - excluded from plan view",Vector3.zero);
        Box(rafters,"Ceiling",V(0,7.33f,-7),V(18.3f,.15f,18.3f),"D5D8C7");
        for(int i=0;i<6;i++)
        {
            float z=1.5f-i*3.3f;
            Box(rafters,"Glulam beam",V(0,6.99f,z),V(18.15f,.52f,.18f),"AA8D66");
            foreach(float x in new[]{-4.5f,4.5f})
                Box(rafters,"Linear luminaire",V(x,6.69f,z),V(3.2f,.07f,.20f),"FFF2CC");
        }
        foreach(var t in rafters.GetComponentsInChildren<Transform>()) t.gameObject.layer=2;
    }
    static void Dummy(Transform parent,CourtLayout layout,CourtRole role,int number)
    {
        bool offense=(int)role<3;
        string jersey=offense?Teal:Rust;
        var actor=Node(parent,role.ToString(),Vector3.zero);
        var preview=actor.gameObject.AddComponent<PreviewActor>(); preview.layout=layout;preview.role=role;
        layout.PlaceAtSpawn(actor,role);
        var body=actor.gameObject.AddComponent<CapsuleCollider>(); body.center=V(0,.9f,0);body.height=1.8f;body.radius=.23f;
        Shape(actor,"Head",PrimitiveType.Sphere,V(0,1.65f,.012f),V(.248f,.29f,.244f),"D5A375");
        Shape(actor,"Hair cap",PrimitiveType.Sphere,V(0,1.748f,.001f),V(.246f,.138f,.244f),"4C4036");
        Shape(actor,"Jersey",PrimitiveType.Capsule,V(0,1.22f,0),V(.45f,.32f,.29f),jersey);
        Box(actor,"Waist stripe",V(0,.979f,0),V(.35f,.04f,.235f),Bone);
        for(int s=-1;s<=1;s+=2)
        {
            var knee=V(s*.17f,.5f,.06f); var ankle=V(s*.22f,.13f,-.01f);
            Rod(actor,"Shorts",V(s*.095f,.95f,0),V(s*.15f,.67f,.035f),.195f,Navy);
            Rod(actor,"Leg",V(s*.15f,.68f,.035f),knee,.13f,"D5A375");
            Rod(actor,"Lower leg",knee,ankle,.105f,"D5A375");
            Shape(actor,"Shoe",PrimitiveType.Sphere,V(s*.22f,.077f,.049f),V(.155f,.15f,.294f),Bone);
            var elbow=V(s*(offense?.30f:.41f),1.12f,.045f);
            Rod(actor,"Upper arm",V(s*.205f,1.39f,0),elbow,.107f,"D5A375");
            Rod(actor,"Forearm",elbow,V(s*(offense?.23f:.50f),offense?1.04f:1.17f,.20f),.085f,"D5A375");
        }
        AddNumbers(actor,number.ToString("00"));
    }
    static void AddNumbers(Transform actor,string number)
    {
        var path=Root+"Materials/Jersey"+number+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m)
        {
            m=new Material(Shader.Find("Unlit/Transparent Cutout"));
            m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Art/Jersey"+number+".png");
            AssetDatabase.CreateAsset(m,path);
        }
        foreach(int side in new[]{-1,1})
        {
            var q=Shape(actor,"Jersey number "+number,PrimitiveType.Quad,V(0,1.25f,side*.148f),V(.22f,.165f,1),Bone);
            q.localRotation=Quaternion.Euler(0,side==1?180:0,0);
            q.GetComponent<Renderer>().sharedMaterial=m;
        }
    }
    public static void FinishPreview()
    {
        var scene=EditorSceneManager.OpenScene(Scene);
        foreach(var actor in UnityEngine.Object.FindObjectsOfType<PreviewActor>())
        {
            var labels=actor.GetComponentsInChildren<TextMesh>();
            if(labels.Length==0) continue;
            var number=labels[0].text;
            foreach(var label in labels) UnityEngine.Object.DestroyImmediate(label.gameObject);
            AddNumbers(actor.transform,number);
        }
        GameObject.Find("Gym daylight").GetComponent<Light>().intensity=.78f;
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        CourtReview.BuildWindows();
        CourtReview.Verify();
    }
    public static void Generate()
    {
        if(File.Exists(Scene)) throw new InvalidOperationException("Scene already exists; edit the saved scene in Unity.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var facility=Node(null,"HalfCourt",Vector3.zero);
        Ground(facility); Basket(facility); Hall(facility);
        foreach(var renderer in facility.GetComponentsInChildren<Renderer>())
            if(renderer.gameObject.layer==2) renderer.shadowCastingMode=ShadowCastingMode.Off;
        PrefabUtility.SaveAsPrefabAssetAndConnect(facility.gameObject,Root+"Prefabs/HalfCourt.prefab",InteractionMode.AutomatedAction);
        var scenario=Node(null,"Scenario - High screen right corner",Vector3.zero);
        var layout=scenario.gameObject.AddComponent<CourtLayout>();
        var starts=Node(scenario,"SpawnPoints",Vector3.zero);
        var so=new SerializedObject(layout);
        var spawns=so.FindProperty("spawnPoints"); spawns.arraySize=6;
        for(int i=0;i<6;i++)
        {
            var t=Node(starts,((CourtRole)i).ToString(),positions[i]); t.rotation=Quaternion.Euler(0,facing[i],0);
            spawns.GetArrayElementAtIndex(i).objectReferenceValue=t;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        var anchors=Node(scenario,"TacticalAnchors",Vector3.zero);
        layout.ScreenPoint=Node(anchors,"ScreenPoint",V(-1.25f,0,-7.4f));
        layout.RollTarget=Node(anchors,"RollTarget",V(-.9f,0,-3.1f));
        layout.WeakSideCorner=Node(anchors,"WeakSideCorner",positions[2]);
        layout.RimTarget=Node(anchors,"RimTarget",V(0,3.05f,-1.575f));
        var demos=Node(null,"PreviewOnly - disable for team integration",Vector3.zero);
        var actors=Node(demos,"Stand-ins",Vector3.zero);
        for(int i=1;i<6;i++) Dummy(actors,layout,(CourtRole)i,i<3?i+3:i+8);
        var rig=Node(demos,"Desktop walkthrough",positions[0]);
        var cc=rig.gameObject.AddComponent<CharacterController>();
        cc.height=1.78f;cc.center=V(0,.89f,0);cc.radius=.25f;cc.skinWidth=.025f;
        var walk=rig.gameObject.AddComponent<Walkthrough>();walk.layout=layout;
        walk.eyes=Node(rig,"Eye level",V(0,1.68f,0)).gameObject.AddComponent<Camera>();
        walk.eyes.gameObject.tag="MainCamera";walk.eyes.fieldOfView=62;walk.eyes.nearClipPlane=.05f;
        walk.eyes.gameObject.AddComponent<AudioListener>();
        walk.overview=Node(demos,"Plan view",V(0,18,-7)).gameObject.AddComponent<Camera>();
        walk.overview.transform.rotation=Quaternion.Euler(90,0,0);walk.overview.orthographic=true;walk.overview.orthographicSize=9.3f;
        walk.overview.cullingMask &= ~(1<<2);walk.overview.clearFlags=CameraClearFlags.SolidColor;
        walk.overview.backgroundColor=new Color(.08f,.16f,.18f);walk.overview.enabled=false;
        walk.mapMarker=Shape(rig,"Ball handler - plan view",PrimitiveType.Sphere,V(0,1.15f,0),V(.55f,.3f,.55f),Teal).gameObject;
        Shape(walk.mapMarker.transform,"Ball",PrimitiveType.Sphere,V(.46f,.25f,.27f),V(.46f,.65f,.46f),"EA882E");
        walk.mapMarker.SetActive(false);
        CourtMapAuthoring.Install(walk);
        BasketballAuthoring.Install(walk);
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.78f,.80f,.78f);
        RenderSettings.ambientEquatorColor=new Color(.68f,.69f,.64f);
        RenderSettings.ambientGroundColor=new Color(.48f,.43f,.37f);
        var light=Node(null,"Gym daylight",V(0,6,0)).gameObject.AddComponent<Light>();light.type=LightType.Directional;
        light.transform.rotation=Quaternion.Euler(48,-34,0);light.intensity=.78f;light.shadows=LightShadows.Soft;light.shadowStrength=.44f;
        rig.rotation=layout.GetSpawn(CourtRole.BallHandler).rotation;
        PlayerSettings.productName="CourtX";PlayerSettings.companyName="CourtX";
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        QualitySettings.antiAliasing=4;
        EditorSceneManager.SaveScene(scene,Scene);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
        AssetDatabase.SaveAssets();
    }
}
