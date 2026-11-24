using System;
using UnityEngine;

namespace CourtX
{
    [Serializable]
    public sealed class FiveZoneArrow
    {
        public string kind, label;
        // Combined court index O1..O5 = 0..4, X1..X5 = 5..9. Unassigned = -1.
        public int player;
        public Vector2 from, to;
        public Vector2[] via;
    }

    [Serializable]
    public sealed class FiveZoneFrame
    {
        public string id, label, title, offenseIntent, defenseIntent, cue;
        public string[] tasks;
        // Team-local indices. A frame without a screen has screener = screened = -1.
        public int holder, screener, screened;
        public Vector2[] offense, defense;
        public FiveZoneArrow[] arrows;
    }

    /// <summary>
    /// CourtLab /zone teaching frames, in source order. Returned frames are shared read-only data.
    /// The 560 px court width maps to 15 m. Source rim (300,84) maps to Unity (0,1.575).
    /// </summary>
    public static class FiveZoneBook
    {
        [Serializable] sealed class Book { public Play[] plays; }
        [Serializable] sealed class Play
        {
            public string name;
            public FiveZoneFrame[] common;
            public Branch[] branches;
        }
        [Serializable] sealed class Branch { public string name; public FiveZoneFrame[] frames; }
        static Book book;

        static Play Get(FivePlay play)
        {
            if (book == null)
            {
                var asset = Resources.Load<TextAsset>("FiveZonePlays");
                if (!asset) throw new InvalidOperationException("Missing FiveZonePlays resource.");
                book = JsonUtility.FromJson<Book>(asset.text);
            }
            return book.plays[(int)play];
        }

        public static string PlayName(FivePlay play) => Get(play).name;
        public static string BranchName(FivePlay play, int branch) => Get(play).branches[branch].name;
        public static int CommonCount(FivePlay play) => Get(play).common.Length;

        // The web demo starts in team view. Unity starts on the central actor of each lesson.
        public static int Recommended(FivePlay play) => play == FivePlay.HighPost ? 4 : 0;

        public static int Count(FivePlay play, int branch)
        {
            var lesson = Get(play);
            return lesson.common.Length + lesson.branches[branch].frames.Length;
        }

        public static FiveZoneFrame Frame(FivePlay play, int branch, int phase)
        {
            var lesson = Get(play);
            return phase < lesson.common.Length
                ? lesson.common[phase]
                : lesson.branches[branch].frames[phase - lesson.common.Length];
        }
    }
}
