using System;
namespace CourtX
{
    [Serializable] public sealed class LearningScenario
    {
        public TacticalDrill drill;
        public int controlled;
        public DefenseCoverage coverage;
        public int seed=4435;
        public LearningScenario Copy()=>new LearningScenario{drill=drill,controlled=controlled,coverage=coverage,seed=seed};
        public LearningScenario Normalize()
        {
            var result=Copy();
            if(!Enum.IsDefined(typeof(TacticalDrill),drill))result.drill=TacticalDrill.PickAndRoll;
            result.controlled=controlled==1?1:0;
            if(result.drill==TacticalDrill.HighPostCut||!Enum.IsDefined(typeof(DefenseCoverage),coverage))result.coverage=DefenseCoverage.Drop;
            return result;
        }
        public DefenseCoverage ResolveCoverage()
        {var value=Normalize();var policy=new ReactiveTactics();policy.Reset(value.coverage,value.seed);return policy.Coverage;}
        public string Key=>drill+"/"+controlled+"/"+coverage+"/"+seed;
        public static LearningScenario FromEpisode(TacticalEpisode episode)
        {
            TacticalDrill drill;
            if(episode==null||!Enum.TryParse(episode.drill,out drill)||!Enum.IsDefined(typeof(TacticalDrill),drill)||episode.controlled<0||episode.controlled>1)return null;
            var result=new LearningScenario{drill=drill,controlled=episode.controlled,seed=episode.seed};
            if(drill==TacticalDrill.HighPostCut)return result;
            if(episode.hasScenarioConfig)
            {
                if(!Enum.IsDefined(typeof(DefenseCoverage),episode.coverageChoice))return null;
                result.coverage=(DefenseCoverage)episode.coverageChoice;return result;
            }
            foreach(var c in new[]{DefenseCoverage.Drop,DefenseCoverage.Switch,DefenseCoverage.Blitz})
                if(episode.coverage==TacticalSession.CoverageLabel(c)){result.coverage=c;return result;}
            return null;
        }
    }
}
