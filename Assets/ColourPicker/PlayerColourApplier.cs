
using UnityEngine;
public class TestColourNow : MonoBehaviour
{
    void Start()
    {
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.material.SetColor("_BaseColor", Color.cyan);
        else Debug.LogError("No Renderer on this object!");
    }
}

/// <summary>
/// Attach this to the object you want to recolour (must have a Renderer).
/// Works with URP/Lit shaders (uses the "_BaseColor" property).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class PlayerColourApplier : MonoBehaviour
{
    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError("[PlayerColourApplier] No Renderer found on this GameObject!");
            return;
        }

        // OPTIONAL: load a saved colour on start
        if (PlayerPrefs.HasKey("PlayerColour"))
        {
            string savedHex = PlayerPrefs.GetString("PlayerColour");
            if (ColorUtility.TryParseHtmlString(savedHex, out Color savedCol))
                SetPreviewColour(savedCol);
        }
    }

    /// <summary>
    /// Called every frame while the user drags on the colour wheel.
    /// </summary>
    public void SetPreviewColour(Color c)
    {
        if (_renderer == null) return;                 // safety
        _renderer.material.SetColor("_BaseColor", c);   // URP Lit uses _BaseColor
    }

    /// <summary>
    /// Called when the user presses the Confirm button.
    /// Saves the colour and logs it.
    /// </summary>
    public void ApplyColour()
    {
        if (_renderer == null) return;                 // safety

        // Grab the colour that is currently on the material
        Color finalCol = _renderer.material.GetColor("_BaseColor");

        // Save it so it survives a later Play session (optional)
        string hex = $"#{ColorUtility.ToHtmlStringRGBA(finalCol)}";
        PlayerPrefs.SetString("PlayerColour", hex);
        PlayerPrefs.Save();

        Debug.Log($"[PlayerColourApplier] Colour locked: {hex}");
    }
}