
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleLeverPress : MonoBehaviour
{
    [Header("Manos virtuales")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Palanca")]
    [SerializeField] private Transform leverVisual;
    [SerializeField] private float detectionDistance = 0.25f;
    [SerializeField] private float activationAngle = -50f;
    [SerializeField] private float rotationDuration = 0.3f;

    private Quaternion initialRotation;
    private bool activated;
    private bool handWasNear;

    private void Start()
    {
        if (leverVisual != null)
            initialRotation = leverVisual.localRotation;
    }

    private void Update()
    {
        if (activated || leverVisual == null)
            return;

        bool handIsNear =
            IsHandNear(leftHand) || IsHandNear(rightHand);

        if (handIsNear && !handWasNear)
            Debug.Log("listo");

        handWasNear = handIsNear;

        if (handIsNear &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            activated = true;
            StartCoroutine(RotateLever());
        }
    }

    private bool IsHandNear(Transform hand)
    {
        if (hand == null)
            return false;

        return Vector3.Distance(
            hand.position,
            leverVisual.position
        ) <= detectionDistance;
    }

    private IEnumerator RotateLever()
    {
        Quaternion startRotation = leverVisual.localRotation;

        Vector3 targetEuler = initialRotation.eulerAngles;
        targetEuler.x = activationAngle;

        Quaternion targetRotation = Quaternion.Euler(targetEuler);

        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / rotationDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            leverVisual.localRotation =
                Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        leverVisual.localRotation = targetRotation;

        Debug.Log("¡Palanca activada!");
    }
}
