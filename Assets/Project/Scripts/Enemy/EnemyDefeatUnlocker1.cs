using UnityEngine;

public class EnemyDefeatUnlocker1 : MonoBehaviour
{
    [Header("Co se má zapnout po smrti")]
    [SerializeField] private GameObject objectToActivate; 

    private void OnDestroy()
    {
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
            Debug.Log("Nepøítel poražen! Konec levelu byl zpøístupnìn.");
        }
    }
}
