using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PlayerMovement : MonoBehaviour
{
    public float ForwardSpeed = 1f;
    public float SideSpeed = 5f;
    public float LevelWidth = 20f;

    Rigidbody _rigidbody;
    Vector2 _touchDelta;
    float _screenWidth;
    float _halfLevelWidth;

    void Awake()
    {
        EnhancedTouchSupport.Enable();
        _screenWidth = Screen.width;
        _halfLevelWidth = LevelWidth / 2f;
    }

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        var pos = _rigidbody.position;
        if (Touch.activeFingers.Count >= 1)
        {
            Touch activeTouch = Touch.activeFingers[0].currentTouch;
            // Debug.Log($"Phase: {activeTouch.phase} | Position: {activeTouch.screenPosition}");

            Vector3 sideDelta = Vector3.zero;
            float t = activeTouch.screenPosition.x / _screenWidth;
            pos.x = Mathf.Lerp(-_halfLevelWidth, _halfLevelWidth, t);
        }

        var forwardDelta = transform.forward * Time.deltaTime * ForwardSpeed;
        _rigidbody.MovePosition(pos + forwardDelta);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"OnCollisionEnter {collision.collider.name}");
    }

    void OnCollisionExit(Collision collision)
    {
        Debug.Log($"OnCollisionExit {collision.collider.name}");
    }
}
