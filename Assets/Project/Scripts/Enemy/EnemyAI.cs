using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI; // NUTNÉ pro Image/Slider

public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack }
    public State currentState = State.Patrol;

    public enum PatrolMode { Waypoints, RandomNavMesh }

    [Header("Patrol Mode")]
    public PatrolMode patrolMode = PatrolMode.Waypoints;
    public bool randomWaypointOrder = false;
    public float randomPatrolRadius = 10f;

    [Header("References")]
    public Transform[] patrolPoints;
    public Transform player;

    [Header("Drops & Effects")]
    public GameObject explosionPrefab;
    public GameObject healPrefab;

    [Header("Movement Settings")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Attack Settings")]
    public float attackDistance = 1.5f;
    public float attackCooldown = 1f;
    public float attackDamage = 20f;

    [Header("Enemy Stats")]
    public float enemyHealth = 50f;
    private float maxEnemyHealth;

    [Header("UI Reference")]
    public Image healthBarFill;

    [Header("Detection Settings")]
    public float chaseDistance = 8f;
    public float viewDistance = 10f;
    [Range(0, 360)]
    public float viewAngle = 90f;
    public LayerMask obstacleMask;

    [Header("State Materials")]
    public Material patrolMaterial;
    public Material chaseMaterial;
    public Material attackMaterial;

    private Renderer rend;
    private NavMeshAgent agent;
    private int patrolIndex = 0;
    private float attackTimer = 0f;
    private Animator anim;
    private Renderer[] childRenderers;
    public float flashDuration = 0.15f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        childRenderers = GetComponentsInChildren<Renderer>();

        rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();

        maxEnemyHealth = enemyHealth;
        UpdateHealthUI();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        switch (currentState)
        {
            case State.Patrol: Patrol(); break;
            case State.Chase: Chase(); break;
            case State.Attack: Attack(); break;
        }

        attackTimer -= Time.deltaTime;

        if (anim != null && agent != null)
        {
            anim.SetBool("isRunning", agent.velocity.magnitude > 0.1f);
        }
    }


    public void TakeDamage(float amount)
    {
        enemyHealth -= amount;
        enemyHealth = Mathf.Clamp(enemyHealth, 0, maxEnemyHealth);

        UpdateHealthUI();

        Debug.Log("Nepøítel dostal zásah! HP: " + enemyHealth);

        foreach (Renderer r in childRenderers)
        {
            StartCoroutine(FlashWithPropertyBlock(r));
        }

        if (enemyHealth <= 0) Die();
    }

    void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = enemyHealth / maxEnemyHealth;
        }
    }


    void Patrol()
    {
        if (rend != null && patrolMaterial != null) rend.material = patrolMaterial;
        agent.speed = patrolSpeed;

        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            if (patrolMode == PatrolMode.Waypoints && patrolPoints.Length > 0)
            {
                patrolIndex = randomWaypointOrder ? Random.Range(0, patrolPoints.Length) : (patrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[patrolIndex].position);
            }
            else if (patrolMode == PatrolMode.RandomNavMesh)
            {
                agent.SetDestination(GetRandomNavMeshPosition());
            }
        }

        if (ShouldStartChasing()) currentState = State.Chase;
    }

    void Chase()
    {
        if (rend != null && chaseMaterial != null) rend.material = chaseMaterial;
        agent.speed = chaseSpeed;

        if (!PlayerInChaseRange() && !PlayerInViewRange())
        {
            currentState = State.Patrol;
            return;
        }

        agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) <= attackDistance)
            currentState = State.Attack;
    }

    void Attack()
    {
        if (rend != null && attackMaterial != null) rend.material = attackMaterial;
        agent.ResetPath();

        if (Vector3.Distance(transform.position, player.position) > attackDistance)
        {
            currentState = State.Chase;
            return;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);

        if (attackTimer <= 0f)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(attackDamage);
            attackTimer = attackCooldown;
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController pc = Object.FindFirstObjectByType<PlayerController>();

            if (pc != null && pc.IsBallMode)
            {
                float damage = pc.isDashing ? 50f : 25f;
                TakeDamage(damage);

                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 knockbackDir = (transform.position - collision.transform.position).normalized;
                    knockbackDir.y = 0.5f;
                    rb.AddForce(knockbackDir * 15f, ForceMode.Impulse);
                }
                StartCoroutine(StunEnemy());
            }
        }
    }

    IEnumerator StunEnemy()
    {
        if (agent.isActiveAndEnabled) agent.isStopped = true;
        yield return new WaitForSeconds(0.5f);
        if (agent.isActiveAndEnabled) agent.isStopped = false;
    }

    IEnumerator FlashWithPropertyBlock(Renderer r)
    {
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        r.GetPropertyBlock(propBlock);
        propBlock.SetColor("_BaseColor", Color.red);
        propBlock.SetColor("_Color", Color.red);
        r.SetPropertyBlock(propBlock);

        yield return new WaitForSeconds(flashDuration);

        r.GetPropertyBlock(propBlock);
        propBlock.Clear();
        r.SetPropertyBlock(propBlock);
    }

    void Die()
    {
        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        if (healPrefab != null) Instantiate(healPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        Destroy(gameObject);
    }


    bool PlayerInChaseRange()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        return dist <= chaseDistance;
    }

    bool PlayerInViewRange()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dirToPlayer) > viewAngle / 2f) return false;
        return !Physics.Raycast(transform.position, dirToPlayer, dist, obstacleMask);
    }

    bool ShouldStartChasing() => PlayerInChaseRange() || PlayerInViewRange();

    Vector3 GetRandomNavMeshPosition()
    {
        Vector3 randomDirection = Random.insideUnitSphere * randomPatrolRadius + transform.position;
        NavMeshHit hit;
        return NavMesh.SamplePosition(randomDirection, out hit, randomPatrolRadius, NavMesh.AllAreas) ? hit.position : transform.position;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, chaseDistance);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}