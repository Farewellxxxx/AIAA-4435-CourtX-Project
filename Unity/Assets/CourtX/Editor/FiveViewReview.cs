using CourtX;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class FiveViewReview
{
    public static void Run()
    {
        var rect=new Rect(100,80,600,560);var p=new Vector2(-3,4.2f);
        FivePlayerReview.Check(Vector2.Distance(FiveCourtView.ScreenPoint(rect,p),new Vector2(280,248))<.01f,"Court coordinates map to the actual displayed rectangle");
        FivePlayerReview.Check(Vector2.Distance(FiveCourtView.WorldPoint(rect,new Vector2(280,248)),p)<.001f,"Pointer coordinates invert the same map with letterbox offset");
        var s=FiveRules.Create(FivePlay.HighPost,FiveDefense.Balanced,0,0);
        s.positions[7]=new Vector2(5,4);s.positions[9]=new Vector2(5,4);
        FivePlayerReview.Check(FiveCourtView.Pick(rect,s,FiveCourtView.ScreenPoint(rect,s.positions[9]))==9,"Overlapping badges select the topmost visible defender");
        s.positions[0]=s.positions[9];FivePlayerReview.Check(FiveCourtView.Pick(rect,s,FiveCourtView.ScreenPoint(rect,s.positions[0]))==0,"Human role badge stays selectable on overlapping markers");
        FivePlayerReview.Check(FiveCourtView.Pick(rect,s,new Vector2(50,30))==-1,"Dragging outside cannot start by selecting an off-court player");
        foreach(var size in new[]{new Vector2Int(1024,576),new Vector2Int(1440,900),new Vector2Int(1920,1080)})
        {
            var layout=new FiveCourtUI.BoardLayout(size.x,size.y);
            FivePlayerReview.Check(layout.Court.width/layout.Scale>630 && Mathf.Abs(layout.Court.width/layout.Court.height-15f/14)<.001f,"Court has grown and retains regulation geometry at "+size);
            FivePlayerReview.Check(layout.Court.xMin>layout.History.xMax&&layout.Court.xMax<layout.Intent.xMin&&layout.Court.yMax<layout.Timeline.yMin&&layout.History.yMax<=size.y,"Board lanes do not overlap at "+size);
        }
        var now=new System.DateTime(2026,11,17,12,0,0);
        var records=new System.Collections.Generic.List<FiveRecording>();
        for(int i=0;i<9;i++)records.Add(new FiveRecording{created=i<2?"11-17 09:00:00":i==2?"11-16 15:20":i<5?"11-16 09:00:00":"11-14 09:00:00"});
        var rows=FiveCourtUI.HistoryLayout(records,now,out float height);
        FivePlayerReview.Check(rows[2].Group=="昨天"&&rows[3].Group==null&&rows[2].Details.StartsWith("15:20 ·"),"Legacy minute-precision replay groups alongside same-day second-precision records and keeps its displayed time");
        foreach(float scale in new[]{.64f,.8f,1.2f})
        {
            var view=new Rect(126,217,200*scale,470*scale);
            int Hit(float x,float y,float scroll=0)=>FiveCourtUI.HistoryHit(view,view.position+new Vector2(x,y)*scale,scroll*scale,scale,rows);
            for(int i=0;i<5;i++)FivePlayerReview.Check(Hit(70,rows[i].Y+20)==i,"Grouped history card selects the original replay index at "+scale);
            FivePlayerReview.Check(Hit(70,rows[0].Y-10)==-1&&Hit(70,rows[2].Y-10)==-1,"Date headers are outside card hit targets");
            FivePlayerReview.Check(Hit(70,rows[1].Y+FiveCourtUI.HistoryRow.Height+2)==-1&&Hit(194,rows[0].Y+20)==-1,"Card gaps and scrollbar do not select a replay");
            float scroll=height-view.height/scale;
            FivePlayerReview.Check(Hit(70,rows[8].Y+20-scroll,scroll)==8,"Last recording remains reachable after scrolling through date headers");
        }
        var active=FivePlayerReview.OpenPass();FiveRules.Pass(active,1);active.time=3;active.phase=2;
        // The open-pass fixture temporarily pins every defender to keep the pass lane clear.
        for(int actor=5;actor<10;actor++)active.pinned[actor]=false;
        var recording=new FiveRecording();recording.Add(active,true);
        var board=new FiveBoard();board.Load(recording);var before=board.State.Copy();
        board.BeginEdit();board.PlaceFrame(6,new Vector2(6.5f,12));
        int mask=board.DefenderShiftMask;
        bool expected=true;
        for(int actor=5;actor<10;actor++)
            expected&=((mask&(1<<actor))!=0)==(actor!=6&&Vector2.Distance(before.positions[actor],board.State.positions[actor])>=.04f);
        FivePlayerReview.Check(mask!=0&&expected&&board.Time==before.time&&board.State.phase==before.phase&&board.State.flight.elapsed==before.flight.elapsed,"Only other moved defenders are emphasized and flight/time remain frozen");
        bool expired=true;float future=Time.realtimeSinceStartup+2;
        for(int actor=5;actor<10;actor++)expired&=board.DefenderShiftOpacity(actor,future)==0;
        FivePlayerReview.Check(expired&&board.Time==before.time&&board.State.flight.elapsed==before.flight.elapsed,"Highlight expiration depends on wall clock without advancing replay or flight");
        board.EndEdit();board.BeginEdit();
        FivePlayerReview.Check(board.DefenderShiftMask==0,"Starting another drag clears stale defense emphasis");
        board.PlaceFrame(5,new Vector2(-6,12));board.EndEdit();board.Restore();
        FivePlayerReview.Check(board.DefenderShiftMask==0&&board.Time==before.time,"Restore clears defense emphasis at the same replay time");
        HistoryRefresh();
        PracticeOverview();
    }
    // Exercise the same sampled inputs that Update sends to the shipped practice scene.
    static void PracticeOverview()
    {
        var scene=EditorSceneManager.OpenScene(FivePlayerAuthoring.Scene,OpenSceneMode.Additive);
        try
        {
            var game=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FiveCourtGame>(true)).Single();
            var player=game.player;
            player.SendMessage("Awake"); // Edit mode does not run normal player lifecycle callbacks.
            game.StartPractice();
            var eyes=player.eyes.transform;
            var initial=player.transform.forward;
            var start=player.transform.position;
            game.DrivePractice(Vector2.up,new Vector2(90,15),.05f,true,false);
            var turned=player.transform.forward;
            var direction=player.transform.position-start;direction.y=0;
            FivePlayerReview.Check(game.Overview&&Vector3.Angle(initial,turned)>85&&eyes.forward.y>.2f,
                "Held Tab accepts mouse yaw and pitch on the first-person camera");
            FivePlayerReview.Check(direction.magnitude>.04f&&Vector3.Dot(direction.normalized,turned)>.95f,
                "Forward movement while holding Tab follows the newly turned heading");
            FivePlayerReview.Check(Vector2.Angle(game.ViewHeading,FiveRules.Flat(turned))<.1f,
                "The live map arrow follows the controlled player's camera heading");
            var rotation=eyes.rotation;
            game.DrivePractice(Vector2.zero,Vector2.zero,.016f,false,false);
            FivePlayerReview.Check(!game.Overview&&Quaternion.Angle(rotation,eyes.rotation)<.01f,
                "Releasing Tab retains the camera orientation set in overview");
        }
        finally{EditorSceneManager.CloseScene(scene,true);}
    }
    static void HistoryRefresh()
    {
        var root=new GameObject("dated history fixture");var game=root.AddComponent<FiveCourtGame>();
        string folder=Path.Combine(ReactiveReview.Folder,"dated-history-"+System.Guid.NewGuid().ToString("N"));
        game.SaveFolder=folder;
        try
        {
            FiveRecording Save(string date,int role)
            {
                var replay=new FiveRecording{created=date};
                replay.Add(FiveRules.Create(FivePlay.HighPost,FiveDefense.Balanced,role,0),true);
                replay.Save(folder);return replay;
            }
            var selected=Save("2026-11-16 12:30:00",3);
            // Give older rounds newer file modification times to exercise calendar ordering.
            for(int i=0;i<60;i++)Save("2026-11-15 12:30:"+i.ToString("00"),0);
            game.LoadHistory();game.SelectReplay(selected.id);
            FivePlayerReview.Check(game.History.Count==61&&game.SelectedReplayIndex==0&&game.Board.State.controlled==3,"History keeps older rounds beyond the former cap and orders by recorded date");
            Save("2026-11-17 08:10:00",2);game.LoadHistory();
            FivePlayerReview.Check(game.SelectedReplayId==selected.id&&game.SelectedReplayIndex==1&&game.Board.Record.id==selected.id,"Refreshing and reordering history preserves the open replay by ID");
            game.ShowPage(0);game.ShowPage(1);
            FivePlayerReview.Check(game.Board.Record.id==selected.id&&game.Board.State.controlled==3,"Returning after history refresh opens the selected recording's own role");
        }
        finally
        {
            Object.DestroyImmediate(root);
            if(Directory.Exists(folder))
            {foreach(var file in Directory.GetFiles(folder,"*.json"))File.Delete(file);Directory.Delete(folder);}
        }
    }
}
