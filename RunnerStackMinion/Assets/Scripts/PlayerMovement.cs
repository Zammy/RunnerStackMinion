using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public interface IPlayerMovement : IService, IInitializable
{
    Vector3 Pos { get; }
    Rigidbody Body { get; }
    void ReadGameInput();
    float MovePlayer(float fixedDeltaTime, int spawnCount);
}

public class PlayerMovement : MonoBehaviour, IPlayerMovement
{
    [Header("Settings")]
    [SerializeField] float ForwardSpeed = 1f;
    [SerializeField] float LevelWidth = 20f;
    [SerializeField] float LevelWidthDecreasePerMob = .05f;
    [SerializeField] float InputMargin = 10f;

    public Vector3 Pos => transform.position;
    public Rigidbody Body => _rigidbody;

    Rigidbody _rigidbody;
    float _screenWidth;
    float _halfLevelWidth;
    float _sideDelta;

    void Awake()
    {
        ServiceLocator.Instance.RegisterService(this);

        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable();

        _screenWidth = Screen.width;
        _halfLevelWidth = LevelWidth / 2f;
    }

    public void Init()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void ReadGameInput()
    {
        var pos = _rigidbody.position;
        if (Touch.activeTouches.Count >= 1)
        {
            Touch activeTouch = Touch.activeTouches[0];
            // Debug.Log($"Phase: {activeTouch.phase} | Position: {activeTouch.screenPosition} | Delta: {activeTouch.delta} | touchId : {activeTouch.touchId}");
            if (activeTouch.phase == TouchPhase.Moved)
            {
                float touchDelta = activeTouch.delta.x;
                float pixelsMargin = InputMargin * Screen.dpi;
                float normalizedTouchDelta = touchDelta / (_screenWidth - pixelsMargin);
                float deltaX = normalizedTouchDelta * LevelWidth;
                _sideDelta = deltaX;
            }
        }
    }

    public float MovePlayer(float fixedDeltaTime, int spawnCount)
    {
        float LevelEdge = CalculateLevelEdge(spawnCount);
        float newX = _rigidbody.position.x + _sideDelta;
        if (newX > LevelEdge)
            _sideDelta = LevelEdge - _rigidbody.position.x;
        else if (newX < -LevelEdge)
            _sideDelta = -LevelEdge - _rigidbody.position.x;

        float forwardDelta = fixedDeltaTime * ForwardSpeed;

        _rigidbody.MovePosition(_rigidbody.position + Vector3.right * _sideDelta + transform.forward * forwardDelta);

        var sideDelta = _sideDelta;
        _sideDelta = 0f;
        return sideDelta;
    }

    float CalculateLevelEdge(int spawnCount)
    {
        return _halfLevelWidth - LevelWidthDecreasePerMob * Mathf.Sqrt(spawnCount / Mathf.PI);
    }
}
