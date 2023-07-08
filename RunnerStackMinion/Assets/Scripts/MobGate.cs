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
    public float DelayBetweenSpawns;

    [Header("Debug")]
    public bool Triggered;
    WaitForSeconds _delay;

    public UnityEvent OnDoorTriggered;

    void Start()
    {
        _delay = new WaitForSeconds(DelayBetweenSpawns);
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

        while (spawnAmount > 0)
        {
            PlayerMobControl.I.SpawnMobAt(SpawnPoint.position);

            spawnAmount--;
            yield return _delay;
        }

        PlayerMovement.I.Paused = false;
    }

}
