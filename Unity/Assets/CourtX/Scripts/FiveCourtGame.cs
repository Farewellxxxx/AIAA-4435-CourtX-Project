using System;
using System.Collections.Generic;
using System.Globalization;
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
        public FivePlay Play;public int Role,Page;
        public int Branch{get;private set;}
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
        public Vector2 ViewHeading=>player?FiveRules.Flat(player.transform.forward):Vector2.zero;
        public FiveActors Visuals{get;private set;}
        int assistedBeat=-1;
        float basketDisplayLeft;
        public string RecordingFolder
        {
            get
            {
                if (!string.IsNullOrEmpty(SaveFolder)) return SaveFolder;
                var root = Application.platform == RuntimePlatform.OSXPlayer
                    ? Application.persistentDataPath
                    : Directory.GetParent(Application.dataPath).FullName;
                return Path.Combine(root, "Replays5v5");
            }
        }
        void Start()
        {
            Application.runInBackground=true;Application.targetFrameRate=60;
            if(player){player.enabled=false;player.acceptsInput=false;player.SetOverview(false);if(player.basketball)player.basketball.enabled=false;}
            Visuals=new FiveActors(this);
            Role=FiveCoach.Recommended(Play);LoadHistory();LoadLesson();SyncPracticeCursor();
        }
        public void LoadHistory()
        {
            History.Clear();SkippedFiles=0;
            if(!Directory.Exists(RecordingFolder))return;
            try
            {
                foreach(var file in Directory.GetFiles(RecordingFolder,"*.json").OrderByDescending(File.GetLastWriteTimeUtc))
                {try{var r=FiveRecording.Read(file);if(r.kind=="actual")History.Add(r);}catch(Exception){SkippedFiles++;}}
                var today=DateTime.Now;
                var ordered=History.OrderByDescending(r=>r.CreatedAt(today)).ToArray();
                History.Clear();History.AddRange(ordered);
            }
            catch(Exception e){Message="无法读取历史回放："+e.Message;}
        }
        public void LoadLesson()
        {
            Page=0;Board.Load(FiveRecording.GuidedLesson(Play,Role,Branch));
            if(!SavePending)Message="";
        }
        public void SelectPlay(FivePlay play){Play=play;Branch=0;Role=FiveCoach.Recommended(play);LoadLesson();}
        public void SelectBranch(int branch)
        {
            if(branch<0||branch>2)return;
            Branch=branch;LoadLesson();
        }
        public void SelectReplay(string id)=>SelectReplay(History.FindIndex(r=>r.id==id));
        public void SelectReplay(int index)
        {
            if(index<0||index>=History.Count)return;Page=1;SelectedReplayId=History[index].id;Board.Load(History[index]);var s=Board.State;
            Play=s.play;Role=s.controlled;Branch=s.playbookVersion==2?s.branch:0;
            if(!SavePending)Message="已打开第 "+(index+1)+" 轮";
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
            if(Live.madeBasket)
            {
                basketDisplayLeft-=Time.unscaledDeltaTime;
                if(basketDisplayLeft<=0)FinishPractice();
                return;
            }
            float dt=Mathf.Min(Time.deltaTime,.05f);
            if(AcceptsInput)
            {
                if(Input.GetKeyDown(KeyCode.V)&&Live.flight?.shot!=true){FinishPractice();return;}
                if(Input.GetKeyDown(KeyCode.Escape)){PausePractice();return;}
                if(Input.GetKeyDown(KeyCode.R)&&Live.flight?.shot!=true){StartPractice();return;}
                if(Input.GetKeyDown(KeyCode.C)&&Live.flight?.shot!=true)CallPlay();
                var motion=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
                Vector2 look=Cursor.lockState==CursorLockMode.Locked
                    ?new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*2:Vector2.zero;
                DrivePractice(motion,look,dt,Input.GetKey(KeyCode.Tab),
                    Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift),Input.GetKeyDown(KeyCode.F));
                if(Input.GetKeyDown(KeyCode.Q)&&Target>=0)
                {
                    if(Target==FiveActors.RimTarget){if(FiveRules.Shoot(Live))LiveRecord.Add(Live,true);}
                    else DirectPass(Target);
                }
            }
            StepPractice(dt);
            if(Live.holder!=Live.controlled||Live.turnover)Target=-1;
            Visuals?.Apply();
            if(Live.madeBasket)basketDisplayLeft=.85f;
            else if(Live.time>=300&&Live.flight?.shot!=true)FinishPractice();
        }
        public void DrivePractice(Vector2 motion,Vector2 look,float seconds,bool overviewHeld,bool sprint,bool assistPressed=false)
        {
            if(!Practicing||Live==null)return;
            Overview=overviewHeld;
            if(player)
            {
                if(assistPressed)assistedBeat=Live.phase;
                if(motion!=Vector2.zero||FiveCoach.InPlace(Live,Live.controlled)||Live.success||Live.turnover)assistedBeat=-1;
                if(assistedBeat==Live.phase)
                {
                    var direction=FiveRules.World(Live.targets[Live.controlled]-Live.positions[Live.controlled]).normalized;
                    var local=player.transform.InverseTransformDirection(direction);
                    motion=new Vector2(local.x,local.z);
                }
                // The HUD draws the 5v5 map over the live first-person camera.
                player.Drive(motion,look,seconds,false,sprint);
                Vector2 actual=FiveRules.Flat(player.transform.position);
                Vector2 position=FiveRules.Clamp(actual);
                if(Vector2.Distance(position,actual)>.01f)Teleport(position);
                Live.positions[Live.controlled]=position;
                Live.facing[Live.controlled]=ViewHeading;
            }
            Target=!Overview&&Live.holder==Live.controlled&&!Live.turnover&&player?FiveActors.PickTarget(player.eyes,Live):-1;
        }
        void Teleport(Vector2 position)
        {
            if(!player)return;var body=player.GetComponent<CharacterController>();if(body)body.enabled=false;
            player.transform.position=FiveRules.World(position);if(body)body.enabled=true;
        }
        public void StartPractice()
        {
            ArchiveRound();
            Live=FiveCoach.Create(Play,Role,Branch);
            LiveRecord=new FiveRecording{name=FiveZoneBook.PlayName(Play),kind="actual",created=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture)};LiveRecord.Add(Live,true);
            Target=-1;assistedBeat=-1;Practicing=true;Overview=false;Message="";Board.Playing=false;
            if(player){player.ResetView();Teleport(Live.positions[Role]);player.transform.rotation=Quaternion.LookRotation(FiveRules.World(new Vector2(0,1.575f)-Live.positions[Role]));}
            Visuals?.Apply();SyncPracticeCursor();
        }
        void SyncPracticeCursor()
        {
            if(!Application.isPlaying)return;
            bool capture=Practicing&&AcceptsInput;
            Cursor.lockState=capture?CursorLockMode.Locked:CursorLockMode.None;
            Cursor.visible=!capture;
        }
        public void PausePractice()
        {if(!Practicing)return;Practicing=false;Overview=false;LiveRecord.Add(Live,true);SyncPracticeCursor();Message="练习已暂停，可以继续当前回合，或从预设开始新一轮。";}
        public void ResumePractice()
        {if(!HasPausedPractice)return;Practicing=true;Overview=false;Message="";SyncPracticeCursor();}
        public bool DirectPass(int receiver)
        {
            if(Live==null||Live.holder!=Live.controlled)return false;
            bool sent=FiveRules.Pass(Live,receiver,true);if(sent)LiveRecord.Add(Live,true);return sent;
        }
        public void CallPlay()
        {if(Live==null)return;assistedBeat=-1;FiveRules.Call(Live);LiveRecord.Add(Live,true);}
        public void StepPractice(float seconds)
        {if(!Practicing||Live==null)return;FiveRules.Step(Live,seconds,Live.controlled);LiveRecord.Add(Live);}
        void ArchiveRound()
        {
            if(LiveRecord==null||Live==null||!string.IsNullOrEmpty(LiveRecord.endReason))return;
            if(Live.time<.05f&&Live.flight?.shot!=true&&!Live.madeBasket)return;
            // Preserve a released shot even if the player closes the window or starts over.
            while(Live.flight?.shot==true)
            {
                FiveRules.Step(Live,1f/60,Live.controlled);
                LiveRecord.Add(Live);
            }
            LiveRecord.Add(Live,true);LiveRecord.endReason=Live.madeBasket?"投篮命中":Live.success?"已完成一次战术决策":Live.turnover?"传球被封堵":"结束练习";
            var record=LiveRecord.Copy();History.Insert(0,record);
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
            SyncPracticeCursor();
            if(History.Count>0)SelectReplay(0);
        }
        void OnApplicationFocus(bool focus){if(!focus&&AcceptsInput&&Practicing)PausePractice();}
        void OnApplicationQuit(){ArchiveRound();RetrySave();}
        void OnGUI(){FiveCourtUI.Draw(this);}
    }
}
