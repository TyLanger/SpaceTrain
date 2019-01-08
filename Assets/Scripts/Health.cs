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

    // should the Health class play sounds?
    // it seems to make sense as hitting something with health should play a hit sound
    AudioSource hitSound;

    // Use this for initialization
    void Start () {
        currentHp = baseHp;
        hitSound = GetComponent<AudioSource>();
	}

    public void ResetHealth()
    {
        // called when the object spawns or respawns
        currentHp = baseHp;
    }

    public void TakeDamage(float damage)
    {
        if(hitSound != null && !hitSound.isPlaying)
        {
            hitSound.Play();
        }

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
