using UnityEngine;

public class MovingCarObstacle : Interactable
{
    [SerializeField] private float movementSpeed = 2f;

    private void Update()
    {
        transform.position += Vector3.back * (movementSpeed * Time.deltaTime);
    }

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
