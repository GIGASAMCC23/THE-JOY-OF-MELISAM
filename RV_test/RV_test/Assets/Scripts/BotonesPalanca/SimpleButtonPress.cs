
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SimpleButtonPress : MonoBehaviour
{
    [Header("Manos virtuales")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Boton")]
    [SerializeField] private Transform buttonVisual;
    [SerializeField] private float detectionDistance = 0.25f;
    [SerializeField] private float pressDepth = 0.026f;

    [Header("Bloqueo (opcional)")]
    [SerializeField] private SimpleLeverPress requiredLever;

    [Header("Logica del puzzle (opcional)")]
    [SerializeField] private ButtonPuzzleManager puzzleManager;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pressSound;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [Header("Eventos")]
    [SerializeField] private UnityEvent onPressed;

    private Vector3 initialLocalPosition;
    private bool pressed;
    private bool handWasNear;

    private void Start()
    {
        if (buttonVisual != null)
            initialLocalPosition = buttonVisual.localPosition;
        else
            Debug.LogWarning($"[BOTON] '{name}' no tiene Button Visual asignado.");

        if (puzzleManager == null && onPressed.GetPersistentEventCount() == 0)
        {
            Debug.LogWarning(
                $"[BOTON] '{name}' no tiene ButtonPuzzleManager ni eventos en On Pressed."
            );
        }
    }

    private void Update()
    {
        if (pressed || buttonVisual == null)
            return;

        bool click =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        bool handIsNear =
            IsHandNear(leftHand) || IsHandNear(rightHand);

        bool bloqueado =
            requiredLever != null && !requiredLever.IsActivated;

        if (bloqueado)
        {
            if (handIsNear && click)
                Debug.Log($"[BOTON] '{name}' bloqueado: activa primero la palanca.");

            handWasNear = handIsNear;
            return;
        }

        if (handIsNear && !handWasNear)
            Debug.Log($"[BOTON] '{name}' listo (mano cerca).");

        handWasNear = handIsNear;

        if (handIsNear && click)
            PressButton();
    }

    private bool IsHandNear(Transform hand)
    {
        if (hand == null)
            return false;

        return Vector3.Distance(
            hand.position,
            buttonVisual.position
        ) <= detectionDistance;
    }

    private void PressButton()
    {
        pressed = true;

        Vector3 pressedPosition = initialLocalPosition;
        pressedPosition.y -= pressDepth;
        buttonVisual.localPosition = pressedPosition;

        Debug.Log($"[BOTON] '{name}' presionado.");

        if (audioSource != null && pressSound != null)
            audioSource.PlayOneShot(pressSound, volume);

        if (puzzleManager != null)
            puzzleManager.ButtonPressed();

        onPressed.Invoke();
    }
}
