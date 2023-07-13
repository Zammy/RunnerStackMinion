using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public enum MobGateType
{
    Add,
    Subtract,
    Multiply,
    Divide,
}

public class MobGate : MonoBehaviour
{
    public MobGateType Type;
    public int Value;

    [Header("Ref")]
    [SerializeField] Transform SpawnPoint;
    [SerializeField] TextMeshProUGUI BillboardText;
    [SerializeField] MobGate ConnectedGate;


    [Header("Settings")]
    public Material TriggerMaterial;
    public float TotalSpawnTime = 1f;

    [Header("Debug")]
    public bool Triggered;

    IPlayerMobControl _mobControl;
    Material _normalMaterial;

    void Start()
    {
        _mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
        _normalMaterial = GetComponentInChildren<Renderer>().material;

        UpdateUI();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Triggered && other.CompareTag("Player"))
        {
            Triggered = true;
            ConnectedGate.Triggered = true;

            GetComponentInChildren<Renderer>().material = TriggerMaterial;

            Execute();
        }
    }

    public void Reset()
    {
        Triggered = false;
        GetComponentInChildren<Renderer>().material = _normalMaterial;
    }

    void UpdateUI()
    {
        switch (Type)
        {
            case MobGateType.Add:
                {
                    BillboardText.text = $"+{Value}";
                    break;
                }
            case MobGateType.Subtract:
                {
                    BillboardText.text = $"-{Value}";
                    break;
                }
            case MobGateType.Multiply:
                {
                    BillboardText.text = $"x {Value}";
                    break;
                }
            case MobGateType.Divide:
                {
                    BillboardText.text = $"÷ {Value}";
                    break;
                }
            default:
                break;
        }
    }

    private void Execute()
    {
        int mobCount = _mobControl.GetMobCount(MobType.Player) + 1;
        int mobDelta = 0;
        switch (Type)
        {
            case MobGateType.Add:
                {
                    mobDelta = Value;
                    break;
                }
            case MobGateType.Subtract:
                {
                    mobDelta = -Value;
                    break;
                }
            case MobGateType.Multiply:
                {
                    mobDelta = mobCount * Value - mobCount;
                    break;
                }
            case MobGateType.Divide:
                {
                    mobDelta = -mobCount / Value;
                    break;
                }
            default:
                break;
        }

        if (mobDelta > 0)
        {
            StartCoroutine(DoSpawnMobs(mobDelta));
        }
        else
        {
            for (int i = 0; i < -mobDelta; i++)
            {
                _mobControl.DespawnRandomPlayerMob();
            }
        }
    }

    IEnumerator DoSpawnMobs(int spawnAmount)
    {
        float spawnRate = (float)spawnAmount / TotalSpawnTime;
        float timeAccumulator = 0f;
        while (spawnAmount > 0)
        {
            yield return null;

            timeAccumulator += Time.deltaTime;
            int spawnThisFrame = Mathf.RoundToInt(spawnRate * timeAccumulator);
            for (int i = 0; i < spawnThisFrame && spawnAmount > 0; i++)
            {
                _mobControl.SpawnMobAt(MobType.Player, SpawnPoint.position);
                spawnAmount--;
                timeAccumulator = 0f;
            }
        }
    }
}
