
using System.Collections;
using UnityEngine;

public class ButtonPuzzleManager : MonoBehaviour
{
    [Header("Ventana")]
    [SerializeField] private Transform window;
    [SerializeField] private float closedX = -0.393f;
    [SerializeField] private float openX = 0.403f;
    [SerializeField] private float openDuration = 2f;

    [Header("Botones")]
    [SerializeField] private int totalButtons = 3;

    private int pressedCount;
    private bool opening;
    private bool opened;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private void Awake()
    {
        if (window == null)
        {
            Debug.LogError("No hay una ventana asignada en ButtonPuzzleManager.");
            return;
        }

        closedPosition = window.localPosition;
        closedPosition.x = closedX;

        openPosition = closedPosition;
        openPosition.x = openX;

        Debug.Log(
            $"Ventana: {window.name}. Posición inicial: {window.localPosition}"
        );
    }

    public void ButtonPressed()
    {
        if (opened || opening)
            return;

        pressedCount = Mathf.Min(pressedCount + 1, totalButtons);

        Debug.Log($"Botones activados: {pressedCount}/{totalButtons}");

        if (pressedCount >= totalButtons)
        {
            if (window == null)
            {
                Debug.LogError("No se puede abrir: falta asignar Window.");
                return;
            }

            opening = true;
            StartCoroutine(OpenWindow());
        }
    }

    public void ButtonReleased()
    {
        // Los botones permanecen activados después de pulsarlos.
    }

    private IEnumerator OpenWindow()
    {
        float duration = Mathf.Max(0.01f, openDuration);
        float elapsed = 0f;

        Vector3 startPosition = window.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = startPosition;
            position.x = Mathf.Lerp(closedX, openX, t);

            window.localPosition = position;

            yield return null;
        }

        Vector3 finalPosition = window.localPosition;
        finalPosition.x = openX;
        window.localPosition = finalPosition;

        opened = true;
        opening = false;

        Debug.Log(
            $"Ventana abierta. Posición local final: {window.localPosition}"
        );
    }
}
