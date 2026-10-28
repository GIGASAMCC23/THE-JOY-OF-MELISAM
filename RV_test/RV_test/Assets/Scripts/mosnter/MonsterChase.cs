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

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (agent == null)
        {
            Debug.LogError(
                "MONSTRUO: No tiene NavMeshAgent."
            );

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

        // =====================================================
        // ¿EL JUGADOR ESTÁ MIRANDO AL MONSTRUO?
        // =====================================================

        bool playerIsLooking = IsPlayerLookingAtMonster();

        // =====================================================
        // EL JUGADOR LO MIRA
        // =====================================================

        if (playerIsLooking)
        {
            StopMonster();

            return;
        }

        // =====================================================
        // EL JUGADOR NO LO MIRA
        // =====================================================

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

        // =====================================================
        // IMPORTANTE
        // =====================================================
        //
        // NO guardamos una posición.
        //
        // Cada frame obtenemos la posición ACTUAL
        // del jugador.
        //
        // Por eso si el jugador se mueve:
        //
        // jugador → nueva posición
        // monstruo → nuevo destino
        //
        // =====================================================

        Vector3 currentPlayerPosition = player.position;

        // No queremos que el monstruo intente subir
        // hasta la altura de la cabeza VR.
        currentPlayerPosition.y = transform.position.y;

        // Actualizar el destino continuamente.
        agent.SetDestination(currentPlayerPosition);

        // =====================================================
        // ANIMACIÓN
        // =====================================================

        if (animator != null)
        {
            animator.SetBool("IsChasing", true);
            animator.SetBool("IsRunning", true);
            animator.SetBool("IsPatrolling", false);
        }

        // =====================================================
        // ORIENTACIÓN
        // =====================================================

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

        if (agent == null)
            return;

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "MONSTRUO: El NavMeshAgent no está sobre el NavMesh."
            );

            return;
        }

        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.stoppingDistance = stoppingDistance;

        // Primer destino.
        // Después ChasePlayer() lo actualizará continuamente.
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
    // DETENER MONSTRUO
    // =========================================================

    private void StopMonster()
    {
        if (agent == null)
            return;

        agent.isStopped = true;

        // No hacemos ResetPath().
        //
        // Así, cuando el jugador deje de mirar,
        // podemos continuar la persecución inmediatamente.

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsPatrolling", false);
        }

        Debug.Log("MONSTRUO ESTÁ SIENDO MIRADO → QUIETO");
    }

    // =========================================================
    // DETENER PERSECUCIÓN COMPLETAMENTE
    // =========================================================

    public void StopChase()
    {
        isChasing = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.speed = walkSpeed;
        }

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
            transform.position -
            playerCamera.transform.position;

        directionToMonster.Normalize();

        float dot = Vector3.Dot(
            playerCamera.transform.forward,
            directionToMonster
        );

        // Si el valor es suficientemente alto,
        // el monstruo está dentro del campo de visión.
        if (dot < lookThreshold)
            return false;

        // =====================================================
        // RAYCAST
        // =====================================================
        //
        // Esto evita que el monstruo se considere "mirado"
        // si hay una pared entre el jugador y el monstruo.
        //

        Ray ray = new Ray(
            playerCamera.transform.position,
            directionToMonster
        );

        RaycastHit hit;

        float distance = Vector3.Distance(
            playerCamera.transform.position,
            transform.position
        );

        if (Physics.Raycast(
            ray,
            out hit,
            distance
        ))
        {
            if (
                hit.transform == transform ||
                hit.transform.IsChildOf(transform)
            )
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

        Gizmos.DrawLine(
            transform.position,
            player.position
        );
    }
}