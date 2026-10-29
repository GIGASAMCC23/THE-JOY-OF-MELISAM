
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SimpleLeverPress : MonoBehaviour
{
    [Header("Manos virtuales")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Palanca")]
    [SerializeField] private Transform leverVisual;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float detectionDistance = 0.4f;
    [SerializeField] private float activationAngle = -50f;
    [SerializeField] private float rotationDuration = 0.3f;

    [Header("Logica del juego (opcional)")]
    [SerializeField] private ButtonPuzzleManager puzzleManager;
    [SerializeField] private GameWinManager gameWinManager;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activationSound;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [Header("Eventos")]
    [SerializeField] private UnityEvent onActivated;

    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = true;

    private Renderer[] leverRenderers;
    private Quaternion initialRotation;
    private bool activated;
    private bool completed;
    private bool handWasNear;

    public bool IsActivated => completed;

    private void Start()
    {
        if (leverVisual != null)
        {
            initialRotation = leverVisual.localRotation;
            leverRenderers = leverVisual.GetComponentsInChildren<Renderer>();

            Bounds b;

            if (ObtenerBounds(out b))
            {
                Debug.Log(
                    $"[PALANCA] '{leverVisual.name}' | centro: {b.center} | " +
                    $"tamaño: {b.size} | pivote: {leverVisual.position}"
                );

                float mayor = Mathf.Max(
                    b.size.x,
                    Mathf.Max(b.size.y, b.size.z)
                );

                if (mayor > 3f)
                {
                    Debug.LogWarning(
                        $"[PALANCA] La malla de '{leverVisual.name}' mide {mayor:F1} m. " +
                        "Asigna solo la manija o utiliza Interaction Point."
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    $"[PALANCA] '{leverVisual.name}' no tiene Renderers."
                );
            }

#if UNITY_EDITOR
            if (leverVisual.gameObject.isStatic)
            {
                Debug.LogWarning(
                    $"[PALANCA] '{leverVisual.name}' está marcado como Static."
                );
            }
#endif
        }
        else
        {
            Debug.LogWarning($"[PALANCA] '{name}' no tiene Lever Visual asignado.");
        }

        if (leftHand == null && rightHand == null)
            Debug.LogWarning("[PALANCA] No hay ninguna mano asignada.");

        if (puzzleManager == null && mostrarDebug)
            Debug.Log("[PALANCA] Sin ButtonPuzzleManager.");

        if (gameWinManager == null && onActivated.GetPersistentEventCount() == 0)
        {
            Debug.LogWarning(
                "[PALANCA] No tiene GameWinManager ni eventos en On Activated."
            );
        }

        if (Mouse.current == null)
            Debug.LogWarning("[PALANCA] No se detecta ningún mouse.");
    }

    private void Update()
    {
        if (activated || leverVisual == null)
            return;

        bool click =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        if (puzzleManager != null && !puzzleManager.IsOpen)
        {
            if (click && mostrarDebug)
                Debug.Log("[PALANCA] La ventana todavía no está abierta.");

            return;
        }

        bool handIsNear =
            IsHandNear(leftHand) || IsHandNear(rightHand);

        if (handIsNear && !handWasNear)
            Debug.Log("[PALANCA] Lista (mano cerca).");

        handWasNear = handIsNear;

        if (click && !handIsNear && mostrarDebug)
        {
            Debug.Log(
                $"[PALANCA] Ninguna mano está a la distancia requerida. " +
                $"Izquierda: {DistanciaALaPalanca(leftHand):F2} m | " +
                $"Derecha: {DistanciaALaPalanca(rightHand):F2} m."
            );
        }

        if (handIsNear && click)
        {
            activated = true;
            StartCoroutine(RotateLever());
        }
    }

    private bool ObtenerBounds(out Bounds bounds)
    {
        bounds = new Bounds(
            leverVisual != null ? leverVisual.position : Vector3.zero,
            Vector3.zero
        );

        if (leverRenderers == null || leverRenderers.Length == 0)
            return false;

        bool primero = true;

        foreach (Renderer r in leverRenderers)
        {
            if (r == null)
                continue;

            if (primero)
            {
                bounds = r.bounds;
                primero = false;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return !primero;
    }

    private float DistanciaALaPalanca(Transform hand)
    {
        if (hand == null)
            return 999f;

        if (interactionPoint != null)
            return Vector3.Distance(hand.position, interactionPoint.position);

        Bounds b;

        if (ObtenerBounds(out b))
            return Vector3.Distance(
                hand.position,
                b.ClosestPoint(hand.position)
            );

        return Vector3.Distance(hand.position, leverVisual.position);
    }

    private float DistanciaAlPivote(Transform hand)
    {
        if (hand == null)
            return 999f;

        return Vector3.Distance(hand.position, leverVisual.position);
    }

    private bool IsHandNear(Transform hand)
    {
        if (hand == null)
            return false;

        return DistanciaALaPalanca(hand) <= detectionDistance;
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

            leverVisual.localRotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        leverVisual.localRotation = targetRotation;

        completed = true;

        Debug.Log("[PALANCA] ¡Palanca activada!");

        if (audioSource != null && activationSound != null)
            audioSource.PlayOneShot(activationSound, volume);

        onActivated.Invoke();

        if (gameWinManager != null)
            gameWinManager.Win();
    }

    [ContextMenu("Probar palanca")]
    private void ProbarPalanca()
    {
        if (activated || leverVisual == null)
            return;

        activated = true;
        StartCoroutine(RotateLever());
    }

    [ContextMenu("Mostrar distancias ahora")]
    private void MostrarDistancias()
    {
        Debug.Log(
            $"[PALANCA] Distancia a la malla → izq: " +
            $"{DistanciaALaPalanca(leftHand):F2} | der: " +
            $"{DistanciaALaPalanca(rightHand):F2} | " +
            $"al pivote → izq: {DistanciaAlPivote(leftHand):F2} | " +
            $"der: {DistanciaAlPivote(rightHand):F2} | " +
            $"límite: {detectionDistance}"
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (leverVisual == null)
            return;

        Renderer[] rs = leverVisual.GetComponentsInChildren<Renderer>();

        if (rs.Length == 0)
            return;

        Bounds b = rs[0].bounds;

        for (int i = 1; i < rs.Length; i++)
            b.Encapsulate(rs[i].bounds);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(b.center, b.size);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            b.center,
            b.size + Vector3.one * detectionDistance * 2f
        );

        if (interactionPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(
                interactionPoint.position,
                detectionDistance
            );
        }
    }
}
