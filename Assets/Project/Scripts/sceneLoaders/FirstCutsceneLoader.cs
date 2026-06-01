using UnityEngine;
using UnityEngine.SceneManagement;

public class FirstCutsceneLoader : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.LoadScene("firstCutscene");
    }
}
