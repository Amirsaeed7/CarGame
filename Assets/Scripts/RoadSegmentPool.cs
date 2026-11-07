using UnityEngine;

public class RoadSegmentPool : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameObject segmentPrefab;
    [SerializeField] private float segmentLength = 30f;
    [SerializeField] private int pooledSegments = 5;
    [SerializeField] private float recycleBehindDistance = 45f;

    private Transform[] segments;

    private void Start()
    {
        if (player == null
            || segmentPrefab == null
            || segmentLength <= 0f
            || pooledSegments < 2
            || recycleBehindDistance < 0f)
        {
            Debug.LogError(
                "Assign a player and road segment prefab, use a positive segment length, pool at least two segments, and use a non-negative recycle distance.",
                this);
            enabled = false;
            return;
        }

        segments = new Transform[pooledSegments];

        for (int i = 0; i < pooledSegments; i++)
        {
            GameObject segment = Instantiate(segmentPrefab, transform);
            segment.name = $"RoadSegment_{i}";
            segment.transform.position = transform.position + Vector3.forward * (segmentLength * i);
            segments[i] = segment.transform;
        }
    }

    private void Update()
    {
        if (segments == null)
        {
            return;
        }

        foreach (Transform segment in segments)
        {
            while (segment.position.z + segmentLength < player.position.z - recycleBehindDistance)
            {
                segment.position += Vector3.forward * (segmentLength * pooledSegments);
            }
        }
    }

    public void ResetForNewRun()
    {
        if (segments == null)
        {
            return;
        }

        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].position = transform.position + Vector3.forward * (segmentLength * i);
        }
    }
}
