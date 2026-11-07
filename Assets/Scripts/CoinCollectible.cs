using UnityEngine;

public class CoinCollectible : Interactable
{
    public override void Interact(GameObject player)
    {
        base.Interact(player);

        if (GameManager.Instance == null)
        {
            Debug.LogError("A GameManager is required to process coin collections.", this);
            return;
        }

        GameManager.Instance.AddScore(10);
        if (GameManager.Instance.IsPlaying && GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayCoinPickup();
        }

        gameObject.SetActive(false);
    }
}
