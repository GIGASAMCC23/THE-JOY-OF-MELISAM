using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioDeEscena : MonoBehaviour
{
    [Header("Escena")]
    [SerializeField] private string escenaDestino = "EscenaVideo";   // nombre exacto de la escena
    [SerializeField] private float retardo = 0.5f;                   // segundos antes de cargar

    private bool cargando;

    // Llamar desde un evento del Inspector (por ejemplo, On Pressed del botón)
    public void Cargar()
    {
        if (cargando)
            return;

        if (!Application.CanStreamedLevelBeLoaded(escenaDestino))
        {
            Debug.LogError(
                $"[ESCENA] La escena '{escenaDestino}' no está en Build Settings " +
                "o el nombre no coincide exactamente."
            );

            return;
        }

        cargando = true;

        Debug.Log($"[ESCENA] Cargando '{escenaDestino}' en {retardo}s...");

        StartCoroutine(CargarConRetardo());
    }

    private IEnumerator CargarConRetardo()
    {
        if (retardo > 0f)
            yield return new WaitForSeconds(retardo);

        SceneManager.LoadScene(escenaDestino);
    }

    [ContextMenu("Probar cambio de escena")]
    private void Probar()
    {
        Cargar();
    }
}