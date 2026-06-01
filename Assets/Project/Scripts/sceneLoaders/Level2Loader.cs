using UnityEngine;
using UnityEngine.SceneManagement;

public class Level2Loader : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.LoadScene("Level2");
    }
}
