using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Interact(other.gameObject);
        }
    }

    public virtual void Interact(GameObject player)
    {
        Debug.Log($"{name} interacted with {player.name}.", this);
    }
}
