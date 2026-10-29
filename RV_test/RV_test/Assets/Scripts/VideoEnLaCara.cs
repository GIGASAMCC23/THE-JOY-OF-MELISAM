using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.XR;

public class VideoEnLaCara : MonoBehaviour
{
    // =========================================================
    // VIDEO
    // =========================================================

    [Header("VIDEO")]
    [SerializeField] private VideoPlayer videoPlayer;       // Render Mode: Render Texture
    [SerializeField] private Material materialCuadro;       // URP Unlit con el Render Texture en Base Map
    [SerializeField] private string escenaSiguiente = "Backrooms";


    // =========================================================
    // CUADRO FRENTE A LA CARA
    // =========================================================

    [Header("CUADRO FRENTE A LA CARA")]
    [SerializeField] private float distancia = 0.6f;                  // qué tan lejos de los ojos
    [SerializeField] private bool ajustarAlCampoDeVision = true;      // calcula el tamaño solo
    [SerializeField] private float margen = 1.1f;                     // >1 = el cuadro sobra un poco por los bordes
    [SerializeField] private bool mantenerProporcion = true;          // true = recorta bordes; false = estira
    [SerializeField] private Vector2 tamanoManual = new Vector2(2.4f, 1.35f);   // solo si no se ajusta solo
    [SerializeField] private bool fondoNegro = true;                  // lo que no cubre el video se ve negro


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("DEBUG")]
    [SerializeField] private bool mostrarDebug = true;

    private GameObject cuadro;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("[VIDEO] No se encontró la Main Camera.");
            return;
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[VIDEO] Falta asignar el Video Player.");
            return;
        }

        if (materialCuadro == null)
        {
            Debug.LogError("[VIDEO] Falta asignar el material del cuadro.");
            return;
        }

        if (fondoNegro)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        CrearCuadro(cam);
        PrepararVideo();
    }


    // =========================================================
    // CUADRO
    // =========================================================

    private void CrearCuadro(Camera cam)
    {
        cuadro = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cuadro.name = "VideoEnLaCara_Cuadro";

        // Sin collider: no debe chocar con nada
        Collider col = cuadro.GetComponent<Collider>();

        if (col != null)
            Destroy(col);

        Renderer r = cuadro.GetComponent<Renderer>();
        r.sharedMaterial = materialCuadro;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        // Pegado a la cámara, justo delante de la cara
        Transform t = cuadro.transform;
        t.SetParent(cam.transform, false);
        t.localPosition = new Vector3(0f, 0f, distancia);
        t.localRotation = Quaternion.identity;

        Vector2 tamano = tamanoManual;

        if (ajustarAlCampoDeVision)
        {
            float tanH;
            float tanV;

            ObtenerAperturaDeVision(cam, out tanH, out tanV);

            float ancho = 2f * distancia * tanH * margen;
            float alto = 2f * distancia * tanV * margen;

            if (mantenerProporcion)
            {
                float aspectoVideo = ObtenerAspectoDelVideo();

                // Cubre todo el campo de visión sin deformar el video
                if (ancho / alto > aspectoVideo)
                    alto = ancho / aspectoVideo;
                else
                    ancho = alto * aspectoVideo;
            }

            tamano = new Vector2(ancho, alto);
        }

        t.localScale = new Vector3(tamano.x, tamano.y, 1f);

        if (mostrarDebug)
        {
            Debug.Log(
                $"[VIDEO] Cuadro creado a {distancia} m de la cámara | " +
                $"tamaño: {tamano.x:F2} x {tamano.y:F2} m | " +
                $"XR activo: {XRSettings.isDeviceActive}"
            );
        }
    }

    // Qué tan abierto es el campo de visión (tangente de la mitad del ángulo)
    private void ObtenerAperturaDeVision(Camera cam, out float tanH, out float tanV)
    {
        Matrix4x4 p = XRSettings.isDeviceActive
            ? cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left)
            : cam.projectionMatrix;

        float m00 = Mathf.Abs(p.m00);
        float m11 = Mathf.Abs(p.m11);

        if (m11 > 0.0001f)
            tanV = (1f + Mathf.Abs(p.m12)) / m11;
        else
            tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

        if (m00 > 0.0001f)
            tanH = (1f + Mathf.Abs(p.m02)) / m00;
        else
            tanH = tanV * cam.aspect;
    }

    private float ObtenerAspectoDelVideo()
    {
        RenderTexture rt = videoPlayer.targetTexture;

        if (rt != null && rt.height > 0)
            return (float)rt.width / rt.height;

        return 16f / 9f;
    }


    // =========================================================
    // VIDEO
    // =========================================================

    private void PrepararVideo()
    {
        if (videoPlayer.renderMode != VideoRenderMode.RenderTexture)
        {
            Debug.LogWarning(
                "[VIDEO] El Video Player no está en Render Mode = Render Texture. " +
                "El cuadro no mostrará la imagen."
            );
        }

        if (videoPlayer.targetTexture == null)
        {
            Debug.LogWarning(
                "[VIDEO] El Video Player no tiene Target Texture: " +
                "arrastra ahí el mismo Render Texture que usa el material."
            );
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;

        videoPlayer.errorReceived += (vp, mensaje) =>
            Debug.LogError("[VIDEO] Error del Video Player: " + mensaje);

        videoPlayer.prepareCompleted += (vp) =>
        {
            if (mostrarDebug)
                Debug.Log("[VIDEO] Video listo, reproduciendo.");

            videoPlayer.Play();
        };

        videoPlayer.loopPointReached += (vp) => IrALaSiguienteEscena();

        videoPlayer.Prepare();
    }

    private void IrALaSiguienteEscena()
    {
        if (!Application.CanStreamedLevelBeLoaded(escenaSiguiente))
        {
            Debug.LogError(
                $"[VIDEO] La escena '{escenaSiguiente}' no está en Build Settings " +
                "o el nombre no coincide exactamente."
            );

            return;
        }

        if (mostrarDebug)
            Debug.Log($"[VIDEO] Terminó el video → cargando '{escenaSiguiente}'.");

        SceneManager.LoadScene(escenaSiguiente);
    }

    [ContextMenu("Probar: saltar a la siguiente escena")]
    private void Probar()
    {
        IrALaSiguienteEscena();
    }
}