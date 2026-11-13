using System.IO;
using CourtX;
using UnityEngine;
public static class FivePracticeReview
{
    public static void Run()
    {
        var root=new GameObject("practice contract fixture");var g=root.AddComponent<FiveCourtGame>();
        try
        {
            g.SaveFolder=Path.Combine(ReactiveReview.Folder,"practice-records");g.Role=2;g.InitialHolder=2;g.LoadLesson();g.Board.Seek(5);g.Board.Move(0,new Vector2(6,12));
            g.StartPractice();FivePlayerReview.Check(g.Practicing&&g.Live!=null,"Start playing creates an ordinary live practice");
            if(g.Live==null)return;
            FivePlayerReview.Check(g.Live.controlled==2&&g.Live.holder==2&&g.Live.positions[0]==new Vector2(0,10.8f)&&g.Live.phase==0,"Starting 3D uses preset/role/possession, never edited 2D midgame state");
            g.Role=0;g.InitialHolder=0;g.StartPractice();
            g.Live.positions[0]=new Vector2(-4,10);g.Live.positions[1]=new Vector2(4,10);g.Live.pinned[1]=true;
            for(int i=5;i<10;i++){g.Live.positions[i]=new Vector2(i-7,2);g.Live.pinned[i]=true;}
            FivePlayerReview.Check(g.DirectPass(1)&&g.Live.flight!=null,"Live Q action passes immediately without holding or release");
            for(int i=0;i<85;i++)g.StepPractice(1f/60);
            FivePlayerReview.Check(g.Live.holder==1&&g.Live.controlled==0,"Normal practice catches a pass while retaining the human role");
            g.Live.success=true;g.Live.phase=4;g.CallPlay();FivePlayerReview.Check(!g.Live.success&&g.Live.phase==0,"Call can run again after a completed attempt");
            g.Live.turnover=true;g.Live.holder=8;g.CallPlay();FivePlayerReview.Check(!g.Live.turnover&&g.Live.holder==0,"Call after turnover restores playable possession");
            g.FinishPractice();FivePlayerReview.Check(!g.Practicing&&g.Page==1&&g.History.Count>0&&g.Board.Record.kind=="actual","Finishing opens the same board on an actual round");
            string path=Path.Combine(g.SaveFolder,g.History[0].id+".json");FivePlayerReview.Check(File.Exists(path)&&FiveRecording.Read(path).frames.Count>1,"A normal 3D round persists reloadable ten-player states");
            string before=JsonUtility.ToJson(g.History[0]);g.Board.Move(5,new Vector2(-6,9));g.Board.StepOnce();
            FivePlayerReview.Check(g.Board.IsBranch&&JsonUtility.ToJson(g.History[0])==before,"What-if on just-finished replay cannot rewrite the saved round");
            string goodFolder=g.SaveFolder,blocked=Path.Combine(goodFolder,"blocked-path");File.WriteAllText(blocked,"file blocks directory creation");g.SaveFolder=blocked;
            g.StartPractice();g.StepPractice(.1f);g.FinishPractice();string firstPending=g.History[0].id;
            g.StartPractice();g.StepPractice(.1f);g.FinishPractice();string secondPending=g.History[0].id;
            FivePlayerReview.Check(g.SavePending,"Failed saves remain visibly pending across later rounds");
            g.SaveFolder=goodFolder;g.RetrySave();
            FivePlayerReview.Check(!g.SavePending&&File.Exists(Path.Combine(goodFolder,firstPending+".json"))&&File.Exists(Path.Combine(goodFolder,secondPending+".json")),"Retry saves every pending round after multiple write failures");
        }
        finally{Object.DestroyImmediate(root);}
    }
}
