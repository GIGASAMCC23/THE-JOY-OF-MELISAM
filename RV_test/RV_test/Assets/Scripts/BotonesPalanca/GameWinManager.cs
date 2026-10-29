using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameWinManager : MonoBehaviour
{
    [Header("Escena final")]
    [SerializeField] private bool cargarEscenaFinal = true;
    [SerializeField] private string escenaFinal = "EscenaFinal";   // nombre exacto de la escena
    [SerializeField] private float retardoAntesDeCargar = 1.5f;    // segundos de espera (sonido, efecto, etc.)

    [Header("Eventos (sonido, UI, etc.)")]
    [SerializeField] private UnityEvent onWin;

    private bool won = false;

    public void Win()
    {
        if (won)
            return;

        won = true;

        Debug.Log("¡Ganaste!");

        onWin.Invoke();

        if (cargarEscenaFinal)
            StartCoroutine(CargarEscena());
    }

    private IEnumerator CargarEscena()
    {
        yield return new WaitForSeconds(retardoAntesDeCargar);
        SceneManager.LoadScene(escenaFinal);
    }
}