using UnityEngine;
using UnityEngine.AI;

public class MonsterController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("Patrulla")]
    [SerializeField] private float distanciaParaNuevoDestino = 1.5f;
    [SerializeField] private float radioPatrulla = 15f;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        BuscarNuevoDestino();
    }

    private void Update()
    {
        if (!agent.pathPending &&
            agent.remainingDistance <= distanciaParaNuevoDestino)
        {
            BuscarNuevoDestino();
        }

        ActualizarAnimacion();
    }

    private void BuscarNuevoDestino()
    {
        Vector3 puntoAleatorio =
            transform.position + Random.insideUnitSphere * radioPatrulla;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(
            puntoAleatorio,
            out hit,
            radioPatrulla,
            NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void ActualizarAnimacion()
    {
        bool caminando =
            agent.hasPath &&
            !agent.isStopped &&
            agent.remainingDistance > distanciaParaNuevoDestino;

        animator.SetBool("IsPatrolling", caminando);
    }
}