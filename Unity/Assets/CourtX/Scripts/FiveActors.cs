using UnityEngine;
namespace CourtX
{
    public sealed class FiveActors
    {
        readonly FiveCourtGame game;readonly LineRenderer ring,guide;
        public FiveActors(FiveCourtGame owner)
        {
            game=owner;if(!owner.player)return;
            ring=MakeRing("Selected pass receiver",.46f,new Color(.81f,.93f,.5f));
            guide=MakeRing("Suggested tactical area",.8f,new Color(.34f,.85f,.70f,.65f));
        }
        LineRenderer MakeRing(string name,float radius,Color color)
        {
            var obj=new GameObject(name);obj.transform.SetParent(game.transform);var line=obj.AddComponent<LineRenderer>();
            var basis=game.indicatorMaterial;
            if(!basis){Object.Destroy(obj);return null;}
            line.sharedMaterial=new Material(basis){color=color};line.useWorldSpace=false;line.loop=true;line.positionCount=48;line.widthMultiplier=.035f;
            for(int i=0;i<48;i++){float a=i*2*Mathf.PI/48;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}return line;
        }
        public static int PickTarget(Camera eye,FiveState state)
        {
            if(!eye||state==null)return -1;int chosen=-1;float best=.22f;
            for(int i=0;i<5;i++)if(i!=state.controlled)
            {
                var p=eye.WorldToViewportPoint(FiveRules.World(state.positions[i],1.2f));
                if(p.z<=.1f)continue;float d=Vector2.Distance(new Vector2(p.x,p.y),new Vector2(.5f,.5f));
                if(d<best){best=d;chosen=i;}
            }
            return chosen;
        }
        public void Apply()
        {
            var s=game.Live;if(s==null)return;
            for(int i=0;i<game.actors.Length;i++)
            {
                var actor=game.actors[i];if(!actor)continue;actor.gameObject.SetActive(i!=s.controlled);
                actor.transform.position=FiveRules.World(s.positions[i]);
                if(s.facing[i].sqrMagnitude>.01f)actor.transform.rotation=Quaternion.LookRotation(FiveRules.World(s.facing[i]));
                actor.Action=i>=5?Vector2.Distance(s.positions[i],s.Ball)<2?ActorAction.HandsUp:ActorAction.Slide:s.holder==i?ActorAction.Dribble:ActorAction.Space;
                if(s.screenActive&&s.screener==i)actor.Action=ActorAction.Screen;
                if(s.flight!=null&&s.flight.to==i)actor.Action=ActorAction.Receive;
            }
            if(game.ball)
            {
                game.ball.isKinematic=true;game.ball.detectCollisions=false;
                float height=s.flight!=null?1.25f+Mathf.Sin(s.flight.elapsed/s.flight.duration*Mathf.PI)*.35f:.25f+Mathf.Abs(Mathf.Sin(s.time*6))* .85f;
                Vector2 position=s.Ball;if(s.holder>=0)position+=s.facing[s.holder]*.45f;
                game.ball.position=FiveRules.World(position,height);
            }
            if(ring){ring.gameObject.SetActive(game.Target>=0&&game.Practicing);if(game.Target>=0)ring.transform.position=FiveRules.World(s.positions[game.Target],.04f);}
            if(guide){guide.gameObject.SetActive(!s.success&&!s.turnover&&game.Practicing);guide.transform.position=FiveRules.World(s.targets[s.controlled],.035f);}
        }
    }
}
