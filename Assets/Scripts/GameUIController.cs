using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private Text speedText;
    [SerializeField] private Text fuelText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Text gameOverScoreText;
    [SerializeField] private Text gameOverHighScoreText;
    [SerializeField] private Slider fuelBar;

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("Assign the GameManager, all UI panels, labels, and fuel bar.", this);
            enabled = false;
            return;
        }

        gameManager.StateChanged += UpdatePanels;
        gameManager.LivesChanged += UpdateLives;
        gameManager.FuelChanged += UpdateFuel;
        gameManager.ScoreChanged += UpdateScore;
        gameManager.HighScoreChanged += UpdateHighScore;
    }

    private void Start()
    {
        UpdateLives(gameManager.Lives);
        UpdateFuel(gameManager.Fuel, gameManager.FuelCapacity);
        UpdateScore(gameManager.Score);
        UpdateHighScore(gameManager.HighScore);
        UpdatePanels(gameManager.State);
    }

    private void Update()
    {
        speedText.text = $"Speed: {Mathf.RoundToInt(gameManager.CurrentSpeed * 3.6f)} km/h";
    }

    private void OnDisable()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.StateChanged -= UpdatePanels;
        gameManager.LivesChanged -= UpdateLives;
        gameManager.FuelChanged -= UpdateFuel;
        gameManager.ScoreChanged -= UpdateScore;
        gameManager.HighScoreChanged -= UpdateHighScore;
    }

    public void StartGame()
    {
        PlayUiClick();
        gameManager.StartGame();
    }

    public void PauseGame()
    {
        PlayUiClick();
        gameManager.PauseGame();
    }

    public void ResumeGame()
    {
        PlayUiClick();
        gameManager.ResumeGame();
    }

    public void RestartGame()
    {
        PlayUiClick();
        gameManager.RestartGame();
    }

    public void ReturnToMainMenu()
    {
        PlayUiClick();
        gameManager.ReturnToMainMenu();
    }

    public void ExitGame()
    {
        PlayUiClick();
#if UNITY_EDITOR
        Debug.Log("Exit Game only closes a built player; it is ignored while running in the Unity Editor.", this);
#endif
        Application.Quit();
    }

    private static void PlayUiClick()
    {
        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayUiClick();
        }
    }

    private bool HasRequiredReferences()
    {
        return gameManager != null
            && mainMenuPanel != null
            && pauseMenuPanel != null
            && gameOverPanel != null
            && hudPanel != null
            && speedText != null
            && fuelText != null
            && livesText != null
            && scoreText != null
            && highScoreText != null
            && gameOverScoreText != null
            && gameOverHighScoreText != null
            && fuelBar != null;
    }

    private void UpdatePanels(GameState state)
    {
        mainMenuPanel.SetActive(state == GameState.Menu);
        pauseMenuPanel.SetActive(state == GameState.Paused);
        gameOverPanel.SetActive(state == GameState.GameOver);
        hudPanel.SetActive(state == GameState.Playing || state == GameState.Paused);
        gameOverScoreText.text = $"Score: {gameManager.Score}";
        gameOverHighScoreText.text = $"High Score: {gameManager.HighScore}";
    }

    private void UpdateLives(int lives)
    {
        livesText.text = $"Lives: {lives}/{gameManager.MaximumLives}";
    }

    private void UpdateFuel(float fuel, float capacity)
    {
        fuelText.text = $"Fuel: {Mathf.CeilToInt(fuel)}/{Mathf.CeilToInt(capacity)}";
        fuelBar.maxValue = capacity;
        fuelBar.value = fuel;
    }

    private void UpdateScore(int score)
    {
        scoreText.text = $"Score: {score}";
        gameOverScoreText.text = $"Score: {score}";
    }

    private void UpdateHighScore(int highScore)
    {
        highScoreText.text = $"High Score: {highScore}";
        gameOverHighScoreText.text = $"High Score: {highScore}";
    }
}
