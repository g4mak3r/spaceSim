using UnityEngine;

namespace SpaceSim.Player
{
    public sealed class ShipCameraEffect : MonoBehaviour
    {
        [SerializeField] private ShipMotor shipMotor;
        [SerializeField] private Camera cam;

        [Header("FOV Settings")]
        [SerializeField, Range(1f, 179f)] private float normalFOV = 60f;
        [SerializeField, Range(1f, 179f)] private float warpFOV = 110f;
        [SerializeField, Min(0f)] private float lerpSpeed = 2f;

        private void Awake()
        {
            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }
        }

        private void Update()
        {
            if (shipMotor == null || cam == null)
            {
                return;
            }

            float targetFOV = shipMotor.IsWarping ? warpFOV : normalFOV;
            float blend = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, blend);
        }
    }
}
