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
    bool LoadLevel(int levelIndex);
    LevelSetting GetLevelSetting(int levelIndex);
}

public class SimpleLevelGenerator : MonoBehaviour, ILevelGenerator
{
    [System.Serializable]
    public struct SegmentSetting
    {
        public GameObject Prefab;
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

    public bool LoadLevel(int levelIndex)
    {
        DisableLevels();

        if (levelIndex >= transform.childCount)
            return false;

        transform.GetChild(levelIndex).gameObject.SetActive(true);
        return true;
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
    public void AddSegmentIndexToLevel(int segmentIndex, int levelIndex)
    {
        Transform levelTrans;
        if (transform.childCount <= levelIndex)
        {
            var lvlGo = new GameObject();
            lvlGo.transform.SetParent(this.transform);
            lvlGo.transform.localPosition = Vector3.zero;
            lvlGo.transform.localRotation = Quaternion.identity;
            lvlGo.name = "Level " + (levelIndex + 1).ToString();
            levelTrans = lvlGo.transform;
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
        var go = (GameObject)PrefabUtility.InstantiatePrefab(segment.Prefab, levelTrans);
        go.transform.position = new Vector3(0f, 0f, spawnPos);
        go.transform.localRotation = Quaternion.identity;
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
