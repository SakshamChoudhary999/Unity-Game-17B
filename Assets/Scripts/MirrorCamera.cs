using UnityEngine;

public class BasicMirrorCamera : MonoBehaviour
{
    public Transform playerCamera;
    public Transform mirror;

    void LateUpdate()
    {
        if (!playerCamera || !mirror) return;

        // Mirror position
        Vector3 localPos = mirror.InverseTransformPoint(playerCamera.position);
        localPos.z *= -1f;
        transform.position = mirror.TransformPoint(localPos);

        // Mirror rotation
        Vector3 localForward = mirror.InverseTransformDirection(playerCamera.forward);
        Vector3 localUp = mirror.InverseTransformDirection(playerCamera.up);

        localForward.z *= -1f;
        localUp.z *= -1f;

        transform.rotation = Quaternion.LookRotation(
            mirror.TransformDirection(localForward),
            mirror.TransformDirection(localUp)
        );
    }
}