using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour {

    public float aggroRange = 5;
    public float attackRange = 1;

    GameObject target;

    float timeBetweenDecisions = 0.5f;

    

	// Use this for initialization
	void Start () {
        //GetComponent<Health>().OnDamage += TookDamage;
        // This class is depreciated
        Debug.Log("Does anything use this?");
	}
	
	// Update is called once per frame
	void FixedUpdate () {

	}

    IEnumerator EvaluateDecisions()
    {
        // if(hp > 50%)
        // try to attack
        // if (distToTarget > attackRange)
        // move towards target
        // MoveTowards(target)
        // else retreat?
        while(true)
        {
            if(CanAttack())
            {
                Attack();
            }
            else
            {
                MoveToGoal(target);
            }


            yield return new WaitForSeconds(timeBetweenDecisions);
        }
    }

    void FindTarget()
    {
        // search through all valid targets
        // find the target with the highest priority
        // nearest train car should be highest
        // players don't have priority until they attack the enemy or get right in its way
    }

    bool CanAttack()
    {
        return true;
    }

    void Attack()
    {

    }

    void StartMovingTowards(Transform goal)
    {
        // tell the enemy to start moving
        // it should move over time until told to stop????
        //GetComponent<Enemy>().StartMoving(goal);
    }

    void StopMoving()
    {
        // tell the enemy to stop moving
        // if it has reached its destination
        //GetComponent<Enemy>().STopMoving();
    }

    void MoveToGoal(GameObject goal)
    {
        
    }

    void Charge()
    {
        // charge in a straight line until you hit something
    }

    void TookDamage(GameObject aggressor, float damage)
    {
        // swap targets
        // unless you've taken more damage from a different target
        Dictionary<GameObject, float> threats = new Dictionary<GameObject, float>();
        // instead of adding, needs to chack if already in there and update the damageTaken
        if( threats.ContainsKey(aggressor))
        {
            threats[aggressor] += damage;
            // or
            // threats[aggressor] = threats[aggressor] + damage; 
        }
        else
        {
            threats.Add(aggressor, damage);

        }

        // get the target with the highest value
        // but don't want the largets values, but rather the key that leads to it
        // target = threats.Values.Max()
        // default to this one
        float maxValue = threats[aggressor];
        target = aggressor;
        foreach (var key in threats.Keys)
        {
            if(threats[key] > maxValue)
            {
                maxValue = threats[key];
                target = key;
            }
        }
        // the target is now the threat with the highest value (most damage dealt to this enemy)
        // however, that probably won't be what I actually want
        // it's probably better to just have the enemy target whomever is closest. Otherwise, the enemy would be really easy to kite.
        // Players would just take turns attacking the enemy. It would spend all of its time just walking between the 2 players
    }
}
