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
    [SerializeField] private SimpleLeverPress requiredLever;   // si lo asignas, el botón no funciona hasta activar esa palanca

    [Header("Logica del puzzle (opcional)")]
    [SerializeField] private ButtonPuzzleManager puzzleManager;

    [Header("Eventos")]
    [SerializeField] private UnityEvent onPressed;             // al presionarse (cambiar de escena, sonido, etc.)

    private Vector3 initialLocalPosition;
    private bool pressed;
    private bool handWasNear;

    private void Start()
    {
        if (buttonVisual != null)
            initialLocalPosition = buttonVisual.localPosition;
        else
            Debug.LogWarning($"[BOTÓN] '{name}' no tiene Button Visual asignado.");

        if (puzzleManager == null && onPressed.GetPersistentEventCount() == 0)
        {
            Debug.LogWarning(
                $"[BOTÓN] '{name}' no tiene ButtonPuzzleManager ni eventos en On Pressed: " +
                "al presionarlo no pasará nada."
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

        bool leftNear = IsHandNear(leftHand);
        bool rightNear = IsHandNear(rightHand);
        bool handIsNear = leftNear || rightNear;

        // ¿Está bloqueado por una palanca?
        bool bloqueado = requiredLever != null && !requiredLever.IsActivated;

        if (bloqueado)
        {
            if (handIsNear && click)
            {
                Debug.Log(
                    $"[BOTÓN] '{name}' bloqueado: primero hay que activar la palanca " +
                    $"'{requiredLever.name}'."
                );
            }

            handWasNear = handIsNear;
            return;
        }

        // Avisar cuando la mano entra en el rango del boton.
        if (handIsNear && !handWasNear)
            Debug.Log($"[BOTÓN] '{name}' listo (mano cerca).");

        handWasNear = handIsNear;

        // Solo presionar cuando la mano esta cerca y haces clic.
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

        Debug.Log($"[BOTÓN] '{name}' presionado.");

        if (puzzleManager != null)
        {
            Debug.Log(
                $"[BOTÓN] '{name}' → avisando al manager " +
                $"'{ButtonPuzzleManager.Ruta(puzzleManager.transform)}'."
            );

            puzzleManager.ButtonPressed();
        }

        // Eventos del Inspector (cambiar de escena, sonido, etc.)
        onPressed.Invoke();
    }
}