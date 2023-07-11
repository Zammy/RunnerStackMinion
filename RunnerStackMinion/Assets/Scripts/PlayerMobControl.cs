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
    int Spawned { get; }

    void SpawnInitial();
    void DespawnRandomPlayerMob();
    void DespawnMob(Mob mob);
    void SpawnMobAt(MobType type, Vector3 position);
    void ApplyCohesionForce();
    void MoveMobs(Vector3 delta);
    void ApplyEncounterForce(Vector3 battlefieldPos);
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

    public int Spawned => Mobs.Count + 1;

    List<Mob> Mobs { get; set; }

    IPlayerMovement _playerMovement;

    void Awake()
    {
        ServiceLocator.Instance.RegisterService(this);

        Mobs = new List<Mob>();
    }

    public void Init()
    {
        _playerMovement = ServiceLocator.Instance.GetService<IPlayerMovement>();
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
        int half = Spawned / 2;
        for (int i = 0; i < half; i++)
        {
            DespawnRandomPlayerMob();
        }
    }

    public void DespawnMob(Mob despawnMob)
    {
        for (int i = 0; i < Mobs.Count; i++)
        {
            var mob = Mobs[i];
            if (mob == despawnMob)
            {
                Mobs.RemoveAt(i);
                Destroy(despawnMob.gameObject);
                break;
            }
        }

        if (despawnMob == _playerMovement.Body)
        {
            OnPlayerDied?.Invoke();
        }
    }

    public void SpawnMobAtPlayer()
    {
        SpawnMobAt(MobType.Player, _playerMovement.Pos + _offsets[Spawned]);
    }

    public void SpawnMobAt(MobType type, Vector3 pos)
    {
        var prefab = MobPrefab;
        if (type == MobType.Enemy)
            prefab = MobEnemyPrefab;

        var mobGo = Instantiate(prefab, pos, Quaternion.identity, transform);
        mobGo.GetComponent<Mob>().Type = type;
        Mobs.Add(mobGo.GetComponent<Mob>());
    }

    public void DespawnRandomPlayerMob()
    {
        int index = UnityEngine.Random.Range(0, Mobs.Count);
        Destroy(Mobs[index].gameObject);
        Mobs.RemoveAt(index);
    }

    public void MoveMobs(Vector3 delta)
    {
        for (int i = 0; i < Mobs.Count; i++)
        {
            var mob = Mobs[i];
            var mobPos = mob.Body.position;
            mobPos += delta;
            mob.Body.MovePosition(mobPos);
        }
    }

    public void ApplyCohesionForce()
    {
        for (int i = 0; i < Mobs.Count; i++)
        {
            var mob = Mobs[i];
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
        for (int i = 0; i < Mobs.Count; i++)
        {
            var mob = Mobs[i];
            var toBattlefield = battlefieldPos - mob.Body.position;
            mob.Body.AddForce(toBattlefield.normalized * toBattlefield.sqrMagnitude * MobEncounterForce, ForceMode.VelocityChange);
            mob.Body.velocity = Vector3.ClampMagnitude(mob.Body.velocity, MobMaxSpeed);
        }
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
