using System.Collections.Generic;
using TMPro;
using UnityEngine;

public interface IPlayerMobControl : IService, IInitializable
{
    int Spawned { get; }
    void DespawnMob();
    void SpawnMobAt(Vector3 position);
}

public class PlayerMobControl : MonoBehaviour, IPlayerMobControl
{
    [Header("Settings")]
    [SerializeField] GameObject MobPrefab;

    [SerializeField] float CohesionForce = 1f;
    [SerializeField] float FormationForce = 1f;
    [SerializeField] float MobMaxSpeed = 1f;

    [Header("Refs")]
    [SerializeField] TextMeshProUGUI SpawnCountText;


    [Header("Debug")]
    [SerializeField] int SpawnOnStartup = 10;
    public int Spawned { get; private set; }
    public List<Rigidbody> Mobs { get; set; }

    IPlayerMovement _playerMovement;

    void Awake()
    {
        ServiceLocator.Instance.RegisterService(this);

        Mobs = new List<Rigidbody>();
    }

    public void Init()
    {
        _playerMovement = ServiceLocator.Instance.GetService<IPlayerMovement>();

        Spawned = 1;
        Spawn();
    }

    [ContextMenu("Spawn")]
    public void Spawn()
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
            DespawnMob();
        }
    }

    public void SpawnMobAtPlayer()
    {
        SpawnMobAt(_playerMovement.Pos + _offsets[Spawned]);
    }

    public void SpawnMobAt(Vector3 pos)
    {
        var mobGo = Instantiate(MobPrefab, pos, Quaternion.identity, transform);
        Mobs.Add(mobGo.GetComponent<Rigidbody>());
        Spawned++;
        UpdateUI();

    }

    public void DespawnMob()
    {
        int index = Random.Range(0, Mobs.Count);
        Destroy(Mobs[index].gameObject);
        Mobs.RemoveAt(index);
        Spawned--;
        UpdateUI();
    }

    public void MoveMobs(Vector3 delta)
    {
        for (int i = 0; i < Mobs.Count; i++)
        {
            var mobBody = Mobs[i];
            var mobPos = mobBody.position;
            mobPos += delta;
            mobBody.MovePosition(mobPos);
        }
    }

    public void ApplyCohesionForce()
    {
        for (int i = 0; i < Mobs.Count; i++)
        {
            var mobBody = Mobs[i];
            var toPlayer = _playerMovement.Pos - mobBody.position;
            mobBody.AddForce(toPlayer.normalized * toPlayer.sqrMagnitude * CohesionForce, ForceMode.VelocityChange);
            if (_offsets.Length > i)
            {
                var toPos = _playerMovement.Pos + _offsets[i] - mobBody.position;
                mobBody.AddForce(toPos.normalized * toPos.sqrMagnitude * FormationForce, ForceMode.VelocityChange);
            }

            mobBody.velocity = Vector3.ClampMagnitude(mobBody.velocity, MobMaxSpeed);
        }
    }

    void UpdateUI()
    {
        //TODO: not its place here
        SpawnCountText.text = Spawned.ToString();
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

    private void OnDrawGizmos()
    {
        for (int i = 0; i < _offsets.Length; i++)
        {
            Gizmos.DrawSphere(transform.position + _offsets[i], .25f);
        }
    }
#endregion
}
