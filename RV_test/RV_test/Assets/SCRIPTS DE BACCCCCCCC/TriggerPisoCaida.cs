using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TriggerPisoCaida : MonoBehaviour
{
    [Header("Picada")]
    public float velocidadPicada = 15f;        // velocidad inicial hacia abajo

    [Header("Transición")]
    public float segundosAntesDelVideo = 2f;
    public string escenaVideo = "EscenaVideo";

    bool activado;

    void OnTriggerEnter(Collider other)
    {
        if (activado || !other.transform.root.CompareTag("Player")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        activado = true;
        StartCoroutine(Secuencia(rb));
    }

    IEnumerator Secuencia(Rigidbody rb)
    {
        // Sin collider el piso ya no lo frena
        CapsuleCollider capsula = rb.GetComponent<CapsuleCollider>();
        if (capsula != null) capsula.enabled = false;

        // Empujón inicial hacia abajo (en Unity anterior a 6: rb.velocity)
        rb.linearVelocity = new Vector3(0f, -velocidadPicada, 0f);

        yield return new WaitForSeconds(segundosAntesDelVideo);
        SceneManager.LoadScene(escenaVideo);
    }
}