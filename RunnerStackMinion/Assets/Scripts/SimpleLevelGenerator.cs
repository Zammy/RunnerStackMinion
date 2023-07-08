using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SimpleLevelGenerator : MonoBehaviour
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
    [SerializeField] int SegmentsToCreateAtStart = 20;
    [SerializeField] float DespawnSegmentsAfter = 50f;

    Queue<GameObject> _spawnedSegments;
    [Header("Debug")]
    [SerializeField] int _numSpawned;
    float[] _segmentRolls;
    float _spawnedUntil;
    float _spawnDistanceToMaintain;
    int _lastSpawnedSegmenet;

    void Awake()
    {
        _spawnedSegments = new Queue<GameObject>();
        _segmentRolls = new float[Segments.Length];
    }

    void Start()
    {
        _numSpawned = 0;
        _spawnedUntil = 0f;

        for (int i = 0; i < SegmentsToCreateAtStart; i++)
        {
            SpawnSegment();
        }

        _spawnDistanceToMaintain = _spawnedUntil;
    }

    void Update()
    {
        var playerPos = PlayerMovement.I.transform.position;
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

    void SpawnSegment()
    {
        int segmentIndex = PickSegmentToSpawn();
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
#endif
}
