using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CourtX;
using Process = System.Diagnostics.Process;

[InitializeOnLoad]
public static class RoutePlaytest
{
    const string Key = "CourtX.PlaytestRound";
    static RoutePlaytest()
    {
        EditorApplication.playModeStateChanged += change =>
        {
            string round=SessionState.GetString(Key,"");
            if(change!=PlayModeStateChange.EnteredPlayMode || round.Length==0) return;
            SessionState.EraseString(Key);
            new GameObject("Editor route recorder").AddComponent<CourtRouteRecorder>().round=round;
        };
    }
    public static void Run()
    {
        string[] args=Environment.GetCommandLineArgs();
        string round=args.SkipWhile(s=>s!="-courtRound").Skip(1).First();
        if(!new[]{"baseline","spacing","map","final","controls-20261106"}.Contains(round)) throw new ArgumentException("Unknown round");
        EditorSceneManager.OpenScene(CourtAuthoring.Scene);
        SessionState.SetString(Key,round);
        EditorApplication.isPlaying=true;
    }
}

[DefaultExecutionOrder(9000)]
public class CourtRouteRecorder : MonoBehaviour
{
    public string round;
    const int Fps=30, Width=1280, Height=720;
    Process encoder;
    Walkthrough player;
    RenderTexture target;
    Texture2D readback;
    int index;
    float lastYaw,lastPitch;
    bool finished;
    string path;

    void Start()
    {
        try
        {
            path=Path.GetFullPath("../Preview/Playtests/"+round+".mp4");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Time.captureFramerate=Fps;
            QualitySettings.vSyncCount=0;
            player=FindObjectOfType<Walkthrough>();
            player.acceptsInput=false;
            player.ResetView();
            target=new RenderTexture(Width,Height,24){antiAliasing=4};
            readback=new Texture2D(Width,Height,TextureFormat.RGB24,false);
            encoder=new Process();
            encoder.StartInfo=new System.Diagnostics.ProcessStartInfo("ffmpeg.exe",
                "-hide_banner -loglevel error -y -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p -movflags +faststart \""+path+"\"")
                {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true};
            encoder.Start();
        }
        catch(Exception ex){ Fail(ex); }
    }
    static float Ramp(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
    void LateUpdate()
    {
        if(finished)return;
        try
        {
            float t=index/(float)Fps;
            if(round=="controls-20261106") ControlsRoute(t);
            else
            {
            // Scan the screen, move to the wing, read the corner, compare with the map, reset.
            float yaw=-34*Ramp(t,1,3)+60*Ramp(t,4,7)-24*Ramp(t,10,12)+49*Ramp(t,13,15);
            float pitch=-9*Ramp(t,1,3)+9*Ramp(t,6,8)+10*Ramp(t,10,12)-10*Ramp(t,13,15);
            Vector2 motion=Vector2.zero;
            if(t>=3 && t<4.5f)motion=Vector2.left*.56f;
            if(t>=7 && t<10)motion=Vector2.up*.62f;
            if(t>=12 && t<13.5f)motion=Vector2.right*.48f;
            if(t>=19 && t<20.5f)motion=Vector2.down*.62f;
            if(index==16*Fps) player.SetOverview(true);
            if(index==19*Fps) player.SetOverview(false);
            if(index==21*Fps) player.ResetView();
            player.Step(motion,new Vector2(yaw-lastYaw,pitch-lastPitch),1f/Fps);
            lastYaw=yaw;lastPitch=pitch;
            }
            if(player.basketball)player.basketball.Pose();
            if(player.overview.enabled && player.planSymbols)player.planSymbols.Refresh();
            var cam=player.overview.enabled?player.overview:player.eyes;
            var active=RenderTexture.active;
            cam.targetTexture=target;cam.Render();
            RenderTexture.active=target;
            readback.ReadPixels(new Rect(0,0,Width,Height),0,0);readback.Apply();
            cam.targetTexture=null;RenderTexture.active=active;
            byte[] bytes=readback.GetRawTextureData();
            encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            if(++index!=(round=="controls-20261106"?26:23)*Fps)return;
            encoder.StandardInput.Close();
            if(!encoder.WaitForExit(30000) || encoder.ExitCode!=0)throw new IOException("Movie encoder failed");
            finished=true;
            Debug.Log("Verified route: "+index+" live frames -> "+path);
            EditorApplication.Exit(0);
        }
        catch(Exception ex){Fail(ex);}
    }
    void ControlsRoute(float t)
    {
        var motion=Vector2.zero;
        bool run=false;
        bool map=(t>=3.2f&&t<4.2f)||(t>=5.3f&&t<5.8f)||(t>=15&&t<16);
        float yaw=45*Ramp(t,6,7),pitch=-12*Ramp(t,0,1);
        if(t>=2&&t<3)motion=Vector2.left;
        if(t>=3&&t<5){motion=Vector2.up;run=t<4;}
        if(t>=5&&t<6)motion=Vector2.down;
        if(index==8*Fps){player.ResetView();lastYaw=0;lastPitch=0;}
        if(t>=8)
        {
            yaw=45*Ramp(t,10,11);
            pitch=5*Ramp(t,10,11)-24*Ramp(t,18,19);
        }
        if(t>=8&&t<10)motion=Vector2.left;
        if(index==11*Fps || index==20*Fps)player.basketball.BeginCharge();
        if(index==366 || index==609)player.basketball.Release();
        if(index==17*Fps || index==23*Fps)player.basketball.Recall();
        if(index==24*Fps){player.ResetView();yaw=0;pitch=0;}
        if(t>=24){yaw=0;pitch=0;lastYaw=0;lastPitch=0;}
        player.Drive(motion,new Vector2(yaw-lastYaw,pitch-lastPitch),1f/Fps,map,run);
        lastYaw=yaw;lastPitch=pitch;
    }
    void Fail(Exception ex)
    {
        finished=true;
        Debug.LogException(ex);
        if(encoder!=null && encoder.StartInfo.RedirectStandardInput)
            try{encoder.StandardInput.Close();}catch(InvalidOperationException){}
        EditorApplication.Exit(1);
    }
}
