using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Weapon {

    public float attackCooldown = 1;
    float attackCooldownLeft = 0;
    public float damage;


    public float windupTime = 1;
    // the amount of time the collider is enabled for
    public float timeAttackEnabled = 0.5f;
    public float recoveryTime = 1;

    Collider damageCollider;

	// Use this for initialization
	void Start () {
        isMelee = true;
        damageCollider = GetComponent<Collider>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if(attackCooldownLeft > 0)
        {
            attackCooldownLeft -= Time.fixedDeltaTime;
        }
	}

    public override void Attack()
    {
        base.Attack();
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        if(CanAttack())
        {
            Debug.Log("Starting Melee Attack");
            attackCooldownLeft = attackCooldown;

            //yield return new WaitForSeconds(windupTime);

            damageCollider.enabled = true;
            yield return new WaitForSeconds(timeAttackEnabled);
            damageCollider.enabled = false;

            //yield return new WaitForSeconds(recoveryTime);
        }

        yield return null;
    }

    bool CanAttack()
    {
        return (attackCooldownLeft <= 0);
    }

    void OnTriggerEnter(Collider col)
    {
        // check the parent of the object
        // trains have their colliders as child objects
        // this may make the enemy not able to damage the player while the player is on the train
        // the player's parent is the train so will the train take damage?
        // the train is also the enemy's parent so will the player be able to hurt them or will theu just hurt the train?
        Health h = col.gameObject.GetComponentInParent<Health>();
        if (h != null)
        {
            // hit something with hp
            h.TakeDamage(damage);

        }
    }
}
