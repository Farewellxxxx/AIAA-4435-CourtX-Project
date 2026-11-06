using UnityEngine;

namespace CourtX
{
    [DefaultExecutionOrder(50)]
    public sealed class PreviewBasketball : MonoBehaviour
    {
        public Walkthrough player;
        public Rigidbody ball;
        public Transform forearm;
        public Transform palm;
        public Transform planBall;
        [Min(.1f)] public float fullChargeSeconds = 1.2f;
        public bool IsCharging { get; private set; }
        public bool IsAirborne { get; private set; }
        public float Charge01 => Mathf.Clamp01(chargeTime / fullChargeSeconds);
        public float LastLaunchSpeed { get; private set; }
        float chargeTime;
        float airborneTime;
        float bouncePhase;

        void Awake()
        {
            Physics.IgnoreCollision(ball.GetComponent<Collider>(), player.GetComponent<CharacterController>());
            Recall();
        }

        public void Recall()
        {
            if (!ball.isKinematic)
            {
                ball.velocity = Vector3.zero;
                ball.angularVelocity = Vector3.zero;
            }
            ball.isKinematic = true;
            ball.detectCollisions = false;
            IsCharging = false;
            IsAirborne = false;
            chargeTime = 0;
            airborneTime = 0;
            bouncePhase = Mathf.PI * .5f;
            Pose();
        }

        public void BeginCharge()
        {
            if (IsAirborne || IsCharging) return;
            IsCharging = true;
            chargeTime = 0;
        }

        public void Release()
        {
            if (!IsCharging) return;
            var eye = player.eyes.transform;
            // Begin within the player's collision envelope so throws next to a wall remain inside it.
            ball.position = eye.position + eye.forward * .08f - Vector3.up * .17f;
            LastLaunchSpeed = Mathf.Lerp(5f, 11f, Charge01);
            ball.isKinematic = false;
            ball.detectCollisions = true;
            ball.velocity = eye.forward * LastLaunchSpeed + Vector3.up * 1.7f;
            ball.angularVelocity = eye.right * 9f;
            IsCharging = false;
            IsAirborne = true;
            airborneTime = 0;
        }

        void Update()
        {
            if (IsCharging) chargeTime = Mathf.Min(fullChargeSeconds, chargeTime + Time.deltaTime);
            if (IsAirborne)
            {
                airborneTime += Time.deltaTime;
                if (airborneTime >= 6f || ball.position.y < -3f) Recall();
                return;
            }
            float frequency = Mathf.Lerp(1.4f, 2.1f, Mathf.Clamp01(player.Speed / player.runSpeed));
            bouncePhase += Time.deltaTime * Mathf.PI * frequency;
        }

        void LateUpdate() => Pose();

        public void Pose()
        {
            var root = player.transform;
            Vector3 hand;
            if (IsCharging)
            {
                ball.position = player.eyes.transform.TransformPoint(new Vector3(.12f, -.27f, .64f));
                hand = ball.position + root.right * .08f - Vector3.up * .05f;
            }
            else
            {
                float height = .13f + Mathf.Abs(Mathf.Sin(bouncePhase)) * 1.12f;
                if (!IsAirborne)
                {
                    ball.position = root.TransformPoint(new Vector3(.43f, height, 1.08f));
                    ball.rotation = root.rotation * Quaternion.Euler(18, 25, bouncePhase * 14);
                }
                hand = root.TransformPoint(new Vector3(.44f, IsAirborne ? 1.05f : Mathf.Max(1.0f, height+.14f), .98f));
            }
            var elbow = root.TransformPoint(new Vector3(.50f,1.15f,.34f));
            var wrist = hand - root.forward * .09f;
            forearm.position = (elbow+wrist)*.5f;
            forearm.rotation = Quaternion.FromToRotation(Vector3.up,wrist-elbow);
            forearm.localScale = new Vector3(.085f,(wrist-elbow).magnitude*.5f,.085f);
            palm.position = hand;
            palm.rotation = root.rotation * Quaternion.Euler(IsCharging ? -35 : 12,0,0);
            planBall.gameObject.SetActive(player.overview.enabled);
            planBall.position = new Vector3(ball.position.x,4.55f,ball.position.z);
        }
    }
}
