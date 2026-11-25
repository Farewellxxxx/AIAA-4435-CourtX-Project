using System;
using System.IO;
using System.Linq;
using CourtX;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FiveWaypointReview
{
    // Capture the same approach in the actual practice scene for visual comparisons.
    public static void CaptureApproach()
    {
        var scene=EditorSceneManager.OpenScene(FivePlayerAuthoring.Scene,OpenSceneMode.Additive);
        try
        {
            var game=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FiveCourtGame>(true)).Single();
            game.player.SendMessage("Awake");
            game.Role=4;
            game.StartPractice();
            var actors=new FiveActors(game);
            var s=game.Live;
            s.phase=1;
            FiveRules.Refresh(s);
            var eye=game.player.eyes;
            var target=FiveRules.World(FiveCoach.RunTarget(s));
            var forward=Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up).normalized;
            var body=game.player.GetComponent<CharacterController>();
            body.enabled=false;
            Directory.CreateDirectory(ReactiveReview.Folder);
            foreach(float distance in new[]{6f,2f,1.05f,.95f})
            {
                game.player.transform.position=target-forward*distance;
                s.positions[s.controlled]=FiveRules.Flat(game.player.transform.position);
                foreach(int pitch in new[]{0,28,65})
                {
                    eye.transform.rotation=Quaternion.LookRotation(forward)*Quaternion.Euler(pitch,0,0);
                    actors.Apply();
                    Capture(eye,Path.Combine(ReactiveReview.Folder,$"{ReactiveReview.Label}-{Mathf.RoundToInt(distance*100)}cm-{pitch}.png"));
                }
            }
        }
        finally{EditorSceneManager.CloseScene(scene,true);}
    }

    static void Capture(Camera eye,string path)
    {
        var previous=RenderTexture.active;
        var originalTarget=eye.targetTexture;
        var rt=RenderTexture.GetTemporary(960,600,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4);
        var pixels=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        try
        {
            eye.targetTexture=rt;
            eye.Render();
            RenderTexture.active=rt;
            pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);
            pixels.Apply();
            File.WriteAllBytes(path,pixels.EncodeToPNG());
        }
        finally
        {
            eye.targetTexture=originalTarget;
            RenderTexture.active=previous;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }

    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene(FivePlayerAuthoring.Scene,OpenSceneMode.Additive);
        try
        {
            var game=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FiveCourtGame>(true)).Single();
            game.player.SendMessage("Awake");
            game.Role=4;
            game.SaveFolder=Path.Combine(ReactiveReview.Folder,"waypoint-records");
            game.StartPractice();
            var actors=new FiveActors(game);
            var s=game.Live;
            int self=s.controlled;
            actors.Apply();
            Check(actors.ArrivalOpacity==0,"Spawning in position does not acknowledge an arrival.");

            var eye=game.player.eyes;
            s.phase=1;
            FiveRules.Refresh(s);
            var destination=FiveCoach.RunTarget(s);
            var forward=FiveRules.Flat(eye.transform.forward).normalized;
            void Stand(float distance)
            {
                var p=destination-forward*distance;
                game.player.transform.position=FiveRules.World(p);
                s.positions[self]=p;
            }
            game.player.GetComponent<CharacterController>().enabled=false;
            Stand(1.05f);
            actors.Apply();
            var arrow=game.transform.Find("Your running waypoint");
            var ring=game.transform.Find("Suggested tactical area");
            Check(arrow&&arrow.gameObject.activeInHierarchy,"A first-person waypoint has a floating arrow.");
            var mat=arrow.GetComponent<MeshRenderer>().sharedMaterial;
            Check(mat&&mat.shader.isSupported&&mat.shader.name=="CourtX/Running marker",
                "The marker shader is included and supported.");
            Check(ring.gameObject.activeInHierarchy&&Mathf.Abs(ring.position.y-.035f)<.001f
                && Mathf.Abs(Vector3.Dot(ring.forward,Vector3.up))>.999f,"The ground marker stays flat on the court.");
            Check(Mathf.Abs(ring.localScale.x*.82f*.5f-.8f)<.001f,"The teaching circle uses its original .8-metre radius.");
            float y=arrow.position.y;
            s.time+=.34f;
            actors.Apply();
            Check(arrow.position.y>y+.015f,"The floating arrow bobs visibly even at the edge of the arrival region.");
            var initialLook=eye.transform.rotation;
            CheckHud(eye);
            foreach(float distance in new[]{1.01f,1.05f,1.5f,2f,6f})
            {
                Stand(distance);
                foreach(var angle in new[]{Vector3.zero,new Vector3(18,15,0),new Vector3(-12,-22,0)})
                {
                    eye.transform.rotation=initialLook*Quaternion.Euler(angle);
                    actors.Apply();
                    var extent=arrow.GetComponent<MeshFilter>().sharedMesh.bounds.extents;
                    var low=eye.WorldToViewportPoint(arrow.TransformPoint(-extent));
                    var high=eye.WorldToViewportPoint(arrow.TransformPoint(extent));
                    float h=high.y-low.y;
                    float w=(high.x-low.x)*eye.aspect;
                    Check(low.z>eye.nearClipPlane && high.z>eye.nearClipPlane && w>0 && Mathf.Abs(w/h-1)<.005f,
                        "The marker face keeps its on-screen proportions while approaching and turning.");
                    Check(h>=.065f && h<=.076f,"The arrow stays legible without growing over the pass target.");
                }
            }
            eye.transform.rotation=initialLook*Quaternion.Euler(0,180,0);
            actors.Apply();
            Check(!arrow.gameObject.activeInHierarchy&&ring.gameObject.activeInHierarchy,
                "Looking back at the previous position does not mirror the arrow into view.");
            eye.transform.rotation=initialLook;
            Stand(1.05f);
            actors.Apply();
            Check(!arrow.GetComponent<Collider>(),"The floating arrow cannot obstruct movement.");

            Stand(.96f);
            game.StepPractice(1f/60);
            actors.Apply();
            Check(actors.ArrivalOpacity>.95f && !arrow.gameObject.activeInHierarchy,
                "Entering the outer tolerance acknowledges arrival and clears the ordinary marker.");
            Stand(1.4f);
            s.time+=.75f;
            actors.Apply();
            Check(actors.ArrivalOpacity>0 && actors.ArrivalOpacity<1 && !arrow.gameObject.activeInHierarchy,
                "Turning away after an ordinary arrival keeps the beat complete as the confirmation fades.");
            s.time+=1;
            actors.Apply();
            Check(actors.ArrivalOpacity==0,"A pending new waypoint does not keep prior feedback visible.");
            s.time=0; // Leave the fixture round unarchived when starting another.
            game.SendMessage("BeginRound",FiveDrill.Create(FivePlay.Horns,2,true));
            s=game.Live;
            s.positions[s.controlled]=new Vector2(-5,10);
            actors.Apply();
            Check(actors.ArrivalOpacity==0 && !arrow.gameObject.activeInHierarchy && !ring.gameObject.activeInHierarchy,
                "The opening of the assessment shows no destination even if the human is away.");
            s.drill.stage=FiveDrillStage.Decision;
            actors.Apply();
            Check(!arrow.gameObject.activeInHierarchy && !ring.gameObject.activeInHierarchy,
                "The assessment decision hides both directional markers.");
            Debug.Log("FiveWaypointReview passed.");
        }
        finally{EditorSceneManager.CloseScene(scene,true);}
    }

    static void CheckHud(Camera eye)
    {
        foreach(var size in new[]{new Vector2(1280,720),new Vector2(1024,768),new Vector2(640,400)})
        foreach(bool teaching in new[]{true,false})
        {
            var view=new Rect(Vector2.zero,size);
            var hud=new FiveCourtUI.PracticeLayout(view,teaching);
            Check(hud.MapPanel.xMax<view.center.x && hud.MapPanel.yMax<view.center.y
                && !hud.MapPanel.Overlaps(hud.Header) && !hud.MapPanel.Overlaps(hud.Footer),
                "The fixed court occupies the top-left and leaves the centre clear at "+size);
            Check(Mathf.Abs(hud.MapCourt.width/hud.MapCourt.height-15f/14)<.001f,
                "The half-court retains its aspect ratio.");
            Check(!FiveCourtUI.TryWaypointEdge(eye,hud,eye.transform.TransformPoint(Vector3.forward*3),out _,out _),
                "An unobstructed waypoint in front needs no edge indicator.");
            foreach(float side in new[]{-2f,0,2f})
            {
                var marker=eye.transform.TransformPoint(new Vector3(side,0,-3));
                Check(FiveCourtUI.TryWaypointEdge(eye,hud,marker,out var tag,out var direction)
                    && direction.y>0 && (side==0?Mathf.Abs(direction.x)<.001f:direction.x*side>0)
                    && view.Contains(tag.center) && !tag.Overlaps(hud.MapPanel) && !tag.Overlaps(hud.Header) && !tag.Overlaps(hud.Footer),
                    "An offscreen waypoint keeps a visible and correctly directed tag at "+size);
            }
        }
    }

    static void Check(bool condition,string description)
    {
        if(!condition)throw new InvalidOperationException(description);
    }
}
