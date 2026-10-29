using UnityEngine;
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

    [Header("Logica del puzzle")]
    [SerializeField] private ButtonPuzzleManager puzzleManager;

    private Vector3 initialLocalPosition;
    private bool pressed;
    private bool handWasNear;

    private void Start()
    {
        if (buttonVisual != null)
            initialLocalPosition = buttonVisual.localPosition;
        else
            Debug.LogWarning($"[BOTÓN] '{name}' no tiene Button Visual asignado.");

        if (puzzleManager == null)
        {
            Debug.LogWarning(
                $"[BOTÓN] '{name}' NO tiene ButtonPuzzleManager asignado: " +
                "al presionarlo no avisará a la ventana."
            );
        }
    }

    private void Update()
    {
        if (pressed || buttonVisual == null)
            return;

        bool leftNear = IsHandNear(leftHand);
        bool rightNear = IsHandNear(rightHand);
        bool handIsNear = leftNear || rightNear;

        // Avisar cuando la mano entra en el rango del boton.
        if (handIsNear && !handWasNear)
            Debug.Log($"[BOTÓN] '{name}' listo (mano cerca).");

        handWasNear = handIsNear;

        // Solo presionar cuando la mano esta cerca y haces clic.
        if (handIsNear &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            PressButton();
        }
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
        else
        {
            Debug.LogError(
                $"[BOTÓN] '{name}' no pudo avisar: falta arrastrar el ButtonPuzzleManager " +
                "al campo Puzzle Manager."
            );
        }
    }
}