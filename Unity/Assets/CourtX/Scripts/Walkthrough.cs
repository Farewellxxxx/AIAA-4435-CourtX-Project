using UnityEngine;

namespace CourtX
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class Walkthrough : MonoBehaviour
    {
        public CourtLayout layout;
        public Camera eyes;
        public Camera overview;
        public GameObject mapMarker;
        public PlanSymbols planSymbols;
        public PreviewBasketball basketball;
        [Min(0.1f)] public float walkSpeed = 3f;
        [Min(0.1f)] public float runSpeed = 5.5f;
        [System.NonSerialized] public bool acceptsInput = true;
        public float Speed { get; private set; }
        CharacterController body;
        float pitch;
        bool showControls = true;
        bool map;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            ResetView();
        }

        public void ResetView()
        {
            body.enabled = false;
            layout.PlaceAtSpawn(transform, CourtRole.BallHandler);
            body.enabled = true;
            pitch = 0f;
            eyes.transform.localRotation = Quaternion.identity;
            SetOverview(false);
            Speed = 0;
            if (basketball) basketball.Recall();
        }

        public void SetOverview(bool active)
        {
            map = active;
            overview.enabled = active;
            eyes.enabled = !active;
            mapMarker.SetActive(active);
            if (planSymbols) planSymbols.Show(active);
        }

        public void Step(Vector2 motion, Vector2 look, float seconds, bool sprint = false)
        {
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -78f, 78f);
            eyes.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            var move = Vector2.ClampMagnitude(motion, 1f);
            var flat = transform.right * move.x + transform.forward * move.y;
            Vector3 before = transform.position;
            body.Move((flat * (sprint ? runSpeed : walkSpeed) + Vector3.down * 3f) * seconds);
            Vector3 travelled = transform.position - before;
            travelled.y = 0;
            Speed = seconds > 0 ? travelled.magnitude / seconds : 0;
            if (map && planSymbols) planSymbols.Refresh();
        }

        public void Drive(Vector2 motion, Vector2 look, float seconds, bool mapHeld, bool sprint)
        {
            if (mapHeld != map) SetOverview(mapHeld);
            Step(motion, look, seconds, sprint);
        }

        void Update()
        {
            if (!acceptsInput) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Cursor.lockState = CursorLockMode.None;
            if (Input.GetMouseButtonDown(0) && !map) Cursor.lockState = CursorLockMode.Locked;
            if (Input.GetKeyDown(KeyCode.R)) ResetView();
            if (Input.GetKeyDown(KeyCode.F1)) showControls = !showControls;
            Vector2 look = Cursor.lockState == CursorLockMode.Locked || Input.GetMouseButton(1)
                ? new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2f : Vector2.zero;
            Drive(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), look,
                Time.deltaTime, Input.GetKey(KeyCode.Tab),
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            if (!basketball) return;
            if (Input.GetKeyDown(KeyCode.E)) basketball.Recall();
            if (Input.GetKeyDown(KeyCode.Q)) basketball.BeginCharge();
            if (Input.GetKeyUp(KeyCode.Q)) basketball.Release();
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            SetOverview(false);
            if (basketball && basketball.IsCharging) basketball.Recall();
        }

        void OnDisable() => Cursor.lockState = CursorLockMode.None;

        void OnGUI()
        {
            if (!showControls) return;
            const string controls = "WASD  walk    •    Hold Shift  run    •    Hold Tab  live map    •    Mouse  look\nHold Q / release  throw    •    E  recall ball    •    R  reset    •    F1  hide    •    Esc  release mouse";
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            var width = Mathf.Min(Screen.width - 20, 780);
            var bounds = new Rect((Screen.width-width)/2, Screen.height-62, width, 52);
            GUI.color = new Color(.035f,.12f,.15f,.9f);
            GUI.DrawTexture(bounds, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(bounds, controls, style);
            if (!basketball || !basketball.IsCharging) return;
            float barWidth = 140;
            var bar = new Rect((Screen.width-barWidth)/2, Screen.height/2+42, barWidth, 6);
            GUI.color = new Color(.08f,.18f,.20f,.85f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            bar.width *= basketball.Charge01;
            GUI.color = new Color(1f,.63f,.18f,1);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width/2-12,Screen.height/2-12,24,24),"+",style);
        }
    }
}
