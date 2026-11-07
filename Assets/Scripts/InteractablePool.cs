using System.Collections.Generic;
using UnityEngine;

public class InteractablePool : MonoBehaviour
{
    [SerializeField] private Interactable prefab;
    [SerializeField] private int initialSize = 5;

    private List<Interactable> items;

    private void Awake()
    {
        if (prefab == null || initialSize < 1)
        {
            Debug.LogError("Assign an interactable prefab and set an initial pool size of at least one.", this);
            enabled = false;
            return;
        }

        items = new List<Interactable>(initialSize);

        for (int i = 0; i < initialSize; i++)
        {
            Interactable item = Instantiate(prefab, transform);
            item.name = $"{prefab.name}_{i}";
            item.gameObject.SetActive(false);
            items.Add(item);
        }
    }

    public Interactable Get()
    {
        if (items == null)
        {
            Debug.LogError("This interactable pool was not initialized.", this);
            return null;
        }

        foreach (Interactable item in items)
        {
            if (!item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                return item;
            }
        }

        Debug.LogWarning($"{name} is out of pooled objects.", this);
        return null;
    }
}
