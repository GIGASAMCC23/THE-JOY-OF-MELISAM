using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class JumpscareManager : MonoBehaviour
{
    [Header("CUADRO DEL JUMPSCARE")]
    [SerializeField] private GameObject cuadro;                // el Quad con la imagen o video (desactivado)
    [SerializeField] private float distanciaDeCamara = 0.5f;   // qué tan lejos de la cara aparece
    [SerializeField] private Vector3 escalaCuadro = new Vector3(1.6f, 1.6f, 1f);

    [Header("AUDIO / VIDEO (opcional)")]
    [SerializeField] private AudioSource audioJumpscare;
    [SerializeField] private VideoPlayer videoJumpscare;

    [Header("DURACIÓN Y FINAL")]
    [SerializeField] private float duracion = 2f;              // segundos que dura antes del final
    [SerializeField] private bool reiniciarEscena = true;      // recarga la escena actual
    [SerializeField] private string escenaDestino = "";        // si lo llenas, carga esa escena en vez de reiniciar

    private bool activado = false;

    public void Activar()
    {
        if (activado)
            return;

        activado = true;
        StartCoroutine(Secuencia());
    }

    // Para probarlo sin el monstruo: clic derecho en el nombre del
    // componente (en play) → "Probar jumpscare".
    [ContextMenu("Probar jumpscare")]
    private void Probar()
    {
        Activar();
    }

    private IEnumerator Secuencia()
    {
        Camera cam = Camera.main;

        if (cuadro != null && cam != null)
        {
            Transform t = cuadro.transform;

            // Pegamos el cuadro a la cámara, justo delante de la cara
            t.SetParent(cam.transform, false);
            t.localPosition = new Vector3(0f, 0f, distanciaDeCamara);
            t.localRotation = Quaternion.identity;
            t.localScale = escalaCuadro;

            cuadro.SetActive(true);
        }
        else
        {
            Debug.LogWarning("JUMPSCARE: falta el cuadro o la Main Camera.");
        }

        if (videoJumpscare != null)
            videoJumpscare.Play();

        if (audioJumpscare != null)
            audioJumpscare.Play();

        yield return new WaitForSeconds(duracion);

        if (!string.IsNullOrEmpty(escenaDestino))
            SceneManager.LoadScene(escenaDestino);
        else if (reiniciarEscena)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}