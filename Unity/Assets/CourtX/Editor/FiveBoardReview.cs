using System;
using System.IO;
using System.Linq;
using CourtX;
using UnityEngine;
public static class FiveBoardReview
{
    public static void Diagnose()
    {
        foreach(FivePlay play in Enum.GetValues(typeof(FivePlay)))
        {
            var r=FiveRecording.Lesson(play,FiveDefense.Balanced,0,0);
            foreach(float time in new[]{3f,6f,12f,24f}){var s=r.Sample(time);Debug.Log("DIAG "+play+" t="+time+" phase="+s.phase+" holder="+s.holder+" risk4="+FiveRules.Risk(s,4)+" p0="+s.positions[0]+" p4="+s.positions[4]+" defense="+string.Join("|",s.positions.Skip(5))+" event="+s.lastEvent);}
        }
    }
    public static void Run()
    {
        var s=FivePlayerReview.OpenPass();var record=new FiveRecording{name="fixture",kind="actual"};record.Add(s,true);
        s.time=1;s.positions[0]=new Vector2(-2,10);s.phase=1;s.eventSerial++;record.Add(s,true);
        string original=JsonUtility.ToJson(record);var board=new FiveBoard();board.Load(record);board.Seek(.5f);
        FivePlayerReview.Check(board.State.positions[0]==new Vector2(-3,10)&&board.State.phase==0,"Fractional replay interpolates geometry without revealing future phase");
        board.Move(7,new Vector2(6,8));
        FivePlayerReview.Check(board.IsBranch&&board.State.positions[7]==new Vector2(6,8),"A replay defender can be moved at an arbitrary timestamp");
        board.Playing=true;board.Tick(.8f);board.Playing=false;
        FivePlayerReview.Check(board.Time>1.2f&&JsonUtility.ToJson(record)==original,"Branch continues past source end without mutating actual record");
        board.Undo();FivePlayerReview.Check(!board.IsBranch&&Mathf.Abs(board.Time-.5f)<.001f,"Undo after continuation returns to original edit point");
        board.Move(2,new Vector2(5,4));board.Playing=true;board.Tick(1);board.Playing=false;board.Seek(.75f);board.Move(3,new Vector2(-6,4));
        FivePlayerReview.Check(board.Record.End<=.751f,"Editing an earlier branch cursor discards its obsolete future");
        board.Restore();board.Move(2,new Vector2(5,4));board.Playing=true;board.Tick(1);board.Playing=false;
        board.Seek(.75f);var beforeEdit=board.Record.Copy();var historical=beforeEdit.Sample(.725f).positions[3];
        board.BeginEdit();board.Move(3,new Vector2(6,12));board.Move(3,new Vector2(5,12));board.EndEdit();
        FivePlayerReview.Check(Vector2.Distance(board.Record.Sample(.725f).positions[3],historical)<.0001f,"Fractional branch drag preserves all positions before its edit boundary");
        FivePlayerReview.Check(board.Record.Sample(.75f).positions[3]==new Vector2(5,12),"Exact edit timestamp selects the final edited pose");
        board.Move(3,new Vector2(4,12));FivePlayerReview.Check(Vector2.Distance(board.Record.Sample(.725f).positions[3],historical)<.0001f,"Repeated edit at same timestamp preserves the original left-hand anchor");
        board.Undo();board.Undo();FivePlayerReview.Check(JsonUtility.ToJson(board.Record)==JsonUtility.ToJson(beforeEdit),"Undo restores complete branch timeline after fractional edits");
        board.Restore();FivePlayerReview.Check(!board.IsBranch&&JsonUtility.ToJson(record)==original,"Restore returns to unmodified real replay");
        var flight=FivePlayerReview.OpenPass();FiveRules.Pass(flight,1);FiveRules.Step(flight,.05f,0);
        var airborne=new FiveRecording{name="flight",kind="actual"};airborne.Add(flight,true);board.Load(airborne);
        var at=board.State.Ball;float elapsed=board.State.flight.elapsed;board.Move(3,new Vector2(-5,6));
        FivePlayerReview.Check(board.State.flight!=null&&board.State.Ball==at&&board.State.flight.elapsed==elapsed,"Airborne edit preserves ball position and progress");
        board.Playing=true;board.Tick(1.3f);board.Playing=false;
        FivePlayerReview.Check(board.State.flight==null&&board.State.holder==1,"Airborne What-if reaches a consistent reception");
        foreach(bool turnover in new[]{false,true})
        {
            var terminal=FivePlayerReview.OpenPass();terminal.time=4;terminal.phase=4;terminal.success=!turnover;terminal.turnover=turnover;terminal.holder=turnover?6:0;
            var ended=new FiveRecording();ended.Add(terminal,true);board.Load(ended);board.Move(2,new Vector2(5,6));var pre=board.State.positions[2];
            board.Playing=true;board.Tick(.5f);board.Playing=false;
            FivePlayerReview.Check(!board.State.success&&!board.State.turnover&&Vector2.Distance(board.State.positions[2],pre)>.1f,"Terminal "+(turnover?"turnover":"success")+" branch resumes rules from the edited positions");
            FivePlayerReview.Check(board.State.holder>=0&&board.State.holder<5,"Terminal branch explicitly restores playable offensive possession");
        }
        var successfulFlight=flight.Copy();successfulFlight.success=true;successfulFlight.phase=4;var terminalAirborne=new FiveRecording();terminalAirborne.Add(successfulFlight,true);board.Load(terminalAirborne);board.Move(3,new Vector2(-5,5));
        FivePlayerReview.Check(!board.State.success&&board.State.flight!=null&&board.State.flight.elapsed==successfulFlight.flight.elapsed,"Reopening success preserves an existing airborne pass and elapsed time");
        string folder=Path.Combine(ReactiveReview.Folder,"record-roundtrip");record.Save(folder);
        var loaded=FiveRecording.Read(Path.Combine(folder,record.id+".json"));
        FivePlayerReview.Check(loaded.frames.Count==2&&loaded.frames[1].positions[0]==new Vector2(-2,10),"Actual record saves and reloads complete states");
        airborne.Save(folder);var loadedFlight=FiveRecording.Read(Path.Combine(folder,airborne.id+".json"));
        FivePlayerReview.Check(loaded.frames[0].flight==null&&loadedFlight.frames[0].flight!=null&&loadedFlight.frames[0].flight.to==1,"Persistence distinguishes no-flight from a real airborne state");
        var invalid=record.Copy();invalid.frames[0].positions[2]=new Vector2(float.NaN,1);
        FivePlayerReview.Check(!FiveRecording.Valid(invalid),"Nonfinite coordinates are rejected before rendering or simulation");
        invalid=record.Copy();invalid.frames[1].time=-1;FivePlayerReview.Check(!FiveRecording.Valid(invalid),"Backward record clocks are rejected");
        invalid=record.Copy();invalid.frames[0].holder=40;FivePlayerReview.Check(!FiveRecording.Valid(invalid),"Invalid possession is rejected before array indexing");
        foreach(FivePlay play in Enum.GetValues(typeof(FivePlay)))foreach(FiveDefense defense in Enum.GetValues(typeof(FiveDefense)))
        {
            var lesson=FiveRecording.Lesson(play,defense,0,0);
            FivePlayerReview.Check(FiveRecording.Valid(lesson)&&lesson.End>8,"Lesson is a complete playable five-player recording");
            FivePlayerReview.Check(lesson.frames.Any(f=>f.phase>=2)&&lesson.frames.Any(f=>f.flight!=null),play+" / "+defense+": teaching progresses to decisions and passes");
        }
    }
}
