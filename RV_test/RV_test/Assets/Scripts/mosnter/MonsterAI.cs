using UnityEngine;
using UnityEngine.AI;

public class MonstruoAI : MonoBehaviour
{
    [Header("REFERENCIAS")]
    public Transform jugador;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("DETECCIÓN")]
    public float radioDeteccion = 15f;

    [Header("VELOCIDADES")]
    public float velocidadCaminar = 1.5f;
    public float velocidadCorrer = 4f;

    [Header("MIRADA")]
    [Range(0f, 1f)]
    public float umbralMirada = 0.65f;

    [Header("DEBUG")]
    public bool mostrarDebug = true;

    private bool detectado = false;
    private bool jugadorLoMira = false;

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (jugador == null)
        {
            Debug.LogError("MONSTRUO: No se asignó el Transform del jugador.");
            return;
        }

        agent.speed = velocidadCaminar;
        agent.isStopped = false;

        animator.SetBool("IsChasing", false);
        animator.SetBool("IsRunning", false);
    }

    void Update()
    {
        if (jugador == null)
            return;

        // =====================================================
        // POSICIÓN ACTUAL DEL JUGADOR
        // =====================================================

        Vector3 posicionJugador = jugador.position;

        // No perseguimos la altura del jugador.
        posicionJugador.y = transform.position.y;

        // =====================================================
        // DISTANCIA
        // =====================================================

        float distancia = Vector3.Distance(
            transform.position,
            posicionJugador
        );

        // =====================================================
        // DETECCIÓN
        // =====================================================

        if (!detectado && distancia <= radioDeteccion)
        {
            detectado = true;

            agent.speed = velocidadCorrer;

            animator.SetBool("IsChasing", true);
            animator.SetBool("IsRunning", true);

            if (mostrarDebug)
            {
                Debug.Log(
                    "MONSTRUO DETECTÓ AL JUGADOR | Distancia: "
                    + distancia.ToString("F2")
                    + " m"
                );
            }
        }

        // =====================================================
        // PERSECUCIÓN
        // =====================================================

        if (detectado)
        {
            jugadorLoMira = EstaSiendoMirado();

            // =================================================
            // EL JUGADOR LO ESTÁ MIRANDO
            // =================================================

            if (jugadorLoMira)
            {
                agent.isStopped = true;

                animator.SetBool("IsRunning", false);
                animator.SetBool("IsChasing", false);

                if (mostrarDebug)
                {
                    Debug.Log(
                        "MONSTRUO ESTÁ SIENDO MIRADO → QUIETO"
                    );
                }
            }

            // =================================================
            // EL JUGADOR NO LO ESTÁ MIRANDO
            // =================================================

            else
            {
                agent.isStopped = false;
                agent.speed = velocidadCorrer;

                // IMPORTANTE:
                // ACTUALIZAMOS EL DESTINO CONTINUAMENTE.

                Vector3 destino = jugador.position;

                // Mantener al monstruo sobre el mismo nivel.
                destino.y = transform.position.y;

                NavMeshPath path = new NavMeshPath();

                if (NavMesh.CalculatePath(
                    transform.position,
                    destino,
                    NavMesh.AllAreas,
                    path))
                {
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        agent.SetDestination(destino);
                    }
                }

                animator.SetBool("IsChasing", true);
                animator.SetBool("IsRunning", true);
            }
        }

        // =====================================================
        // TODAVÍA NO DETECTA
        // =====================================================

        else
        {
            agent.isStopped = false;
            agent.speed = velocidadCaminar;

            animator.SetBool("IsChasing", false);
            animator.SetBool("IsRunning", false);
        }
    }

    // =========================================================
    // ¿EL JUGADOR ESTÁ MIRANDO AL MONSTRUO?
    // =========================================================

    bool EstaSiendoMirado()
    {
        Camera camara = Camera.main;

        if (camara == null)
            return false;

        Vector3 direccionAlMonstruo =
            (transform.position - camara.transform.position).normalized;

        float productoPunto = Vector3.Dot(
            camara.transform.forward,
            direccionAlMonstruo
        );

        return productoPunto >= umbralMirada;
    }

    // =========================================================
    // DEBUG VISUAL
    // =========================================================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            radioDeteccion
        );
    }
}