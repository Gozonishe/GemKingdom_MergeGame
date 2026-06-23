using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    [Header("Core Systems")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private UIManager uiManager;

    [Header("Prototype Settings")]
    [SerializeField] private bool initializeOnStart = true;

    public BoardManager BoardManager => boardManager;
    public ItemSpawner ItemSpawner => itemSpawner;
    public UIManager UIManager => uiManager;

    private void Start()
    {
        if (!initializeOnStart)
        {
            return;
        }

        InitializeGame();
    }

    public void InitializeGame()
    {
        // Entry point for the Merge-2 prototype startup flow.
        boardManager?.InitializeBoard();
        uiManager?.ShowGameplayUI();
    }

    public void RestartGame()
    {
        InitializeGame();
    }
}
