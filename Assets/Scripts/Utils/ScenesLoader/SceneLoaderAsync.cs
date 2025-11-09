using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoaderAsync : MonoBehaviour
{
    public string nomeCena;
    public GameObject loadingScreen;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CarregarCenaAsync();
        }
    }
    public void CarregarCenaAsync()
    {
        StartCoroutine(CarregarCenaCoroutine());
    }

    IEnumerator CarregarCenaCoroutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nomeCena);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            loadingScreen.SetActive(true);
            Debug.Log("Progresso: " + (asyncLoad.progress * 100f) + "%");

            // Ativa a cena quando estiver pronta (ou depois de uma condição sua)
            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
