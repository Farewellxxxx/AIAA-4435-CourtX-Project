using System;
using System.Collections.Generic;
using System.IO;
using CourtX;
using UnityEngine;

public static class ReadabilityReview
{
    public static void RunRules()
    {
        var lines=new List<string>();int failures=0;
        void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
        var replay=new RecapPlayback();
        replay.Reset(LearningCatalog.Load(new LearningScenario{coverage=DefenseCoverage.Switch}),-1);replay.Seek(3.5f);
        var frame=replay.Sample();
        // Recorded overlap, independently inspected in the 1920x1080 screenshot:
        // (819,545) is inside the foreground 12 badge, although 04's centre is nearer.
        var large=new Rect(448,282,735,542);
        Check(RecapCourtView.PickActor(large,frame,new Vector2(819,545))==4,"Overlapping visible 12 badge wins over obscured closer 04");
        var small=new Rect(224,141,367.5f,271);
        Check(RecapCourtView.PickActor(small,frame,new Vector2(409.5f,272.5f))==4,"Overlap selection remains correct at half scale");
        var letterboxed=new Rect(448,322,735,542);
        Check(RecapCourtView.PickActor(letterboxed,frame,new Vector2(819,585))==4,"Overlap selection follows letterbox offset");
        Check(RecapCourtView.PickActor(large,frame,new Vector2(1044,378))==2,"Separate 05 badge remains selectable");
        Check(RecapCourtView.PickActor(large,frame,new Vector2(400,545))==-1,"Outside court does not select a player");
        Check(RecapCourtView.PickActor(large,null,new Vector2(819,545))==-1,"Empty record does not create a selection");
        lines.Add("Failures: "+failures);Directory.CreateDirectory(ReactiveReview.Folder);
        File.WriteAllLines(Path.Combine(ReactiveReview.Folder,ReactiveReview.Label+"-checks.txt"),lines);
        if(failures>0)throw new Exception(failures+" readability checks failed");
    }
}
