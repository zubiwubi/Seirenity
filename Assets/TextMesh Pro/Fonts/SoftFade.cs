using UnityEngine;
using UnityEngine.UI;          // for regular UI Text
using TMPro;                  // for TextMeshProUGUI (optional)

[RequireComponent(typeof(CanvasGroup))]
public class SoftFade : MonoBehaviour
{
    // How long a full fade‑in or fade‑out takes (seconds)
    [SerializeField] private float fadeDuration = 1.5f;

    // Optional pause at fully visible / fully invisible
    [SerializeField] private float holdTime = 0.5f;

    // Whether the animation should loop forever
    [SerializeField] private bool loop = true;

    private CanvasGroup cg;
    private float timer;
    private bool fadingIn = true;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        cg.alpha = 0f;               // start invisible
    }

    private void Update()
    {
        // Advance timer
        timer += Time.unscaledDeltaTime; // use unscaled time so UI isn’t affected by game pause

        // Handle the hold periods
        if (timer < holdTime)
            return;                 // stay at current alpha during hold

        // Normalized progress of the current fade (0 → 1)
        float t = (timer - holdTime) / fadeDuration;
        t = Mathf.Clamp01(t);

        // Apply a smooth easing (soft start/end)
        float eased = Mathf.SmoothStep(0f, 1f, t);

        // Set alpha based on direction
        cg.alpha = fadingIn ? eased : 1f - eased;

        // When the fade finishes, switch direction
        if (t >= 1f)
        {
            fadingIn = !fadingIn;   // reverse
            timer = 0f;             // reset timer for next phase
        }
    }
}