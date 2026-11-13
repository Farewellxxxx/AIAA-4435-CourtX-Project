#if COURTX_FIVE_REVIEW
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CourtX;
using UnityEngine;

public sealed class FivePlayerPlaytest:MonoBehaviour
{
    FiveCourtGame game;int failures;readonly List<string> checks=new List<string>();
    const string Folder="E:/study documents/2026 Fall/AIAA 4435 Inter/CourtX/Preview/Playtests/FivePlayer-20261114";
    static readonly List<string> runtimeErrors=new List<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Application.logMessageReceived+=(text,trace,type)=>{if(type==LogType.Error||type==LogType.Exception)runtimeErrors.Add(text);};
        var g=FindObjectOfType<FiveCourtGame>();if(g)g.AcceptsInput=false;new GameObject("Five-player QA only").AddComponent<FivePlayerPlaytest>();
    }
    void Check(bool value,string name){checks.Add((value?"PASS ":"FAIL ")+name);if(!value)failures++;}
    IEnumerator Shot(string name)
    {
        yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();
        var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();
        File.WriteAllBytes(Path.Combine(Folder,name+".png"),texture.EncodeToPNG());Destroy(texture);
    }
    IEnumerator Start()
    {
        Directory.CreateDirectory(Folder);Application.runInBackground=true;QualitySettings.vSyncCount=0;
        yield return null;game=FindObjectOfType<FiveCourtGame>();game.AcceptsInput=false;game.SaveFolder=Path.Combine(Folder,"QA-SyntheticSessions");
        while(!UnityEngine.Rendering.SplashScreen.isFinished)yield return null;
        Check(game.Visuals!=null&&game.indicatorMaterial!=null,"Startup completes with an explicitly included indicator material");
        Check(FindObjectsOfType<TacticalSession>().Length==0,"New scene does not expose legacy four-page UI");
        Check(game.actors.Length==10&&game.actors.All(a=>a!=null)&&game.actors.Distinct().Count()==10,"Scene owns ten distinct character models");
        Check(game.actors.All(a=>a.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Eye")==2),"All characters have two physical front eyes");
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,576),new Vector2Int(1920,1080)})
        {
            Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.5f);string tag=size.x+"x"+size.y;
            game.Play=FivePlay.Screen;game.Defense=FiveDefense.Balanced;game.LoadLesson();game.Board.Seek(1.6f);yield return Shot(tag+"-lesson");
            Check(Screen.width==size.x&&Screen.height==size.y,tag+": actual rendered resolution");
            var r=FiveCourtUI.CourtPixels;Check(r.x>=0&&r.y>=0&&r.xMax<=Screen.width&&r.yMax<=Screen.height,tag+": complete court remains within window");
            string original=JsonUtility.ToJson(game.Board.Record);game.Board.Move(7,new Vector2(-5.5f,7.5f));
            typeof(FiveCourtUI).GetField("selected",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,7);
            typeof(FiveCourtUI).GetField("prior",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,game.Board.Record);
            game.Board.StepOnce();yield return Shot(tag+"-whatif");
            Check(game.Board.IsBranch,"Editable lesson uses branch state at "+tag);game.Board.Restore();Check(JsonUtility.ToJson(game.Board.Record)==original,"Restore preserves original lesson at "+tag);
        }
        Screen.SetResolution(1280,720,false);yield return new WaitForSecondsRealtime(.5f);
        foreach(FivePlay play in System.Enum.GetValues(typeof(FivePlay)))
        {game.Play=play;game.Defense=FiveDefense.Perimeter;game.LoadLesson();game.Board.Seek(7);yield return Shot("play-"+play);}
        for(int role=0;role<5;role++)
        {
            game.Role=role;game.InitialHolder=role;game.StartPractice();yield return null;
            Check(game.actors.Count(a=>a.gameObject.activeInHierarchy)==9&&!game.actors[role].gameObject.activeSelf,"Role "+role+": nine NPCs and fixed human identity");
            Check(Vector2.Distance(FiveRules.Flat(game.player.transform.position),game.Live.positions[role])<.01f,"Role "+role+": player spawns at corresponding five-player position");
        }
        game.Role=0;game.InitialHolder=0;game.Play=FivePlay.Screen;game.StartPractice();yield return Shot("3d-first-person");
        typeof(FiveCourtGame).GetField("<Overview>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,true);yield return Shot("3d-overview");
        typeof(FiveCourtGame).GetField("<Overview>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,false);
        var controller=game.player.GetComponent<CharacterController>();controller.enabled=false;
        game.player.transform.position=FiveRules.World(game.Live.positions[6]+game.Live.facing[6]*1.8f);
        game.player.transform.rotation=Quaternion.LookRotation(FiveRules.World(-game.Live.facing[6]));controller.enabled=true;yield return Shot("3d-eyes");
        game.StartPractice();game.Live.positions[0]=new Vector2(-4,10);game.Live.positions[1]=new Vector2(4,10);game.Live.pinned[1]=true;
        for(int i=5;i<10;i++){game.Live.positions[i]=new Vector2(i-7,2);game.Live.pinned[i]=true;}
        Check(game.DirectPass(1)&&game.Live.flight!=null,"Single direct-pass action releases immediately in actual player");
        yield return new WaitForSecondsRealtime(1.5f);Check(game.Live.holder==1,"Player receives an assisted pass without charge timing");
        game.CallPlay();Check(game.Live.phase==0&&!game.Live.turnover,"Player can call a new sequence");
        game.FinishPractice();yield return Shot("actual-replay");
        Check(game.Page==1&&!game.Practicing&&game.Board.Record.kind=="actual","Actual practice returns to replay entrance");
        var actual=game.History[0];string saved=File.ReadAllText(Path.Combine(game.RecordingFolder,actual.id+".json"));
        game.Board.Seek(.4f);game.Board.Move(6,new Vector2(5,8));game.Board.StepOnce();yield return Shot("actual-replay-whatif");
        Check(game.Board.IsBranch&&saved==File.ReadAllText(Path.Combine(game.RecordingFolder,actual.id+".json")),"What-if on real round leaves disk evidence untouched");
        game.LoadHistory();Check(game.History.Any(r=>r.id==actual.id),"Saved round loads through normal history reader");
        foreach(string error in runtimeErrors){failures++;checks.Add("ERROR "+error);}
        checks.Add("Failures: "+failures);checks.Add("Synthetic automated QA sessions, not user performance evidence.");
        File.WriteAllLines(Path.Combine(Folder,"player-checks.txt"),checks);Application.Quit(failures==0?0:1);
    }
}
#endif
