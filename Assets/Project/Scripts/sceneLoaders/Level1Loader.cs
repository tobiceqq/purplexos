using UnityEngine;
using UnityEngine.SceneManagement;

public class Level1Loader : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.LoadScene("Level1");
    }
}
