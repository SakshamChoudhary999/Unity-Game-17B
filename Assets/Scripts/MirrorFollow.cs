using UnityEngine;

public class MirrorFollow : MonoBehaviour
{
    public Transform playerCamera;
    public Transform mirror;

    [Header("Horror Settings")]
    public float positionDelay = 0.08f;
    public float rotationDelay = 0.08f;

    private Vector3 velocity;
    private Quaternion currentRotation;

    void Start()
    {
        currentRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (playerCamera == null || mirror == null) return;

        // Direction from mirror to player
        Vector3 dir = playerCamera.position - mirror.position;

        float dotNormal = Vector3.Dot(dir, mirror.forward);
        Vector3 mirroredDir = dir - 2f * dotNormal * mirror.forward;
        Vector3 targetPosition = mirror.position + mirroredDir;

        // Smooth position (delay)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            positionDelay
        );

        // Reflect rotation
        Vector3 mirroredForward = Vector3.Reflect(playerCamera.forward, mirror.forward);
        Vector3 mirroredUp = Vector3.Reflect(playerCamera.up, mirror.forward);

        Quaternion targetRotation = Quaternion.LookRotation(mirroredForward, mirroredUp);

        // Smooth rotation (delay)
        currentRotation = Quaternion.Slerp(
            currentRotation,
            targetRotation,
            Time.deltaTime / rotationDelay
        );

        transform.rotation = currentRotation;
    }
}