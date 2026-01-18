using UnityEngine;
using System.Collections;

public class CameraZoomController : MonoBehaviour
{
    [Header("Zoom Targets")]
    public Transform startPosition; // Far away
    public Transform endPosition;   // Player Base view

    [Header("Settings")]
    public float zoomDuration = 2.0f;
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isZooming = false;

    public void StartZoom(System.Action onComplete = null)
    {
        if (isZooming) return;
        StartCoroutine(ZoomRoutine(onComplete));
    }

    private IEnumerator ZoomRoutine(System.Action onComplete)
    {
        isZooming = true;
        float elapsed = 0f;
        
        Vector3 initialPos = transform.position;
        Quaternion initialRot = transform.rotation;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = zoomCurve.Evaluate(elapsed / zoomDuration);

            transform.position = Vector3.Lerp(initialPos, endPosition.position, t);
            transform.rotation = Quaternion.Lerp(initialRot, endPosition.rotation, t);

            yield return null;
        }

        transform.position = endPosition.position;
        transform.rotation = endPosition.rotation;
        
        isZooming = false;
        onComplete?.Invoke();
    }
}
