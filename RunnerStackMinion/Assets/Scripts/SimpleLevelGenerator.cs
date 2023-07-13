using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class LevelSetting
{
    public int[] StarsScores;
}

public interface ILevelGenerator : IService, IInitializable
{
    void LoadLevel(int levelIndex);
    LevelSetting GetLevelSetting(int levelIndex);
}

public class SimpleLevelGenerator : MonoBehaviour, ILevelGenerator
{
    [System.Serializable]
    public struct SegmentSetting
    {
        public GameObject Prefab;
        public float SegmentSize; //TODO: remove
    }

    [Header("Settings")]
    public SegmentSetting[] Segments;

    public LevelSetting[] Levels;

    void Awake()
    {
        ServiceLocator.Instance.RegisterService(this);
    }

    public void Init()
    {
        DisableLevels();
    }

    public void LoadLevel(int levelIndex)
    {
        //TODO return false if no next level
        DisableLevels();
        transform.GetChild(levelIndex).gameObject.SetActive(true);
    }

    public LevelSetting GetLevelSetting(int levelIndex)
    {
        return Levels[levelIndex];
    }

    private void DisableLevels()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var level = transform.GetChild(i);
            level.gameObject.SetActive(false);
        }
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

    public void AddSegmentIndexToLevel(int segmentIndex, int levelIndex)
    {
        Transform levelTrans;
        if (transform.childCount <= levelIndex)
        {
            var go = new GameObject();
            go.transform.SetParent(this.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.name = "Level " + (levelIndex + 1).ToString();
            levelTrans = go.transform;
        }
        else
        {
            levelTrans = transform.GetChild(levelIndex);
        }

        float spawnPos = 0f;
        for (int i = 0; i < levelTrans.childCount; i++)
        {
            var segmentTrans = levelTrans.GetChild(i);
            spawnPos += CalculateSegmentSize(levelTrans.GetChild(i));
        }

        var segment = Segments[segmentIndex];
        Instantiate(segment.Prefab, new Vector3(0f, 0f, spawnPos), Quaternion.identity, levelTrans);
    }

    float CalculateSegmentSize(Transform segment)
    {
        var floor = segment.Find("Floor");
        if (!floor)
        {
            Debug.LogError($"{segment.name} has no Floor!");
            return 20f;
        }
        return floor.transform.localScale.z;
    }
#endif
}
