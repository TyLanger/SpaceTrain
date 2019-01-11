using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThumbCursor : MonoBehaviour {

    public float moveSpeed = 0.1f;

    Transform playerTrans;

    bool isLockedOn = false;
    Transform lockOnTarget;

    Vector3 lockOnOffset;
    Vector3 input = Vector3.zero;
    Vector3 playerOffset;

	// Use this for initialization
	void Start () {

        playerOffset = transform.position;
    }
	
	// Update is called once per frame
    void Update()
    {
        input = new Vector3(Input.GetAxisRaw("RightHorizontal"), 0, Input.GetAxisRaw("RightVertical"));

    }

	void FixedUpdate () {


        if (isLockedOn)
        {
            lockOnOffset = Vector3.MoveTowards(lockOnOffset, lockOnOffset + input, moveSpeed);

            // follow your target when they move
            if (lockOnTarget != null)
            {
                //transform.position = lockOnTarget.position + lockOnOffset + playerOffset;
                transform.position = lockOnTarget.position + lockOnOffset;
            }
        }
        else
        {
            playerOffset = Vector3.MoveTowards(playerOffset, playerOffset + input, moveSpeed);

            if (playerTrans != null)
            {
                transform.position = playerTrans.position + playerOffset;
            }
            else
            {
                transform.position = playerOffset;
            }
        }
    }

    public void SetPlayer(Transform playerTransform)
    {
        playerTrans = playerTransform;
    }

    public Vector3 GetAimLocation()
    {
        if(isLockedOn)
        {
            return lockOnTarget.position;
        }
        else
        {
            return transform.position;
        }
    }

    void LostLockOn()
    {
        //Debug.Log("Lost lock");
        Enemy e = lockOnTarget.GetComponent<Enemy>();
        if(e != null)
        {
            //Debug.Log("Unsub");
            e.OnDeath -= LostLockOn;
        }
        isLockedOn = false;
        lockOnTarget = null;
        playerOffset = (transform.position - playerTrans.position);
    }

    void OnTriggerEnter(Collider col)
    {
        if (!isLockedOn)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy e = col.GetComponent<Enemy>();
                if(e != null)
                {
                    //Debug.Log("Sub");

                    e.OnDeath += LostLockOn;
                }
                // lock on to this enemy
                isLockedOn = true;
                lockOnTarget = col.transform;
                transform.position = lockOnTarget.position;
                lockOnOffset = (transform.position - lockOnTarget.position);
            }
        }
    }

    void OnTriggerExit(Collider col)
    {
        // Can't rely on this to clear the lock on when the enemy dies
        // when they die, they don't leave the trigger
        if (isLockedOn)
        {
            if (col.CompareTag("Enemy"))
            {
                //Debug.Log("Trigger Exit");
                LostLockOn();
            }
        }
    }
}
