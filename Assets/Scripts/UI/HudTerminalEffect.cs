using SpaceSim.Player;
using TMPro;
using UnityEngine;

namespace SpaceSim.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class HudTerminalEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI textMesh;
        [SerializeField] private ShipMotor shipMotor;

        [Header("Drive Colors")]
        [SerializeField] private Color riftingColor = new Color(0.78f, 0.70f, 0.50f, 0.82f);
        [SerializeField] private Color vectorColor = new Color(0.34f, 0.70f, 1f, 1f);
        [SerializeField] private Color warpColor = new Color(0.40f, 0.95f, 0.60f, 1f);
        [SerializeField] private Color warpReadyColor = new Color(0.35f, 0.82f, 0.52f, 0.78f);

        [SerializeField, HideInInspector] private int paletteRevision;
        private const int CurrentPaletteRevision = 1;

        private void Awake()
        {
            ApplyCurrentPalette();

            if (textMesh == null)
            {
                textMesh = GetComponent<TextMeshProUGUI>();
            }

            if (shipMotor == null)
            {
                shipMotor = FindFirstObjectByType<ShipMotor>();
            }
        }

        private void OnValidate()
        {
            ApplyCurrentPalette();
        }

        private void Update()
        {
            if (textMesh == null || shipMotor == null)
            {
                return;
            }

            if (shipMotor.IsWarping)
            {
                SetText("/// DRIVE: WARP", warpColor);
                return;
            }

            if (shipMotor.IsWarpDisengaging)
            {
                SetText("/// WARP: DECEL", warpColor);
                return;
            }

            if (shipMotor.IsWarpCharging)
            {
                float pulse = 0.82f + Mathf.Sin(Time.unscaledTime * 8f) * 0.18f;
                Color chargingColor = warpColor;
                chargingColor.a *= pulse;
                SetText($"/// WARP IN: {shipMotor.WarpCountdown}", chargingColor);
                return;
            }

            if (shipMotor.CanChargeWarp)
            {
                SetText("/// HOLD SPACE TO WARP", warpReadyColor);
                return;
            }

            if (shipMotor.IsVectorBoosting)
            {
                float pulse = 0.84f + Mathf.Sin(Time.unscaledTime * 5.5f) * 0.16f;
                Color boosted = vectorColor;
                boosted.a *= pulse;
                SetText("/// DRIVE: VECTOR", boosted);
                return;
            }

            SetText("/// DRIVE: RIFTING", riftingColor);
        }

        private void ApplyCurrentPalette()
        {
            if (paletteRevision >= CurrentPaletteRevision)
            {
                return;
            }

            // Muted warm sand instead of the previous saturated yellow.
            riftingColor = new Color(0.78f, 0.70f, 0.50f, 0.82f);
            paletteRevision = CurrentPaletteRevision;
        }

        private void SetText(string value, Color color)
        {
            textMesh.text = value;
            textMesh.color = color;
        }
    }
}
