using UnityEngine;

public class DebugAnimatorMonstruo : MonoBehaviour
{
    [SerializeField] private Animator animator;

    bool chasing, running, patrolling;
    string estadoPrevio = "";

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator == null) return;

        bool c = animator.GetBool("IsChasing");
        bool r = animator.GetBool("IsRunning");
        bool p = animator.GetBool("IsPatrolling");

        if (c != chasing || r != running || p != patrolling)
        {
            chasing = c;
            running = r;
            patrolling = p;

            Debug.Log($"[Anim] t={Time.time:F2} | IsChasing={c} IsRunning={r} IsPatrolling={p}");
        }

        string estado = NombreEstado();

        if (estado != estadoPrevio)
        {
            estadoPrevio = estado;
            Debug.Log($"[Anim] t={Time.time:F2} | ESTADO → {estado}");
        }
    }

    string NombreEstado()
    {
        AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(0);

        if (s.IsName("Idle")) return "Idle";
        if (s.IsName("Walk")) return "Walk";
        if (s.IsName("Run")) return "Run";
        if (s.IsName("Jumpscare")) return "Jumpscare";

        return "otro";
    }
}