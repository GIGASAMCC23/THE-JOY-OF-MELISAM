using UnityEngine;
using UnityEngine.AI;

public class MonsterAudio : MonoBehaviour
{
    // =========================================================
    // REFERENCIAS
    // =========================================================

    [Header("REFERENCIAS")]
    [SerializeField] private NavMeshAgent agent;


    // =========================================================
    // CLIPS
    // =========================================================

    [Header("CLIPS")]
    [SerializeField] private AudioClip[] pasos;              // varios clips de pasos
    [SerializeField] private AudioClip rugidoDeteccion;      // al detectar al jugador
    [SerializeField] private AudioClip respiracion;          // se reproduce en loop
    [SerializeField] private AudioClip sonidoJumpscare;


    // =========================================================
    // VOLÚMENES
    // =========================================================

    [Header("VOLÚMENES")]
    [Range(0f, 1f)][SerializeField] private float volumenPasos = 0.8f;
    [Range(0f, 1f)][SerializeField] private float volumenRugido = 1f;
    [Range(0f, 1f)][SerializeField] private float volumenRespiracion = 0.6f;
    [Range(0f, 1f)][SerializeField] private float volumenJumpscare = 1f;


    // =========================================================
    // PASOS
    // =========================================================

    [Header("PASOS")]
    [SerializeField] private float velocidadMinimaPasos = 0.2f;   // por debajo no suenan pasos
    [SerializeField] private float umbralCorrer = 3f;             // velocidad a partir de la cual "corre"
    [SerializeField] private float intervaloCaminar = 0.65f;      // segundos entre pasos caminando
    [SerializeField] private float intervaloCorrer = 0.35f;       // segundos entre pasos corriendo
    [SerializeField] private Vector2 variacionPitch = new Vector2(0.9f, 1.1f);


    // =========================================================
    // SONIDO 3D
    // =========================================================

    [Header("SONIDO 3D")]
    [SerializeField] private float distanciaMinima = 2f;     // hasta aquí suena a volumen completo
    [SerializeField] private float distanciaMaxima = 25f;    // más lejos que esto ya no se escucha
    [SerializeField] private AudioRolloffMode atenuacion = AudioRolloffMode.Linear;
    [SerializeField] private bool jumpscareEn2D = true;      // el jumpscare suena "dentro de la cabeza"


    // =========================================================
    // VARIABLES
    // =========================================================

    private AudioSource fuentePasos;
    private AudioSource fuenteVoz;
    private AudioSource fuenteRespiracion;
    private AudioSource fuenteJumpscare;

    private float siguientePaso = 0f;
    private int ultimoPaso = -1;
    private bool rugidoReproducido = false;
    private bool silenciado = false;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        fuentePasos = CrearFuente(true, false);
        fuenteVoz = CrearFuente(true, false);
        fuenteRespiracion = CrearFuente(true, true);
        fuenteJumpscare = CrearFuente(!jumpscareEn2D, false);
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // La respiración suena todo el tiempo
        if (respiracion != null)
        {
            fuenteRespiracion.clip = respiracion;
            fuenteRespiracion.volume = volumenRespiracion;
            fuenteRespiracion.Play();
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (silenciado)
            return;

        ActualizarPasos();
    }


    // =========================================================
    // PASOS
    // =========================================================

    private void ActualizarPasos()
    {
        if (agent == null || pasos == null || pasos.Length == 0)
            return;

        if (!agent.isOnNavMesh || agent.isStopped)
        {
            siguientePaso = 0f;
            return;
        }

        float velocidad = agent.velocity.magnitude;

        // Quieto: no suenan pasos
        if (velocidad < velocidadMinimaPasos)
        {
            siguientePaso = 0f;
            return;
        }

        if (Time.time < siguientePaso)
            return;

        ReproducirPaso();

        float intervalo = velocidad >= umbralCorrer
            ? intervaloCorrer
            : intervaloCaminar;

        siguientePaso = Time.time + intervalo;
    }

    private void ReproducirPaso()
    {
        int indice = Random.Range(0, pasos.Length);

        // Evita repetir el mismo clip dos veces seguidas
        if (pasos.Length > 1 && indice == ultimoPaso)
            indice = (indice + 1) % pasos.Length;

        ultimoPaso = indice;

        if (pasos[indice] == null)
            return;

        fuentePasos.pitch = Random.Range(variacionPitch.x, variacionPitch.y);
        fuentePasos.PlayOneShot(pasos[indice], volumenPasos);
    }


    // =========================================================
    // EVENTOS (los llama MonstruoPersecucion)
    // =========================================================

    // Primera detección del jugador → rugido (una sola vez)
    public void AlDetectarJugador()
    {
        if (rugidoReproducido || silenciado)
            return;

        rugidoReproducido = true;

        if (rugidoDeteccion != null)
        {
            fuenteVoz.pitch = 1f;
            fuenteVoz.PlayOneShot(rugidoDeteccion, volumenRugido);
        }
    }

    // El monstruo alcanza al jugador → jumpscare
    public void AlJumpscare()
    {
        if (silenciado)
            return;

        silenciado = true;

        // Cortamos todo lo demás para que el jumpscare se escuche limpio
        fuentePasos.Stop();
        fuenteVoz.Stop();
        fuenteRespiracion.Stop();

        if (sonidoJumpscare != null)
        {
            fuenteJumpscare.pitch = 1f;
            fuenteJumpscare.PlayOneShot(sonidoJumpscare, volumenJumpscare);
        }
    }


    // =========================================================
    // PRUEBAS (clic derecho en el nombre del componente, en play)
    // =========================================================

    [ContextMenu("Probar rugido")]
    private void ProbarRugido()
    {
        rugidoReproducido = false;
        AlDetectarJugador();
    }

    [ContextMenu("Probar jumpscare")]
    private void ProbarJumpscare()
    {
        AlJumpscare();
    }


    // =========================================================
    // CREAR AUDIO SOURCE
    // =========================================================

    private AudioSource CrearFuente(bool es3D, bool loop)
    {
        AudioSource fuente = gameObject.AddComponent<AudioSource>();

        fuente.playOnAwake = false;
        fuente.loop = loop;
        fuente.spatialBlend = es3D ? 1f : 0f;
        fuente.rolloffMode = atenuacion;
        fuente.minDistance = distanciaMinima;
        fuente.maxDistance = distanciaMaxima;
        fuente.dopplerLevel = 0f;

        return fuente;
    }
}