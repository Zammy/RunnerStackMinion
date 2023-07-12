using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    InMenu,
    GameStart,
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
        _c.ChangeStateTo(GameState.GameStart);
    }
}

public class GameStartState : GameStateBase
{
    protected IPlayerMobControl _mobControl;

    public GameStartState(GameController controller) : base(controller)
    {
        _mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
    }

    public override void Enter()
    {
        base.Enter();

        _mobControl.SpawnInitial();

        _c.ChangeStateTo(GameState.GameMoving);
    }
}

public abstract class UpdateGameUIState : GameStateBase
{
    protected IPlayerMobControl _mobControl;
    protected IPlayerMovement _playerMovement;

    public UpdateGameUIState(GameController controller) : base(controller)
    {
        _mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
        _playerMovement = ServiceLocator.Instance.GetService<IPlayerMovement>();
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        _c.SpawnCountText.text = _mobControl.GetMobCount(MobType.Player).ToString();
    }
}

public class GameMovingState : UpdateGameUIState
{
    ILevelGenerator _levelGenerator;

    public GameMovingState(GameController controller) : base(controller)
    {
        _levelGenerator = ServiceLocator.Instance.GetService<ILevelGenerator>();
    }

    public override void Enter()
    {

        _c.InGameCanvas.gameObject.SetActive(true);
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        _playerMovement.ReadGameInput();
        _levelGenerator.OnPlayerMoved(deltaTime, _playerMovement.Pos);
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        float sideDelta = _playerMovement.MovePlayer(fixedDeltaTime, _mobControl.GetMobCount(MobType.Player));
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

    public override void Enter()
    {
        base.Enter();
        _mobControl.OnPlayerDied += OnPlayerDied;
    }

    public override void Exit()
    {
        base.Exit();
        _mobControl.OnPlayerDied -= OnPlayerDied;
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        if (_mobControl.GetMobCount(MobType.Enemy) == 0)
        {
            _c.ChangeStateTo(GameState.GameMoving);
            return;
        }

        int playerMobCount = _mobControl.GetMobCount(MobType.Player);
        _mobControl.ApplyEncounterForce(_battlefieldPos);
        if (playerMobCount > 0)
        {
            var pos = _playerMovement.Body.position;
            pos.x = 0f;
            _playerMovement.Body.AddForce((pos - _playerMovement.Body.position) * .25f, ForceMode.VelocityChange);
        }
        else
        {
            _mobControl.ApplyEncounterForceToPlayer(_battlefieldPos);
        }
    }

    public override void OnEvent(GameEvent gameEvent)
    {
        base.OnEvent(gameEvent);

        var mobEncounterEvent = gameEvent as MobEncounterEvent;
        if (mobEncounterEvent != null)
        {
            _battlefieldPos = mobEncounterEvent.Pos;
            _enemyMobs = mobEncounterEvent.EnemiesCount;
        }
    }

    private void OnPlayerDied()
    {
        _c.ChangeStateTo(GameState.GameOver);
    }

    Vector3 _battlefieldPos;
    int _enemyMobs;
}

public class GameOverState : GameStateBase
{
    public GameOverState(GameController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        _c.GameOverCanvas.gameObject.SetActive(true);
        _c.GameOverButton.onClick.AddListener(OnGameOverButtonClicked);
        _c.MainMenuVirtualCam.SetActive(true);
    }

    private void OnGameOverButtonClicked()
    {
        SceneManager.LoadScene("Game");
    }
}

public abstract class GameEvent { }
public class MobEncounterEvent : GameEvent
{
    public int EnemiesCount { get; set; }

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
    public Canvas GameOverCanvas;
    public Button GameOverButton;

    GameStateBase _currentState;
    Dictionary<GameState, GameStateBase> _states;

    [Header("Debug")]
    [ReadOnly]
    [SerializeField] string _currentStateName;

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
            { GameState.GameStart, new GameStartState(this) },
            { GameState.GameMoving, new GameMovingState(this) },
            { GameState.GameEncounter, new GameEncounterState(this) },
            { GameState.GameOver, new GameOverState(this) }
        };

        ChangeStateTo(GameState.InMenu);
    }

    public void Tick(float deltaTime)
    {
        _currentState?.Tick(deltaTime);
        _currentStateName = _currentState.GetType().Name;
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
