using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
            FivePlayerReview.Check(FiveRecording.Valid(lesson)&&lesson.End>0,"Lesson is a complete playable five-player recording");
            FivePlayerReview.Check(lesson.frames.Any(f=>f.phase>=2)&&lesson.frames.Any(f=>f.flight!=null),play+" / "+defense+": teaching progresses to decisions and passes");
        }
    }

    public static void RunLessonTiming()
    {
        const string complete="演练结束 · 配合达成", intercepted="演练结束 · 配合被截断", timeout="演练结束 · 未形成机会";
        var method=typeof(FiveRecording).GetMethod("RecordLesson",BindingFlags.NonPublic|BindingFlags.Static);
        if(method==null)
        {
            FivePlayerReview.Check(false,"Lesson generator supports outcome-aware recording instead of fixed 25-second playback");
            return;
        }
        FiveRecording Record(FiveState state)=>(FiveRecording)method.Invoke(null,new object[]{state});
        FiveState PassFixture(float time,bool success)
        {
            var state=FivePlayerReview.OpenPass();
            state.time=time;
            state.phase=success?2:0;
            state.positions[1]=new Vector2(0,13);
            state.positions[2]=new Vector2(4,10);
            state.pinned[2]=true;
            FiveRules.Pass(state,2);
            return state;
        }
        bool Tail(FiveRecording record,float seconds)
        {
            var settled=record.frames.FindIndex(f=>f.flight==null);
            return settled>0&&Mathf.Abs(record.End-record.frames[settled].time-seconds)<.001f
                &&record.frames.Skip(settled).All(f=>f.flight==null&&f.eventSerial==record.frames[settled].eventSerial);
        }

        var success=PassFixture(2,true);
        success.request=3;success.requestUntil=8;
        var settled=Record(success);
        FivePlayerReview.Check(success.success&&settled.endReason==complete&&settled.frames.Last().holder==2&&Tail(settled,1.5f),
            "Provisional success plays through the catch plus 1.5 seconds without a second pass");
        var blocked=PassFixture(2,true);
        blocked.positions[5]=(blocked.flight.origin+blocked.flight.destination)*.5f;
        var failure=Record(blocked);
        FivePlayerReview.Check(blocked.success&&failure.frames.Last().turnover&&failure.endReason==intercepted&&Tail(failure,1.2f),
            "Interception overrides provisional success and holds the settled failure for 1.2 seconds");
        var expired=FivePlayerReview.OpenPass();
        expired.time=24.9f;
        expired.passCooldown=20;
        var noResult=Record(expired);
        FivePlayerReview.Check(noResult.End==25&&noResult.endReason==timeout&&noResult.frames.Last().flight==null,
            "A lesson without a result or airborne ball stops at exactly 25 seconds");
        var finalTick=FivePlayerReview.OpenPass();
        finalTick.time=25-1f/60;finalTick.phase=2;finalTick.phaseAge=1;
        var wouldPass=finalTick.Copy();FiveRules.Step(wouldPass,1f/60);
        var atDeadline=Record(finalTick);
        FivePlayerReview.Check(wouldPass.flight!=null&&atDeadline.End==25&&atDeadline.frames.Last().flight==null,
            "The cutoff tick does not release an otherwise available new pass");
        var late=Record(PassFixture(24.9f,false));
        FivePlayerReview.Check(late.Sample(25).flight!=null&&late.End>25&&late.End<26.5f&&late.endReason==timeout
            &&late.frames.Last().holder==2&&Tail(late,0),"A non-scoring deadline pass settles before the no-opportunity verdict");
        var lateScore=Record(PassFixture(24.9f,true));
        FivePlayerReview.Check(lateScore.Sample(25).flight!=null&&lateScore.endReason==complete&&Tail(lateScore,1.5f),
            "A scoring deadline pass retains the settled outcome and full success tail");

        var board=new FiveBoard();
        board.Load(failure);board.Seek(failure.End-1.2f+.01f);
        var presentation=typeof(FiveBoard).GetProperty("LessonEnding");
        var detail=typeof(FiveBoard).GetProperty("LessonEndingDetail");
        FivePlayerReview.Check((string)presentation?.GetValue(board)==intercepted&&((string)detail?.GetValue(board))?.Contains("X1")==true,
            "The failure reason is visible for the settled observation tail");
        board.Seek(failure.Start);
        FivePlayerReview.Check(string.IsNullOrEmpty((string)presentation?.GetValue(board)),
            "A provisional success in the air does not reveal any terminal copy");
        board.Load(settled);board.Seek(settled.End-1.49f);
        FivePlayerReview.Check((string)presentation?.GetValue(board)==complete,"Settled success is visible throughout its observation tail");
        board.Load(noResult);board.Seek(24.95f);
        FivePlayerReview.Check(string.IsNullOrEmpty((string)presentation?.GetValue(board)),"Timeout is hidden during the ordinary phase");
        board.Seek(25);
        FivePlayerReview.Check((string)presentation?.GetValue(board)==timeout,"Timeout is shown at the lesson endpoint");
        var actual=new FiveRecording{name="actual repeated call",endReason="结束练习"};
        var state=FivePlayerReview.OpenPass();
        actual.Add(state,true);state.time=18;state.success=true;actual.Add(state,true);
        state.time=25;FiveRules.Call(state);actual.Add(state,true);
        state.time=68;actual.Add(state,true);
        var original=JsonUtility.ToJson(actual);
        board.Load(actual);board.Seek(50);board.Playing=true;for(int i=0;i<4;i++)board.Tick(5);
        FivePlayerReview.Check(board.End==68&&board.Time==68&&!board.Playing&&!board.Record.Sample(25).success
            &&string.IsNullOrEmpty((string)presentation?.GetValue(board))&&JsonUtility.ToJson(actual)==original,
            "Actual replay retains its entire 68 seconds and repeated call without a lesson terminal message");

        foreach(FivePlay play in Enum.GetValues(typeof(FivePlay)))
        foreach(FiveDefense defense in Enum.GetValues(typeof(FiveDefense)))
        {
            bool valid=true;int successes=0,turnovers=0,timeouts=0;float min=float.PositiveInfinity,max=0;
            for(int role=0;role<5;role++)
            for(int holder=0;holder<5;holder++)
            {
                var lesson=FiveRecording.Lesson(play,defense,role,holder);
                var final=lesson.frames.Last();
                if(role==0&&((play==FivePlay.HighPost&&defense==FiveDefense.Balanced)||(play==FivePlay.Horns&&defense==FiveDefense.Rim)))
                    Debug.Log($"LESSON PRESET {play}/{defense} holder={FiveRules.Number(holder)} {lesson.End:F2}s {lesson.endReason}");
                min=Mathf.Min(min,lesson.End);max=Mathf.Max(max,lesson.End);
                if(final.turnover)turnovers++;else if(final.success)successes++;else timeouts++;
                valid&=FiveRecording.Valid(lesson)&&final.flight==null&&lesson.End>0&&lesson.End<=28.02f
                    &&lesson.endReason==(final.turnover?intercepted:final.success?complete:timeout);
            }
            FivePlayerReview.Check(valid,$"{play}/{defense}: 25 role/holder combinations settle at {min:F2}–{max:F2}s, {successes} success, {turnovers} interruption, {timeouts} timeout");
        }
    }
}
