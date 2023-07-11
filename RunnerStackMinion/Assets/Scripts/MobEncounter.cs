using UnityEngine;

public class MobEncounter : MonoBehaviour
{
    [Header("Settings")]
    public int EnemyMobCount = 10;

    [Header("Refs")]
    [SerializeField] Transform SpawnPoint;

    void Start()
    {
        var mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
        for (int i = 0; i < EnemyMobCount; i++)
        {
            var spawnTranslation = Random.insideUnitCircle;
            var spawnTranslation3d = new Vector3(spawnTranslation.x, 0f, spawnTranslation.y);
            mobControl.SpawnMobAt(MobType.Enemy, SpawnPoint.position + spawnTranslation3d);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ServiceLocator.Instance.GetService<IGameController>()
                .RaiseEvent(new MobEncounterEvent()
                {
                    Pos = transform.position
                });

            this.enabled = false;
        }
    }
}
