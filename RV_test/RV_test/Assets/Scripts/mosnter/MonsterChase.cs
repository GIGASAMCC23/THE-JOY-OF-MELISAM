using UnityEngine;
using UnityEngine.AI;

public class MonsterChase : MonoBehaviour
{
    [Header("REFERENCIAS")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("JUGADOR")]
    [SerializeField] private Transform player;

    [Header("VELOCIDADES")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float runSpeed = 4.5f;

    [Header("PERSECUCIÓN")]
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("MIRADA")]
    [Range(0f, 1f)]
    [SerializeField] private float lookThreshold = 0.65f;

    [Header("ROTACIÓN")]
    [SerializeField] private float rotationSpeed = 8f;

    private bool isChasing = false;
    private bool wasLooked = false;
    private MonsterController patrol;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        patrol = GetComponent<MonsterController>();
    }

    private void Start()
    {
        if (agent == null)
        {
            Debug.LogError("MONSTRUO: No tiene NavMeshAgent.");
            return;
        }

        agent.speed = walkSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = false;

        SetIdleAnimation();
    }

    private void Update()
    {
        if (!isChasing)
            return;

        if (player == null)
            return;

        if (!agent.isOnNavMesh)
            return;

        // ¿El jugador está mirando al monstruo?
        bool playerIsLooking = IsPlayerLookingAtMonster();

        if (playerIsLooking != wasLooked)
        {
            wasLooked = playerIsLooking;
            Debug.Log(playerIsLooking
                ? "MONSTRUO ESTÁ SIENDO MIRADO → QUIETO"
                : "MONSTRUO YA NO ESTÁ SIENDO MIRADO → PERSIGUE");
        }

        if (playerIsLooking)
        {
            StopMonster();
            return;
        }

        ChasePlayer();
    }

    // =========================================================
    // PERSEGUIR AL JUGADOR
    // =========================================================

    private void ChasePlayer()
    {
        if (player == null)
            return;

        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.stoppingDistance = stoppingDistance;

        // Cada frame se toma la posición ACTUAL del jugador.
        Vector3 currentPlayerPosition = player.position;

        // El monstruo no intenta subir a la altura de la cabeza VR.
        currentPlayerPosition.y = transform.position.y;

        agent.SetDestination(currentPlayerPosition);

        if (animator != null)
        {
            animator.SetBool("IsChasing", true);
            animator.SetBool("IsRunning", true);
            animator.SetBool("IsPatrolling", false);
        }

        RotateTowardsMovement();
    }

    // =========================================================
    // GIRAR HACIA DONDE SE ESTÁ MOVIENDO
    // =========================================================

    private void RotateTowardsMovement()
    {
        Vector3 direction = agent.velocity;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    // =========================================================
    // COMENZAR PERSECUCIÓN
    // =========================================================

    public void StartChase(Transform target)
    {
        if (target == null)
            return;

        player = target;
        isChasing = true;

        // Apagamos la patrulla para que no pelee con la persecución
        // (destinos aleatorios y parámetros del Animator).
        if (patrol != null)
            patrol.enabled = false;

        if (agent == null)
            return;

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("MONSTRUO: El NavMeshAgent no está sobre el NavMesh.");
            return;
        }

        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.stoppingDistance = stoppingDistance;

        // Primer destino. Después ChasePlayer() lo actualiza continuamente.
        Vector3 initialPosition = player.position;
        initialPosition.y = transform.position.y;

        agent.SetDestination(initialPosition);

        if (animator != null)
        {
            animator.SetBool("IsChasing", true);
            animator.SetBool("IsRunning", true);
            animator.SetBool("IsPatrolling", false);
        }

        Debug.Log("MONSTRUO COMENZÓ A PERSEGUIR");
    }

    // =========================================================
    // DETENER MONSTRUO (cuando lo miran)
    // =========================================================

    private void StopMonster()
    {
        if (agent == null)
            return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // No hacemos ResetPath(): así, cuando el jugador deje de mirar,
        // la persecución continúa de inmediato.

        SetIdleAnimation();
    }

    // =========================================================
    // DETENER PERSECUCIÓN COMPLETAMENTE
    // =========================================================

    public void StopChase()
    {
        isChasing = false;
        wasLooked = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.speed = walkSpeed;
        }

        // Volvemos a encender la patrulla.
        if (patrol != null)
            patrol.enabled = true;

        SetIdleAnimation();

        Debug.Log("MONSTRUO DEJÓ DE PERSEGUIR");
    }

    // =========================================================
    // IDLE
    // =========================================================

    private void SetIdleAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool("IsChasing", false);
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsPatrolling", false);
        animator.SetBool("IsAttacking", false);
    }

    // =========================================================
    // COMPROBAR SI EL JUGADOR LO ESTÁ MIRANDO
    // =========================================================

    private bool IsPlayerLookingAtMonster()
    {
        Camera playerCamera = Camera.main;

        if (playerCamera == null)
            return false;

        Vector3 directionToMonster =
            transform.position - playerCamera.transform.position;

        directionToMonster.Normalize();

        float dot = Vector3.Dot(
            playerCamera.transform.forward,
            directionToMonster
        );

        // Fuera del campo de visión.
        if (dot < lookThreshold)
            return false;

        // Raycast: evita que cuente como "mirado" si hay una pared en medio.
        // Ignora los triggers (por ejemplo, las manos del player en Is Trigger).
        Ray ray = new Ray(
            playerCamera.transform.position,
            directionToMonster
        );

        float distance = Vector3.Distance(
            playerCamera.transform.position,
            transform.position
        );

        RaycastHit hit;

        if (Physics.Raycast(
            ray,
            out hit,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                return true;
            }

            return false;
        }

        return false;
    }

    // =========================================================
    // ESTADO
    // =========================================================

    public bool IsChasing()
    {
        return isChasing;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, player.position);
    }
}