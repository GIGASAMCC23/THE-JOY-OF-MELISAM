using UnityEngine;
using UnityEngine.Events;

public class GameWinManager : MonoBehaviour
{
    [SerializeField] UnityEvent onWin;
    bool won = false;

    public void Win()
    {
        if (won) return;
        won = true;
        Debug.Log("¡Ganaste!");
        onWin.Invoke(); // aquí conectas UI, sonido, cargar escena, etc.
    }
}