using System.Collections;
using UnityEngine;

public class ButtonPuzzleManager : MonoBehaviour
{
    public enum OpenMode
    {
        Deactivate,   // la ventana desaparece (se desactiva)
        Slide         // la ventana se desliza en X
    }

    [Header("Ventana")]
    [SerializeField] private OpenMode openMode = OpenMode.Deactivate;
    [SerializeField] private Transform window;
    [SerializeField] private GameObject[] extraObjectsToHide;   // otras piezas (vidrio, marco, otra hoja...)
    [SerializeField] private float hideDelay = 0f;              // segundos antes de que desaparezca

    [Header("Solo modo Slide")]
    [SerializeField] private float closedX = -0.393f;
    [SerializeField] private float openX = 0.403f;
    [SerializeField] private float openDuration = 2f;

    [Header("Botones")]
    [SerializeField] private int totalButtons = 3;

    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = true;

    private const float DuracionMaximaRazonable = 30f;
    private const float DuracionPorDefecto = 2f;

    private int pressedCount;
    private bool opening;
    private bool opened;

    // La palanca consulta esto para saber si ya puede usarse
    public bool IsOpen => opened;

    // Ruta completa del objeto en la jerarquía (para los logs)
    public static string Ruta(Transform t)
    {
        if (t == null)
            return "(null)";

        string ruta = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            ruta = t.name + "/" + ruta;
        }

        return ruta;
    }

    // Si el valor del Inspector es absurdo, avisa y usa uno razonable
    private float ObtenerDuracionSegura()
    {
        if (openDuration < 0.01f || openDuration > DuracionMaximaRazonable)
        {
            Debug.LogWarning(
                $"[PUZZLE] Open Duration = {openDuration} no es un valor razonable " +
                $"(esperado entre 0.01 y {DuracionMaximaRazonable} s). " +
                $"Se usarán {DuracionPorDefecto} s. Corrígelo en el Inspector."
            );

            return DuracionPorDefecto;
        }

        return openDuration;
    }

    private void Awake()
    {
        if (window == null)
        {
            Debug.LogError("[PUZZLE] No hay una ventana asignada en ButtonPuzzleManager.");
            return;
        }

        if (transform.IsChildOf(window))
        {
            Debug.LogWarning(
                "[PUZZLE] Este manager es hijo de la ventana. Al desactivarla también se " +
                "desactivaría el manager. Muévelo a un objeto aparte."
            );
        }

        if (!mostrarDebug)
            return;

        Debug.Log(
            $"[PUZZLE] Manager '{Ruta(transform)}' | modo: {openMode} | " +
            $"ventana: '{window.name}' | piezas extra a ocultar: " +
            $"{(extraObjectsToHide != null ? extraObjectsToHide.Length : 0)} | " +
            $"botones necesarios={totalButtons}"
        );

        if (openMode == OpenMode.Slide)
        {
            Debug.Log(
                $"[PUZZLE] Slide: localPosition inicial {window.localPosition} | " +
                $"closedX={closedX} openX={openX} | duración={openDuration}s"
            );

#if UNITY_EDITOR
            if (window.gameObject.isStatic)
            {
                Debug.LogWarning(
                    $"[PUZZLE] '{window.name}' está marcado como Static: aunque el código " +
                    "cambie su posición, el dibujo NO se moverá. Desmarca Static o usa el modo Deactivate."
                );
            }
#endif

            Animator anim = window.GetComponentInParent<Animator>();

            if (anim != null)
            {
                Debug.LogWarning(
                    $"[PUZZLE] La ventana (o un padre) tiene un Animator ('{anim.name}'). " +
                    "Si controla su posición, puede estar pisando el movimiento."
                );
            }

            Rigidbody rb = window.GetComponentInParent<Rigidbody>();

            if (rb != null)
            {
                Debug.LogWarning(
                    $"[PUZZLE] La ventana (o un padre) tiene Rigidbody ('{rb.name}', " +
                    $"isKinematic={rb.isKinematic}). La física podría estar moviéndola."
                );
            }
        }
    }

    public void ButtonPressed()
    {
        if (mostrarDebug)
        {
            Debug.Log(
                $"[PUZZLE] ButtonPressed() recibido en '{Ruta(transform)}' | " +
                $"opened={opened}, opening={opening}, contador={pressedCount}/{totalButtons}"
            );
        }

        if (opened || opening)
        {
            if (mostrarDebug)
                Debug.Log("[PUZZLE] Señal ignorada: la ventana ya se está abriendo o ya está abierta.");

            return;
        }

        pressedCount = Mathf.Min(pressedCount + 1, totalButtons);

        Debug.Log($"[PUZZLE] Botones activados: {pressedCount}/{totalButtons}");

        if (pressedCount >= totalButtons)
        {
            if (window == null)
            {
                Debug.LogError("[PUZZLE] No se puede abrir: falta asignar Window.");
                return;
            }

            if (!isActiveAndEnabled)
            {
                Debug.LogError(
                    "[PUZZLE] El objeto del manager está desactivado: no se puede continuar."
                );
                return;
            }

            Debug.Log("[PUZZLE] Todos los botones presionados → abriendo ventana.");

            IniciarApertura();
        }
    }

    public void ButtonReleased()
    {
        // Los botones permanecen activados después de pulsarlos.
    }

    private void IniciarApertura()
    {
        opening = true;

        if (openMode == OpenMode.Deactivate)
            StartCoroutine(HideWindow());
        else
            StartCoroutine(OpenWindow());
    }

    // =========================================================
    // MODO DEACTIVATE: la ventana desaparece
    // =========================================================

    private IEnumerator HideWindow()
    {
        if (hideDelay > 0f)
        {
            if (mostrarDebug)
                Debug.Log($"[PUZZLE] La ventana desaparecerá en {hideDelay}s.");

            yield return new WaitForSeconds(hideDelay);
        }

        // Marcamos como abierta ANTES de desactivar, por si algo
        // de este objeto dependiera de la ventana.
        opened = true;
        opening = false;

        if (window != null)
            window.gameObject.SetActive(false);

        if (extraObjectsToHide != null)
        {
            foreach (GameObject obj in extraObjectsToHide)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        Debug.Log(
            $"[PUZZLE] Ventana desactivada ('{(window != null ? window.name : "-")}'). " +
            "La palanca ya puede usarse."
        );
    }

    // =========================================================
    // MODO SLIDE: la ventana se desliza en X
    // =========================================================

    private IEnumerator OpenWindow()
    {
        float duration = ObtenerDuracionSegura();
        float elapsed = 0f;
        float proximoLog = 0.5f;

        Vector3 startPosition = window.localPosition;

        if (mostrarDebug)
        {
            Debug.Log(
                $"[PUZZLE] OpenWindow: X {startPosition.x:F3} → {openX:F3} " +
                $"en {duration}s (Time.timeScale={Time.timeScale})"
            );
        }

        if (Time.timeScale == 0f)
        {
            Debug.LogWarning(
                "[PUZZLE] Time.timeScale es 0: la animación no avanzará nunca."
            );
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = startPosition;
            position.x = Mathf.Lerp(startPosition.x, openX, t);

            window.localPosition = position;

            if (mostrarDebug && elapsed >= proximoLog)
            {
                proximoLog += 0.5f;

                Debug.Log(
                    $"[PUZZLE] Moviendo ventana... X actual: {window.localPosition.x:F3} " +
                    $"(progreso {t * 100f:F0}%)"
                );
            }

            yield return null;
        }

        Vector3 finalPosition = window.localPosition;
        finalPosition.x = openX;
        window.localPosition = finalPosition;

        opened = true;
        opening = false;

        Debug.Log(
            $"[PUZZLE] Ventana abierta. Posición local final: {window.localPosition}"
        );
    }

    // =========================================================
    // PRUEBAS: clic derecho en el nombre del componente (en play)
    // =========================================================

    [ContextMenu("Probar: abrir ventana ahora")]
    private void ProbarAbrirVentana()
    {
        if (window == null)
        {
            Debug.LogError("[PUZZLE] Falta asignar Window.");
            return;
        }

        if (opened || opening)
        {
            Debug.Log("[PUZZLE] Ya está abierta o abriéndose.");
            return;
        }

        IniciarApertura();
    }

    [ContextMenu("Probar: simular un botón")]
    private void ProbarSimularBoton()
    {
        ButtonPressed();
    }

    [ContextMenu("Mostrar estado")]
    private void MostrarEstado()
    {
        Debug.Log(
            $"[PUZZLE] Estado: botones {pressedCount}/{totalButtons} | " +
            $"opening={opening} | opened={opened} | modo={openMode}"
        );
    }
}