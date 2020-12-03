using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    // Other classes use this to find global variables

    // there can only be one of this class so this is the only field that needs to be static
    public static StatsManager _instance;

    [Header("Movement")]
    public float BaseMoveSpeed = 0.18f;

    public float PlayerMoveMultiplier = 1;
    public float EnemyMoveMultiplier = 0.5f;

    [Header("Projectiles")]
    public float BaseProjectileSpeed = 5f;

    void Awake()
    {
        if(_instance != null)
        {
            Destroy(_instance);
        }
        else
        {
            _instance = this;
        }

        // maybe need this, but it messes up this class in the hierarchy (puts in under the dont destroy tab)
        //DontDestroyOnLoad(this);
    }

    public static StatsManager Instance()
    {
        return _instance;
    }

}
