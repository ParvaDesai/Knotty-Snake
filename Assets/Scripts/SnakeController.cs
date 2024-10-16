using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    public PlayerData PlayerData { get; private set; }
    
    [Header("Body Parts")]
    [SerializeField] private SnakeSegment HeadSegment;
    [SerializeField] private SnakeSegment BodySegment;
    [Tooltip("Initial snake body size")] [Range(0,5)] public int InitSnakeSize = 3;          
    
    [Header("Movement Fields")]
    [Range(1, 20)] public float InitSnakeSpeed = 5f;
    
    private float currSnakeSpeed;
    private float moveTimer;
    private bool isSpeedBoosted = false;
    private Coroutine speedBoostCoroutine;

    private bool isShieldActive = false;
    
    private int score = 0;
    
    private List<SnakeSegment> segments;
    
    private Vector2Int moveDirection;
    private Vector2Int playerPosition;

    #region MonoBehaviour Methods

    void Update()
    {
        if (PlayerData.IsAlive)
        {
            SetInputDirection();
        }
    }

    void Update()
    {
        if (PlayerData.IsAlive)
        {
            SetInputDirection();
            HandleMovement();
            CheckCollisions();
        }
    }

    #endregion

    #region Setup related Methods

    public void Initialize(PlayerData playerData, List<SnakeController> otherPlayers = null)
    {
        SetPlayerData(playerData);
        
        segments = new List<SnakeSegment>();

        switch (PlayerData.PlayerID)
        {
            case 1:
                moveDirection = Vector2Int.right;
                break;
            case 2:
                moveDirection = Vector2Int.left;
                break;
        }
        
        InitializeSnakeBody();
        
        currSnakeSpeed = InitSnakeSpeed;
        moveTimer = 1f / currSnakeSpeed;
    }

    public void SetPlayerData(PlayerData data)
    {
        PlayerData = data;
    }

    private void InitializeSnakeBody()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            Destroy(segments[i].gameObject);
        }

        segments.Clear();

        for (int i = 0; i < InitSnakeSize; i++)
        {
            SnakeSegment segment;
            
            // Head Segment
            if (i == 0)
            {
                segment = Instantiate(HeadSegment.transform, this.transform, true).GetComponent<SnakeSegment>();
                
                segment.SetPosition(Vector2Int.zero);
                playerPosition = segment.GetPosition();
                
                segment.SetColor(PlayerData.Color.HeadColor);
            }
            else
            {
                segment = Instantiate(BodySegment.transform, this.transform, true).GetComponent<SnakeSegment>();
                
                // Set init pos for body segments so that at the start they don't trigger self collisions
                segment.SetPosition(new Vector2Int(playerPosition.x - moveDirection.x, playerPosition.y - moveDirection.y));
                
                segment.SetColor(PlayerData.Color.BodyColor);
            }
            
            segment.SetPlayerID(PlayerData.PlayerID);
            
            segments.Add(segment);
        }
    }

    #endregion

    #region Movement
    
    private void SetInputDirection()
    {
        // Only allow turning up or down while moving in the x-dir
        if (moveDirection.x != 0f)
        {
            if (Input.GetKeyDown(PlayerData.InputKeyBinding.UpKey))
            {
                moveDirection = Vector2Int.up;
            }
            else if (Input.GetKeyDown(PlayerData.InputKeyBinding.DownKey))
            {
                moveDirection = Vector2Int.down;
            }
        }

        // Only allow turning left or right while moving in the y-dir
        else if (moveDirection.y != 0f)
        {
            if (Input.GetKeyDown(PlayerData.InputKeyBinding.LeftKey))
            {
                moveDirection = Vector2Int.left;
            }
            else if (Input.GetKeyDown(PlayerData.InputKeyBinding.RightKey))
            {
                moveDirection = Vector2Int.right;
            }
        }
    }
    
    private void HandleMovement()
    {
        moveTimer -= Time.deltaTime;

        if (moveTimer <= 0f)
        {
            UpdateMovement();
            ScreenWrap();

            moveTimer = 1f / currSnakeSpeed;
        }
    }

    private void UpdateMovement()
    {
        Vector2Int prevPos = segments[0].GetPosition();
        
        // Updating the head position
        playerPosition.x = segments[0].GetPosition().x + moveDirection.x;
        playerPosition.y = segments[0].GetPosition().y + moveDirection.y;
        segments[0].SetPosition(new Vector2Int(playerPosition.x, playerPosition.y));
        
        // Make the snake body move forward, each segment is following the one in front of it 
        for (int i = 1; i < segments.Count; i++)
        {
            Vector2Int currPos = segments[i].GetPosition();
            segments[i].SetPosition(prevPos);
            prevPos = currPos;
        }
    }

    private void ScreenWrap()
    {
        Bounds bounds = GameManager.Instance.GetWrappedAreaBounds();
        
        // screen wrapping in x dir
        if(segments[0].GetPosition().x >= bounds.max.x)
        {
            segments[0].SetPosition(new Vector2Int((int)bounds.min.x, segments[0].GetPosition().y));  
        }
        else if (segments[0].GetPosition().x <= bounds.min.x)
        {
            segments[0].SetPosition(new Vector2Int((int)bounds.max.x, segments[0].GetPosition().y));     
        }
        
        // screen wrapping in y dir
        if (segments[0].GetPosition().y >= bounds.max.y)
        {
            segments[0].SetPosition(new Vector2Int(segments[0].GetPosition().x, (int)bounds.min.y));          
        }
        else if (segments[0].GetPosition().y <= bounds.min.y)
        {
            segments[0].SetPosition(new Vector2Int(segments[0].GetPosition().x, (int)bounds.max.y));          
        }
    }

    #endregion

    #region Collision Detection

    private void CheckCollisions()
    {
        CheckForSelfCollision();
    }

    private void CheckForSelfCollision()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[0].GetPosition() == segments[i].GetPosition())
            {
                if (!isShieldActive)
                {
                    PlayerData.MarkAsDead();
                }
                
                return;
            }
        }
    }
    #region Food & Powerups

    public void OnItemCollection(ICollectible item)
    {
        item.OnCollect(this);
    }

    public void OnSelfCollision()
    {
        if (!isShieldActive)
        {
            PlayerData.MarkAsDead();
        }
    }
    
    public void IncreaseLength(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            SnakeSegment segment = Instantiate(BodySegment.transform, this.transform, true).GetComponent<SnakeSegment>();
            segment.SetPosition(segments[segments.Count - 1].GetPosition());
            segments.Add(segment);
        }
    }

    public void DecreaseLength(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Destroy(segments[segments.Count - 1].gameObject);
            segments.RemoveAt(segments.Count - 1);
        }
    }
    
    public void ActivateShield(float duration)
    {
        StartCoroutine(ShieldCoroutine(duration));
    }

    public void BoostScore(int amount)
    {
        score += amount;
        UIManager.Instance.DisplayScore(score);
    }

    public void SpeedUp(float speedMultiplier, float duration)
    {
        if (isSpeedBoosted && speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
        }
        
        speedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine(speedMultiplier, duration));
    }
    
    private IEnumerator ShieldCoroutine(float duration)
    {
        isShieldActive = true;
        yield return new WaitForSeconds(duration);
        isShieldActive = false;
    }
    
    private IEnumerator SpeedBoostCoroutine(float speedMultiplier, float duration)
    {
        isSpeedBoosted = true;
        currSnakeSpeed *= speedMultiplier;
        
        yield return new WaitForSeconds(duration);

        currSnakeSpeed = InitSnakeSpeed;
        isSpeedBoosted = false;
    }

    #endregion
}