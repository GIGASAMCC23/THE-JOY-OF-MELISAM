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
    [SerializeField] private MonsterAudio audioMonstruo;


    // =========================================================
    // PATRULLA (antes de detectar al jugador)
    // =========================================================

    [Header("PATRULLA")]
    [SerializeField] private bool patrullarTodoElNavMesh = true;       // si no, patrulla cerca de donde está
    [SerializeField] private float radioPatrulla = 15f;                // solo si lo de arriba está desactivado
    [SerializeField] private float distanciaMinimaNuevoDestino = 4f;   // evita destinos pegados
    [SerializeField] private float distanciaLlegadaPatrulla = 1f;
    [SerializeField] private float pausaMinima = 1f;                   // espera al llegar a un punto
    [SerializeField] private float pausaMaxima = 4f;
    [SerializeField] private float tiempoMaximoAtascado = 2f;


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


    // =========================================================
    // JUMPSCARE
    // =========================================================

    [Header("JUMPSCARE")]
    [SerializeField] private JumpscareManager jumpscare;     // opcional (cuadro en pantalla)
    [SerializeField] private float distanciaJumpscare = 1.5f;


    // =========================================================
    // MIRADA
    // =========================================================

    [Header("MIRADA")]
    [SerializeField] private bool detenerseCuandoLoMiran = true;

    [Range(0f, 1f)]
    [SerializeField] private float limiteMirada = 0.70f;


    // =========================================================
    // ANIMACIÓN
    // =========================================================

    [Header("ANIMACIÓN")]
    [SerializeField] private bool forzarIdleAlDetenerse = true;
    [SerializeField] private string nombreEstadoIdle = "Idle";


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
    private bool jumpscareActivado = false;

    // Patrulla
    private bool tieneDestinoPatrulla = false;
    private bool esperandoEnPatrulla = false;
    private float finEsperaPatrulla = 0f;
    private float tiempoAtascado = 0f;
    private Vector3 destinoPatrulla;

    // Datos del NavMesh para elegir puntos al azar
    private bool usarTriangulacion = false;
    private Vector3[] navVertices;
    private int[] navIndices;
    private float[] areasAcumuladas;
    private float areaTotal = 0f;

    // Para el debug del Animator
    private string estadoAnimPrevio = "";
    private bool dbgChasing, dbgRunning, dbgPatrolling;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioMonstruo == null)
            audioMonstruo = GetComponent<MonsterAudio>();

        // Si todavía existe el script viejo de patrulla, lo apagamos:
        // la patrulla ahora vive en este script.
        Behaviour patrullaVieja = GetComponent("MonsterController") as Behaviour;

        if (patrullaVieja != null)
        {
            patrullaVieja.enabled = false;

            if (mostrarDebug)
                Debug.Log("MONSTRUO: MonsterController apagado (la patrulla ahora está en este script).");
        }
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

        // Configurar NavMesh.
        // El agente se detiene más cerca que la distancia del jumpscare,
        // para que el jumpscare siempre alcance a dispararse.
        agent.speed = velocidadCaminar;
        agent.stoppingDistance = distanciaJumpscare * 0.5f;
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;

        PrepararNavMesh();

        PonerIdle();

        if (mostrarDebug)
            Debug.Log("MONSTRUO: Sistema de persecución iniciado correctamente.");
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        RegistrarAnimatorDebug();

        // Después del jumpscare el monstruo ya no hace nada más
        if (jumpscareActivado)
            return;

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
                esperandoEnPatrulla = false;
                tieneDestinoPatrulla = false;

                // Rugido de la primera detección
                if (audioMonstruo != null)
                    audioMonstruo.AlDetectarJugador();

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
                // Todavía no lo detecta → merodea
                Patrullar();
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
    // PATRULLA
    // =========================================================

    private void Patrullar()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.speed = velocidadCaminar;

        // --- Pausa entre destinos ---
        if (esperandoEnPatrulla)
        {
            agent.isStopped = true;
            PonerIdle();

            if (Time.time >= finEsperaPatrulla)
            {
                esperandoEnPatrulla = false;
                agent.isStopped = false;

                ElegirNuevoDestinoPatrulla();
            }

            return;
        }

        agent.isStopped = false;

        // --- Sin destino → elegir uno ---
        if (!tieneDestinoPatrulla)
        {
            ElegirNuevoDestinoPatrulla();

            if (tieneDestinoPatrulla)
                PonerCaminar();
            else
                PonerIdle();

            return;
        }

        // --- ¿Llegó al destino? ---
        float distanciaLlegada = Mathf.Max(
            distanciaLlegadaPatrulla,
            agent.stoppingDistance + 0.1f
        );

        if (!agent.pathPending &&
            agent.remainingDistance <= distanciaLlegada)
        {
            tieneDestinoPatrulla = false;
            tiempoAtascado = 0f;

            float pausa = Random.Range(pausaMinima, pausaMaxima);

            if (pausa > 0.01f)
            {
                esperandoEnPatrulla = true;
                finEsperaPatrulla = Time.time + pausa;

                agent.isStopped = true;
                PonerIdle();
            }

            return;
        }

        // --- ¿Está atascado? ---
        if (!agent.pathPending && agent.velocity.sqrMagnitude < 0.01f)
        {
            tiempoAtascado += Time.deltaTime;

            if (tiempoAtascado >= tiempoMaximoAtascado)
            {
                tiempoAtascado = 0f;
                ElegirNuevoDestinoPatrulla();
            }
        }
        else
        {
            tiempoAtascado = 0f;
        }

        if (mostrarDebug && tieneDestinoPatrulla)
            Debug.DrawLine(transform.position, destinoPatrulla, Color.cyan);

        PonerCaminar();
    }

    private void ElegirNuevoDestinoPatrulla()
    {
        tieneDestinoPatrulla = false;

        const int intentos = 15;

        for (int i = 0; i < intentos; i++)
        {
            Vector3 candidato;

            if (!ObtenerPuntoAleatorio(out candidato))
                continue;

            // Que no sea un punto pegado al monstruo
            float distancia = Vector3.Distance(transform.position, candidato);

            if (distancia < distanciaMinimaNuevoDestino)
                continue;

            // Que el camino hasta ahí esté completo
            NavMeshPath camino = new NavMeshPath();

            if (agent.CalculatePath(candidato, camino) &&
                camino.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(camino);

                destinoPatrulla = candidato;
                tieneDestinoPatrulla = true;
                tiempoAtascado = 0f;

                if (mostrarDebug)
                {
                    Debug.Log(
                        "MONSTRUO PATRULLA → nuevo destino a "
                        + distancia.ToString("F1") + " m"
                    );
                }

                return;
            }
        }

        if (mostrarDebug)
            Debug.LogWarning("MONSTRUO PATRULLA: no se encontró un destino válido, reintentando.");
    }

    // Prepara los triángulos del NavMesh para elegir puntos repartidos
    // de forma pareja por toda la zona azul.
    private void PrepararNavMesh()
    {
        usarTriangulacion = false;

        if (!patrullarTodoElNavMesh)
            return;

        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();

        if (tri.indices == null || tri.indices.Length < 3)
        {
            Debug.LogWarning(
                "MONSTRUO: No se encontró un NavMesh horneado. " +
                "Patrullará solo dentro del radio."
            );

            return;
        }

        navVertices = tri.vertices;
        navIndices = tri.indices;

        int cantidad = navIndices.Length / 3;
        areasAcumuladas = new float[cantidad];

        float acumulado = 0f;

        for (int i = 0; i < cantidad; i++)
        {
            Vector3 a = navVertices[navIndices[i * 3]];
            Vector3 b = navVertices[navIndices[i * 3 + 1]];
            Vector3 c = navVertices[navIndices[i * 3 + 2]];

            float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;

            acumulado += area;
            areasAcumuladas[i] = acumulado;
        }

        areaTotal = acumulado;
        usarTriangulacion = areaTotal > 0.01f;
    }

    private bool ObtenerPuntoAleatorio(out Vector3 punto)
    {
        punto = transform.position;

        NavMeshHit hit;

        if (usarTriangulacion)
        {
            // Triángulo al azar (más probable cuanto más grande es)
            float r = Random.value * areaTotal;

            int bajo = 0;
            int alto = areasAcumuladas.Length - 1;

            while (bajo < alto)
            {
                int medio = (bajo + alto) / 2;

                if (areasAcumuladas[medio] >= r)
                    alto = medio;
                else
                    bajo = medio + 1;
            }

            Vector3 a = navVertices[navIndices[bajo * 3]];
            Vector3 b = navVertices[navIndices[bajo * 3 + 1]];
            Vector3 c = navVertices[navIndices[bajo * 3 + 2]];

            // Punto al azar dentro del triángulo
            float r1 = Mathf.Sqrt(Random.value);
            float r2 = Random.value;

            Vector3 p =
                (1f - r1) * a +
                r1 * (1f - r2) * b +
                r1 * r2 * c;

            if (NavMesh.SamplePosition(p, out hit, 1.5f, agent.areaMask))
            {
                punto = hit.position;
                return true;
            }

            return false;
        }

        // Modo radio: cerca de donde está el monstruo
        Vector3 aleatorio =
            transform.position + Random.insideUnitSphere * radioPatrulla;

        if (NavMesh.SamplePosition(aleatorio, out hit, radioPatrulla, agent.areaMask))
        {
            punto = hit.position;
            return true;
        }

        return false;
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

        // Alcanzó al jugador → JUMPSCARE
        if (distancia <= distanciaJumpscare)
        {
            ActivarJumpscare();
            return;
        }

        agent.isStopped = false;

        PonerCorrer();
    }


    // =========================================================
    // JUMPSCARE
    // =========================================================

    private void ActivarJumpscare()
    {
        if (jumpscareActivado)
            return;

        jumpscareActivado = true;

        if (mostrarDebug)
            Debug.Log("MONSTRUO: ¡JUMPSCARE!");

        // Quieto
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Animación de ataque
        if (animator != null)
        {
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsPatrolling", false);
            animator.SetBool("IsAttacking", true);
        }

        // Sonido del jumpscare
        if (audioMonstruo != null)
            audioMonstruo.AlJumpscare();

        // Cuadro en pantalla (solo si lo tienes asignado)
        if (jumpscare != null)
            jumpscare.Activar();
    }


    // =========================================================
    // CAMINAR
    // =========================================================

    private void PonerCaminar()
    {
        if (animator == null)
            return;

        animator.SetBool("IsChasing", false);
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsPatrolling", true);
        animator.SetBool("IsAttacking", false);
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
    // FORZAR IDLE
    // Si el Animator intenta irse a otro estado mientras el
    // monstruo está quieto, lo devuelve a Idle.
    // =========================================================

    private void ForzarIdle()
    {
        if (!forzarIdleAlDetenerse || animator == null)
            return;

        bool enTransicion = animator.IsInTransition(0);

        AnimatorStateInfo actual =
            animator.GetCurrentAnimatorStateInfo(0);

        AnimatorStateInfo siguiente =
            animator.GetNextAnimatorStateInfo(0);

        bool yaEsIdle =
            !enTransicion && actual.IsName(nombreEstadoIdle);

        bool vaHaciaIdle =
            enTransicion && siguiente.IsName(nombreEstadoIdle);

        if (yaEsIdle || vaHaciaIdle)
            return;

        animator.CrossFadeInFixedTime(nombreEstadoIdle, 0.1f);
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
        ForzarIdle();
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
    // DEBUG DEL ANIMATOR
    // Imprime en consola cuando cambian los parámetros o el estado.
    // =========================================================

    private void RegistrarAnimatorDebug()
    {
        if (!mostrarDebug || animator == null)
            return;

        bool c = animator.GetBool("IsChasing");
        bool r = animator.GetBool("IsRunning");
        bool p = animator.GetBool("IsPatrolling");

        if (c != dbgChasing || r != dbgRunning || p != dbgPatrolling)
        {
            dbgChasing = c;
            dbgRunning = r;
            dbgPatrolling = p;

            Debug.Log(
                "[Anim] t=" + Time.time.ToString("F2") +
                " | IsChasing=" + c +
                " IsRunning=" + r +
                " IsPatrolling=" + p
            );
        }

        string estado = NombreEstadoAnimator();

        if (estado != estadoAnimPrevio)
        {
            estadoAnimPrevio = estado;

            Debug.Log(
                "[Anim] t=" + Time.time.ToString("F2") +
                " | ESTADO → " + estado
            );
        }
    }

    private string NombreEstadoAnimator()
    {
        AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(0);

        if (s.IsName("Idle")) return "Idle";
        if (s.IsName("Walk")) return "Walk";
        if (s.IsName("Run")) return "Run";
        if (s.IsName("Jumpscare")) return "Jumpscare";

        return "otro";
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // Radio de detección
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            radioDeteccion
        );

        // Distancia del jumpscare
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            distanciaJumpscare
        );

        // Destino actual de la patrulla
        if (Application.isPlaying && tieneDestinoPatrulla)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(destinoPatrulla, 0.3f);
        }
    }
}