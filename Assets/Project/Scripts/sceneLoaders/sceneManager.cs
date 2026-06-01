using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneManager : MonoBehaviour
{

    public void LoadIntro()
    {
        SceneManager.LoadScene("intro");

    }

    public void LoadLevel1()
    {
        SceneManager.LoadScene("intro");

    }
    public void LoadCutsceneLevel1()
    {
        SceneManager.LoadScene("CutsceneLevel1");
    }


    public void LoadLevel2()
    {
        SceneManager.LoadScene("Level2");

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LoadLevel2();
        }
    }




}
