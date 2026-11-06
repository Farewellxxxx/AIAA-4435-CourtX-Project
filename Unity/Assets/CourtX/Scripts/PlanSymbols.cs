using UnityEngine;

namespace CourtX
{
    [DefaultExecutionOrder(100)]
    public sealed class PlanSymbols : MonoBehaviour
    {
        public Transform[] subjects;
        public Transform[] badges;
        public Transform heading;
        public void Show(bool visible)
        {
            gameObject.SetActive(visible);
            if(!visible) return;
            Refresh();
        }
        void LateUpdate() => Refresh();
        public void Refresh()
        {
            for(int i=0;i<subjects.Length;i++)
            {
                Vector3 p=subjects[i].position;
                badges[i].position=new Vector3(p.x,4.5f,p.z);
            }
            var player=subjects[(int)CourtRole.BallHandler];
            heading.position=badges[(int)CourtRole.BallHandler].position+player.forward*.55f+Vector3.up*.01f;
            heading.rotation=Quaternion.Euler(90,player.eulerAngles.y,0);
        }
    }
}
