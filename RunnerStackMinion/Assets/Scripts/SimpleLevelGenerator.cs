using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public interface ILevelGenerator : IService, IInitializable
{
    void OnPlayerMoved(float deltaTime, Vector3 playerPos);
}

public class SimpleLevelGenerator : MonoBehaviour, ILevelGenerator
{
    [System.Serializable]
    public struct SegmentSetting
    {
        public GameObject Prefab;
        public float SpawnWeight;
        public float SegmentSize;
    }

    [Header("Settings")]
    [SerializeField] SegmentSetting[] Segments;
    [SerializeField] float DecreaseWeightOnRecentlySpawnedBy = 0.25f;
    [SerializeField] int SegmentsToCreate = 20;
    [SerializeField] float DespawnSegmentsAfter = 50f;

    readonly Queue<GameObject> _spawnedSegments = new Queue<GameObject>();

    [Header("Debug")]
    [ReadOnly]
    [SerializeField]
    int _numSpawned;

    [ReadOnly]
    [SerializeField]
    float _spawnedUntil;
    float[] _segmentRolls;
    float _spawnDistanceToMaintain;
    int _lastSpawnedSegmenet;

    void Awake()
    {
        ServiceLocator.Instance.RegisterService(this);

        _segmentRolls = new float[Segments.Length];
    }

    public void Init()
    {

        _numSpawned = 0;
        _spawnedUntil = 0f;

        SpawnLevelStatic();

        _spawnDistanceToMaintain = _spawnedUntil;
    }

    public void OnPlayerMoved(float deltaTime, Vector3 playerPos)
    {
        if (playerPos.z > (_spawnedUntil - _spawnDistanceToMaintain))
        {
            SpawnSegment();
        }
        var oldestSegment = _spawnedSegments.Peek();
        if (playerPos.z - oldestSegment.transform.position.z > DespawnSegmentsAfter)
        {
            DespawnSegment(_spawnedSegments.Dequeue());
        }
    }

    [ContextMenu("SpawnLevelStatic")]
    public void SpawnLevelStatic()
    {
        for (int i = 0; i < SegmentsToCreate; i++)
        {
            if (i < 2) //first two segments to be base
                SpawnSegment(0);
            else if (i == 2)
                SpawnSegment(5);
            else
                SpawnSegment();
        }
    }

    void SpawnSegment(int segmentIndex = -1)
    {
        if (segmentIndex == -1)
            segmentIndex = PickSegmentToSpawn();
        var segment = Segments[segmentIndex];
        var pos = transform.position;
        pos.z += _spawnedUntil;
        var segmentGo = Instantiate(segment.Prefab, pos, Quaternion.identity, transform);
        if (Mathf.Approximately(segment.SegmentSize, 0f))
        {
            Debug.LogError($"Segment {segmentGo.name} without size!");
            _spawnedUntil += 20f;
        }
        else
            _spawnedUntil += segment.SegmentSize;
        _spawnedSegments.Enqueue(segmentGo);
        _numSpawned++;
        _lastSpawnedSegmenet = segmentIndex;
    }

    int PickSegmentToSpawn()
    {
        float best = 0f;
        int index = 0;
        for (int i = 0; i < Segments.Length; i++)
        {
            float chance = Segments[i].SpawnWeight + Random.Range(0f, 1f);
            if (i == _lastSpawnedSegmenet)
                chance -= DecreaseWeightOnRecentlySpawnedBy;
            if (chance > best)
            {
                best = chance;
                index = i;
            }
        }
        return index;
    }

    void DespawnSegment(GameObject segment)
    {
        Destroy(segment);
    }

#if UNITY_EDITOR
    [ContextMenu("CalculateSizes")]
    public void CalculateSizes()
    {
        for (int i = 0; i < Segments.Length; i++)
        {
            if (!Segments[i].Prefab)
                continue;
            var floor = Segments[i].Prefab.transform.Find("Floor");
            if (!floor)
            {
                Debug.LogError($"{Segments[i].Prefab.name} has no Floor!");
                continue;
            }
            Segments[i].SegmentSize = floor.transform.localScale.z;
        }
        EditorUtility.SetDirty(gameObject);
    }

    [ContextMenu("ClearSpawned")]
    public void ClearSpawned()
    {
        _numSpawned = 0;
        _spawnedUntil = 0f;

        while (_spawnedSegments.Count > 0)
        {
            DespawnSegment(_spawnedSegments.Dequeue());
        }
    }
#endif
}
