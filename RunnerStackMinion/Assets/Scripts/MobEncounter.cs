using System.Collections.Generic;
using UnityEngine;

public class MobEncounter : MonoBehaviour
{
    [Header("Settings")]
    public int EnemyMobCount = 10;

    [Header("Refs")]
    [SerializeField] Transform SpawnPoint;
    [SerializeField] Transform BattlefieldPoint;

    List<Mob> _mobs;

    void Start()
    {
        _mobs = new List<Mob>();

        var mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
        for (int i = 0; i < EnemyMobCount; i++)
        {
            var spawnTranslation = Random.insideUnitCircle;
            var spawnTranslation3d = new Vector3(spawnTranslation.x, 0f, spawnTranslation.y);
            _mobs.Add(mobControl.SpawnMobAt(MobType.Enemy, SpawnPoint.position + spawnTranslation3d));
        }
    }

    void FixedUpdate()
    {
        for (int i = 0; i < _mobs.Count; i++)
        {
            var mob = _mobs[i];
            var toSpawnPoint = SpawnPoint.position - mob.Body.position;
            mob.Body.AddForce(toSpawnPoint, ForceMode.VelocityChange);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ServiceLocator.Instance.GetService<IGameController>()
                .RaiseEvent(new MobEncounterEvent()
                {
                    BattlefieldPos = BattlefieldPoint.position,
                    EnemiesCount = EnemyMobCount
                });

            this.enabled = false;
        }
    }
}
