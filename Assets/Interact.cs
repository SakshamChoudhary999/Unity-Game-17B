using UnityEngine;
using TMPro;

public class Interact : MonoBehaviour
{
    public float range = 3f;
    public TextMeshProUGUI promptText;

    private IInteractable currentInteractable;

    void Update()
    {
        DetectInteractable();

        if (currentInteractable != null)
        {
            promptText.text = currentInteractable.GetPrompt();

            if (Input.GetKeyDown(KeyCode.E))
            {
                currentInteractable.Interact();
            }
        }
        else
        {
            promptText.text = "";
        }
    }

    void DetectInteractable()
    {
        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            // 🔥 KEY FIX → works with child colliders
            currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
        }
        else
        {
            currentInteractable = null;
        }
    }
}