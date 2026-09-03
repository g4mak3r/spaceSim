using UnityEngine;

namespace SpaceSim.Player
{
    /// <summary>
    /// WARP presentation rig. Cockpit and diegetic HUD are parented under one
    /// RectTransform so every screen-space element shares the exact same warp pull.
    /// RIFTING and VECTOR do not alter FOV or cockpit geometry.
    /// </summary>
    public sealed class ShipCameraEffect : MonoBehaviour
    {
        private const string WarpRigName = "[WARP-VISUAL-RIG]";

        [SerializeField] private ShipMotor shipMotor;
        [SerializeField] private Camera cam;
        [SerializeField] private RectTransform cockpitRect;

        [Header("Unified WARP Lens")]
        [SerializeField, Range(1f, 179f)] private float idleFOV = 60f;
        [SerializeField, Range(1f, 179f)] private float warpTravelFOV = 104f;

        [Header("Unified Cockpit + HUD Pull")]
        [SerializeField, Range(0f, 0.5f)] private float warpHorizontalExpansion = 0.24f;
        [SerializeField, Range(0f, 0.35f)] private float warpVerticalExpansion = 0.14f;
        [SerializeField, Min(0f)] private float warpDownShift = 12f;

        [SerializeField, HideInInspector] private int visualTuningRevision;
        private const int CurrentVisualTuningRevision = 4;

        private RectTransform _warpRig;
        private Vector3 _rigBaseScale = Vector3.one;
        private Vector2 _rigBasePosition;

        private void Awake()
        {
            ApplyVisualTuningRevision();

            if (shipMotor == null)
            {
                shipMotor = FindFirstObjectByType<ShipMotor>();
            }

            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }

            if (cockpitRect == null)
            {
                GameObject cockpit = GameObject.Find("Cockpit");
                if (cockpit != null)
                {
                    cockpitRect = cockpit.GetComponent<RectTransform>();
                }
            }

            BuildUnifiedVisualRig();
        }

        private void LateUpdate()
        {
            if (shipMotor == null)
            {
                return;
            }

            // ShipMotor owns the only temporal smoothing. Every visual consumer
            // reads exactly the same intensity so dust, stars, cockpit, HUD and FOV
            // enter / leave WARP on the same curve and in the same frame.
            float warp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shipMotor.WarpIntensity));

            if (cam != null)
            {
                cam.fieldOfView = Mathf.Lerp(idleFOV, warpTravelFOV, warp);
            }

            if (_warpRig != null)
            {
                _warpRig.localScale = new Vector3(
                    _rigBaseScale.x * (1f + warpHorizontalExpansion * warp),
                    _rigBaseScale.y * (1f + warpVerticalExpansion * warp),
                    _rigBaseScale.z);

                _warpRig.anchoredPosition = _rigBasePosition + Vector2.down * (warpDownShift * warp);
            }
        }

        private void BuildUnifiedVisualRig()
        {
            if (cockpitRect == null)
            {
                return;
            }

            Canvas canvas = cockpitRect.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Canvas rootCanvas = canvas.rootCanvas;
            RectTransform rootRect = rootCanvas.transform as RectTransform;
            if (rootRect == null)
            {
                return;
            }

            Transform existing = rootRect.Find(WarpRigName);
            if (existing != null)
            {
                _warpRig = existing as RectTransform;
            }
            else
            {
                GameObject rigObject = new GameObject(WarpRigName, typeof(RectTransform));
                _warpRig = rigObject.GetComponent<RectTransform>();
                _warpRig.SetParent(rootRect, false);
                _warpRig.anchorMin = Vector2.zero;
                _warpRig.anchorMax = Vector2.one;
                _warpRig.offsetMin = Vector2.zero;
                _warpRig.offsetMax = Vector2.zero;
                _warpRig.pivot = new Vector2(0.5f, 0.5f);
                _warpRig.localScale = Vector3.one;
                _warpRig.anchoredPosition = Vector2.zero;
                _warpRig.SetSiblingIndex(cockpitRect.GetSiblingIndex());
            }

            // Keep authored world/screen positions while making all cockpit display
            // elements children of the same visual rig. This is the important bit:
            // the HUD can no longer lag geometrically behind the cockpit.
            ReparentToRig(cockpitRect);
            ReparentNamedRoot(rootRect, "DiegeticUI");
            ReparentNamedRoot(rootRect, "AIText");

            _rigBaseScale = _warpRig.localScale;
            _rigBasePosition = _warpRig.anchoredPosition;
        }

        private void ReparentNamedRoot(RectTransform root, string objectName)
        {
            Transform target = root.Find(objectName);
            if (target is RectTransform rect)
            {
                ReparentToRig(rect);
            }
        }

        private void ReparentToRig(RectTransform rect)
        {
            if (rect == null || rect == _warpRig || rect.parent == _warpRig)
            {
                return;
            }

            // The rig fills the same root-canvas rect, so preserving authored local
            // UI coordinates is safer and more deterministic than worldPositionStays.
            Vector2 anchorMin = rect.anchorMin;
            Vector2 anchorMax = rect.anchorMax;
            Vector2 pivot = rect.pivot;
            Vector2 sizeDelta = rect.sizeDelta;
            Vector2 anchoredPosition = rect.anchoredPosition;
            Quaternion localRotation = rect.localRotation;
            Vector3 localScale = rect.localScale;

            rect.SetParent(_warpRig, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.localRotation = localRotation;
            rect.localScale = localScale;
        }

        private void ApplyVisualTuningRevision()
        {
            if (visualTuningRevision >= CurrentVisualTuningRevision)
            {
                return;
            }

            idleFOV = 60f;
            warpTravelFOV = 104f;
            warpHorizontalExpansion = 0.24f;
            warpVerticalExpansion = 0.14f;
            warpDownShift = 12f;
            visualTuningRevision = CurrentVisualTuningRevision;
        }
    }
}
