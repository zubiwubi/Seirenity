
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles the colour‑wheel UI, reads the pixel under the cursor,
/// and forwards the colour to the player for a live preview.
/// Attach this component to the SAME GameObject that has the RawImage
/// (the colour‑wheel UI element).
/// </summary>
public class ColourPickerController : MonoBehaviour,
                                      IPointerDownHandler,
                                      IDragHandler
{
    // ---------- UI references (drag these in the Inspector) ----------
    [Header("UI References")]
    [Tooltip("RawImage that displays the colour wheel")]
    [SerializeField] private RawImage colourWheelRawImage;   // drag the RawImage component here

    [Tooltip("Root object that contains the wheel + confirm button")]
    [SerializeField] private GameObject pickerRoot;          // parent that holds wheel & button

    [Tooltip("Button that locks the colour")]
    [SerializeField] private Button confirmButton;           // confirm button component

    // ---------- Target ----------
    [Header("Target")]
    [Tooltip("Component on the player that receives colour updates")]
    [SerializeField] private PlayerColourApplier playerApplier; // player object

    // ---------- Internal ----------
    private Texture2D wheelTexture;

    private void Awake()
    {
        // ----- Guard against missing references (errors appear in the Console) -----
        if (colourWheelRawImage == null)
        {
            Debug.LogError("[ColourPicker] colourWheelRawImage not assigned.");
            return;
        }
        if (pickerRoot == null)
        {
            Debug.LogError("[ColourPicker] pickerRoot not assigned.");
            return;
        }
        if (confirmButton == null)
        {
            Debug.LogError("[ColourPicker] confirmButton not assigned.");
            return;
        }
        if (playerApplier == null)
        {
            Debug.LogError("[ColourPicker] playerApplier not assigned.");
            return;
        }

        // ----- Get the readable texture from the RawImage -----
        wheelTexture = colourWheelRawImage.texture as Texture2D;
        if (wheelTexture == null)
        {
            Debug.LogError("[ColourPicker] Wheel texture is not a readable Texture2D. " +
                           "Enable 'Read/Write Enabled' on the PNG import settings.");
        }

        // ----- Hook up the Confirm button -----
        confirmButton.onClick.AddListener(() =>
        {
            playerApplier.ApplyColour();   // lock colour / optional save
            HidePicker();
        });
    }

    // -----------------------------------------------------------------
    // Show / hide the whole picker UI
    // -----------------------------------------------------------------
    public void ShowPicker()
    {
        if (pickerRoot) pickerRoot.SetActive(true);
    }

    public void HidePicker()
    {
        if (pickerRoot) pickerRoot.SetActive(false);
    }

    // -----------------------------------------------------------------
    // UI pointer callbacks – fire when you click/drag on the wheel
    // -----------------------------------------------------------------
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[Picker] OnPointerDown fired");
        UpdateColourFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("[Picker] OnDrag fired");
        UpdateColourFromPointer(eventData);
    }

    // -----------------------------------------------------------------
    // Core colour‑sampling logic
    // -----------------------------------------------------------------
    private void UpdateColourFromPointer(PointerEventData ev)
    {
        if (wheelTexture == null) return; // safety

        // Convert screen point → local point inside the RawImage rect
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            colourWheelRawImage.rectTransform,
            ev.position,
            ev.pressEventCamera,
            out Vector2 localPoint);

        // Normalise to UV space (0‑1)
        Rect r = colourWheelRawImage.rectTransform.rect;
        float u = (localPoint.x + r.width * 0.5f) / r.width;
        float v = (localPoint.y + r.height * 0.5f) / r.height;

        // Clamp in case the pointer goes slightly outside the wheel
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        int texX = Mathf.FloorToInt(u * wheelTexture.width);
        int texY = Mathf.FloorToInt(v * wheelTexture.height);
        Color sampled = wheelTexture.GetPixel(texX, texY);

        // Ignore transparent border (outside the circular wheel)
        if (sampled.a < 0.1f) return;

        // Forward colour to the player for live preview
        playerApplier?.SetPreviewColour(sampled);
    }
}