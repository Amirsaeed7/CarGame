using UnityEngine;

public class StationaryCarObstacle : Interactable
{
    public override void Interact(GameObject player)
    {
        base.Interact(player);

        if (GameManager.Instance == null)
        {
            Debug.LogError("A GameManager is required to process obstacle collisions.", this);
            return;
        }

        GameManager.Instance.TakeDamage();
    }
}
