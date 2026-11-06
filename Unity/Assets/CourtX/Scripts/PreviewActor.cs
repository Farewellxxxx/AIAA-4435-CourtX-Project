using UnityEngine;

namespace CourtX
{
    public sealed class PreviewActor : MonoBehaviour
    {
        public CourtLayout layout;
        public CourtRole role;
        void Awake() => layout.PlaceAtSpawn(transform, role);
    }
}
