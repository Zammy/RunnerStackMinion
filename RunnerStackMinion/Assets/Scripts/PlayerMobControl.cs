using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerMobControl : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] GameObject MobPrefab;

    [SerializeField] float CohesionForce = 1f;
    [SerializeField] float MobMaxSpeed = 1f;

    [Header("Refs")]
    [SerializeField] TextMeshProUGUI SpawnCountText;


    [Header("Debug")]
    [SerializeField] int SpawnOnStartup = 10;
    [SerializeField] int _spawned;

    public List<Rigidbody> Mobs { get; set; }

    void Awake()
    {
        Mobs = new List<Rigidbody>();
    }

    void Start()
    {
        _spawned = 0;
        Spawn();
    }

    [ContextMenu("Spawn")]
    public void Spawn()
    {
        for (int i = 0; i < SpawnOnStartup; i++)
        {
            SpawnMob();
        }
    }

    public void SpawnMob()
    {
        var spawnTranslation = Random.insideUnitCircle;
        var spawnTranslation3d = new Vector3(spawnTranslation.x, 0f, spawnTranslation.y);
        var mobGo = Instantiate(MobPrefab, PlayerMovement.I.transform.position + spawnTranslation3d, Quaternion.identity, transform);
        Mobs.Add(mobGo.GetComponent<Rigidbody>());

        _spawned++;
        //TODO: not its place here
        SpawnCountText.text = _spawned.ToString();
    }

    public void DespawnMob()
    {
        int index = Random.Range(0, Mobs.Count);
        Destroy(Mobs[index].gameObject);
        Mobs.RemoveAt(index);
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
}
