using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace CourtX
{
    [DefaultExecutionOrder(-50)]
    public sealed class FiveCourtGame : MonoBehaviour
    {
        public Walkthrough player;
        public TacticalActor[] actors=new TacticalActor[10];
        public Rigidbody ball;
        public Material indicatorMaterial;
        public FiveBoard Board{get;}=new FiveBoard();
        public readonly List<FiveRecording> History=new List<FiveRecording>();
        public FivePlay Play;public FiveDefense Defense;public int Role,InitialHolder,Page;
        public FiveState Live{get;private set;}
        public FiveRecording LiveRecord{get;private set;}
        public bool Practicing{get;private set;}
        public bool AcceptsInput=true;
        public int Target{get;private set;}=-1;
        public string Message="",SaveFolder="";
        public int SkippedFiles;
        public string SelectedReplayId{get;private set;}="";
        public int SelectedReplayIndex=>History.FindIndex(r=>r.id==SelectedReplayId);
        public bool HasPausedPractice=>!Practicing&&Live!=null&&LiveRecord!=null&&string.IsNullOrEmpty(LiveRecord.endReason);
        public bool SavePending=>unsaved.Count>0;
        public bool Overview{get;private set;}
        public FiveActors Visuals{get;private set;}
        int manualTarget=-1;float manualUntil;
        public string RecordingFolder=>string.IsNullOrEmpty(SaveFolder)?Path.GetFullPath(Path.Combine(Application.dataPath,"../Replays5v5")):SaveFolder;
        void Start()
        {
            Application.runInBackground=true;Application.targetFrameRate=60;
            if(player){player.enabled=false;player.acceptsInput=false;player.SetOverview(false);if(player.basketball)player.basketball.enabled=false;}
            Visuals=new FiveActors(this);
            LoadHistory();LoadLesson();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        public void LoadHistory()
        {
            History.Clear();SkippedFiles=0;
            if(!Directory.Exists(RecordingFolder))return;
            try
            {
                foreach(var file in Directory.GetFiles(RecordingFolder,"*.json").OrderByDescending(File.GetLastWriteTimeUtc).Take(60))
                {try{var r=FiveRecording.Read(file);if(r.kind=="actual")History.Add(r);}catch(Exception){SkippedFiles++;}}
            }
            catch(Exception e){Message="无法读取历史回放："+e.Message;}
        }
        public void LoadLesson(){Page=0;Board.Load(FiveRecording.Lesson(Play,Defense,Role,InitialHolder));}
        public void SelectReplay(int index)
        {
            if(index<0||index>=History.Count)return;Page=1;SelectedReplayId=History[index].id;Board.Load(History[index]);var s=Board.State;
            Play=s.play;Defense=s.defense;Role=s.controlled;InitialHolder=History[index].frames[0].holder;
            if(!SavePending)Message="已打开第 "+(index+1)+" 轮 · "+History[index].created+" · "+History[index].End.ToString("F1")+" 秒";
        }
        public void ShowPage(int page)
        {if(page==0)LoadLesson();else if(History.Count>0)SelectReplay(Mathf.Max(0,SelectedReplayIndex));else{Page=1;Board.Playing=false;}}
        public void BoardShortcut(KeyCode key,bool pointerActive)
        {
            if(pointerActive)return;
            if(key==KeyCode.Space)Board.Playing=!Board.Playing;
            if(key==KeyCode.LeftArrow)Board.Jump(-1);
            if(key==KeyCode.RightArrow)Board.Jump(1);
        }
        void Update()
        {
            if(!Practicing)
            {
                Board.Tick(Time.unscaledDeltaTime);
                if(AcceptsInput&&Input.GetKeyDown(KeyCode.Space))BoardShortcut(KeyCode.Space,FiveCourtUI.Dragging);
                if(AcceptsInput&&Input.GetKeyDown(KeyCode.LeftArrow))BoardShortcut(KeyCode.LeftArrow,FiveCourtUI.Dragging);
                if(AcceptsInput&&Input.GetKeyDown(KeyCode.RightArrow))BoardShortcut(KeyCode.RightArrow,FiveCourtUI.Dragging);
                return;
            }
            float dt=Mathf.Min(Time.deltaTime,.05f);
            if(AcceptsInput)
            {
                if(Input.GetKeyDown(KeyCode.V)){FinishPractice();return;}
                if(Input.GetKeyDown(KeyCode.Escape)){PausePractice();return;}
                if(Input.GetKeyDown(KeyCode.R)){StartPractice();return;}
                if(Input.GetKeyDown(KeyCode.C))CallPlay();
                if(Input.GetMouseButtonDown(0))Cursor.lockState=CursorLockMode.Locked;
                Overview=Input.GetKey(KeyCode.Tab);
                if(player)
                {
                    Vector2 look=Cursor.lockState==CursorLockMode.Locked&&!Overview?new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*2:Vector2.zero;
                    player.Drive(new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")),look,dt,false,Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
                    Vector2 position=FiveRules.Clamp(FiveRules.Flat(player.transform.position));
                    if(Vector2.Distance(position,FiveRules.Flat(player.transform.position))>.01f)Teleport(position);
                    Live.positions[Live.controlled]=position;
                    Live.facing[Live.controlled]=new Vector2(player.transform.forward.x,-player.transform.forward.z);
                    Target=FiveActors.PickTarget(player.eyes,Live);
                }
                for(int i=0;i<5;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i)&&i!=Live.controlled){manualTarget=i;manualUntil=Time.time+3;}
                if(Time.time<manualUntil)Target=manualTarget;
                if(Input.GetKeyDown(KeyCode.Q))
                {
                    if(Live.holder!=Live.controlled)FiveRules.Event(Live,"当前无球，按 E 向持球队友要球。");
                    else if(!DirectPass(Target))FiveRules.Event(Live,"请指向一位队友，看到目标号码后按 Q。");
                }
                if(Input.GetKeyDown(KeyCode.E)){FiveRules.Request(Live,Live.controlled);LiveRecord.Add(Live,true);}
            }
            StepPractice(dt);Visuals?.Apply();
            if(Live.time>=300)FinishPractice();
        }
        void Teleport(Vector2 position)
        {
            if(!player)return;var body=player.GetComponent<CharacterController>();if(body)body.enabled=false;
            player.transform.position=FiveRules.World(position);if(body)body.enabled=true;
        }
        public void StartPractice()
        {
            ArchiveRound();
            Live=FiveRules.Create(Play,Defense,Role,InitialHolder);Live.demo=false;
            LiveRecord=new FiveRecording{name=FiveRules.Plays[(int)Play],kind="actual",created=DateTime.Now.ToString("MM-dd HH:mm:ss")};LiveRecord.Add(Live,true);
            Target=-1;manualUntil=0;Practicing=true;Overview=false;Message="";Board.Playing=false;
            if(player){player.ResetView();Teleport(Live.positions[Role]);player.transform.rotation=Quaternion.LookRotation(FiveRules.World(new Vector2(0,1.575f)-Live.positions[Role]));}
            Visuals?.Apply();if(Application.isPlaying){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        }
        public void PausePractice()
        {if(!Practicing)return;Practicing=false;Overview=false;LiveRecord.Add(Live,true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;Message="练习已暂停，可以继续当前回合，或从预设开始新一轮。";}
        public void ResumePractice()
        {if(!HasPausedPractice)return;Practicing=true;Overview=false;Message="";Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        public bool DirectPass(int receiver)
        {
            if(Live==null||Live.holder!=Live.controlled)return false;
            bool sent=FiveRules.Pass(Live,receiver);if(sent)LiveRecord.Add(Live,true);return sent;
        }
        public void CallPlay()
        {if(Live==null)return;FiveRules.Call(Live);LiveRecord.Add(Live,true);}
        public void StepPractice(float seconds)
        {if(!Practicing||Live==null)return;FiveRules.Step(Live,seconds,Live.controlled);LiveRecord.Add(Live);}
        void ArchiveRound()
        {
            if(LiveRecord==null||Live==null||!string.IsNullOrEmpty(LiveRecord.endReason)||Live.time<.05f)return;
            LiveRecord.Add(Live,true);LiveRecord.endReason=Live.success?"已完成一次战术决策":Live.turnover?"传球被封堵":"结束练习";
            var record=LiveRecord.Copy();History.Insert(0,record);if(History.Count>30)History.RemoveAt(History.Count-1);
            TrySave(record);
        }
        readonly Dictionary<string,FiveRecording> unsaved=new Dictionary<string,FiveRecording>();
        void TrySave(FiveRecording record)
        {
            try{record.Save(RecordingFolder);unsaved.Remove(record.id);Message=SavePending?"仍有 "+unsaved.Count+" 轮尚未保存，可点击重试。":"本轮已保存。可以暂停拖动，在真实回放上探索另一种站位。";}
            catch(Exception e){unsaved[record.id]=record;Message=unsaved.Count+" 轮尚未保存："+e.Message;Debug.LogWarning(Message);}
        }
        public void RetrySave(){foreach(var record in unsaved.Values.ToArray())TrySave(record);}
        public void FinishPractice()
        {
            if(Live==null)return;
            // Very short rounds still contain an explicit final state.
            if(Live.time<.05f){Live.time=.05f;LiveRecord.Add(Live,true);}
            ArchiveRound();Practicing=false;Overview=false;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(History.Count>0)SelectReplay(0);
        }
        void OnApplicationFocus(bool focus){if(!focus&&AcceptsInput&&Practicing)PausePractice();}
        void OnApplicationQuit(){ArchiveRound();RetrySave();}
        void OnGUI(){FiveCourtUI.Draw(this);}
    }
}
