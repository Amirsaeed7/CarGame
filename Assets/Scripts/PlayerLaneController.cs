using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLaneController : MonoBehaviour
{
    [SerializeField] private InputActionReference laneChangeAction;
    [SerializeField] private float laneSpacing = 3.5f;
    [SerializeField] private float laneChangeSpeed = 12f;
    [SerializeField] private float forwardSpeed = 12f;

    private const int LeftLane = 0;
    private const int RightLane = 2;
    private int currentLane = 1;

    public float ForwardSpeed => forwardSpeed;

    private void OnEnable()
    {
        if (laneChangeAction == null || laneChangeAction.action == null)
        {
            Debug.LogError("Assign the Player/Move input action to Lane Change Action.", this);
            enabled = false;
            return;
        }

        laneChangeAction.action.performed += HandleLaneChange;
        laneChangeAction.action.Enable();
    }

    private void OnDisable()
    {
        if (laneChangeAction == null || laneChangeAction.action == null)
        {
            return;
        }

        laneChangeAction.action.performed -= HandleLaneChange;
        laneChangeAction.action.Disable();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            return;
        }

        float targetX = (currentLane - 1) * laneSpacing;
        Vector3 position = transform.position;
        position.x = Mathf.MoveTowards(position.x, targetX, laneChangeSpeed * Time.deltaTime);
        position.z += forwardSpeed * Time.deltaTime;
        transform.position = position;
    }

    private void HandleLaneChange(InputAction.CallbackContext context)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            return;
        }

        float horizontal = context.ReadValue<Vector2>().x;

        if (horizontal < -0.5f)
        {
            currentLane = Mathf.Max(currentLane - 1, LeftLane);
        }
        else if (horizontal > 0.5f)
        {
            currentLane = Mathf.Min(currentLane + 1, RightLane);
        }
    }

    public void SetForwardSpeed(float speed)
    {
        forwardSpeed = Mathf.Max(0f, speed);
    }

    public void ResetForNewRun(Vector3 startingPosition, float speed)
    {
        currentLane = 1;
        forwardSpeed = Mathf.Max(0f, speed);
        transform.position = startingPosition;
    }
}
