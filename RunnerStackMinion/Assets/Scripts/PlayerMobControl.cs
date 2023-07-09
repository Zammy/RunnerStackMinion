using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerMobControl : Monotone<PlayerMobControl>
{
    [Header("Settings")]
    [SerializeField] GameObject MobPrefab;

    [SerializeField] float CohesionForce = 1f;
    [SerializeField] float MobMaxSpeed = 1f;

    [Header("Refs")]
    [SerializeField] TextMeshProUGUI SpawnCountText;


    [Header("Debug")]
    [SerializeField] int SpawnOnStartup = 10;
    public int Spawned;

    public List<Rigidbody> Mobs { get; set; }

    protected override void Awake()
    {
        base.Awake();

        Mobs = new List<Rigidbody>();
    }

    void Start()
    {
        Spawned = 0;
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

    public void SpawnMobAtPlayer()
    {
        var spawnTranslation = Random.insideUnitCircle;
        var spawnTranslation3d = new Vector3(spawnTranslation.x, 0f, spawnTranslation.y);
        SpawnMobAt(PlayerMovement.I.transform.position + spawnTranslation3d);
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
            var toPlayer = PlayerMovement.I.transform.position - mobBody.position;
            mobBody.AddForce(toPlayer.normalized * toPlayer.sqrMagnitude * CohesionForce, ForceMode.VelocityChange);
            mobBody.velocity = Vector3.ClampMagnitude(mobBody.velocity, MobMaxSpeed);
        }
    }

    void UpdateUI()
    {
        //TODO: not its place here
        SpawnCountText.text = Spawned.ToString();
    }
}
