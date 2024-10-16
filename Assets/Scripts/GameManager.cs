using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [SerializeField] private BoxCollider2D WrappedArea;
    [SerializeField] private SnakeController snakePrefab;

    [SerializeField] private KeyBinding[] InputKeyBindings;
    [SerializeField] private PlayerColor[] PlayerColors;
    
    private List<Vector2Int> playerStartPositions = new List<Vector2Int>();
    private List<PlayerData> players = new List<PlayerData>();

    private List<PlayerData> alivePlayers = new List<PlayerData>();
    
    public bool IsGameOver { get; private set; }

    #region MonoBehaviour Methods

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        InitializeGame();
    }

    #endregion

    private void InitializeGame()
    {
        int playerCount = 0;
        
        switch (GameSettings.Instance.SelectedGameMode)
        {
            case GameMode.SinglePlayer:
                playerCount = 1;
                break;
            case GameMode.CoOp:
                playerCount = 2;
                break;
        }
        
        SpawnPlayers(playerCount);
    }

    private void SpawnPlayers(int count)
    {
        players.Clear();
        alivePlayers.Clear();
        
        SetInitPlayerPositions(count);
        
        for (int i = 0; i < count; i++)
        {
            GameObject snakeInstance = Instantiate(snakePrefab.gameObject, Vector3.zero, Quaternion.identity);
            snakeInstance.name = $"Player [{i + 1}]";
            PlayerData playerData = new PlayerData(i + 1, InputKeyBindings[i], PlayerColors[i], playerStartPositions[i], snakeInstance.GetComponent<SnakeController>());
            
            players.Add(playerData);
            alivePlayers.Add(playerData);
        }

        if (players.Count > 1)
        {
            for (int i = 0; i < count; i++)
            {
                List<SnakeController> otherPlayers = new List<SnakeController>();
                
                for (int j = 0; j < players.Count; j++)
                {
                    if (i != j)
                    {
                        otherPlayers.Add(players[j].SnakeController);
                    }
                }
                
                players[i].SnakeController.Initialize(players[i], otherPlayers);
            }
        }
        else
        {
            players[0].SnakeController.Initialize(players[0]);
        }
    }
    
    private void SetInitPlayerPositions(int count)
    {
        playerStartPositions.Clear();
        
        Vector3 wrappedCenter = WrappedArea.bounds.center;
        
        if (count == 1)
        {
            Vector2Int pos = new Vector2Int((int)wrappedCenter.x, (int)wrappedCenter.y);
            playerStartPositions.Add(pos);
        }
        else if (count == 2)
        {
            float xOffset = WrappedArea.bounds.extents.x / 2;
            float yOffset = WrappedArea.bounds.extents.y / 2;
            
            Vector2Int pos1 = new Vector2Int((int)(wrappedCenter.x - xOffset), (int)(wrappedCenter.y - yOffset));
            Vector2Int pos2 = new Vector2Int((int)(wrappedCenter.x + xOffset), (int)(wrappedCenter.y + yOffset));
            
            playerStartPositions.Add(pos1);
            playerStartPositions.Add(pos2);
        }
    }

    public void OnPlayerDeath(PlayerData deadPlayerData)
    {
        deadPlayerData.MarkAsDead();
        alivePlayers.Remove(deadPlayerData);
        
        // CheckForWinCondition();
    }

    public void CheckForGameOverCondition()
    {
        if (alivePlayers.Count == 1)
        {
            DeclareWinner(alivePlayers[0]);
        }
        else if (alivePlayers.Count == 0)
        {
            DeclareDraw();
        }
    }

    private void DeclareWinner(PlayerData winner)
    {
        Debug.Log($"Player {winner.PlayerID} wins!");
        IsGameOver = true;
    }

    private void DeclareDraw()
    {
        Debug.Log("It's a draw!");
        IsGameOver = true;
    }

    public Bounds GetWrappedAreaBounds()
    {
        return WrappedArea.bounds;
    }
}
