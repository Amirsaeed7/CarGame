using UnityEngine;

public class FuelCanCollectible : Interactable
{
    public override void Interact(GameObject player)
    {
        base.Interact(player);

        if (GameManager.Instance == null)
        {
            Debug.LogError("A GameManager is required to process fuel collections.", this);
            return;
        }

        GameManager.Instance.Refuel(30f);
        if (GameManager.Instance.IsPlaying && GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayFuelPickup();
        }

        gameObject.SetActive(false);
    }
}
