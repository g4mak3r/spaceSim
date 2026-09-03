using System.Collections.Generic;
using SpaceSim.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSim.UI
{
    /// <summary>
    /// WARP-only emissive perspective echo for the diegetic cockpit displays.
    /// The real HUD is attached to the cockpit visual rig; these ghosts expand
    /// toward the viewer from the screen centre, so light appears to rush past the
    /// pilot instead of smearing sideways like ordinary UI motion blur.
    /// </summary>
    public sealed class WarpHudLightTrails : MonoBehaviour
    {
        private const string WarpRigName = "[WARP-VISUAL-RIG]";

        [SerializeField] private ShipMotor shipMotor;

        [Header("Perspective Light Echo")]
        [SerializeField, Range(3, 10)] private int trailLayers = 8;
        [SerializeField, Range(0f, 0.8f)] private float maxPerspectiveExpansion = 0.52f;
        [SerializeField, Min(0f)] private float maxRadialDrift = 14f;
        [SerializeField, Range(0f, 1f)] private float firstLayerAlpha = 0.30f;
        [SerializeField, Range(0f, 1f)] private float layerAlphaFalloff = 0.70f;
        [SerializeField, Min(0f)] private float lightPulseSpeed = 7.2f;
        [SerializeField, Range(0f, 0.3f)] private float lightPulseDepth = 0.055f;

        private readonly List<TextTrail> _textTrails = new();
        private readonly List<ImageTrail> _imageTrails = new();
        private Canvas _rootCanvas;
        private Transform _sourceRoot;
        private bool _built;

        private sealed class TextTrail
        {
            public TextMeshProUGUI Source;
            public TextMeshProUGUI[] Ghosts;
            public Material[] Materials;
        }

        private sealed class ImageTrail
        {
            public Image Source;
            public Image[] Ghosts;
        }

        public void Configure(ShipMotor motor)
        {
            shipMotor = motor;
        }

        private void Start()
        {
            if (shipMotor == null)
            {
                shipMotor = FindFirstObjectByType<ShipMotor>();
            }

            BuildTrails();
        }

        private void LateUpdate()
        {
            if (!_built || shipMotor == null)
            {
                return;
            }

            // No second SmoothDamp here. ShipMotor.WarpIntensity is the common
            // timeline used by stars, dust, FOV, cockpit and HUD.
            float intensity = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shipMotor.WarpIntensity));
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * lightPulseSpeed)
                * lightPulseDepth
                * intensity;

            UpdateTextTrails(intensity, pulse);
            UpdateImageTrails(intensity, pulse);
        }

        private void BuildTrails()
        {
            if (_built)
            {
                return;
            }

            Canvas nearestCanvas = GetComponentInParent<Canvas>();
            if (nearestCanvas == null)
            {
                return;
            }

            _rootCanvas = nearestCanvas.rootCanvas;
            _sourceRoot = _rootCanvas.transform.Find(WarpRigName) ?? _rootCanvas.transform;

            TextMeshProUGUI[] texts = _sourceRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI source in texts)
            {
                if (source == null || source.name.StartsWith("[WARP-GLOW]"))
                {
                    continue;
                }

                _textTrails.Add(CreateTextTrail(source));
            }

            Image[] images = _sourceRoot.GetComponentsInChildren<Image>(true);
            foreach (Image source in images)
            {
                if (source == null
                    || source.name.StartsWith("[WARP-GLOW]")
                    || source.name.Contains("Cockpit"))
                {
                    continue;
                }

                _imageTrails.Add(CreateImageTrail(source));
            }

            _built = true;
        }

        private TextTrail CreateTextTrail(TextMeshProUGUI source)
        {
            var trail = new TextTrail
            {
                Source = source,
                Ghosts = new TextMeshProUGUI[trailLayers],
                Materials = new Material[trailLayers]
            };

            for (int i = 0; i < trailLayers; i++)
            {
                GameObject ghostObject = new GameObject(
                    $"[WARP-GLOW] {source.name} {i + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));

                ghostObject.layer = source.gameObject.layer;
                ghostObject.transform.SetParent(source.transform.parent, false);
                ghostObject.transform.SetSiblingIndex(source.transform.GetSiblingIndex());

                TextMeshProUGUI ghost = ghostObject.GetComponent<TextMeshProUGUI>();
                ghost.raycastTarget = false;
                ghost.font = source.font;
                ghost.fontSize = source.fontSize;
                ghost.fontStyle = source.fontStyle;
                ghost.alignment = source.alignment;
                ghost.enableAutoSizing = source.enableAutoSizing;
                ghost.fontSizeMin = source.fontSizeMin;
                ghost.fontSizeMax = source.fontSizeMax;
                ghost.overflowMode = source.overflowMode;
                ghost.margin = source.margin;

                Material material = source.fontSharedMaterial != null
                    ? new Material(source.fontSharedMaterial)
                    : null;

                if (material != null)
                {
                    material.name = $"{source.name} Warp Perspective Glow {i + 1}";
                    material.hideFlags = HideFlags.DontSave;
                    material.EnableKeyword("GLOW_ON");

                    float layer01 = (i + 1f) / trailLayers;
                    if (material.HasProperty("_GlowOuter"))
                    {
                        material.SetFloat("_GlowOuter", Mathf.Lerp(0.16f, 0.52f, layer01));
                    }

                    if (material.HasProperty("_GlowPower"))
                    {
                        material.SetFloat("_GlowPower", Mathf.Lerp(0.42f, 0.22f, layer01));
                    }

                    ghost.fontSharedMaterial = material;
                }

                ghostObject.SetActive(false);
                trail.Ghosts[i] = ghost;
                trail.Materials[i] = material;
            }

            return trail;
        }

        private ImageTrail CreateImageTrail(Image source)
        {
            var trail = new ImageTrail
            {
                Source = source,
                Ghosts = new Image[trailLayers]
            };

            for (int i = 0; i < trailLayers; i++)
            {
                GameObject ghostObject = new GameObject(
                    $"[WARP-GLOW] {source.name} {i + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

                ghostObject.layer = source.gameObject.layer;
                ghostObject.transform.SetParent(source.transform.parent, false);
                ghostObject.transform.SetSiblingIndex(source.transform.GetSiblingIndex());

                Image ghost = ghostObject.GetComponent<Image>();
                ghost.raycastTarget = false;
                ghost.sprite = source.sprite;
                ghost.type = source.type;
                ghost.preserveAspect = source.preserveAspect;
                ghost.fillMethod = source.fillMethod;
                ghost.fillOrigin = source.fillOrigin;
                ghost.fillClockwise = source.fillClockwise;
                ghost.fillAmount = source.fillAmount;
                ghost.material = source.material;

                ghostObject.SetActive(false);
                trail.Ghosts[i] = ghost;
            }

            return trail;
        }

        private void UpdateTextTrails(float intensity, float pulse)
        {
            foreach (TextTrail trail in _textTrails)
            {
                TextMeshProUGUI source = trail.Source;
                if (source == null)
                {
                    continue;
                }

                Vector2 direction = GetScreenRadialDirection(source.rectTransform);

                for (int i = 0; i < trail.Ghosts.Length; i++)
                {
                    TextMeshProUGUI ghost = trail.Ghosts[i];
                    if (ghost == null)
                    {
                        continue;
                    }

                    bool visible = source.gameObject.activeInHierarchy && intensity > 0.003f;
                    if (ghost.gameObject.activeSelf != visible)
                    {
                        ghost.gameObject.SetActive(visible);
                    }

                    if (!visible)
                    {
                        continue;
                    }

                    SyncRect(source.rectTransform, ghost.rectTransform);
                    ghost.text = source.text;
                    ghost.fontSize = source.fontSize;
                    ghost.fontStyle = source.fontStyle;
                    ghost.alignment = source.alignment;
                    ghost.margin = source.margin;

                    float layer01 = (i + 1f) / trail.Ghosts.Length;
                    float depth = Mathf.Pow(layer01, 1.35f) * intensity;

                    // Perspective zoom does most of the work; positional movement is
                    // intentionally small. The echo grows toward the pilot instead
                    // of looking like text sliding left/right across the display.
                    float expansion = 1f + maxPerspectiveExpansion * depth;
                    Vector3 baseScale = source.rectTransform.localScale;
                    ghost.rectTransform.localScale = new Vector3(
                        baseScale.x * expansion,
                        baseScale.y * expansion,
                        baseScale.z);
                    ghost.rectTransform.anchoredPosition += direction * (maxRadialDrift * depth);

                    float alpha = firstLayerAlpha * Mathf.Pow(layerAlphaFalloff, i) * intensity * pulse;
                    Color sourceColor = source.color;
                    ghost.color = new Color(
                        sourceColor.r,
                        sourceColor.g,
                        sourceColor.b,
                        Mathf.Clamp01(sourceColor.a * alpha));

                    Material material = trail.Materials[i];
                    if (material != null && material.HasProperty("_GlowColor"))
                    {
                        float emission = Mathf.Lerp(1.8f, 5.0f, layer01) * intensity * pulse;
                        material.SetColor(
                            "_GlowColor",
                            new Color(
                                sourceColor.r * emission,
                                sourceColor.g * emission,
                                sourceColor.b * emission,
                                alpha));
                    }
                }
            }
        }

        private void UpdateImageTrails(float intensity, float pulse)
        {
            foreach (ImageTrail trail in _imageTrails)
            {
                Image source = trail.Source;
                if (source == null)
                {
                    continue;
                }

                Vector2 direction = GetScreenRadialDirection(source.rectTransform);

                for (int i = 0; i < trail.Ghosts.Length; i++)
                {
                    Image ghost = trail.Ghosts[i];
                    if (ghost == null)
                    {
                        continue;
                    }

                    bool visible = source.gameObject.activeInHierarchy && intensity > 0.003f;
                    if (ghost.gameObject.activeSelf != visible)
                    {
                        ghost.gameObject.SetActive(visible);
                    }

                    if (!visible)
                    {
                        continue;
                    }

                    SyncRect(source.rectTransform, ghost.rectTransform);
                    ghost.sprite = source.sprite;
                    ghost.fillAmount = source.fillAmount;

                    float layer01 = (i + 1f) / trail.Ghosts.Length;
                    float depth = Mathf.Pow(layer01, 1.35f) * intensity;
                    float expansion = 1f + maxPerspectiveExpansion * 0.65f * depth;
                    Vector3 baseScale = source.rectTransform.localScale;
                    ghost.rectTransform.localScale = new Vector3(
                        baseScale.x * expansion,
                        baseScale.y * expansion,
                        baseScale.z);
                    ghost.rectTransform.anchoredPosition += direction * (maxRadialDrift * 0.7f * depth);

                    float alpha = firstLayerAlpha
                        * 0.48f
                        * Mathf.Pow(layerAlphaFalloff, i)
                        * intensity
                        * pulse;
                    Color sourceColor = source.color;
                    ghost.color = new Color(
                        sourceColor.r,
                        sourceColor.g,
                        sourceColor.b,
                        Mathf.Clamp01(sourceColor.a * alpha));
                }
            }
        }

        private Vector2 GetScreenRadialDirection(RectTransform rect)
        {
            Camera canvasCamera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _rootCanvas.worldCamera;

            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(canvasCamera, rect.position);
            Vector2 direction = screenPosition - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            if (direction.sqrMagnitude < 16f)
            {
                direction = Vector2.up;
            }

            return direction.normalized;
        }

        private static void SyncRect(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta;
            target.anchoredPosition = source.anchoredPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private void OnDestroy()
        {
            foreach (TextTrail trail in _textTrails)
            {
                if (trail.Materials == null)
                {
                    continue;
                }

                foreach (Material material in trail.Materials)
                {
                    if (material != null)
                    {
                        Destroy(material);
                    }
                }
            }
        }
    }
}
