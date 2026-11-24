using System;
using System.Linq;
using CourtX;
using UnityEngine;
public static class FiveFrameReview
{
    static FiveRecording Record(FiveState state){var r=new FiveRecording();r.Add(state,true);return r;}
    public static void Run()
    {
        foreach(FivePlay play in Enum.GetValues(typeof(FivePlay)))foreach(FiveDefense defense in Enum.GetValues(typeof(FiveDefense)))
        {
            var state=FiveRules.Create(play,defense,0,0);state.time=9;state.phaseAge=2;state.phase=2;
            var record=Record(state);var board=new FiveBoard();board.Load(record);string original=JsonUtility.ToJson(record);
            board.PlaceFrame(6,new Vector2(6.8f,12));
            FivePlayerReview.Check(Enumerable.Range(5,5).Any(i=>i!=6&&Vector2.Distance(board.State.positions[i],state.positions[i])>.35f),play+" / "+defense+": paused defender displacement immediately reallocates other defenders");
            FivePlayerReview.Check(!board.Playing&&board.Time==9&&board.State.phase==2&&board.State.phaseAge==2,"Single-frame rearrangement never advances time or play stage");
            FivePlayerReview.Check(board.State.positions[6]==new Vector2(6.8f,12)&&board.State.pinned[6]&&Enumerable.Range(0,5).All(i=>board.State.positions[i]==state.positions[i]),"Dragged defender stays fixed and all offense stays on the frozen frame");
            int moving=Enumerable.Range(5,5).Where(i=>i!=6).OrderByDescending(i=>Vector2.Distance(board.State.positions[i],state.positions[i])).First();
            float now=Time.realtimeSinceStartup;
            var start=board.DisplayPosition(moving,now-1);var middle=board.DisplayPosition(moving,now+.08f);var end=board.DisplayPosition(moving,now+2);
            FivePlayerReview.Check(Vector2.Distance(start,state.positions[moving])<.02f&&Vector2.Distance(middle,start)>.01f&&Vector2.Distance(middle,end)>.01f&&end==board.State.positions[moving]&&board.Time==9,"Help defender moves through an intermediate position with frozen clip time");
            var once=board.State.Copy();board.BeginEdit();board.PlaceFrame(5,new Vector2(-6.5f,11));board.PlaceFrame(5,new Vector2(-6.2f,11));board.EndEdit();
            FivePlayerReview.Check(board.State.positions[6]==once.positions[6]&&board.State.positions[5]==new Vector2(-6.2f,11),"Several manually placed defenders remain where the user put them");
            board.Undo();FivePlayerReview.Check(JsonUtility.ToJson(board.State)==JsonUtility.ToJson(once),"Undo coalesces a paused layout drag with all defensive reactions");
            board.Restore();FivePlayerReview.Check(JsonUtility.ToJson(record)==original&&JsonUtility.ToJson(board.State)==JsonUtility.ToJson(state),"Restore returns to the exact original frame without changing actual record");
        }
        var baseState=FiveRules.Create(FivePlay.HighPost,FiveDefense.Balanced,0,0);var b=new FiveBoard();b.Load(Record(baseState));
        b.PlaceFrame(2,new Vector2(5,6));var first=b.State.Copy();b.PlaceFrame(2,new Vector2(5,6));
        FivePlayerReview.Check(Enumerable.Range(5,5).All(i=>b.State.positions[i]==first.positions[i]),"Same paused layout produces stable deterministic defensive assignment");
        FivePlayerReview.Check(Enumerable.Range(5,5).Any(i=>Vector2.Distance(first.positions[i],baseState.positions[i])>.3f)&&Enumerable.Range(0,5).Where(i=>i!=2).All(i=>first.positions[i]==baseState.positions[i]),"Moving an off-ball attacker changes defensive coverage but not other offense");
        var air=FivePlayerReview.OpenPass();FiveRules.Pass(air,1);air.success=true;air.phase=4;air.time=7;air.phaseAge=3;air.flight.elapsed=.1f;
        b.Load(Record(air));b.PlaceFrame(3,new Vector2(-5,3));
        FivePlayerReview.Check(b.State.success&&b.State.phase==4&&b.Time==7&&b.State.flight.elapsed==.1f&&b.State.Ball==air.Ball,"Even terminal/airborne paused edits preserve the exact phase, clock and ball flight");
        b.Playing=true;b.Tick(.2f);FivePlayerReview.Check(!b.IsBranch,"Playback after a single-frame study explicitly returns to original footage");
        var lost=baseState.Copy();lost.turnover=true;lost.holder=6;lost.phase=3;lost.time=12;b.Load(Record(lost));b.PlaceFrame(1,new Vector2(-5,4));
        FivePlayerReview.Check(b.State.Ball==lost.Ball&&b.State.turnover&&b.State.holder==6,"Defender holding an intercepted ball remains on the frozen turnover frame");
        foreach(var policy in new[]{FiveDefense.Rim,FiveDefense.Perimeter})
        {
            var coverage=FiveRules.Create(FivePlay.HighPost,policy,0,0);b.Load(Record(coverage));b.PlaceFrame(0,new Vector2(0,policy==FiveDefense.Rim?6.5f:5));
            FivePlayerReview.Check(Enumerable.Range(5,5).Min(i=>Vector2.Distance(b.State.positions[i],b.State.Ball))<1.3f,policy+": primary coverage uses the selected defense help depth");
        }
        var root=new GameObject("history selection fixture");var game=root.AddComponent<FiveCourtGame>();
        try
        {
            var older=Record(baseState);var newer=Record(air);game.History.Add(newer);game.History.Add(older);game.SelectReplay(1);
            FivePlayerReview.Check(game.Board.Record.id==older.id,"Selecting an older round loads its own recording identity");
            game.ShowPage(0);game.ShowPage(1);
            FivePlayerReview.Check(game.Board.Record.id==older.id,"Returning to replay remembers the selected round instead of opening newest");
            game.History.Insert(0,Record(baseState));game.ShowPage(0);game.ShowPage(1);
            FivePlayerReview.Check(game.Board.Record.id==older.id,"Remembered history selection survives list insertion by identity");
            foreach(var key in new[]{KeyCode.Space,KeyCode.LeftArrow,KeyCode.RightArrow})
            {game.Board.PlaceFrame(2,new Vector2(6,7));game.BoardShortcut(key,true);FivePlayerReview.Check(game.Board.IsFrameStudy&&!game.Board.Playing,"Playback shortcut "+key+" cannot replace the record while a pointer drag owns it");}
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
