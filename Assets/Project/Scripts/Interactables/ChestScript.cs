using UnityEngine;

public class ChestScript : MonoBehaviour
{
    public int energyAmount = 50;
    private bool isOpen = false;

    private PlayerStats playerInRange;

    private void Update()
    {
        if (playerInRange != null && !isOpen && Input.GetKeyDown(KeyCode.E))
        {
            playerInRange.AddEnergy(energyAmount);
            isOpen = true;
            Destroy(gameObject, 0.01f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = other.GetComponent<PlayerStats>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = null;
        }
    }
}