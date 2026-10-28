using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerFisico : MonoBehaviour
{
    [Header("Spawn (opcional)")]
    public bool usarSpawnFijo = false;
    public Vector3 posicionSpawn = new Vector3(-2.93f, 2.844f, 4.54f);

    [Header("Caída")]
    public float velocidadMaxCaida = 30f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;

        // Sin rotación y sin deslizarse en X ni en Z: la física solo actúa en Y
        rb.constraints = RigidbodyConstraints.FreezeRotation
                       | RigidbodyConstraints.FreezePositionX
                       | RigidbodyConstraints.FreezePositionZ;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.maxDepenetrationVelocity = 0.5f;   // evita que te "escupa" si hay solapamiento

        if (usarSpawnFijo)
            transform.position = posicionSpawn;
    }

    void FixedUpdate()
    {
        Vector3 v = rb.linearVelocity;   // en Unity anterior a 6: rb.velocity

        // La física nunca debe subirte, solo caer
        if (v.y > 0f) v.y = 0f;
        if (v.y < -velocidadMaxCaida) v.y = -velocidadMaxCaida;

        rb.linearVelocity = v;
    }
}