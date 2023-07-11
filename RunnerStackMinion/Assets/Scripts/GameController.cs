using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameState
{
    InMenu,
    // GameStart, //If we want to have count down
    GameMoving,
    GameNotMoving,
    GameOver
}

public abstract class GameStateBase
{
    protected readonly GameController _c;

    public GameStateBase(GameController controller)
    {
        _c = controller;
    }

    public virtual void Enter() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void FixedTick(float fixedDeltaTime) { }
    public virtual void Exit() { }
}

public class InMenuState : GameStateBase
{
    public InMenuState(GameController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        _c.MainMenuCanvas.gameObject.SetActive(true);
        _c.MainMenuVirtualCam.gameObject.SetActive(true);
        _c.StartGameButton.onClick.AddListener(OnStartGameButtonClicked);
    }

    public override void Exit()
    {
        _c.MainMenuCanvas.gameObject.SetActive(false);
        _c.MainMenuVirtualCam.gameObject.SetActive(false);
        _c.StartGameButton.onClick.RemoveListener(OnStartGameButtonClicked);
    }

    public void OnStartGameButtonClicked()
    {
        _c.ChangeStateTo(GameState.GameMoving);
    }
}

public class GameMovingState : GameStateBase
{
    IPlayerMobControl _mobControl;
    IPlayerMovement _playerMovement;

    public GameMovingState(GameController controller) : base(controller)
    {
        _mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
        _playerMovement = ServiceLocator.Instance.GetService<IPlayerMovement>();
    }

    public override void Enter()
    {
        _mobControl.SpawnInitial();
        _c.InGameCanvas.gameObject.SetActive(true);
    }

    public override void Tick(float deltaTime)
    {
        _playerMovement.ReadGameInput();
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        float sideDelta = _playerMovement.MovePlayer(fixedDeltaTime, _mobControl.Spawned);
        _mobControl.ApplyCohesionForce();
        _mobControl.MoveMobs(new Vector3(sideDelta, 0f, 0f));
    }

    public override void Exit()
    {
        _c.InGameCanvas.gameObject.SetActive(false);
    }
}


public interface IGameController : IService, IInitializable, ITickable, ITickableFixed
{
}

public class GameController : MonoBehaviour, IGameController
{
    [Header("Refs")]
    public Canvas MainMenuCanvas;
    public Button StartGameButton;
    public Canvas InGameCanvas;
    public GameObject MainMenuVirtualCam;

    GameStateBase _currentState;
    Dictionary<GameState, GameStateBase> _states;

    void Awake()
    {
        ServiceLocator.Instance.RegisterService(this);
    }

    public void Init()
    {
        _currentState = null;

        _states = new Dictionary<GameState, GameStateBase>()
        {
            { GameState.InMenu, new InMenuState(this) },
            { GameState.GameMoving, new GameMovingState(this)},
        };

        ChangeStateTo(GameState.InMenu);
    }

    public void Tick(float deltaTime)
    {
        _currentState?.Tick(deltaTime);
    }

    public void TickFixed(float fixedDeltaTime)
    {
        _currentState?.FixedTick(fixedDeltaTime);
    }

    public void ChangeStateTo(GameState state)
    {
        if (_currentState != null)
        {
            _currentState.Exit();
        }
        _currentState = _states[state];
        _currentState.Enter();
    }
}
