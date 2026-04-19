using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour, IInteractable
{
    private bool isOpen = false;
    private bool isMoving = false;

    [Header("Door Settings")]
    public float openAngle = 90f;
    public float speed = 2f;

    [Header("Linked Door")]
    public Door linkedDoor;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
    }

    public void Interact()
    {
        if (isMoving) return;

        ToggleDoor();

        if (linkedDoor != null && linkedDoor.isOpen != this.isOpen)
        {
            linkedDoor.ToggleDoor();
        }
    }

    public string GetPrompt()
    {
        return isOpen ? "[E] Close" : "[E] Open";
    }

    public void ToggleDoor()
    {
        StopAllCoroutines();
        StartCoroutine(RotateDoor(isOpen ? closedRotation : openRotation));
        isOpen = !isOpen;
    }

    IEnumerator RotateDoor(Quaternion targetRotation)
    {
        isMoving = true;

        while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * speed);

            yield return null;
        }

        transform.localRotation = targetRotation;
        isMoving = false;
    }
}