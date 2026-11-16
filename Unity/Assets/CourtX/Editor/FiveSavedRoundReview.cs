using System;
using System.IO;
using System.Linq;
using CourtX;
using UnityEngine;
public static class FiveSavedRoundReview
{
    public static void Run()
    {
        string folder=Path.Combine(FivePlayerAuthoring.Delivery,"Replays5v5");
        var files=Directory.GetFiles(folder,"*.json");var originals=files.ToDictionary(f=>f,File.ReadAllText);
        var root=new GameObject("saved rounds read-only acceptance");var game=root.AddComponent<FiveCourtGame>();
        try
        {
            game.SaveFolder=folder;game.LoadHistory();int count=game.History.Count;
            if(count!=files.Length||count<2)throw new Exception("Expected existing selectable rounds");
            var calendar=FiveCourtUI.HistoryLayout(game.History,DateTime.Now,out _);
            if(calendar.Any(row=>row.Group=="日期未知"||row.Details.StartsWith("时间未知",StringComparison.Ordinal)))
                throw new Exception("An existing 1116 recording lost its legacy date or time");
            for(int i=0;i<count;i++)
            {
                var record=game.History[i];game.SelectReplay(i);
                if(game.Board.Record.id!=record.id||game.Board.End!=record.End)throw new Exception("Wrong replay identity/duration");
                game.ShowPage(0);game.ShowPage(1);
                if(game.Board.Record.id!=record.id)throw new Exception("Tab change forgot selected history");
                float at=record.End*.5f;game.Board.Seek(at);game.Board.PlaceFrame(6,new Vector2(5.5f,11));
                if(!game.Board.IsFrameStudy||Mathf.Abs(game.Board.Time-at)>.001f)throw new Exception("Actual saved round did not support frozen layout");
                game.Board.Restore();if(game.Board.Record.id!=record.id)throw new Exception("Restore changed the actual round");
            }
            if(originals.Any(p=>File.ReadAllText(p.Key)!=p.Value))throw new Exception("Existing saved evidence changed");
            Directory.CreateDirectory(ReactiveReview.Folder);
            File.WriteAllText(Path.Combine(ReactiveReview.Folder,"existing-rounds-checks.txt"),"PASS "+count+" existing rounds have valid calendar groups and clocks, independently load by identity and duration, remember tab selection, support frozen-frame edits and restore.\nPASS Original files unchanged.\nNo native pointer actions were performed.\n");
            Debug.Log("EXISTING ROUNDS PASSED: "+count);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
