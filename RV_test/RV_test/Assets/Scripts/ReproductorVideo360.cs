using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class ReproductorVideo360 : MonoBehaviour
{
    public VideoPlayer videoPlayer; 
    public string escenaSiguiente = "Backrooms";  

    void Start()
    {
        videoPlayer.prepareCompleted += _ => videoPlayer.Play();
        videoPlayer.loopPointReached += _ => SceneManager.LoadScene(escenaSiguiente);
        videoPlayer.Prepare();
    }
}