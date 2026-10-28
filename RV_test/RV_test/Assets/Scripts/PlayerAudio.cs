using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    // =========================================================
    // REFERENCIAS
    // =========================================================

    [Header("REFERENCIAS")]
    [SerializeField] private Transform camara;            // si está vacío, usa la Main Camera
    [SerializeField] private Transform monstruo;          // si está vacío, busca MonstruoPersecucion
    [SerializeField] private Animator animatorMonstruo;   // si está vacío, usa el Animator del monstruo


    // =========================================================
    // CLIPS
    // =========================================================

    [Header("CLIPS")]
    [SerializeField] private AudioClip[] pasos;
    [SerializeField] private AudioClip respiracionPesada;   // loop
    [SerializeField] private AudioClip pitidoMirada;        // loop
    [SerializeField] private AudioClip sonidoJumpscare;


    // =========================================================
    // VOLUMEN GENERAL
    // =========================================================

    [Header("VOLUMEN GENERAL")]
    [Range(0f, 1f)][SerializeField] private float volumenGeneral = 1f;


    // =========================================================
    // PASOS
    // =========================================================

    [Header("PASOS")]
    [Range(0f, 1f)][SerializeField] private float volumenPasos = 0.7f;
    [SerializeField] private float distanciaPorPaso = 0.75f;        // metros recorridos entre paso y paso
    [SerializeField] private float velocidadMinima = 0.25f;         // por debajo no suenan pasos
    [SerializeField] private float velocidadMaximaValida = 8f;      // por encima se ignora (teletransportes)
    [SerializeField] private Vector2 variacionPitch = new Vector2(0.92f, 1.08f);


    // =========================================================
    // RESPIRACIÓN PESADA
    // =========================================================

    [Header("RESPIRACIÓN PESADA")]
    [Range(0f, 1f)][SerializeField] private float volumenMinimoRespiracion = 0.15f;  // monstruo lejos
    [Range(0f, 1f)][SerializeField] private float volumenMaximoRespiracion = 1f;     // monstruo cerca
    [SerializeField] private float distanciaVolumenMinimo = 25f;   // a esta distancia (o más) suena el volumen mínimo
    [SerializeField] private float distanciaVolumenMaximo = 3f;    // a esta distancia (o menos) suena el volumen máximo
    [SerializeField] private float suavizadoRespiracion = 3f;      // qué tan rápido sigue los cambios


    // =========================================================
    // PITIDO AL MIRAR AL MONSTRUO
    // =========================================================

    [Header("PITIDO AL MIRAR")]
    [Range(0f, 1f)][SerializeField] private float volumenPitido = 0.5f;
    [Range(0f, 1f)][SerializeField] private float limiteMirada = 0.70f;   // igual que el del monstruo
    [SerializeField] private float distanciaMaximaPitido = 30f;
    [SerializeField] private float velocidadSubidaPitido = 3f;             // volumen por segundo
    [SerializeField] private float velocidadBajadaPitido = 2f;


    // =========================================================
    // JUMPSCARE
    // =========================================================

    [Header("JUMPSCARE")]
    [Range(0f, 1f)][SerializeField] private float volumenJumpscare = 1f;
    [SerializeField] private string nombreEstadoJumpscare = "Jumpscare";


    // =========================================================
    // VARIABLES
    // =========================================================

    private AudioSource fuentePasos;
    private AudioSource fuenteRespiracion;
    private AudioSource fuentePitido;
    private AudioSource fuenteJumpscare;

    private Vector3 posicionAnterior;
    private bool tienePosicionAnterior = false;
    private float distanciaAcumulada = 0f;
    private int ultimoPaso = -1;

    private float volumenRespiracionActual;
    private float volumenPitidoActual = 0f;

    private bool jumpscareReproducido = false;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // Todos son 2D: suenan "desde nosotros"
        fuentePasos = CrearFuente(false);
        fuenteRespiracion = CrearFuente(true);
        fuentePitido = CrearFuente(true);
        fuenteJumpscare = CrearFuente(false);

        volumenRespiracionActual = volumenMinimoRespiracion;
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;

        if (camara == null)
            Debug.LogError("PLAYER AUDIO: No se encontró la Main Camera.");

        if (monstruo == null)
        {
            MonstruoPersecucion m = FindFirstObjectByType<MonstruoPersecucion>();

            if (m != null)
                monstruo = m.transform;
        }

        if (monstruo == null)
            Debug.LogWarning("PLAYER AUDIO: No se encontró al monstruo (MonstruoPersecucion).");

        if (animatorMonstruo == null && monstruo != null)
            animatorMonstruo = monstruo.GetComponentInChildren<Animator>();

        // Respiración y pitido en loop (el pitido empieza en silencio)
        if (respiracionPesada != null)
        {
            fuenteRespiracion.clip = respiracionPesada;
            fuenteRespiracion.volume = volumenRespiracionActual * volumenGeneral;
            fuenteRespiracion.Play();
        }
        else
        {
            Debug.LogWarning("PLAYER AUDIO: Falta el clip de respiración pesada.");
        }

        if (pitidoMirada != null)
        {
            fuentePitido.clip = pitidoMirada;
            fuentePitido.volume = 0f;
            fuentePitido.Play();
        }
        else
        {
            Debug.LogWarning("PLAYER AUDIO: Falta el clip del pitido.");
        }

        if (sonidoJumpscare == null)
            Debug.LogWarning("PLAYER AUDIO: Falta el clip del jumpscare.");
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (camara == null)
            return;

        VigilarJumpscare();

        if (jumpscareReproducido)
            return;

        ActualizarPasos();
        ActualizarRespiracion();
        ActualizarPitido();
    }


    // =========================================================
    // PASOS (según lo que se mueve la cámara, en horizontal)
    // Funciona con locomoción, caminando en la vida real
    // y con el simulador (que mueve la cámara).
    // =========================================================

    private void ActualizarPasos()
    {
        Vector3 pos = camara.position;
        pos.y = 0f;

        if (!tienePosicionAnterior)
        {
            posicionAnterior = pos;
            tienePosicionAnterior = true;
            return;
        }

        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        float distancia = Vector3.Distance(pos, posicionAnterior);
        float velocidad = distancia / dt;

        posicionAnterior = pos;

        // Teletransporte o salto brusco: ignorar
        if (velocidad > velocidadMaximaValida)
        {
            distanciaAcumulada = 0f;
            return;
        }

        // Casi quieto: reiniciar
        if (velocidad < velocidadMinima)
        {
            distanciaAcumulada = 0f;
            return;
        }

        distanciaAcumulada += distancia;

        if (distanciaAcumulada >= distanciaPorPaso)
        {
            distanciaAcumulada = 0f;
            ReproducirPaso();
        }
    }

    private void ReproducirPaso()
    {
        if (pasos == null || pasos.Length == 0)
            return;

        int indice = Random.Range(0, pasos.Length);

        // Evita repetir el mismo clip dos veces seguidas
        if (pasos.Length > 1 && indice == ultimoPaso)
            indice = (indice + 1) % pasos.Length;

        ultimoPaso = indice;

        if (pasos[indice] == null)
            return;

        fuentePasos.pitch = Random.Range(variacionPitch.x, variacionPitch.y);
        fuentePasos.PlayOneShot(pasos[indice], volumenPasos * volumenGeneral);
    }


    // =========================================================
    // RESPIRACIÓN PESADA
    // Más cerca el monstruo → más volumen (entre mínimo y máximo).
    // =========================================================

    private void ActualizarRespiracion()
    {
        if (respiracionPesada == null)
            return;

        float objetivo = volumenMinimoRespiracion;

        if (monstruo != null)
        {
            float distancia = DistanciaHorizontalAlMonstruo();

            // 0 = lejos (o más), 1 = cerca (o menos)
            float cercania = 1f - Mathf.InverseLerp(
                distanciaVolumenMaximo,
                distanciaVolumenMinimo,
                distancia
            );

            objetivo = Mathf.Lerp(
                volumenMinimoRespiracion,
                volumenMaximoRespiracion,
                cercania
            );
        }

        volumenRespiracionActual = Mathf.Lerp(
            volumenRespiracionActual,
            objetivo,
            Time.deltaTime * suavizadoRespiracion
        );

        fuenteRespiracion.volume = volumenRespiracionActual * volumenGeneral;
    }


    // =========================================================
    // PITIDO AL MIRAR AL MONSTRUO
    // =========================================================

    private void ActualizarPitido()
    {
        if (pitidoMirada == null)
            return;

        float objetivo = EstaMirandoAlMonstruo() ? volumenPitido : 0f;

        float velocidad = objetivo > volumenPitidoActual
            ? velocidadSubidaPitido
            : velocidadBajadaPitido;

        volumenPitidoActual = Mathf.MoveTowards(
            volumenPitidoActual,
            objetivo,
            velocidad * Time.deltaTime
        );

        fuentePitido.volume = volumenPitidoActual * volumenGeneral;
    }

    private bool EstaMirandoAlMonstruo()
    {
        if (monstruo == null || camara == null)
            return false;

        Vector3 direccion = monstruo.position - camara.position;
        float distancia = direccion.magnitude;

        if (distancia > distanciaMaximaPitido)
            return false;

        if (distancia <= 0.01f)
            return true;

        direccion.Normalize();

        float punto = Vector3.Dot(camara.forward, direccion);

        return punto >= limiteMirada;
    }


    // =========================================================
    // JUMPSCARE
    // Suena cuando el Animator del monstruo entra al estado
    // "Jumpscare", sin importar quién lo haya disparado.
    // =========================================================

    private void VigilarJumpscare()
    {
        if (jumpscareReproducido || animatorMonstruo == null)
            return;

        AnimatorStateInfo actual =
            animatorMonstruo.GetCurrentAnimatorStateInfo(0);

        bool enJumpscare = actual.IsName(nombreEstadoJumpscare);

        if (!enJumpscare && animatorMonstruo.IsInTransition(0))
        {
            AnimatorStateInfo siguiente =
                animatorMonstruo.GetNextAnimatorStateInfo(0);

            enJumpscare = siguiente.IsName(nombreEstadoJumpscare);
        }

        if (enJumpscare)
            AlJumpscare();
    }

    // También se puede llamar desde otro script.
    public void AlJumpscare()
    {
        if (jumpscareReproducido)
            return;

        jumpscareReproducido = true;

        // Cortamos todo lo demás para que el jumpscare se escuche limpio
        fuentePasos.Stop();
        fuenteRespiracion.Stop();
        fuentePitido.Stop();

        if (sonidoJumpscare != null)
        {
            fuenteJumpscare.pitch = 1f;
            fuenteJumpscare.PlayOneShot(
                sonidoJumpscare,
                volumenJumpscare * volumenGeneral
            );
        }

        Debug.Log("PLAYER AUDIO: sonido de jumpscare.");
    }

    // Para probarlo: clic derecho en el nombre del componente (en play)
    [ContextMenu("Probar jumpscare")]
    private void ProbarJumpscare()
    {
        AlJumpscare();
    }


    // =========================================================
    // UTILIDADES
    // =========================================================

    private float DistanciaHorizontalAlMonstruo()
    {
        Vector3 a = camara.position;
        Vector3 b = monstruo.position;

        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private AudioSource CrearFuente(bool loop)
    {
        AudioSource fuente = gameObject.AddComponent<AudioSource>();

        fuente.playOnAwake = false;
        fuente.loop = loop;
        fuente.spatialBlend = 0f;   // 2D: suena desde nosotros
        fuente.dopplerLevel = 0f;

        return fuente;
    }
}