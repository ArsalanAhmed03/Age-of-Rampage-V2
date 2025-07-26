using UnityEngine;

public class LookingDirection : MonoBehaviour
{
    public enum Direction
    {
        Left,
        Right,
    }

    public Direction CurrentDirection { get; private set; } = Direction.Left;

    [Header("Set where sprite is looking at on start")]
    public bool currentlyLookingRight = false;

    private void Start()
    {
        LookAtCenter();
    }

    /// <summary>
    /// Sets the direction and flips the sprite accordingly.
    /// </summary>
    public void SetDirection(Direction direction)
    {
        if (CurrentDirection == direction)
            return;

        CurrentDirection = direction;
        currentlyLookingRight = (direction == Direction.Right);
        float yRotation = (direction == Direction.Left) ? 180f : 0f;
        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        Debug.Log($"SetDirection: Now looking {direction}");
    }

    /// <summary>
    /// Looks at a specific world position (flips left/right).
    /// </summary>
    public void LookAt(Vector2 targetPosition)
    {
        Direction dir = (targetPosition.x < transform.position.x) ? Direction.Left : Direction.Right;
        SetDirection(dir);
        Debug.Log($"LookAt: Looking {dir} at position {targetPosition}");
    }

    /// <summary>
    /// Looks at the world center (0,0).
    /// </summary>
    public void LookAtCenter()
    {
        Vector3 direction = Vector3.zero - transform.position;

        if (direction.x < 0)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
        else
        {
            // transform.localScale = new Vector3(
            //     -Mathf.Abs(transform.localScale.x),
            //     transform.localScale.y,
            //     transform.localScale.z
            // );
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.flipX = true;
        }
    }

    /// <summary>
    /// Returns true if the game object is currently looking towards the center (0,0).
    /// </summary>
    public bool IsLookingTowardsCenter()
    {
        // Determine which direction the center is relative to the current position
        Direction centerDirection =
            (Vector2.zero.x < transform.position.x) ? Direction.Left : Direction.Right;
        return CurrentDirection == centerDirection;
    }
}
