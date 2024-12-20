using UnityEngine;
using System.Collections;

public class FireManager : MonoBehaviour
{
    [Header("Fire Settings")]
    [SerializeField] private ParticleSystem fireParticleSystem;
    [SerializeField] private float extinguishDelay = 1f;
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float damageInterval = 0.5f;
    [SerializeField] private float waterNeeded = 3f;
    [SerializeField] private float treeDestructionTime = 40f;
    [SerializeField] private CoinManager coinManager;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem steamEffect;
    
    [Header("Scaling Settings")]
    [SerializeField] private float baseEmissionRate = 40f;
    [SerializeField] private float maxEmissionMultiplier = 2f;
    [SerializeField] private float baseDamageAmount = 10f;
    [SerializeField] private float maxDamageMultiplier = 2f;
    
    private bool isExtinguishing = false;
    private bool isExtinguished = false;
    private FirePillReward pillReward;
    private float nextDamageTime = 0f;
    private float waterAccumulated = 0f;
    private GameManager gameManager;
    private Transform parentTree;

    private void Start()
    {
        // Store base damage amount
        baseDamageAmount = damageAmount;
        
        // Get references
        if (fireParticleSystem == null)
        {
            fireParticleSystem = GetComponent<ParticleSystem>();
        }
        
        pillReward = GetComponent<FirePillReward>();
        gameManager = FindObjectOfType<GameManager>();
        parentTree = transform.parent;
        coinManager = CoinManager.Instance;
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found in the scene!");
        }

        // Make sure fire is playing
        if (fireParticleSystem != null && !fireParticleSystem.isPlaying)
        {
            fireParticleSystem.Play();
        }

        // Start the tree destruction timer
        StartCoroutine(TreeDestructionTimer());
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger Enter with: {other.gameObject.name}, Tag: {other.tag}");
    }

    private void OnTriggerStay(Collider other)
    {
        if (isExtinguished)
        {
            Debug.Log("Fire is extinguished, no damage");
            HUD.Instance.PopUpText("Fire is extinguished, no damage", 2);
            return;
        }

        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player detected, Time: {Time.time}, Next damage time: {nextDamageTime}");
            
            if (Time.time >= nextDamageTime)
            {
                if (gameManager != null)
                {
                    gameManager.DamagePlayer(damageAmount);
                    nextDamageTime = Time.time + damageInterval;
                    Debug.Log($"Fire damaged player for {damageAmount} damage");
                }
                else
                {
                    Debug.LogError("GameManager is null when trying to damage player");
                }
            }
        }
    }

    public void ApplyWater(float amount)
    {
        if (isExtinguished || isExtinguishing) return;
        
        // Accumulate water
        waterAccumulated += amount;
        Debug.Log($"Water accumulated: {waterAccumulated}/{waterNeeded}");
        
        // Play steam effect when water hits fire
        if (steamEffect != null && !steamEffect.isPlaying)
        {
            steamEffect.Play();
        }

        // Start extinguishing when enough water is accumulated
        if (waterAccumulated >= waterNeeded)
        {
            StartCoroutine(ExtinguishFireRoutine());
        }
    }

    private IEnumerator ExtinguishFireRoutine()
    {
        isExtinguishing = true;
        
        // Gradually reduce particle emission
        if (fireParticleSystem != null)
        {
            var emission = fireParticleSystem.emission;
            var startRate = emission.rateOverTime.constant;
            float elapsedTime = 0f;
            
            while (elapsedTime < extinguishDelay)
            {
                float newRate = Mathf.Lerp(startRate, 0f, elapsedTime / extinguishDelay);
                emission.rateOverTime = newRate;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            ExtinguishFire();
        }
    }

    private void ExtinguishFire()
    {
        isExtinguished = true;
        Debug.Log("Fire extinguished - Checking for pill reward");

        if (coinManager != null) {
            coinManager.AddCoins(4);
        }

        // Stop fire particles
        if (fireParticleSystem != null)
        {
            fireParticleSystem.Stop();
            var emission = fireParticleSystem.emission;
            emission.rateOverTime = 0;
        }

        // Spawn reward if available
        if (pillReward != null)
        {
            Debug.Log("FirePillReward component found - Calling SpawnReward()");
            pillReward.SpawnReward();
        }
        else
        {
            Debug.LogWarning("No FirePillReward component found on fire object!");
        }

        // Destroy the fire object after a delay
        Destroy(gameObject, 2f);
    }

    private IEnumerator TreeDestructionTimer()
    {
        yield return new WaitForSeconds(treeDestructionTime);
        
        if (!isExtinguished && parentTree != null)
        {
            // Get the TreeReference component from the parent tree
            TreeReference treeRef = parentTree.GetComponent<TreeReference>();
            if (treeRef != null)
            {
                // Remove the tree from the terrain
                treeRef.RemoveTree();
                
                // Damage the world when a tree is destroyed
                if (gameManager != null)
                {
                    gameManager.DamageWorld(0.5f); // You can adjust this value as needed
                }
            }
            
            // Destroy the fire object
            Destroy(gameObject);
        }
    }

    private void UpdateFireIntensity()
    {
        if (gameManager == null || fireParticleSystem == null) return;
        
        // Get current world health percentage (0-1)
        float worldHealthPercent = gameManager.GetWorldHealthPercentage();
        
        // Calculate multipliers based on world health
        float emissionMultiplier = 1f + (worldHealthPercent * (maxEmissionMultiplier - 1f));
        float damageMultiplier = 1f + (worldHealthPercent * (maxDamageMultiplier - 1f));
        
        // Update particle emission
        var emission = fireParticleSystem.emission;
        emission.rateOverTime = baseEmissionRate * emissionMultiplier;
        
        // Update damage amount
        damageAmount = baseDamageAmount * damageMultiplier;
    }

    private void Update()
    {
        if (!isExtinguished)
        {
            UpdateFireIntensity();
        }
    }
} 