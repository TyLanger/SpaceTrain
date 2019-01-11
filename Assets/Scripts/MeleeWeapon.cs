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
        Health h = col.gameObject.GetComponent<Health>();
        if (h != null)
        {
            // hit something with hp
            h.TakeDamage(damage);

        }
    }
}
