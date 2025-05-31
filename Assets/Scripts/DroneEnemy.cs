using UnityEngine;
using System.Collections;

public class DroneEnemy : MonoBehaviour
{
    [Header("Drone Settings")]
    public float moveSpeed = 5f;
    public float detectionRange = 10f;
    public float shootRange = 8f;
    public float shootInterval = 2f;
    public int maxHealth = 3;
    
    [Header("Chase Behavior")]
    public float maxChaseDistance = 20f; // Stop chasing beyond this distance
    public float chaseTimeout = 10f; // Stop chasing after this many seconds
    public float returnToPatrolDelay = 2f; // Wait before returning to patrol
    
    [Header("Movement")]
    public float patrolRadius = 15f;
    public float hoverHeight = 5f;
    
    [Header("Laser Settings")]
    public GameObject laserPrefab;
    public Transform laserSpawnPoint;
    public LineRenderer laserBeam;
    public float laserDuration = 0.5f;
    public LayerMask obstacleLayer = 1;
    
    private Transform player;
    private Vector3 patrolCenter;
    private Vector3 targetPosition;
    private float lastShootTime;
    private int currentHealth;
    private bool isDead = false;
    
    // Chase tracking
    private float chaseStartTime;
    private Vector3 lastKnownPlayerPosition;
    
    // States
    private enum DroneState { Patrolling, Chasing, Attacking, Returning }
    private DroneState currentState = DroneState.Patrolling;
    
    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("Drone found player: " + player.name);
        }
        else
        {
            Debug.LogError("No GameObject with 'Player' tag found!");
        }
        
        // Set patrol center
        patrolCenter = transform.position;
        
        // Initialize health
        currentHealth = maxHealth;
        
        // Set random patrol target
        SetRandomPatrolTarget();
        
        // Setup laser beam
        if (laserBeam == null)
            laserBeam = GetComponent<LineRenderer>();
        
        if (laserBeam != null)
        {
            laserBeam.enabled = false;
            laserBeam.startWidth = 0.1f;
            laserBeam.endWidth = 0.1f;
        }
    }
    
    void Update()
    {
        if (isDead) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // State machine
        switch (currentState)
        {
            case DroneState.Patrolling:
                Patrol();
                if (distanceToPlayer <= detectionRange)
                {
                    StartChase();
                }
                break;
                
            case DroneState.Chasing:
                ChasePlayer();
                
                // Check if should stop chasing
                if (ShouldStopChasing(distanceToPlayer))
                {
                    Debug.Log("Drone stopped chasing - player escaped!");
                    currentState = DroneState.Returning;
                }
                else if (distanceToPlayer <= shootRange && CanSeePlayer())
                {
                    currentState = DroneState.Attacking;
                }
                break;
                
            case DroneState.Attacking:
                AttackPlayer();
                
                // Check if should stop chasing while attacking
                if (ShouldStopChasing(distanceToPlayer))
                {
                    Debug.Log("Drone stopped attacking - player escaped!");
                    currentState = DroneState.Returning;
                }
                else if (distanceToPlayer > shootRange || !CanSeePlayer())
                {
                    currentState = DroneState.Chasing;
                }
                break;
                
            case DroneState.Returning:
                ReturnToPatrol();
                break;
        }
        
        // Face movement direction
        if (currentState != DroneState.Attacking)
            FaceMovementDirection();
        else
            FacePlayer();
    }
    
    void Patrol()
    {
        // Check if path to patrol target is clear
        Vector3 direction = (targetPosition - transform.position).normalized;
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position, direction, out hit, Vector3.Distance(transform.position, targetPosition), obstacleLayer))
        {
            // Obstacle detected, find new patrol target
            SetRandomPatrolTarget();
            return;
        }
        
        // Move towards patrol target
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        
        // If reached target, set new random target
        if (Vector3.Distance(transform.position, targetPosition) < 1f)
        {
            SetRandomPatrolTarget();
        }
    }
    
    void ChasePlayer()
    {
        Vector3 chaseTarget = player.position + Vector3.up * hoverHeight;
        
        // Check if path to player is clear
        Vector3 direction = (chaseTarget - transform.position).normalized;
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position, direction, out hit, Vector3.Distance(transform.position, chaseTarget), obstacleLayer))
        {
            // Obstacle detected, try to go around it
            Vector3 avoidanceDirection = Vector3.Cross(direction, Vector3.up).normalized;
            chaseTarget = transform.position + avoidanceDirection * 2f + Vector3.up * hoverHeight;
        }
        
        transform.position = Vector3.MoveTowards(transform.position, chaseTarget, moveSpeed * 1.2f * Time.deltaTime);
    }
    
    void AttackPlayer()
    {
        // Hover in place and shoot
        if (Time.time - lastShootTime >= shootInterval)
        {
            ShootLaser();
            lastShootTime = Time.time;
        }
    }
    
    void SetRandomPatrolTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        targetPosition = patrolCenter + new Vector3(randomCircle.x, hoverHeight, randomCircle.y);
    }
    
    bool CanSeePlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position, dirToPlayer, out hit, shootRange, obstacleLayer))
        {
            return hit.collider.CompareTag("Player");
        }
        
        return Vector3.Distance(transform.position, player.position) <= shootRange;
    }
    
    void ShootLaser()
    {
        StartCoroutine(FireLaserBeam());
    }
    
    IEnumerator FireLaserBeam()
    {
        // Perform raycast to check if laser actually hits player
        Vector3 laserDirection = (player.position - laserSpawnPoint.position).normalized;
        RaycastHit hit;
        bool hitPlayer = false;
        Vector3 laserEndPoint = player.position;
        
        if (Physics.Raycast(laserSpawnPoint.position, laserDirection, out hit, shootRange))
        {
            laserEndPoint = hit.point;
            if (hit.collider.CompareTag("Player"))
            {
                hitPlayer = true;
                Debug.Log("Laser hit player!");
            }
            else
            {
                Debug.Log("Laser hit " + hit.collider.name + " instead of player");
            }
        }
        
        // Enable laser beam with correct end point
        if (laserBeam != null)
        {
            laserBeam.enabled = true;
            laserBeam.SetPosition(0, laserSpawnPoint.position);
            laserBeam.SetPosition(1, laserEndPoint);
        }
        
        // Spawn laser projectile if prefab exists
        if (laserPrefab != null)
        {
            GameObject laser = Instantiate(laserPrefab, laserSpawnPoint.position, 
                Quaternion.LookRotation(laserDirection));
        }
        
        // Only damage player if laser actually hit them
        if (hitPlayer)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }
            else
            {
                Debug.LogError("PlayerHealth component not found on player GameObject!");
            }
        }
        
        // Keep laser visible for duration
        yield return new WaitForSeconds(laserDuration);
        
        // Disable laser beam
        if (laserBeam != null)
            laserBeam.enabled = false;
    }
    
    void FaceMovementDirection()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);
    }
    
    void StartChase()
    {
        currentState = DroneState.Chasing;
        chaseStartTime = Time.time;
        lastKnownPlayerPosition = player.position;
        Debug.Log("Drone started chasing player!");
    }
    
    bool ShouldStopChasing(float distanceToPlayer)
    {
        // Stop if player is too far from patrol center
        float distanceFromHome = Vector3.Distance(transform.position, patrolCenter);
        if (distanceFromHome > maxChaseDistance)
        {
            Debug.Log("Drone too far from patrol area");
            return true;
        }
        
        // Stop if been chasing too long
        if (Time.time - chaseStartTime > chaseTimeout)
        {
            Debug.Log("Drone chase timeout");
            return true;
        }
        
        // Stop if player is very far away (lost them)
        if (distanceToPlayer > detectionRange * 2.5f)
        {
            Debug.Log("Player too far away");
            return true;
        }
        
        return false;
    }
    
    void ReturnToPatrol()
    {
        // Move back towards patrol center
        Vector3 returnTarget = patrolCenter + Vector3.up * hoverHeight;
        transform.position = Vector3.MoveTowards(transform.position, returnTarget, moveSpeed * Time.deltaTime);
        
        // If close enough to patrol center, resume patrolling
        if (Vector3.Distance(transform.position, returnTarget) < 2f)
        {
            Debug.Log("Drone returned to patrol");
            currentState = DroneState.Patrolling;
            SetRandomPatrolTarget();
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        isDead = true;
        
        // Add death effects here (explosion, sound, etc.)
        
        // Destroy after delay
        Destroy(gameObject, 1f);
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw shoot range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRange);
        
        // Draw patrol area
        Gizmos.color = Color.blue;
        Vector3 center = Application.isPlaying ? patrolCenter : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);
    }
}

// Simple laser projectile (optional - for visual effect)
public class LaserProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 3f;
    
    void Start()
    {
        Destroy(gameObject, lifetime);
    }
    
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}