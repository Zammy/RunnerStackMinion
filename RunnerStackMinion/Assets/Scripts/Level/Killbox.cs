using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Killbox : MonoBehaviour
{
    IPlayerMobControl _mobControl;

    void Start()
    {
        _mobControl = ServiceLocator.Instance.GetService<IPlayerMobControl>();
    }

    void OnTriggerEnter(Collider other)
    {
        var mob = other.GetComponentInParent<Mob>();
        if (mob)
        {
            _mobControl.DespawnMob(mob);
        }
    }
}
