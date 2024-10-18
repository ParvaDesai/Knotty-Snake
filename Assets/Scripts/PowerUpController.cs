using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpController : MonoBehaviour
{
    [SerializeField] private Collider2D SpawningArea;
    [SerializeField] private List<PowerUp> PowerUpObjects;
    [SerializeField] private float spawnRate;
    
    private ItemSpawner<PowerUp> itemSpawner;
    
    void Start()
    {
        itemSpawner = new ItemSpawner<PowerUp>(SpawningArea, this.transform, PowerUpObjects, spawnRate);
        
        itemSpawner.StartSpawning();
    }
    
    void Update()
    {
        if (GameManager.Instance.IsGameOver)
            return;
        
        itemSpawner.UpdateSpawner();
    }
    
    public List<PowerUp> GetItems()
    {
        return itemSpawner.GetActiveItems();
    }
}