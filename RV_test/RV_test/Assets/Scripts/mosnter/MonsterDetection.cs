using UnityEngine;

public class MonsterDetection : MonoBehaviour
{
    [Header("JUGADOR")]
    [SerializeField] private Transform player;

    [Header("DETECCIÓN")]
    [SerializeField] private float detectionRadius = 15f;

    [Header("PERSECUCIÓN")]
    [SerializeField] private MonsterChase monsterChase;

    private bool detected = false;

    private void Awake()
    {
        // Buscar automáticamente MonsterChase
        if (monsterChase == null)
            monsterChase = GetComponent<MonsterChase>();
    }

    private void Start()
    {
        // Si no asignamos manualmente el jugador,
        // buscamos la Main Camera del VR.
        if (player == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                player = mainCamera.transform;
                Debug.Log("MONSTRUO: Jugador encontrado mediante Main Camera.");
            }
            else
            {
                Debug.LogError(
                    "MONSTRUO: No se encontró una Main Camera."
                );
            }
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        if (monsterChase == null)
            return;

        // Una vez detectado, MonsterChase se encarga
        // completamente de la persecución.
        if (detected)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance <= detectionRadius)
        {
            detected = true;

            Debug.Log(
                "MONSTRUO DETECTÓ AL JUGADOR | Distancia: "
                + distance.ToString("F2")
                + " m"
            );

            // Le entregamos el Transform REAL del jugador
            // al sistema de persecución.
            monsterChase.StartChase(player);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );
    }
}