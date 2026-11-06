using UnityEngine;

namespace CourtX
{
    public enum CourtRole { BallHandler, Screener, CornerShooter, OnBallDefender, ScreenDefender, HelpDefender }

    public sealed class CourtLayout : MonoBehaviour
    {
        [Tooltip("Ordered by CourtRole. Place each transform at foot level; blue axis is facing.")]
        [SerializeField] Transform[] spawnPoints = new Transform[6];
        public Transform ScreenPoint;
        public Transform RollTarget;
        public Transform WeakSideCorner;
        public Transform RimTarget;

        public Transform GetSpawn(CourtRole role) => spawnPoints[(int)role];
        public void PlaceAtSpawn(Transform actor, CourtRole role)
        {
            var spawn = GetSpawn(role);
            actor.SetPositionAndRotation(spawn.position, spawn.rotation);
        }
    }
}
