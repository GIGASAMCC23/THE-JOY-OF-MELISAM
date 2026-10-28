using UnityEngine;
using UnityEngine.AI;

public class MonstruoPersecucion : MonoBehaviour
{
    // =========================================================
    // REFERENCIAS
    // =========================================================

    [Header("REFERENCIAS")]
    [SerializeField] private Transform objetivoJugador;
    [SerializeField] private Transform camaraJugador;

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;


    // =========================================================
    // DETECCIÓN
    // =========================================================

    [Header("DETECCIÓN")]
    [SerializeField] private float radioDeteccion = 15f;


    // =========================================================
    // MOVIMIENTO
    // =========================================================

    [Header("MOVIMIENTO")]
    [SerializeField] private float velocidadCaminar = 1.5f;
    [SerializeField] private float velocidadCorrer = 4.5f;
    [SerializeField] private float distanciaMinima = 2f;


    // =========================================================
    // MIRADA
    // =========================================================

    [Header("MIRADA")]
    [SerializeField] private bool detenerseCuandoLoMiran = true;

    [Range(0f, 1f)]
    [SerializeField] private float limiteMirada = 0.70f;


    // =========================================================
    // ACTUALIZACIÓN
    // =========================================================

    [Header("ACTUALIZACIÓN")]
    [SerializeField] private float frecuenciaActualizacion = 0.05f;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("DEBUG")]
    [SerializeField] private bool mostrarDebug = true;


    // =========================================================
    // VARIABLES
    // =========================================================

    private bool detectado = false;
    private float siguienteActualizacion = 0f;
    private bool estabaMirando = false;

    // Script de patrulla (se apaga al detectar al jugador)
    private MonsterController patrulla;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        patrulla = GetComponent<MonsterController>();
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Buscar Main Camera
        if (camaraJugador == null)
        {
            Camera cam = Camera.main;

            if (cam != null)
            {
                camaraJugador = cam.transform;

                if (mostrarDebug)
                    Debug.Log("MONSTRUO: Main Camera encontrada.");
            }
        }

        // El objetivo será la cámara
        if (objetivoJugador == null)
            objetivoJugador = camaraJugador;

        // Comprobaciones
        if (camaraJugador == null)
            Debug.LogError("MONSTRUO ERROR: No se encontró Main Camera.");

        if (agent == null)
        {
            Debug.LogError("MONSTRUO ERROR: No tiene NavMeshAgent.");
            return;
        }

        // Configurar NavMesh
        agent.speed = velocidadCaminar;
        agent.stoppingDistance = distanciaMinima;
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;

        PonerIdle();

        if (mostrarDebug)
            Debug.Log("MONSTRUO: Sistema de persecución iniciado correctamente.");
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (camaraJugador == null)
            return;

        if (agent == null)
            return;

        if (!agent.isOnNavMesh)
            return;

        // Posición real de la cámara, al mismo nivel que el monstruo
        Vector3 posicionJugador = camaraJugador.position;
        posicionJugador.y = transform.position.y;

        float distancia = Vector3.Distance(
            transform.position,
            posicionJugador
        );

        // ---------------------------------------------------------
        // DETECCIÓN
        // ---------------------------------------------------------

        if (!detectado)
        {
            if (distancia <= radioDeteccion)
            {
                detectado = true;

                // Apagamos la patrulla: durante la persecución
                // este script es el único que maneja destino y Animator.
                if (patrulla != null)
                    patrulla.enabled = false;

                if (mostrarDebug)
                {
                    Debug.Log(
                        "MONSTRUO DETECTÓ AL JUGADOR | Distancia: "
                        + distancia.ToString("F2")
                        + " m"
                    );
                }
            }
            else
            {
                Caminar();
                return;
            }
        }

        // ---------------------------------------------------------
        // ¿EL JUGADOR LO ESTÁ MIRANDO?
        // ---------------------------------------------------------

        bool loEstaMirando = EstaSiendoMirado();

        if (loEstaMirando != estabaMirando)
        {
            estabaMirando = loEstaMirando;

            if (mostrarDebug)
            {
                Debug.Log(loEstaMirando
                    ? "MONSTRUO → JUGADOR LO ESTÁ MIRANDO"
                    : "MONSTRUO → JUGADOR DEJÓ DE MIRARLO");
            }
        }

        // Si lo miran → quieto
        if (detenerseCuandoLoMiran && loEstaMirando)
        {
            DetenerMonstruo();
            return;
        }

        // Si no lo miran → perseguir
        PerseguirJugador(posicionJugador, distancia);
    }


    // =========================================================
    // PERSEGUIR
    // =========================================================

    private void PerseguirJugador(
        Vector3 posicionJugador,
        float distancia)
    {
        if (!agent.isOnNavMesh)
            return;

        // Actualizar el destino cada cierto tiempo
        if (Time.time >= siguienteActualizacion)
        {
            siguienteActualizacion =
                Time.time + frecuenciaActualizacion;

            agent.SetDestination(posicionJugador);

            if (mostrarDebug)
            {
                Debug.DrawLine(
                    transform.position,
                    posicionJugador,
                    Color.red,
                    frecuenciaActualizacion
                );
            }
        }

        agent.speed = velocidadCorrer;

        // Muy cerca → se detiene
        if (distancia <= distanciaMinima)
        {
            agent.isStopped = true;
            PonerIdle();
            return;
        }

        agent.isStopped = false;

        PonerCorrer();
    }


    // =========================================================
    // CAMINAR (antes de detectar al jugador)
    // =========================================================

    private void Caminar()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.speed = velocidadCaminar;

        if (animator != null)
        {
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsPatrolling", true);
            animator.SetBool("IsAttacking", false);
        }
    }


    // =========================================================
    // IDLE
    // =========================================================

    private void PonerIdle()
    {
        if (animator == null)
            return;

        animator.SetBool("IsChasing", false);
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsPatrolling", false);
        animator.SetBool("IsAttacking", false);
    }


    // =========================================================
    // CORRER
    // =========================================================

    private void PonerCorrer()
    {
        if (animator == null)
            return;

        animator.SetBool("IsChasing", true);
        animator.SetBool("IsRunning", true);
        animator.SetBool("IsPatrolling", false);
        animator.SetBool("IsAttacking", false);
    }


    // =========================================================
    // DETENER
    // =========================================================

    private void DetenerMonstruo()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        PonerIdle();
    }


    // =========================================================
    // COMPROBAR MIRADA
    // =========================================================

    private bool EstaSiendoMirado()
    {
        if (camaraJugador == null)
            return false;

        Vector3 direccion =
            transform.position - camaraJugador.position;

        float distancia = direccion.magnitude;

        if (distancia <= 0.01f)
            return true;

        direccion.Normalize();

        float punto = Vector3.Dot(
            camaraJugador.forward,
            direccion
        );

        // Fuera del campo de visión
        if (punto < limiteMirada)
            return false;

        // Si la cámara apunta al monstruo, lo consideramos mirado
        return true;
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            radioDeteccion
        );
    }
}