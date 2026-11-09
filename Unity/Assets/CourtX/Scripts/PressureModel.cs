using UnityEngine;
namespace CourtX
{
    public static class PressureModel
    {
        // Adapted from BasketWorld core/pressure.py (MIT, Evan Zamir 2025).
        // See ThirdPartyNotices/BasketWorld-MIT.txt. Continuous metres replace hex distance.
        // This is relative pressure, NOT a calibrated turnover probability or a random steal.
        public static float Evaluate(Vector3 holder,Vector3[] defenders)
        {
            var drive=TacticalRules.Flat(new Vector3(0,0,-1.575f)-holder).normalized;
            float complement=1;
            foreach(var defender in defenders)
            {
                var delta=TacticalRules.Flat(defender-holder);float distance=delta.magnitude;
                if(distance>2.8f||Vector3.Dot(drive,delta.normalized)<0)continue;
                float pressure=.75f*Mathf.Exp(-1.2f*Mathf.Max(0,distance-1));
                complement*=1-pressure;
            }
            return Mathf.Clamp01(1-complement);
        }
    }
}
