using System;
using System.Collections;
using UnityEngine;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    private const int StartingLives = 3;
    private const string HighScoreKey = "HighScore";

    public static GameManager Instance { get; private set; }

    [SerializeField] private PlayerLaneController player;
    [SerializeField] private RoadSegmentPool roadPool;
    [SerializeField] private InteractableSpawner interactableSpawner;
    [SerializeField] private bool startInMenu;
    [SerializeField] private float fuelCapacity = 100f;
    [SerializeField] private float fuelDrainPerSecond = 2f;
    [SerializeField] private float startingSpeed = 12f;
    [SerializeField] private float speedIncreasePerDistance = 0.03f;
    [SerializeField] private float distanceToMaximumDifficulty = 600f;
    [SerializeField] private float distancePerScorePoint = 5f;
    [SerializeField] private float minimumObstacleSpawnInterval = 1.2f;
    [SerializeField, Range(0f, 1f)] private float maximumObstacleSpawnChance = 0.8f;
    [SerializeField] private float collisionFlashDuration = 0.6f;
    [SerializeField] private GameState state = GameState.Menu;

    private Renderer[] playerRenderers;
    private Material[] playerMaterials;
    private Color[] originalPlayerColors;
    private Color[] originalPlayerEmissionColors;
    private bool[] originalPlayerEmissionEnabled;
    private Coroutine flashCoroutine;
    private Vector3 startingPlayerPosition;
    [SerializeField] private float currentFuel;
    [SerializeField] private float distanceTravelled;
    private int distanceScoreAwarded;
    [SerializeField] private int lives;
    [SerializeField] private int score;
    [SerializeField] private int highScore;

    public GameState State => state;
    public bool IsPlaying => State == GameState.Playing;
    public int Lives => lives;
    public int MaximumLives => StartingLives;
    public float Fuel => currentFuel;
    public float FuelCapacity => fuelCapacity;
    public int Score => score;
    public int HighScore => highScore;
    public float CurrentSpeed => player != null ? player.ForwardSpeed : 0f;
    public float DistanceTravelled => distanceTravelled;

    public event Action<GameState> StateChanged;
    public event Action<int> LivesChanged;
    public event Action DamageTaken;
    public event Action<float, float> FuelChanged;
    public event Action<int> ScoreChanged;
    public event Action<int> HighScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void Start()
    {
        if (player == null
            || roadPool == null
            || interactableSpawner == null
            || fuelCapacity <= 0f
            || fuelDrainPerSecond < 0f
            || startingSpeed <= 0f
            || speedIncreasePerDistance <= 0f
            || distanceToMaximumDifficulty <= 0f
            || distancePerScorePoint <= 0f
            || minimumObstacleSpawnInterval <= 0f)
        {
            Debug.LogError("Assign the player, road pool, and spawner, and configure valid gameplay values.", this);
            enabled = false;
            return;
        }

        startingPlayerPosition = player.transform.position;
        RefreshPlayerRenderers();

        if (playerRenderers.Length == 0)
        {
            Debug.LogWarning("No player Renderer was found; collision color feedback will be unavailable.", this);
        }

        if (startInMenu)
        {
            ResetRun();
            SetState(GameState.Menu);
        }
        else
        {
            StartGame();
        }
    }

    public void RefreshPlayerRenderers()
    {
        if (player == null)
        {
            Debug.LogError("Cannot refresh player visuals because no player is assigned.", this);
            return;
        }

        playerRenderers = player.GetComponentsInChildren<Renderer>();
        playerMaterials = new Material[playerRenderers.Length];
        originalPlayerColors = new Color[playerRenderers.Length];
        originalPlayerEmissionColors = new Color[playerRenderers.Length];
        originalPlayerEmissionEnabled = new bool[playerRenderers.Length];
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            playerMaterials[i] = playerRenderers[i].material;
            originalPlayerColors[i] = playerMaterials[i].color;
            if (playerMaterials[i].HasProperty("_EmissionColor"))
            {
                originalPlayerEmissionColors[i] = playerMaterials[i].GetColor("_EmissionColor");
                originalPlayerEmissionEnabled[i] = playerMaterials[i].IsKeywordEnabled("_EMISSION");
            }
        }
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        currentFuel = Mathf.Max(0f, currentFuel - fuelDrainPerSecond * Time.deltaTime);
        FuelChanged?.Invoke(currentFuel, fuelCapacity);

        if (currentFuel <= 0f)
        {
            EndGame();
            return;
        }

        UpdateDifficulty();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartGame()
    {
        if (player == null || roadPool == null || interactableSpawner == null)
        {
            Debug.LogError("GameManager cannot start until its player, road pool, and spawner are assigned.", this);
            return;
        }

        ResetRun();
        SetState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (IsPlaying)
        {
            SetState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (State == GameState.Paused)
        {
            SetState(GameState.Playing);
        }
    }

    public void RestartGame()
    {
        StartGame();
    }

    public void ReturnToMainMenu()
    {
        SetState(GameState.Menu);
    }

    public void TakeDamage()
    {
        if (!IsPlaying)
        {
            return;
        }

        lives = Mathf.Max(0, lives - 1);
        LivesChanged?.Invoke(lives);
        DamageTaken?.Invoke();
        FlashPlayer();

        if (lives == 0)
        {
            EndGame();
        }
    }

    public void AddScore(int amount)
    {
        if (!IsPlaying || amount <= 0)
        {
            return;
        }

        score += amount;
        ScoreChanged?.Invoke(score);

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
            HighScoreChanged?.Invoke(highScore);
        }
    }

    public void Refuel(float amount)
    {
        if (!IsPlaying || amount <= 0f)
        {
            return;
        }

        currentFuel = Mathf.Min(fuelCapacity, currentFuel + amount);
        FuelChanged?.Invoke(currentFuel, fuelCapacity);
    }

    private void ResetRun()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
            RestorePlayerColors();
        }

        lives = StartingLives;
        currentFuel = fuelCapacity;
        score = 0;
        distanceTravelled = 0f;
        distanceScoreAwarded = 0;

        player.ResetForNewRun(startingPlayerPosition, startingSpeed);
        roadPool.ResetForNewRun();
        interactableSpawner.ResetForNewRun();
        LivesChanged?.Invoke(lives);
        FuelChanged?.Invoke(currentFuel, fuelCapacity);
        ScoreChanged?.Invoke(score);
        HighScoreChanged?.Invoke(highScore);
    }

    private void UpdateDifficulty()
    {
        distanceTravelled = Mathf.Max(0f, player.transform.position.z - startingPlayerPosition.z);
        int distanceScore = Mathf.FloorToInt(distanceTravelled / distancePerScorePoint);
        if (distanceScore > distanceScoreAwarded)
        {
            AddScore(distanceScore - distanceScoreAwarded);
            distanceScoreAwarded = distanceScore;
        }

        float progress = Mathf.Clamp01(distanceTravelled / distanceToMaximumDifficulty);
        float speed = startingSpeed + distanceTravelled * speedIncreasePerDistance;
        float spawnInterval = Mathf.Lerp(
            interactableSpawner.BaseSpawnInterval,
            minimumObstacleSpawnInterval,
            progress);
        float obstacleChance = Mathf.Lerp(
            interactableSpawner.BaseObstacleSpawnChance,
            maximumObstacleSpawnChance,
            progress);

        player.SetForwardSpeed(speed);
        interactableSpawner.SetDifficulty(spawnInterval, obstacleChance);
    }

    private void EndGame()
    {
        SetState(GameState.GameOver);
        Debug.Log($"Game Over. Score: {score}, High Score: {highScore}.", this);
    }

    private void SetState(GameState newState)
    {
        state = newState;
        Time.timeScale = newState == GameState.Playing ? 1f : 0f;
        StateChanged?.Invoke(state);
    }

    private void FlashPlayer()
    {
        if (playerRenderers == null || playerRenderers.Length == 0)
        {
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashPlayerRoutine());
    }

    private IEnumerator FlashPlayerRoutine()
    {
        float elapsed = 0f;
        const float flashInterval = 0.06f;

        while (elapsed < collisionFlashDuration)
        {
            bool flashRed = Mathf.FloorToInt(elapsed / flashInterval) % 2 == 0;
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null && playerMaterials[i] != null)
                {
                    SetPlayerFlash(i, flashRed);
                }
            }

            yield return new WaitForSecondsRealtime(flashInterval);
            elapsed += flashInterval;
        }

        RestorePlayerColors();
        flashCoroutine = null;
    }

    private void RestorePlayerColors()
    {
        if (playerRenderers == null || originalPlayerColors == null)
        {
            return;
        }

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null && playerMaterials[i] != null)
            {
                SetPlayerFlash(i, false);
            }
        }
    }

    private void SetPlayerFlash(int rendererIndex, bool flashRed)
    {
        Material material = playerMaterials[rendererIndex];
        material.color = flashRed ? Color.red : originalPlayerColors[rendererIndex];

        if (!material.HasProperty("_EmissionColor"))
        {
            return;
        }

        if (flashRed)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.red * 2f);
        }
        else
        {
            material.SetColor("_EmissionColor", originalPlayerEmissionColors[rendererIndex]);
            if (!originalPlayerEmissionEnabled[rendererIndex])
            {
                material.DisableKeyword("_EMISSION");
            }
        }
    }
}
