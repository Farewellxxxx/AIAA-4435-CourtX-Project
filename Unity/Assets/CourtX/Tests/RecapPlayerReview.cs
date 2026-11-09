// Included only in the separate QA player. No test runner is compiled into Windows_Recap.
#if COURTX_RECAP_REVIEW
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CourtX;
using UnityEngine;
using Process=System.Diagnostics.Process;

public sealed class RecapPlayerReview:MonoBehaviour
{
    TacticalSession s;string folder,root;Process encoder;bool recording;int frames;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){new GameObject("Recap QA player recorder").AddComponent<RecapPlayerReview>();}
    sealed class LongProvider:ITacticalRecapProvider
    {
        public Task<string> GenerateAsync(string json,string local,CancellationToken cancellation)
            =>Task.FromResult(string.Concat(Enumerable.Repeat("长文本显示测试：观察队友与篮球的相对位置，再判断传球窗口。\n",50))+"长文本末尾检查标记");
    }
    IEnumerator Wait(float seconds){yield return new WaitForSecondsRealtime(seconds);}
    void Load(string filename)
    {
        s.RecapProvider=null;s.StartScenario(TacticalDrill.HighPostCut,false,DefenseCoverage.Drop,4435);
        var e=JsonUtility.FromJson<TacticalEpisode>(File.ReadAllText(Path.Combine(root,"docs/verification/reactive-20261109/examples",filename+".json")));
        var live=s.Journal.Episode;live.frames=e.frames;live.decisions=e.decisions;live.events=e.events;live.controlled=e.controlled;
        live.coverage=e.coverage;live.drill=e.drill;s.Journal.Finish(e.duration,e.endReason);s.OpenReview();
    }
    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();var image=Capture();
        File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);
    }
    Texture2D Capture()
    {
        var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();return image;
    }
    IEnumerator Record()
    {
        while(recording)
        {
            yield return new WaitForEndOfFrame();var image=Capture();
            // ReadPixels includes IMGUI and is bottom-up RGB; ffmpeg flips it once.
            var data=image.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(data,0,data.Length);Destroy(image);frames++;
        }
    }
    IEnumerator Start()
    {
        folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));root=Path.GetFullPath(Path.Combine(folder,"../../.."));
        Screen.SetResolution(1280,720,false);Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=30;
        yield return Wait(1);s=FindObjectOfType<TacticalSession>();s.acceptsInput=false;
        encoder=new Process {StartInfo=new System.Diagnostics.ProcessStartInfo("ffmpeg.exe","-hide_banner -loglevel error -y -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 20 -pix_fmt yuv420p -movflags +faststart \""+folder+"/recap-playback-qa.mp4\"")
            {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true}};encoder.Start();recording=true;StartCoroutine(Record());
        Load("HighPostCut-Help-role0");s.Replay.Seek(s.Replay.StartTime);s.Replay.Speed=.5f;s.Replay.Toggle();yield return Wait(8);
        s.Replay.Seek(s.Journal.Episode.decisions[0].time+.3f);yield return Wait(.3f);yield return Shot("recap-first-pass");yield return Wait(2);
        s.Replay.Jump(1);yield return Wait(.3f);yield return Shot("recap-next-pass");yield return Wait(2);
        Load("PickAndRoll-Switch-role1");s.Replay.Seek(s.Replay.StartTime);s.Replay.Toggle();yield return Wait(8);yield return Shot("recap-offball");
        s.RecapProvider=new LongProvider();s.OpenReview();yield return Wait(1);yield return Shot("recap-long-top");
        typeof(TacticalRecapUI).GetField("scroll",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new Vector2(0,100000));
        yield return Wait(.5f);yield return Shot("recap-long-bottom");yield return Wait(2);
        recording=false;yield return new WaitForEndOfFrame();encoder.StandardInput.Close();encoder.WaitForExit(30000);
        Screen.SetResolution(1024,576,false);yield return Wait(1);yield return Shot("recap-small-long-bottom");
        s.RecapProvider=null;s.RetryScenario();yield return Wait(.2f);s.OpenReview();yield return Wait(.3f);yield return Shot("recap-small-no-pass");
        File.WriteAllText(Path.Combine(folder,"native-qa-result.txt"),"Rendered real standalone player at 1280x720 and 1024x576.\nVideo frames: "+frames+"\nffmpeg exit: "+encoder.ExitCode+"\nRecorded examples: HighPostCut role0; Switch role1.\nOptional long text: QA fixture only, no network request.\n");
        Application.Quit(encoder.ExitCode);
    }
}
#endif
