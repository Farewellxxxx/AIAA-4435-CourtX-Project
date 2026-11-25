#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

namespace CourtX
{
    internal static class FiveTouch
    {
        enum Action { None, Pause, Replay, Regroup, Approach, Release }
        static int moveFinger = -1, lookFinger = -1, menuFinger = -1;
        static Action menuAction;
        static Vector2 motion;

        internal static Rect SafeArea
        {
            get
            {
                var area = Screen.safeArea;
                return new Rect(area.x, Screen.height - area.yMax, area.width, area.height);
            }
        }

        readonly struct Layout
        {
            public readonly Rect Safe, Pause, Replay, Regroup, Approach, Release;
            public readonly Vector2 Stick;
            public readonly float Radius, Scale;
            public Layout(Rect safe)
            {
                Safe = safe;
                Scale = Mathf.Min(safe.height / 800f, safe.width / 1150f);
                float u = Scale;
                Radius = 70 * u;
                Stick = new Vector2(safe.x + 110 * u, safe.yMax - 140 * u);
                float right = safe.xMax - 22 * u, top = safe.y + 78 * u;
                Pause = new Rect(right - 106 * u, top, 106 * u, 62 * u);
                Replay = new Rect(Pause.x - 116 * u, top, 106 * u, 62 * u);
                Regroup = new Rect(Replay.x - 116 * u, top, 106 * u, 62 * u);
                Release = new Rect(right - 166 * u, safe.yMax - 235 * u, 166 * u, 80 * u);
                Approach = new Rect(right - 166 * u, Release.yMax + 12 * u, 166 * u, 70 * u);
            }
            public Action Hit(Vector2 p, FiveCourtGame game)
            {
                if (Pause.Contains(p)) return Action.Pause;
                if (Replay.Contains(p)) return Action.Replay;
                if (!game.IsDrill && Regroup.Contains(p)) return Action.Regroup;
                if (Release.Contains(p)) return Action.Release;
                if (Approach.Contains(p)) return Action.Approach;
                return Action.None;
            }
        }

        internal static void Reset()
        {
            moveFinger = lookFinger = menuFinger = -1;
            motion = Vector2.zero;
            menuAction = Action.None;
        }

        internal static void Drive(FiveCourtGame game, float dt)
        {
            var layout = new Layout(SafeArea);
            var look = Vector2.zero;
            bool approach = false, release = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                var p = new Vector2(touch.position.x, Screen.height - touch.position.y);
                int id = touch.fingerId;
                if (touch.phase == TouchPhase.Began && layout.Safe.Contains(p))
                {
                    var action = layout.Hit(p, game);
                    if (action == Action.Release) release = true;
                    else if (action == Action.Approach) approach = true;
                    else if (action != Action.None)
                    {
                        if (menuFinger == -1) { menuFinger = id; menuAction = action; }
                    }
                    else if (Vector2.Distance(p, layout.Stick) < layout.Radius * 1.6f)
                    {
                        if (moveFinger == -1) moveFinger = id;
                    }
                    else if (lookFinger == -1) lookFinger = id;
                }
                bool ending = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                if (id == moveFinger)
                {
                    motion = Vector2.ClampMagnitude(new Vector2(p.x - layout.Stick.x, layout.Stick.y - p.y) / layout.Radius, 1);
                    if (motion.sqrMagnitude < .01f) motion = Vector2.zero;
                    if (ending) { moveFinger = -1; motion = Vector2.zero; }
                }
                if (id == lookFinger)
                {
                    if (touch.phase == TouchPhase.Moved)
                        look += touch.deltaPosition * (150f / Mathf.Max(1, layout.Safe.height));
                    if (ending) lookFinger = -1;
                }
                if (id == menuFinger && ending)
                {
                    var action = menuAction;
                    menuFinger = -1; menuAction = Action.None;
                    if (touch.phase != TouchPhase.Ended || layout.Hit(p, game) != action) continue;
                    if (action == Action.Pause) { game.PausePractice(); return; }
                    if (game.Live.flight?.shot == true) continue;
                    if (action == Action.Replay) { game.FinishPractice(); return; }
                    if (action == Action.Regroup) game.CallPlay();
                }
            }
            game.DrivePractice(motion, look, dt, motion.sqrMagnitude > .81f, approach);
            if (release && game.Target >= 0) game.ReleaseAt(game.Target);
        }

        internal static void Draw(FiveCourtGame game, GUIStyle text)
        {
            var layout = new Layout(SafeArea);
            var panel = new Color(.05f, .12f, .10f, .84f);
            var disabled = new Color(.10f, .15f, .13f, .54f);
            var oldColor = text.normal.textColor;
            void Key(Rect rect, string label, bool enabled)
            {
                FiveCourtView.Box(rect, enabled ? panel : disabled);
                text.normal.textColor = enabled ? Color.white : new Color(.72f, .76f, .73f);
                GUI.Label(rect, label, text);
            }
            bool canInterrupt = game.Live.flight?.shot != true;
            Key(layout.Pause, "暂停", true);
            Key(layout.Replay, "看回放", canInterrupt);
            if (!game.IsDrill) Key(layout.Regroup, "重组", canInterrupt);
            string release = "出球";
            if (game.CanActWithBall && game.Target >= 0)
                release = game.Target == FiveActors.RimTarget ? "投篮" : "传给 " + FiveRules.Number(game.Target);
            Key(layout.Release, release, game.Live.holder == game.Live.controlled);
            Key(layout.Approach, "辅助跑位", game.ShowRunTarget);
            FiveCourtView.Disc(layout.Stick, layout.Radius, panel);
            var knob = layout.Stick + new Vector2(motion.x, -motion.y) * layout.Radius * .64f;
            FiveCourtView.Disc(knob, layout.Radius * .33f, FiveCourtView.Accent);
            var caption = new Rect(layout.Stick.x - layout.Radius, layout.Stick.y + layout.Radius,
                2 * layout.Radius, 33 * layout.Scale);
            text.normal.textColor = Color.white;
            GUI.Label(caption, "移动", text);
            text.normal.textColor = oldColor;
        }
    }
}
#endif
