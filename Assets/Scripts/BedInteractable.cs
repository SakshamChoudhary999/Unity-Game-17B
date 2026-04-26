using UnityEngine;

// ─────────────────────────────────────────────
//  BedInteractable — Apartment 17B
//  Attach to your Bed GameObject.
//  TimeManager enables it at 11:30 PM.
//  Player presses [E] → sleep sequence begins.
// ─────────────────────────────────────────────

public class BedInteractable : MonoBehaviour, IInteractable
{
    // canInteract avoids shadowing Unity's built-in `enabled` property
    private bool canInteract = false;

    // Called by TimeManager at 11:30 PM
    public void Enable()
    {
        canInteract = true;
    }

    public void Interact()
    {
        if (!canInteract) return;

        // Disable immediately — prevents E-spam firing TriggerSleep twice
        canInteract = false;
        TimeManager.Instance?.TriggerSleep();
    }

    public string GetPrompt()
    {
        return canInteract ? "[E] Sleep" : "";
    }
}