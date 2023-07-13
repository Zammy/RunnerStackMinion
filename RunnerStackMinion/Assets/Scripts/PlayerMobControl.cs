using System;
using System.Collections.Generic;
using UnityEngine;

public enum MobType
{
    Player,
    Enemy
}

public interface IPlayerMobControl : IService, IInitializable
{
    // event Action<Mob> OnDespawned;
    event Action OnPlayerDied;

    int GetMobCount(MobType type);
    void SpawnInitial();
    void DespawnRandomPlayerMob();
    void DespawnMob(Mob mob);
    Mob SpawnMobAt(MobType type, Vector3 position);
    void ApplyCohesionForce();
    void MoveMobs(Vector3 delta);
    void ApplyEncounterForce(Vector3 battlefieldPos);
    void ApplyEncounterForceToPlayer(Vector3 battlefieldPos);
}

public class PlayerMobControl : MonoBehaviour, IPlayerMobControl
{
    [Header("Settings")]
    [SerializeField] GameObject MobPrefab;
    [SerializeField] GameObject MobEnemyPrefab;

    [SerializeField] float CohesionForce = 1f;
    [SerializeField] float FormationForce = 1f;
    [SerializeField] float MobMaxSpeed = 1f;
    [SerializeField] float MobEncounterForce = 1f;

    [Header("Debug")]
    [SerializeField] int SpawnOnStartup = 10;

    public event Action OnPlayerDied;
    // public event Action<Mob> OnDespawned;

    IPlayerMovement _playerMovement;
    List<Mob>[] _mobs;
    int _mobTypesCount;

    void Awake()
    {
        ServiceLocator.Instance.RegisterService(this);
    }

    public void Init()
    {
        _mobTypesCount = Enum.GetNames(typeof(MobType)).Length;
        _mobs = new List<Mob>[_mobTypesCount];
        for (int i = 0; i < _mobTypesCount; i++)
        {
            _mobs[i] = new List<Mob>();
        }

        _playerMovement = ServiceLocator.Instance.GetService<IPlayerMovement>();
    }

    public int GetMobCount(MobType type)
    {
        return _mobs[(int)type].Count;
    }

    [ContextMenu("Spawn")]
    public void SpawnInitial()
    {
        for (int i = 0; i < SpawnOnStartup; i++)
        {
            SpawnMobAtPlayer();
        }
    }

    [ContextMenu("HalfMobSize")]
    public void Despawn()
    {
        int half = GetMobCount(MobType.Player) / 2;
        for (int i = 0; i < half; i++)
        {
            DespawnRandomPlayerMob();
        }
    }

    public void DespawnMob(Mob mob)
    {
        int typeIndex = (int)mob.Type;
        for (int i = 0; i < _mobs[typeIndex].Count; i++)
        {
            var otherMob = _mobs[typeIndex][i];
            if (mob == otherMob)
            {
                _mobs[typeIndex].RemoveAt(i);
                Destroy(otherMob.gameObject);
                return;
            }
        }

        if (mob.Body == _playerMovement.Body)
        {
            OnPlayerDied?.Invoke();
            return;
        }

        throw new UnityException("Should not happen!");
    }

    public void SpawnMobAtPlayer()
    {
        SpawnMobAt(MobType.Player, _playerMovement.Pos + _offsets[GetMobCount(MobType.Player)]);
    }

    public Mob SpawnMobAt(MobType type, Vector3 pos)
    {
        var prefab = MobPrefab;
        if (type == MobType.Enemy)
            prefab = MobEnemyPrefab;

        var mobGo = Instantiate(prefab, pos, Quaternion.identity, transform);
        var mob = mobGo.GetComponent<Mob>();
        mob.Type = type;
        _mobs[(int)type].Add(mob);
        return mob;
    }

    public void DespawnRandomPlayerMob()
    {
        int typeIndex = (int)MobType.Player;
        if (_mobs[typeIndex].Count == 0)
        {
            OnPlayerDied?.Invoke();
            return;
        }
        int index = UnityEngine.Random.Range(0, _mobs[typeIndex].Count);
        Destroy(_mobs[typeIndex][index].gameObject);
        _mobs[typeIndex].RemoveAt(index);
    }

    public void MoveMobs(Vector3 delta)
    {
        int typeIndex = (int)MobType.Player;
        for (int i = 0; i < _mobs[typeIndex].Count; i++)
        {
            var mob = _mobs[typeIndex][i];
            var mobPos = mob.Body.position;
            mobPos += delta;
            mob.Body.MovePosition(mobPos);
        }
    }

    public void ApplyCohesionForce()
    {
        int typeIndex = (int)MobType.Player;
        for (int i = 0; i < _mobs[typeIndex].Count; i++)
        {
            var mob = _mobs[typeIndex][i];
            if (mob.Type != MobType.Player)
                continue;
            var toPlayer = _playerMovement.Pos - mob.Body.position;
            mob.Body.AddForce(toPlayer.normalized * toPlayer.sqrMagnitude * CohesionForce, ForceMode.VelocityChange);
            if (_offsets.Length > i)
            {
                var toPos = _playerMovement.Pos + _offsets[i] - mob.Body.position;
                mob.Body.AddForce(toPos.normalized * toPos.sqrMagnitude * FormationForce, ForceMode.VelocityChange);
            }

            mob.Body.velocity = Vector3.ClampMagnitude(mob.Body.velocity, MobMaxSpeed);
        }
    }

    public void ApplyEncounterForce(Vector3 battlefieldPos)
    {
        for (int y = 0; y < _mobTypesCount; y++)
        {
            for (int i = 0; i < _mobs[y].Count; i++)
            {
                var mob = _mobs[y][i];
                var toBattlefield = battlefieldPos - mob.Body.position;
                float distance = toBattlefield.magnitude;
                distance = Mathf.Max(1f, distance);
                var force = toBattlefield.normalized * distance * MobEncounterForce;
                mob.Body.AddForce(force, ForceMode.VelocityChange);
                mob.Body.velocity = Vector3.ClampMagnitude(mob.Body.velocity, MobMaxSpeed);
            }
        }
    }

    public void ApplyEncounterForceToPlayer(Vector3 battlefieldPos)
    {
        var body = _playerMovement.Body;
        var toBattlefield = battlefieldPos - body.position;
        body.AddForce(toBattlefield.normalized * MobEncounterForce * 2f, ForceMode.VelocityChange);
    }

    #region Formation Positions
    static int CircleSize(int circle)
    {
        if (circle == 0)
            return 1;
        return 6 * circle;
    }

    static void CalculateCircleAndSize(int index, out int circleIndex, out int circleSize, out int totalCircles)
    {
        circleSize = 0;
        circleIndex = 1;
        totalCircles = 1;

        while (index > 0)
        {
            circleSize = CircleSize(circleIndex);
            if (index > circleSize)
            {
                circleIndex++;
                index -= circleSize;
                totalCircles += circleSize;
            }
            else
            {
                break;
            }
        }
    }

    readonly Vector3[] kStartPositions = new Vector3[]
    {
        new Vector3(-1f, 0f, 0f),
        new Vector3(-0.5f, 0f, +0.886f),
        new Vector3(0.5f, 0f, +0.886f),
        new Vector3(1f, 0f, 0f),
        new Vector3(0.5f, 0f, -0.886f),
        new Vector3(-0.5f, 0f, -0.886f),
    };
    readonly Vector3[] kDirections = new Vector3[]
    {
        new Vector3(0.5f, 0f, 0.886f),
        new Vector3(1f, 0f, 0f),
        new Vector3(0.5f, 0f, -0.886f),
        new Vector3(-0.5f, 0f, -0.886f),
        new Vector3(-1f, 0f, 0f),
        new Vector3(-0.5f, 0f, 0.886f)
    };

    const int OffsetBufferCircleSize = 10;
    [ContextMenu("CalculateOffsets")]
    void CalculateOffsets()
    {
        int bufferSize = 0;
        for (int i = 0; i < OffsetBufferCircleSize; i++)
        {
            bufferSize += CircleSize(i);
        }

        _offsets = new Vector3[bufferSize];
        _offsets[0] = Vector3.zero;

        for (int i = 1; i < bufferSize; i++)
        {
            CalculateCircleAndSize(i, out int circleIndex, out int circleSize, out int totalCircles);

            int directionSize = circleSize / 6;
            int indexRow = (i - totalCircles) / directionSize;
            int indexInRow = (i - totalCircles) % directionSize;

            _offsets[i] = kStartPositions[indexRow] * circleIndex + kDirections[indexRow] * indexInRow;
        }
    }

    [HideInInspector]
    [SerializeField]
    Vector3[] _offsets;

    // private void OnDrawGizmos()
    // {
    //     for (int i = 0; i < _offsets.Length; i++)
    //     {
    //         Gizmos.DrawSphere(transform.position + _offsets[i], .25f);
    //     }
    // }
    #endregion
}
