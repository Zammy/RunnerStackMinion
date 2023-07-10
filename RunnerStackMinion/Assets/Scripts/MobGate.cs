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
    WaitForSeconds _delay;

    public UnityEvent OnDoorTriggered;

    void Start()
    {
        UpdateUI();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Triggered && other.CompareTag("Player"))
        {
            Triggered = true;
            ConnectedGate.Triggered = true;

            OnDoorTriggered?.Invoke();
            GetComponentInChildren<Renderer>().material = TriggerMaterial;

            Execute();
        }
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
                    mobDelta = PlayerMobControl.I.Spawned * Value - PlayerMobControl.I.Spawned;
                    break;
                }
            case MobGateType.Divide:
                {
                    mobDelta = -PlayerMobControl.I.Spawned / Value;
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
                if (PlayerMobControl.I.Spawned == 0)
                {
                    Debug.Log("GAME OVER!");
                    break;
                }
                PlayerMobControl.I.DespawnMob();
            }
        }
    }

    IEnumerator DoSpawnMobs(int spawnAmount)
    {
        PlayerMovement.I.Paused = true;

        float spawnRate = (float)spawnAmount / TotalSpawnTime;
        float timeAccumulator = 0f;
        while (spawnAmount > 0)
        {
            yield return null;

            timeAccumulator += Time.deltaTime;
            int spawnThisFrame = Mathf.RoundToInt(spawnRate * timeAccumulator);
            for (int i = 0; i < spawnThisFrame && spawnAmount > 0; i++)
            {
                PlayerMobControl.I.SpawnMobAt(SpawnPoint.position);
                spawnAmount--;
                timeAccumulator = 0f;
            }
        }

        PlayerMovement.I.Paused = false;
    }

}