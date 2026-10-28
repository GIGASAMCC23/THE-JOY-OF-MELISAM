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


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // =====================================================
        // BUSCAR MAIN CAMERA
        // =====================================================

        if (camaraJugador == null)
        {
            Camera cam = Camera.main;

            if (cam != null)
            {
                camaraJugador = cam.transform;

                if (mostrarDebug)
                {
                    Debug.Log(
                        "MONSTRUO: Main Camera encontrada."
                    );
                }
            }
        }


        // =====================================================
        // EL OBJETIVO SERÁ LA CÁMARA
        // =====================================================

        if (objetivoJugador == null)
        {
            objetivoJugador = camaraJugador;
        }


        // =====================================================
        // COMPROBACIONES
        // =====================================================

        if (camaraJugador == null)
        {
            Debug.LogError(
                "MONSTRUO ERROR: No se encontró Main Camera."
            );
        }

        if (agent == null)
        {
            Debug.LogError(
                "MONSTRUO ERROR: No tiene NavMeshAgent."
            );

            return;
        }


        // =====================================================
        // CONFIGURAR NAVMESH
        // =====================================================

        agent.speed = velocidadCaminar;

        agent.stoppingDistance = distanciaMinima;

        agent.isStopped = false;

        agent.updatePosition = true;

        agent.updateRotation = true;


        // =====================================================
        // ANIMACIÓN INICIAL
        // =====================================================

        PonerIdle();


        if (mostrarDebug)
        {
            Debug.Log(
                "MONSTRUO: Sistema de persecución iniciado correctamente."
            );
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // COMPROBACIONES
        // =====================================================

        if (camaraJugador == null)
            return;

        if (agent == null)
            return;

        if (!agent.isOnNavMesh)
            return;


        // =====================================================
        // POSICIÓN REAL DE LA CÁMARA
        // =====================================================

        Vector3 posicionJugador =
            camaraJugador.position;


        // =====================================================
        // MANTENER AL MONSTRUO EN EL MISMO NIVEL
        // =====================================================

        posicionJugador.y =
            transform.position.y;


        // =====================================================
        // DISTANCIA
        // =====================================================

        float distancia =
            Vector3.Distance(
                transform.position,
                posicionJugador
            );


        // =====================================================
        // DETECCIÓN
        // =====================================================

        if (!detectado)
        {
            if (distancia <= radioDeteccion)
            {
                detectado = true;

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


        // =====================================================
        // ¿EL JUGADOR LO ESTÁ MIRANDO?
        // =====================================================

        bool loEstaMirando =
            EstaSiendoMirado();


        // =====================================================
        // CAMBIÓ EL ESTADO DE MIRADA
        // =====================================================

        if (loEstaMirando != estabaMirando)
        {
            estabaMirando = loEstaMirando;

            if (mostrarDebug)
            {
                if (loEstaMirando)
                {
                    Debug.Log(
                        "MONSTRUO → JUGADOR LO ESTÁ MIRANDO"
                    );
                }
                else
                {
                    Debug.Log(
                        "MONSTRUO → JUGADOR DEJÓ DE MIRARLO"
                    );
                }
            }
        }


        // =====================================================
        // SI LO ESTÁ MIRANDO
        // =====================================================

        if (
            detenerseCuandoLoMiran &&
            loEstaMirando
        )
        {
            DetenerMonstruo();

            return;
        }


        // =====================================================
        // SI NO LO ESTÁ MIRANDO → PERSEGUIR
        // =====================================================

        PerseguirJugador(
            posicionJugador,
            distancia
        );
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


        // =====================================================
        // ACTUALIZAR DESTINO
        // =====================================================

        if (Time.time >= siguienteActualizacion)
        {
            siguienteActualizacion =
                Time.time +
                frecuenciaActualizacion;


            // ESTA ES LA PARTE MÁS IMPORTANTE
            //
            // El destino se obtiene de la CÁMARA
            // cada 0.05 segundos.

            agent.SetDestination(
                posicionJugador
            );


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


        // =====================================================
        // VELOCIDAD
        // =====================================================

        agent.speed =
            velocidadCorrer;


        // =====================================================
        // DISTANCIA MÍNIMA
        // =====================================================

        if (distancia <= distanciaMinima)
        {
            agent.isStopped = true;

            PonerIdle();

            return;
        }


        // =====================================================
        // CONTINUAR MOVIÉNDOSE
        // =====================================================

        agent.isStopped = false;


        // =====================================================
        // ANIMACIÓN
        // =====================================================

        PonerCorrer();
    }


    // =========================================================
    // CAMINAR
    // =========================================================

    private void Caminar()
    {
        if (!agent.isOnNavMesh)
            return;


        agent.isStopped = false;

        agent.speed =
            velocidadCaminar;


        if (animator != null)
        {
            animator.SetBool(
                "IsChasing",
                false
            );

            animator.SetBool(
                "IsRunning",
                false
            );

            animator.SetBool(
                "IsPatrolling",
                true
            );

            animator.SetBool(
                "IsAttacking",
                false
            );
        }
    }


    // =========================================================
    // IDLE
    // =========================================================

    private void PonerIdle()
    {
        if (animator == null)
            return;


        animator.SetBool(
            "IsChasing",
            false
        );

        animator.SetBool(
            "IsRunning",
            false
        );

        animator.SetBool(
            "IsPatrolling",
            false
        );

        animator.SetBool(
            "IsAttacking",
            false
        );
    }


    // =========================================================
    // CORRER
    // =========================================================

    private void PonerCorrer()
    {
        if (animator == null)
            return;


        animator.SetBool(
            "IsChasing",
            true
        );

        animator.SetBool(
            "IsRunning",
            true
        );

        animator.SetBool(
            "IsPatrolling",
            false
        );

        animator.SetBool(
            "IsAttacking",
            false
        );
    }


    // =========================================================
    // DETENER
    // =========================================================

    private void DetenerMonstruo()
    {
        if (!agent.isOnNavMesh)
            return;


        agent.isStopped = true;


        PonerIdle();
    }


    // =========================================================
    // COMPROBAR MIRADA
    // =========================================================

    private bool EstaSiendoMirado()
    {
        if (camaraJugador == null)
            return false;


        // =====================================================
        // DIRECCIÓN CÁMARA → MONSTRUO
        // =====================================================

        Vector3 direccion =
            transform.position -
            camaraJugador.position;


        float distancia =
            direccion.magnitude;


        if (distancia <= 0.01f)
            return true;


        direccion.Normalize();


        // =====================================================
        // PRODUCTO PUNTO
        // =====================================================

        float punto =
            Vector3.Dot(
                camaraJugador.forward,
                direccion
            );


        // =====================================================
        // NO ESTÁ MIRANDO
        // =====================================================

        if (punto < limiteMirada)
        {
            return false;
        }


        // =====================================================
        // IMPORTANTE:
        // AQUÍ NO DEPENDEMOS DEL RAYCAST.
        //
        // Si la cámara está apuntando directamente
        // al monstruo, lo consideramos mirado.
        // =====================================================

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