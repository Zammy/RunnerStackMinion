using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameState
{
    InMenu,
    // GameStart, //If we want to have count down
    GameMoving,
    GameEncounter,
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
    public virtual void OnEvent(GameEvent gameEvent) { }
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

public abstract class UpdateGameUIState : GameStateBase
{
    protected IPlayerMobControl _mobControl;

    public UpdateGameUIState(GameController controller) : base(controller)
    {
        _mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        _c.SpawnCountText.text = _mobControl.Spawned.ToString();
    }
}

public class GameMovingState : UpdateGameUIState
{
    IPlayerMovement _playerMovement;

    public GameMovingState(GameController controller) : base(controller)
    {
        _playerMovement = ServiceLocator.Instance.GetService<IPlayerMovement>();
    }

    public override void Enter()
    {
        _mobControl.SpawnInitial();
        _c.InGameCanvas.gameObject.SetActive(true);
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

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

    public override void OnEvent(GameEvent gameEvent)
    {
        base.OnEvent(gameEvent);

        var mobEncounterEvent = gameEvent as MobEncounterEvent;
        if (mobEncounterEvent != null)
        {
            _c.ChangeStateToAndRaise(GameState.GameEncounter, gameEvent);
        }
    }
}

public class GameEncounterState : UpdateGameUIState
{
    public GameEncounterState(GameController controller) : base(controller)
    {
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        if (_mobControl.Spawned > 1)
        {
            _mobControl.ApplyEncounterForce(_battlefieldPos);
        }
    }

    public override void OnEvent(GameEvent gameEvent)
    {
        base.OnEvent(gameEvent);

        var mobEncounterEvent = gameEvent as MobEncounterEvent;
        if (mobEncounterEvent != null)
        {
            _battlefieldPos = mobEncounterEvent.Pos;

            
        }
    }

    Vector3 _battlefieldPos;
}

public abstract class GameEvent { }
public class MobEncounterEvent : GameEvent
{
    public Vector3 Pos { get; set; }
}

public interface IGameController : IService, IInitializable, ITickable, ITickableFixed
{
    void RaiseEvent(GameEvent gameEvent);
}

//TODO split FSM and View in separate interfaces
public class GameController : MonoBehaviour, IGameController
{
    [Header("Refs")]
    public Canvas MainMenuCanvas;
    public Button StartGameButton;
    public Canvas InGameCanvas;
    public GameObject MainMenuVirtualCam;
    public TextMeshProUGUI SpawnCountText;

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
            { GameState.GameEncounter, new GameEncounterState(this)},
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

    public void ChangeStateToAndRaise(GameState state, GameEvent gameEvent)
    {
        ChangeStateTo(state);
        _currentState?.OnEvent(gameEvent);
    }

    public void RaiseEvent(GameEvent gameEvent)
    {
        _currentState?.OnEvent(gameEvent);
    }
}
