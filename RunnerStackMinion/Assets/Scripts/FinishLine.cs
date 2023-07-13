using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishLine : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ServiceLocator.Instance.GetService<IGameController>()
                .RaiseEvent(new LevelFinishedEvent());
            this.enabled = false;
        }
    }
}
