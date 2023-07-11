using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayerMobControl))]
public class PlayerMobControlEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!Application.isPlaying)
            return;

        var playerMobControl = (PlayerMobControl)target;
        if (GUILayout.Button("Spawn"))
        {
            playerMobControl.SpawnInitial();
        }
        if (GUILayout.Button("Add Mob"))
        {
            playerMobControl.SpawnMobAtPlayer();
        }
        if (GUILayout.Button("Remove Mob"))
        {
            playerMobControl.DespawnRandomPlayerMob();
        }
        if (GUILayout.Button("Half mob size"))
        {
            playerMobControl.Despawn();
        }
    }
}
