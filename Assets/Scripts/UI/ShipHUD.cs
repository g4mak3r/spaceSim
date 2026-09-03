using SpaceSim.Core;
using SpaceSim.Player;
using SpaceSim.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceSim.UI
{
    public sealed class ShipHUD : MonoBehaviour
    {
        public static ShipHUD Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private Transform playerShip;
        [SerializeField] private ShipMotor shipMotor;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI locationText;
        [SerializeField] private TextMeshProUGUI distanceText;

        [Header("Navigation Bar")]
        [SerializeField] private RectTransform navMarker;
        [SerializeField, Min(0f)] private float maxOffset = 60f;
        [SerializeField, Min(0f)] private float smoothTime = 8f;

        [Header("Telemetry")]
        [SerializeField, Min(0.05f)] private float telemetryRefreshInterval = 0.18f;

        private float _currentMarkerX;
        private float _nextTelemetryRefresh;
        private Image _navMarkerImage;
        private string _locationName = "DEEP SPACE";
        private bool _hasNavigationFix;
        private float _navigationDistance;
        private Vector3 _navigationDirection;
        private Vector3 _navigationLocalDirection;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (navMarker != null)
            {
                _navMarkerImage = navMarker.GetComponent<Image>();
            }

            ConfigureTelemetryDisplay();
            SetupWarpLightTrails();
        }

        private void Start()
        {
            UpdateLocationName("DEEP SPACE");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            UpdateSpeedDisplay();
            UpdateNavigationBar();
            UpdateTelemetryDisplay();
        }

        private void ConfigureTelemetryDisplay()
        {
            if (locationText == null)
            {
                return;
            }

            RectTransform rect = locationText.rectTransform;
            rect.sizeDelta = new Vector2(
                Mathf.Max(rect.sizeDelta.x, 430f),
                Mathf.Max(rect.sizeDelta.y, 82f));

            locationText.enableAutoSizing = true;
            locationText.fontSizeMin = 11f;
            locationText.fontSizeMax = 20f;
            locationText.lineSpacing = -8f;
            locationText.enableWordWrapping = false;
        }

        private void UpdateSpeedDisplay()
        {
            if (shipMotor == null || speedText == null)
            {
                return;
            }

            float displaySpeed = shipMotor.SignedForwardSpeed < -0.5f
                ? -shipMotor.CurrentSpeed
                : shipMotor.CurrentSpeed;

            speedText.text = $"{Mathf.RoundToInt(displaySpeed)} UNIT/S";
        }

        public void UpdateLocationName(string locationName)
        {
            _locationName = string.IsNullOrWhiteSpace(locationName)
                ? "UNKNOWN"
                : locationName.ToUpperInvariant();

            _nextTelemetryRefresh = 0f;
            UpdateTelemetryDisplay(true);
        }

        private void UpdateNavigationBar()
        {
            if (playerShip == null || navMarker == null || GalaxyManager.Instance == null)
            {
                _hasNavigationFix = false;
                return;
            }

            StarSystem nearestSystem = GalaxyManager.Instance.GetNearestSystem(playerShip.position);
            if (nearestSystem == null)
            {
                _hasNavigationFix = false;
                SetNavigationVisible(false);
                SetDistanceText("---");
                return;
            }

            Vector3 targetPosition = nearestSystem.SunTransform != null
                ? nearestSystem.SunTransform.position
                : nearestSystem.transform.position;

            Vector3 toTarget = targetPosition - playerShip.position;
            float distance = toTarget.magnitude;

            if (distance <= Mathf.Epsilon)
            {
                _hasNavigationFix = false;
                return;
            }

            _hasNavigationFix = true;
            _navigationDistance = distance;
            _navigationDirection = toTarget / distance;
            _navigationLocalDirection = playerShip.InverseTransformDirection(_navigationDirection);

            float angle = Mathf.Atan2(_navigationLocalDirection.x, _navigationLocalDirection.z) * Mathf.Rad2Deg;
            float normalizedAngle = Mathf.Clamp(angle / 90f, -1f, 1f);
            float targetX = normalizedAngle * maxOffset;

            float blend = 1f - Mathf.Exp(-smoothTime * Time.deltaTime);
            _currentMarkerX = Mathf.Lerp(_currentMarkerX, targetX, blend);
            navMarker.anchoredPosition = new Vector2(_currentMarkerX, navMarker.anchoredPosition.y);

            bool isBehind = _navigationLocalDirection.z < -0.2f;
            SetNavigationVisible(!isBehind);

            if (_navMarkerImage != null)
            {
                _navMarkerImage.color = Mathf.Abs(normalizedAngle) < 0.1f
                    ? Color.cyan
                    : new Color(0.7f, 0.8f, 1f, 0.6f);
            }

            SetDistanceText(FormatDistance(distance));
        }

        private void UpdateTelemetryDisplay(bool force = false)
        {
            if (locationText == null || playerShip == null || shipMotor == null)
            {
                return;
            }

            if (!force && Time.unscaledTime < _nextTelemetryRefresh)
            {
                return;
            }

            _nextTelemetryRefresh = Time.unscaledTime + telemetryRefreshInterval;

            Vector3 velocity = shipMotor.Velocity;
            float speed = velocity.magnitude;
            float forwardSpeed = Vector3.Dot(velocity, playerShip.forward);
            Vector3 lateralVelocity = velocity - playerShip.forward * forwardSpeed;
            float drift = lateralVelocity.magnitude;
            float heading = Mathf.Repeat(playerShip.eulerAngles.y, 360f);

            string line1 = $"/// SECTOR: <b>{_locationName}</b>";
            string line2 = $"VEL {speed:000.0} // DRIFT {drift:00.0} // HDG {heading:000}°";
            string line3;
            string line4;

            if (_hasNavigationFix)
            {
                float closingSpeed = Vector3.Dot(velocity, _navigationDirection);
                float bearing = Mathf.Atan2(_navigationLocalDirection.x, _navigationLocalDirection.z) * Mathf.Rad2Deg;
                float alignment = Mathf.Clamp01(Vector3.Dot(playerShip.forward, _navigationDirection));
                string eta = closingSpeed > 0.5f
                    ? FormatEta(_navigationDistance / closingSpeed)
                    : "--:--";

                line3 = $"RNG {FormatDistanceCompact(_navigationDistance)} // CLOS {FormatSigned(closingSpeed)} // ETA {eta}";
                line4 = $"BRG {FormatSignedAngle(bearing)} // ALIGN {alignment * 100f:00}% // {GetNavigationState(closingSpeed, alignment)}";
            }
            else
            {
                Vector3 positionK = playerShip.position / 1000f;
                int loadedSystems = GalaxyManager.Instance != null
                    ? GalaxyManager.Instance.GeneratedSystems.Count
                    : 0;

                line3 = $"POS X{FormatSignedCompact(positionK.x)} Y{FormatSignedCompact(positionK.y)} Z{FormatSignedCompact(positionK.z)}K";
                line4 = $"NAV SCAN // SYS {loadedSystems:00} // FIX NONE";
            }

            locationText.text = $"{line1}\n<size=76%>{line2}\n{line3}\n{line4}</size>";
        }

        private static string GetNavigationState(float closingSpeed, float alignment)
        {
            if (closingSpeed < -0.5f)
            {
                return "RECEDING";
            }

            if (alignment > 0.96f && closingSpeed > 0.5f)
            {
                return "NAV LOCK";
            }

            if (alignment < 0.15f)
            {
                return "OFF AXIS";
            }

            return "TRACK";
        }

        private static string FormatDistance(float distance)
        {
            return distance > 10000f
                ? $"{distance / 1000f:F1}k KM"
                : $"{Mathf.RoundToInt(distance)} KM";
        }

        private static string FormatDistanceCompact(float distance)
        {
            if (distance >= 1000f)
            {
                return $"{distance / 1000f:0.0}K";
            }

            return $"{distance:0}";
        }

        private static string FormatEta(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f || seconds > 5999f)
            {
                return "--:--";
            }

            int totalSeconds = Mathf.CeilToInt(seconds);
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }

        private static string FormatSigned(float value)
        {
            return value >= 0f
                ? $"+{value:0.0}"
                : $"{value:0.0}";
        }

        private static string FormatSignedCompact(float value)
        {
            return value >= 0f
                ? $"+{value:0.0}"
                : $"{value:0.0}";
        }

        private static string FormatSignedAngle(float value)
        {
            int rounded = Mathf.RoundToInt(value);
            return rounded >= 0
                ? $"+{rounded:000}°"
                : $"-{Mathf.Abs(rounded):000}°";
        }

        private void SetNavigationVisible(bool visible)
        {
            if (navMarker.gameObject.activeSelf != visible)
            {
                navMarker.gameObject.SetActive(visible);
            }
        }

        private void SetDistanceText(string value)
        {
            if (distanceText != null)
            {
                distanceText.text = value;
            }
        }
        private void SetupWarpLightTrails()
        {
            Canvas localCanvas = GetComponentInParent<Canvas>();
            if (localCanvas == null)
            {
                return;
            }

            Canvas rootCanvas = localCanvas.rootCanvas;
            WarpHudLightTrails trails = rootCanvas.GetComponent<WarpHudLightTrails>();
            if (trails == null)
            {
                trails = rootCanvas.gameObject.AddComponent<WarpHudLightTrails>();
            }

            trails.Configure(shipMotor);
        }

    }
}
