using UnityEngine;
namespace CourtX
{
    public sealed class FiveActors
    {
        readonly FiveCourtGame game;
        readonly LineRenderer ring;
        readonly Transform guide,arrow;
        const float GroundRingRadius=.8f;
        FiveState round;
        Vector2 previousWaypoint;
        bool wasApproaching;
        float arrivalTime=float.NegativeInfinity;

        public float ArrivalOpacity=>round==null?0:Mathf.Clamp01((arrivalTime+1.05f-round.time)/.45f);

        public FiveActors(FiveCourtGame owner)
        {
            game=owner;if(!owner.player||!owner.indicatorMaterial)return;
            ring=MakeRing("Selected pass receiver",.46f,new Color(.81f,.93f,.5f));
            ring.gameObject.SetActive(false);
            var material=Resources.Load<Material>("FiveRunMarker");
            var quad=Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            guide=MakeMarker("Suggested tactical area",material,quad,true);
            guide.localRotation=Quaternion.Euler(90,0,0);
            // The painted target sits comfortably inside the player's arrival tolerance.
            guide.localScale=Vector3.one*(2f*GroundRingRadius/.82f);
            arrow=MakeMarker("Your running waypoint",material,quad,false);
        }
        LineRenderer MakeRing(string name,float radius,Color color)
        {
            var obj=new GameObject(name);
            obj.transform.SetParent(game.transform,false);
            var line=obj.AddComponent<LineRenderer>();
            line.sharedMaterial=game.indicatorMaterial;
            var tint=new MaterialPropertyBlock();
            tint.SetColor("_Color",color);
            line.SetPropertyBlock(tint);
            line.useWorldSpace=false;
            line.loop=true;
            line.widthMultiplier=.035f;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows=false;
            line.positionCount=48;
            for(int i=0;i<48;i++){float a=i*2*Mathf.PI/48;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}return line;
        }
        Transform MakeMarker(string name,Material material,Mesh quad,bool onFloor)
        {
            var obj=new GameObject(name);
            obj.transform.SetParent(game.transform,false);
            obj.AddComponent<MeshFilter>().sharedMesh=quad;
            var renderer=obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            if(onFloor)
            {
                var properties=new MaterialPropertyBlock();
                properties.SetFloat("_GroundRing",1);
                properties.SetColor("_Fill",new Color(.34f,.85f,.70f));
                renderer.SetPropertyBlock(properties);
            }
            obj.SetActive(false);
            return obj.transform;
        }

        void UpdateWaypoint(FiveState s)
        {
            if(!guide)return;
            var position=s.positions[s.controlled];
            if(round!=s)
            {
                round=s;
                arrivalTime=float.NegativeInfinity;
                wasApproaching=false;
            }
            // Tick can advance a via point or open the assessment decision before this view updates.
            if(wasApproaching&&Vector2.Distance(position,previousWaypoint)<=FiveCoach.PlayerArrival)
                arrivalTime=s.time;
            bool show=game.ShowRunTarget;
            wasApproaching=show;
            guide.gameObject.SetActive(show);
            if(!show){arrow.gameObject.SetActive(false);return;}
            var destination=FiveCoach.RunTarget(s);
            previousWaypoint=destination;
            float distance=Vector2.Distance(position,destination);
            guide.position=FiveRules.World(destination,.035f);
            var eye=game.player.eyes;
            arrow.position=WaypointAnchor(eye,destination,distance,s.time);
            float depth=Vector3.Dot(arrow.position-eye.transform.position,eye.transform.forward);
            arrow.gameObject.SetActive(depth>eye.nearClipPlane);
            if(depth<=eye.nearClipPlane)return;
            // A camera-parallel face has the same proportions at every pitch and bearing.
            arrow.rotation=eye.transform.rotation;
            float viewHeight=2f*depth*Mathf.Tan(eye.fieldOfView*.5f*Mathf.Deg2Rad);
            arrow.localScale=Vector3.one*Mathf.Min(viewHeight*.072f,.96f);
        }
        public static Vector3 WaypointAnchor(Camera eye,Vector2 destination,float distance,float time)
        {
            float lift=Mathf.Min(distance*.095f,.74f);
            float bob=Mathf.Min(distance*.023f,.12f)*Mathf.Sin(time*4.5f);
            return FiveRules.World(destination,eye.transform.position.y+lift+bob);
        }
        public const int RimTarget=10;
        public static Vector2 Crosshair(Camera eye)=>eye?eye.pixelRect.center:new Vector2(Screen.width,Screen.height)*.5f;
        public static int PickTarget(Camera eye,FiveState state,bool includeRim=true)
        {
            if(!eye||state==null)return -1;
            Vector2 cursor=Crosshair(eye);
            var rim=FiveRules.World(FiveFlight.Rim,FiveFlight.RimHeight);
            var hoop=eye.WorldToScreenPoint(rim);
            float radius=Vector2.Distance(hoop,eye.WorldToScreenPoint(rim+eye.transform.right*.24f));
            float best=1.001f;int chosen=-1;
            if(includeRim&&hoop.z>=eye.nearClipPlane&&hoop.z<=eye.farClipPlane)
            {
                float score=Vector2.Distance(cursor,hoop)/(radius+eye.pixelHeight*.02f);
                if(score<best){best=score;chosen=RimTarget;}
            }
            for(int i=0;i<5;i++)if(i!=state.controlled)
            {
                var chest=FiveRules.World(state.positions[i],1.15f);
                var feet=chest-Vector3.up*.9f;
                var head=chest+Vector3.up*.55f;
                var forward=eye.transform.forward;
                float feetDepth=Vector3.Dot(feet-eye.transform.position,forward);
                float headDepth=Vector3.Dot(head-eye.transform.position,forward);
                if(Mathf.Max(feetDepth,headDepth)<eye.nearClipPlane||
                   Mathf.Min(feetDepth,headDepth)>eye.farClipPlane)continue;

                // Project only the visible body segment. A point behind the eye
                // otherwise projects onto the opposite side of the crosshair.
                float depthSpan=headDepth-feetDepth;
                if(Mathf.Abs(depthSpan)>.00001f)
                {
                    var axis=head-feet;
                    head+=axis*((Mathf.Clamp(headDepth,eye.nearClipPlane,eye.farClipPlane)-headDepth)/depthSpan);
                    feet+=axis*((Mathf.Clamp(feetDepth,eye.nearClipPlane,eye.farClipPlane)-feetDepth)/depthSpan);
                }
                var low=eye.WorldToScreenPoint(feet);
                var high=eye.WorldToScreenPoint(head);
                float along;
                float distance=FiveRules.SegmentDistance(cursor,low,high,out along);
                // Screen-space interpolation is perspective-corrected to measure
                // the silhouette width at the same depth as the nearest body point.
                float worldAlong=eye.orthographic?along:
                    along*low.z/Mathf.Lerp(high.z,low.z,along);
                var nearest=Vector3.Lerp(feet,head,worldAlong);
                float width=Vector2.Distance(eye.WorldToScreenPoint(nearest),
                    eye.WorldToScreenPoint(nearest+eye.transform.right*.32f));
                float score=distance/(width+eye.pixelHeight*.011f);
                if(score<best){best=score;chosen=i;}
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
                if(s.flight?.shot==true&&s.flight.from==i)actor.Action=ActorAction.Shoot;
            }
            if(game.ball)
            {
                game.ball.isKinematic=true;game.ball.detectCollisions=false;
                float height=s.flight!=null?s.flight.Height:s.madeBasket?2.5f:.25f+Mathf.Abs(Mathf.Sin(s.time*6))* .85f;
                Vector2 position=s.Ball;if(s.holder>=0)position+=s.facing[s.holder]*.45f;
                game.ball.position=FiveRules.World(position,height);
            }
            if(ring)
            {
                ring.gameObject.SetActive(game.Target>=0&&game.Practicing);
                if(game.Target==RimTarget)ring.transform.position=FiveRules.World(FiveFlight.Rim,FiveFlight.RimHeight);
                else if(game.Target>=0)ring.transform.position=FiveRules.World(s.positions[game.Target],.04f);
            }
            UpdateWaypoint(s);
        }
    }
}
