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
    GameOver,
    LevelFinished,
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
    IGameController _controller;
    ILevelGenerator _levelGenerator;
    IPlayerMovement _player;

    public InMenuState(GameController controller) : base(controller)
    {
        _controller = ServiceLocator.Instance.GetService<IGameController>();
        _levelGenerator = ServiceLocator.Instance.GetService<ILevelGenerator>();
        _player = ServiceLocator.Instance.GetService<IPlayerMovement>();
    }

    public override void Enter()
    {
        base.Enter();

        _c.MainMenuCanvas.gameObject.SetActive(true);
        _c.MainMenuVirtualCam.gameObject.SetActive(true);
        _c.StartGameButton.onClick.AddListener(OnStartGameButtonClicked);

        _player.Body.position = Vector3.zero;
        if (!_levelGenerator.LoadLevel(_controller.CurrentLevel))
        {
            _controller.CurrentLevel = 0;
            _levelGenerator.LoadLevel(0);
        }
    }

    public override void Exit()
    {
        base.Exit();

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
    IPlayerMobControl _mobControl;

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

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        _c.SpawnCountText.text = (_mobControl.GetMobCount(MobType.Player) + 1).ToString();
        _c.MetersPassedText.text = $"{Mathf.RoundToInt(_playerMovement.Pos.z)}m passed";
    }

    private void OnPlayerDied()
    {

        _c.ChangeStateTo(GameState.GameOver);
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
        base.Enter();

        _c.InGameCanvas.gameObject.SetActive(true);
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        _playerMovement.ReadGameInput();
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        base.FixedTick(fixedDeltaTime);

        float sideDelta = _playerMovement.MovePlayer(fixedDeltaTime, _mobControl.GetMobCount(MobType.Player));
        _mobControl.ApplyCohesionForce();
        _mobControl.MoveMobs(new Vector3(sideDelta, 0f, 0f));
    }

    public override void Exit()
    {
        base.Exit();

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
        var levelFinishedEvent = gameEvent as LevelFinishedEvent;
        if (levelFinishedEvent != null)
        {
            _c.ChangeStateTo(GameState.LevelFinished);
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
        base.FixedTick(fixedDeltaTime);

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
            _battlefieldPos = mobEncounterEvent.BattlefieldPos;
            _enemyMobs = mobEncounterEvent.EnemiesCount;
        }
    }

    Vector3 _battlefieldPos;
    int _enemyMobs;
}

public class GameOverState : GameStateBase
{
    IGameController _controller;


    public GameOverState(GameController controller) : base(controller)
    {
        _controller = ServiceLocator.Instance.GetService<IGameController>();
    }

    public override void Enter()
    {
        base.Enter();

        _c.GameOverCanvas.gameObject.SetActive(true);
        _c.GameOverButton.onClick.AddListener(OnButtonClicked);
        _c.MainMenuVirtualCam.SetActive(true);
    }

    public override void Exit()
    {
        base.Exit();

        _c.GameOverCanvas.gameObject.SetActive(false);
        _c.GameOverButton.onClick.RemoveListener(OnButtonClicked);
        _c.MainMenuVirtualCam.SetActive(false);
    }

    private void OnButtonClicked()
    {
        _controller.CurrentLevel = 0;
        _c.ChangeStateTo(GameState.InMenu);
    }
}

public class LevelFinishedState : GameStateBase
{
    ILevelGenerator _levelGenerator;
    IPlayerMobControl _mobControl;
    IPlayerMovement _player;
    IGameController _controller;

    Coroutine _scoringCoroutine;
    bool _speedup;

    public LevelFinishedState(GameController controller) : base(controller)
    {
        _levelGenerator = ServiceLocator.Instance.GetService<ILevelGenerator>();
        _mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
        _player = ServiceLocator.Instance.GetService<IPlayerMovement>();
        _controller = ServiceLocator.Instance.GetService<IGameController>();
    }

    public override void Enter()
    {
        base.Enter();

        _speedup = false;
        _c.MainMenuVirtualCam.SetActive(true);
        _c.LevelFinishedCanvas.gameObject.SetActive(true);
        _c.LevelFinishedButton.onClick.AddListener(this.OnButtonClicked);

        for (int i = 0; i < 3; i++)
        {
            _c.LevelFinishedStars[i].SetActive(false);
        }

        float metersPassed = _player.Pos.z;
        int playerMobCount = _mobControl.GetMobCount(MobType.Player);
        int[] stars = _levelGenerator.GetLevelSetting(_controller.CurrentLevel).StarsScores;
        _scoringCoroutine = _c.StartCoroutine(DoScoring((int)metersPassed, playerMobCount + 1, stars));
    }

    public override void Exit()
    {
        base.Exit();

        _c.MainMenuVirtualCam.SetActive(false);
        _c.LevelFinishedCanvas.gameObject.SetActive(false);
        _c.LevelFinishedButton.onClick.RemoveListener(this.OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (_scoringCoroutine != null)
        {
            _speedup = true;
            return;
        }

        _controller.CurrentLevel++;
        _c.ChangeStateTo(GameState.InMenu);
    }

    IEnumerator DoScoring(int metersPassed, int mobsAlive, int[] starsScoring)
    {
        _c.LevelFinishedDistance.text = $"{metersPassed} distance";
        _c.LevelFinishedMobsAlive.text = $"x 0 mobs";

        yield return new WaitForSeconds(1f);

        int totalScore = 0;
        int mobsScored = 0;
        while (mobsScored < mobsAlive)
        {
            mobsScored += 1;
            _c.LevelFinishedMobsAlive.text = $"x {mobsScored} mobs";
            _mobControl.DespawnRandomPlayerMob();
            totalScore = (int)(mobsScored * metersPassed);
            _c.LevelFinishedFinalScore.text = totalScore.ToString();
            if (!_speedup)
                yield return new WaitForSeconds(.05f);
        }

        if (!_speedup)
            yield return new WaitForSeconds(1f);

        for (int i = 0; i < 3; i++)
        {
            if (totalScore > starsScoring[i])
            {
                _c.LevelFinishedStars[i].SetActive(true);
                if (!_speedup)
                    yield return new WaitForSeconds(.4f);
            }
            else
            {
                break;
            }
        }

        _scoringCoroutine = null;
    }
}

public abstract class GameEvent { }
public class MobEncounterEvent : GameEvent
{
    public int EnemiesCount { get; set; }
    public Vector3 BattlefieldPos { get; set; }
}

public class LevelFinishedEvent : GameEvent { }

public interface IGameController : IService, IInitializable, ITickable, ITickableFixed
{
    int CurrentLevel { get; set; }
    void RaiseEvent(GameEvent gameEvent);
}

//TODO split FSM and View in separate interfaces
public class GameController : MonoBehaviour, IGameController
{
    [Header("Refs")]
    public Canvas MainMenuCanvas;
    public Button StartGameButton;
    public GameObject MainMenuVirtualCam;

    public Canvas InGameCanvas;
    public TextMeshProUGUI SpawnCountText;
    public TextMeshProUGUI MetersPassedText;

    public Canvas GameOverCanvas;
    public Button GameOverButton;

    public Canvas LevelFinishedCanvas;
    public Button LevelFinishedButton;
    public TextMeshProUGUI LevelFinishedDistance;
    public TextMeshProUGUI LevelFinishedMobsAlive;
    public TextMeshProUGUI LevelFinishedFinalScore;
    public GameObject[] LevelFinishedStars;

    GameStateBase _currentState;
    Dictionary<GameState, GameStateBase> _states;

    [Header("Debug")]
    [ReadOnly]
    [SerializeField] string _currentStateName;

    public int CurrentLevel { get; set; }

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
            { GameState.GameOver, new GameOverState(this) },
            { GameState.LevelFinished, new LevelFinishedState(this) },
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
