using CourtX;
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
    }
}
