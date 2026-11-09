using UnityEngine;
namespace CourtX
{
    public enum DefenseCoverage { Drop, Switch, Blitz, Random }
    public enum DefenseStage { Contain, Engaged, Recover }
    // Coordinator owns assignments and intentions only. TacticalBall owns possession.
    public sealed class ReactiveTactics
    {
        public DefenseCoverage Coverage { get; private set; }
        public DefenseStage Stage { get; private set; }
        public int[] Marks { get; } = {0,1,2};
        public Vector3[] DefenseTargets { get; } = new Vector3[3];
        public bool HelpActive { get; private set; }
        public float ReactionDelay { get; private set; }
        float eligible,engaged,helpIn,helpOut;
        bool armed,used;
        static readonly Vector3 rim=new Vector3(0,0,-1.575f);
        public void Reset(DefenseCoverage coverage,int seed)
        {
            var random=new System.Random(seed);
            Coverage=coverage==DefenseCoverage.Random?(DefenseCoverage)random.Next(3):coverage;
            ReactionDelay=.22f+(float)random.NextDouble()*.08f;
            Stage=DefenseStage.Contain;HelpActive=armed=used=false;
            eligible=engaged=helpIn=helpOut=0;
            for(int i=0;i<3;i++){Marks[i]=i;DefenseTargets[i]=Vector3.zero;}
        }
        public void Step(TacticalDrill drill,TacticalPhase phase,int holder,int flightTarget,Vector3[] p,float dt)
        {
            int owner=holder>=0?holder:flightTarget;
            bool pnr=drill==TacticalDrill.PickAndRoll;
            if(pnr&&phase==TacticalPhase.ScreenReady&&holder==0)armed=true;
            bool usedScreen=pnr&&armed&&!used&&phase==TacticalPhase.Roll&&holder==0&&TacticalRules.Distance(p[0],p[1])<3.8f;
            eligible=usedScreen?eligible+dt:0;
            if(eligible>=ReactionDelay)
            {
                used=true;Stage=DefenseStage.Engaged;engaged=0;
                if(Coverage==DefenseCoverage.Switch){Marks[0]=1;Marks[1]=0;}
            }
            if(Stage==DefenseStage.Engaged)
            {
                engaged+=dt;
                if(holder!=0||phase==TacticalPhase.Complete||phase==TacticalPhase.Turnover||p[0].z< -10.2f||
                    TacticalRules.Distance(p[0],p[1])>5.4f||(Coverage==DefenseCoverage.Blitz&&engaged>2.8f))Stage=DefenseStage.Recover;
            }
            bool trap=Stage==DefenseStage.Engaged&&Coverage==DefenseCoverage.Blitz;
            bool threat=owner>=0&&owner<2&&((p[owner].z> -5.8f&&Mathf.Abs(p[owner].x)<3.4f)||trap);
            if(threat){helpIn+=dt;helpOut=0;if(helpIn>=.28f)HelpActive=true;}
            else {helpIn=0;helpOut+=dt;if(helpOut>=.50f)HelpActive=false;}
            if(owner==2||phase==TacticalPhase.Turnover)HelpActive=false;
            for(int i=0;i<3;i++)
            {
                var pos=p[Marks[i]];
                DefenseTargets[i]=pos+(rim-pos).normalized*(Marks[i]==owner?1.25f:1.45f);
            }
            if(Stage==DefenseStage.Engaged)
            {
                if(Coverage==DefenseCoverage.Drop)
                {
                    DefenseTargets[0]=p[0]+Vector3.back*.85f;
                    DefenseTargets[1]=new Vector3(Mathf.Lerp(p[0].x,p[1].x,.6f),0,Mathf.Clamp(p[0].z+2.6f,-6.3f,-3.1f));
                }
                else if(trap)
                {DefenseTargets[0]=p[0]+new Vector3(-.85f,0,.4f);DefenseTargets[1]=p[0]+new Vector3(.85f,0,.4f);}
            }
            if(HelpActive&&owner>=0&&owner<3)
                DefenseTargets[2]=trap?p[1]+Vector3.forward*.8f:Vector3.Lerp(p[2],p[owner],.65f)+Vector3.forward*.45f;
            for(int i=0;i<3;i++)DefenseTargets[i]=TacticalRules.ClampCourt(DefenseTargets[i]);
        }
    }
}
