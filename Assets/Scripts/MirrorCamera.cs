using UnityEngine;
using System.Collections;

// ─────────────────────────────────────────────
//  BasicMirrorCamera — Apartment 17B (UPGRADED)
//
//  Phase 1 (Before Horror):
//    Mirror reflects normally — instant sync.
//
//  Phase 2 (After 3:17 AM):
//    Reflection is delayed by horrorDelayFrames.
//    Also applies subtle random head tilts so the
//    reflection occasionally tilts its head before
//    the player moves — reads as deeply uncanny.
//
//  Attach to the Mirror Camera child GameObject.
//  Assign playerCamera and mirror in Inspector.
// ─────────────────────────────────────────────

public class BasicMirrorCamera : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    public Transform mirror;

    [Header("Horror Delay")]
    [Tooltip("Frames of delay applied to mirror reflection during horror phase")]
    public int horrorDelayFrames = 25;       // ~0.4 sec at 60 fps

    [Tooltip("Max angle (degrees) the reflection tilts before player does")]
    public float horrorTiltDegrees = 4f;

    [Tooltip("Speed of tilt interpolation")]
    public float tiltSpeed = 80f;             // degrees per second

    // ── Ring buffer ───────────────────────────
    private Vector3[]    _posBuffer;
    private Quaternion[] _rotBuffer;
    private int          _head = 0;

    // ── State ─────────────────────────────────
    private bool  _horrorActive  = false;
    private float _currentTilt   = 0f;
    private float _targetTilt    = 0f;

    // ── Unity ─────────────────────────────────

    void Start()
    {
        int bufSize = Mathf.Max(horrorDelayFrames + 4, 8);
        _posBuffer  = new Vector3[bufSize];
        _rotBuffer  = new Quaternion[bufSize];

        // Pre-fill buffer so first frames aren't garbage
        Vector3    initPos = playerCamera != null ? playerCamera.position : Vector3.zero;
        Quaternion initRot = playerCamera != null ? playerCamera.rotation : Quaternion.identity;
        for (int i = 0; i < bufSize; i++)
        {
            _posBuffer[i] = initPos;
            _rotBuffer[i] = initRot;
        }

        // Wire to TimeManager
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnHorrorBegins += ActivateHorrorMode;
        else
            Debug.LogWarning("[MirrorCamera] TimeManager not found — horror delay inactive.");
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnHorrorBegins -= ActivateHorrorMode;
    }

    void ActivateHorrorMode()
    {
        _horrorActive = true;
        StartCoroutine(RandomTiltScheduler());
        Debug.Log("[MirrorCamera] Horror delay active.");
    }

    // Periodically trigger a subtle pre-emptive head tilt
    IEnumerator RandomTiltScheduler()
    {
        while (_horrorActive)
        {
            yield return new WaitForSeconds(Random.Range(10f, 22f));
            _targetTilt = (Random.value > 0.5f ? 1f : -1f) * horrorTiltDegrees;
            yield return new WaitForSeconds(Random.Range(1.2f, 2.5f));
            _targetTilt = 0f;
        }
    }

    void LateUpdate()
    {
        if (!playerCamera || !mirror) return;

        // ── Write current camera state into ring buffer ──
        _posBuffer[_head] = playerCamera.position;
        _rotBuffer[_head] = playerCamera.rotation;
        _head = (_head + 1) % _posBuffer.Length;

        // ── Choose which frame to read ──
        Vector3    readPos;
        Quaternion readRot;

        if (_horrorActive)
        {
            // Read `horrorDelayFrames` behind the write head
            int readIdx = (_head - horrorDelayFrames + _posBuffer.Length) % _posBuffer.Length;
            readPos = _posBuffer[readIdx];
            readRot = _rotBuffer[readIdx];
        }
        else
        {
            // Read most recent (no delay)
            int readIdx = (_head - 1 + _posBuffer.Length) % _posBuffer.Length;
            readPos = _posBuffer[readIdx];
            readRot = _rotBuffer[readIdx];
        }

        // ── Apply smooth tilt to the reflection ──
        _currentTilt = Mathf.MoveTowards(_currentTilt, _targetTilt, tiltSpeed * Time.deltaTime);
        Quaternion tiltQ = Quaternion.AngleAxis(_currentTilt, mirror.forward) * readRot;

        // ── Mirror position across mirror plane ──
        Vector3 localPos = mirror.InverseTransformPoint(readPos);
        localPos.z *= -1f;
        transform.position = mirror.TransformPoint(localPos);

        // ── Mirror rotation ──
        Vector3 localFwd = mirror.InverseTransformDirection(tiltQ * Vector3.forward);
        Vector3 localUp  = mirror.InverseTransformDirection(tiltQ * Vector3.up);
        localFwd.z *= -1f;
        localUp.z  *= -1f;

        transform.rotation = Quaternion.LookRotation(
            mirror.TransformDirection(localFwd),
            mirror.TransformDirection(localUp)
        );
    }
}