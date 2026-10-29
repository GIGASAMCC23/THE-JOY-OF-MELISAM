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
    [SerializeField] private Transform interactionPoint;      // opcional: punto exacto donde se "agarra"
    [SerializeField] private float detectionDistance = 0.4f;  // se mide hasta la malla de la palanca
    [SerializeField] private float activationAngle = -50f;
    [SerializeField] private float rotationDuration = 0.3f;

    [Header("Lógica del juego (opcional)")]
    [SerializeField] private ButtonPuzzleManager puzzleManager;   // si lo asignas, la palanca solo funciona con la ventana abierta
    [SerializeField] private GameWinManager gameWinManager;       // si lo asignas, lleva a la escena final

    [Header("Eventos")]
    [SerializeField] private UnityEvent onActivated;              // al terminar de girar (luz, sonido, etc.)

    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = true;

    private Renderer[] leverRenderers;
    private Quaternion initialRotation;
    private bool activated;
    private bool completed;
    private bool handWasNear;

    // Otros scripts (por ejemplo, el botón) consultan esto
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
                    $"[PALANCA] '{leverVisual.name}' | centro de la malla: {b.center} | " +
                    $"tamaño: {b.size} | pivote: {leverVisual.position} | " +
                    $"distancia pivote-centro: {Vector3.Distance(leverVisual.position, b.center):F2} m"
                );

                float mayor = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));

                if (mayor > 3f)
                {
                    Debug.LogWarning(
                        $"[PALANCA] La malla de '{leverVisual.name}' mide {mayor:F1} m. " +
                        "Si Lever Visual es un grupo grande, la detección será demasiado amplia: " +
                        "asigna solo la manija o usa Interaction Point."
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    $"[PALANCA] '{leverVisual.name}' no tiene Renderers: la distancia se medirá al pivote."
                );
            }

#if UNITY_EDITOR
            if (leverVisual.gameObject.isStatic)
            {
                Debug.LogWarning(
                    $"[PALANCA] '{leverVisual.name}' está marcado como Static: aunque el código " +
                    "la gire, el dibujo NO se moverá. Desmarca Static en ese objeto."
                );
            }
#endif
        }
        else
        {
            Debug.LogWarning($"[PALANCA] '{name}' no tiene Lever Visual asignado.");
        }

        if (leftHand == null && rightHand == null)
            Debug.LogWarning("[PALANCA] No hay ninguna mano asignada (Left Hand / Right Hand).");

        if (puzzleManager == null && mostrarDebug)
            Debug.Log("[PALANCA] Sin ButtonPuzzleManager: no depende de ninguna ventana.");

        if (gameWinManager == null && onActivated.GetPersistentEventCount() == 0)
        {
            Debug.LogWarning(
                "[PALANCA] No tiene GameWinManager ni eventos en On Activated: " +
                "al activarla no pasará nada."
            );
        }

        if (Mouse.current == null)
            Debug.LogWarning("[PALANCA] No se detecta ningún mouse (Mouse.current es null).");
    }

    private void Update()
    {
        if (activated || leverVisual == null)
            return;

        bool click =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        // Bloqueada hasta que la ventana termine de abrirse (solo si hay puzzle)
        if (puzzleManager != null && !puzzleManager.IsOpen)
        {
            if (click && mostrarDebug)
            {
                Debug.Log(
                    "[PALANCA] Click ignorado: la ventana todavía no está abierta (IsOpen = false)."
                );
            }

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
                $"[PALANCA] Click ignorado: ninguna mano está a ≤ {detectionDistance} m de la palanca. " +
                $"Distancia a la malla → izq: {DistanciaALaPalanca(leftHand):F2} | der: {DistanciaALaPalanca(rightHand):F2}  " +
                $"(al pivote → izq: {DistanciaAlPivote(leftHand):F2} | der: {DistanciaAlPivote(rightHand):F2}). " +
                "Un valor de 999 significa mano sin asignar."
            );
        }

        if (handIsNear && click)
        {
            activated = true;
            StartCoroutine(RotateLever());
        }
    }

    // =========================================================
    // DISTANCIAS
    // =========================================================

    private bool ObtenerBounds(out Bounds bounds)
    {
        bounds = new Bounds(leverVisual != null ? leverVisual.position : Vector3.zero, Vector3.zero);

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

    // Distancia de la mano hasta la palanca (hasta su malla, no su pivote)
    private float DistanciaALaPalanca(Transform hand)
    {
        if (hand == null)
            return 999f;

        if (interactionPoint != null)
            return Vector3.Distance(hand.position, interactionPoint.position);

        Bounds b;

        if (ObtenerBounds(out b))
            return Vector3.Distance(hand.position, b.ClosestPoint(hand.position));

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

    // =========================================================
    // GIRO DE LA PALANCA
    // =========================================================

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

        completed = true;

        Debug.Log("[PALANCA] ¡Palanca activada!");

        // Eventos del Inspector (luz, sonido, etc.)
        onActivated.Invoke();

        // Victoria / escena final (solo si está asignado)
        if (gameWinManager != null)
            gameWinManager.Win();
    }

    // =========================================================
    // PRUEBAS (clic derecho en el nombre del componente, en play)
    // =========================================================

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
            $"[PALANCA] Distancia a la malla → izq: {DistanciaALaPalanca(leftHand):F2} | " +
            $"der: {DistanciaALaPalanca(rightHand):F2}  " +
            $"(al pivote → izq: {DistanciaAlPivote(leftHand):F2} | der: {DistanciaAlPivote(rightHand):F2})  " +
            $"| límite: {detectionDistance}"
        );
    }

    // =========================================================
    // GIZMOS: ver la zona de detección en la ventana Scene
    // =========================================================

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

        // Verde: la malla de la palanca
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(b.center, b.size);

        // Amarillo: zona donde la mano cuenta como "cerca"
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(b.center, b.size + Vector3.one * detectionDistance * 2f);

        if (interactionPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(interactionPoint.position, detectionDistance);
        }
    }
}