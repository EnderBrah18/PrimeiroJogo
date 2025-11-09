using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Nome da cena a carregar (deve estar adicionada no Build Settings)
    public string nomeCena;

    public void CarregarCena()
    {
        SceneManager.LoadScene(nomeCena);
    }
}
