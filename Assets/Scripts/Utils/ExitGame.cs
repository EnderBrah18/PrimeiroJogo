using UnityEngine;

public class ExitGame : MonoBehaviour
{
    // Este método pode ser chamado por um botão da UI
    public void QuitGame()
    {
#if UNITY_EDITOR
        // Fecha o modo de Play no Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // Fecha o aplicativo em uma build
            Application.Quit();
#endif
    }
}
