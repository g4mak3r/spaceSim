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

        private float _currentMarkerX;
        private Image _navMarkerImage;

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
        }

        private void UpdateSpeedDisplay()
        {
            if (shipMotor == null || speedText == null)
            {
                return;
            }

            speedText.text = $"{Mathf.RoundToInt(shipMotor.CurrentSpeed)} UNIT/S";
        }

        public void UpdateLocationName(string locationName)
        {
            if (locationText == null)
            {
                return;
            }

            string displayName = string.IsNullOrWhiteSpace(locationName)
                ? "UNKNOWN"
                : locationName.ToUpperInvariant();

            locationText.text = $"/// SECTOR: {displayName}";
        }

        private void UpdateNavigationBar()
        {
            if (playerShip == null || navMarker == null || GalaxyManager.Instance == null)
            {
                return;
            }

            StarSystem nearestSystem = GalaxyManager.Instance.GetNearestSystem(playerShip.position);
            if (nearestSystem == null)
            {
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
                return;
            }

            Vector3 localDirection = playerShip.InverseTransformDirection(toTarget / distance);
            float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float normalizedAngle = Mathf.Clamp(angle / 90f, -1f, 1f);
            float targetX = normalizedAngle * maxOffset;

            float blend = 1f - Mathf.Exp(-smoothTime * Time.deltaTime);
            _currentMarkerX = Mathf.Lerp(_currentMarkerX, targetX, blend);
            navMarker.anchoredPosition = new Vector2(_currentMarkerX, navMarker.anchoredPosition.y);

            bool isBehind = localDirection.z < -0.2f;
            SetNavigationVisible(!isBehind);

            if (_navMarkerImage != null)
            {
                _navMarkerImage.color = Mathf.Abs(normalizedAngle) < 0.1f
                    ? Color.cyan
                    : new Color(0.7f, 0.8f, 1f, 0.6f);
            }

            SetDistanceText(distance > 10000f
                ? $"{distance / 1000f:F1}k KM"
                : $"{Mathf.RoundToInt(distance)} KM");
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
    }
}
