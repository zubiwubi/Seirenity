using UnityEngine;

/// <summary>
/// Simple bridge that lets a UI button open the colour‑picker UI.
/// Attach this component to any GameObject that is always present
/// (e.g., a UI manager, the Canvas, or a dedicated empty object).
/// </summary>
public class SettingsMenuOpener : MonoBehaviour
{
    // Drag the GameObject that has the ColourPickerController component onto this slot.
    [SerializeField] private ColourPickerController picker;

    /// <summary>
    /// Called by a UI Button's OnClick event.
    /// It simply tells the picker to become visible.
    /// </summary>
    public void OpenColourPicker()
    {
        // Safety check – helps you spot a missing reference early.
        if (picker == null)
        {
            Debug.LogError("[SettingsMenuOpener] No ColourPickerController assigned!");
            return;
        }

        picker.ShowPicker();   // the method defined in ColourPickerController
    }
}