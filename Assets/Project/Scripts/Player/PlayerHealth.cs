using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
public class PlayerHealth : MonoBehaviour
{
    [Header("Nastavení Zdraví")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Reference")]
    public Image healthBarFill;

    [Header("Efekt Poškození (Blikání)")]
    [SerializeField] private Image damageScreenTint; 
    [SerializeField] private Color damageColor = new Color(1f, 0f, 0f, 0.4f); 
    [SerializeField] private float flashDuration = 0.3f; 

    private Coroutine damageFlashCoroutine;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        if (damageScreenTint != null)
        {
            damageScreenTint.gameObject.SetActive(false);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        if (currentHealth > 0)
        {
            if (damageFlashCoroutine != null) StopCoroutine(damageFlashCoroutine);

            damageFlashCoroutine = StartCoroutine(FlashDamageScreenRoutine());
        }

        if (currentHealth <= 0)
        {
            GameOver();
        }
    }

    private IEnumerator FlashDamageScreenRoutine()
    {
        if (damageScreenTint == null) yield break;

        damageScreenTint.gameObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / flashDuration;

            damageScreenTint.color = Color.Lerp(damageColor, new Color(damageColor.r, damageColor.g, damageColor.b, 0f), normalizedTime);

            yield return null;
        }

        damageScreenTint.gameObject.SetActive(false);
    }

    void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }
    }

    void GameOver()
    {
        Debug.Log("Purplex byl znièen!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}