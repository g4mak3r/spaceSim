using System.Collections;
using TMPro;
using UnityEngine;

namespace SpaceSim.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class HudTerminalEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI textMesh;

        [Header("Settings")]
        [SerializeField] private string[] statusMessages =
        {
            "///CONNECTING",
            "///SYNCING SENSORS",
            "///SCANNING AREA",
            "///SIGNAL LOST",
            "///RE-ESTABLISHING"
        };

        [SerializeField] private string[] glitchChars = { "#", "%", "&", "!", "?", "0", "1" };

        private readonly WaitForSeconds _glitchDelay = new WaitForSeconds(0.1f);
        private readonly WaitForSeconds _typingDelay = new WaitForSeconds(0.4f);
        private readonly WaitForSeconds _messageDelay = new WaitForSeconds(1.5f);

        private void Awake()
        {
            if (textMesh == null)
            {
                textMesh = GetComponent<TextMeshProUGUI>();
            }
        }

        private void OnEnable()
        {
            if (textMesh != null && statusMessages.Length > 0)
            {
                StartCoroutine(TerminalLoop());
            }
        }

        private IEnumerator TerminalLoop()
        {
            int index = 0;

            while (enabled)
            {
                string baseMessage = statusMessages[index];

                if (baseMessage.Contains("LOST"))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        textMesh.text = $"///SIG{GetGlitch()}AL L{GetGlitch()}ST";
                        yield return _glitchDelay;
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        textMesh.text = baseMessage + new string('.', i);
                        yield return _typingDelay;
                    }
                }

                index = (index + 1) % statusMessages.Length;
                yield return _messageDelay;
            }
        }

        private string GetGlitch()
        {
            return glitchChars.Length == 0
                ? "#"
                : glitchChars[Random.Range(0, glitchChars.Length)];
        }
    }
}
