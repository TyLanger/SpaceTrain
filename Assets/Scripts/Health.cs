using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Health : MonoBehaviour {

    // Used by things that have health
    // player, enemies, train cars


    public float baseHp = 100;
    [SerializeField]
    float currentHp;

    public event Action OnDeath;
    public event Action OnDamage;


    // Use this for initialization
    void Start () {
        currentHp = baseHp;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ResetHealth()
    {
        // called when the object spawns or respawns
        currentHp = baseHp;
    }

    public void TakeDamage(float damage)
    {
        if(OnDamage != null)
        {
            OnDamage();
        }
        currentHp -= damage;
        if(currentHp <= 0)
        {
            currentHp = 0;
            Death();
        }
    }

    void Death()
    {
        //Destroy(gameObject);
        if(OnDeath != null)
        {
            OnDeath();
        }
    }
}
