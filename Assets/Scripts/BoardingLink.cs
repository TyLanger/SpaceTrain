using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardingLink : MonoBehaviour {

    public Transform trainPoint;
    public Transform groundPoint;

    Train train;
    Vector3 trainOffset;
    Vector3 groundOffset;
    //Vector3 onboardOffset;



    // Debugging
    Vector3 trainPointLookahead;
    Vector3 groundPointLookahead;

    [Range(0.1f, 0.8f)]
    public float drawRadius = 0.1f;

	// Use this for initialization
	void Start () {
        train = GetComponentInParent<Train>();
        if(train == null)
        {
            Debug.Log("Boarding Link has no train parent");
        }
        // is storing the offset of the transform to the train AND the transform to the ground child redundant? probably
        // could probably just store offset of train to ground point
        trainOffset = train.transform.position - transform.position;
        groundOffset = groundPoint.position - transform.position;
        //onboardOffset = trainPoint.position - transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		// update the height of the ground point to remain attached to the ground?
	}

    public Vector3 GroundPointAtPosition(Vector3 trainPos, float trainAngle)
    {
        // given a train position and angle,
        // where will the ground point be?

        // find the positon by rotating the offset (calcuated in start) and adding to the train pos
        Vector3 myPos = trainPos - new Vector3(trainOffset.x * Mathf.Cos(trainAngle) - trainOffset.z * Mathf.Sin(trainAngle), 0, trainOffset.x * Mathf.Sin(trainAngle) + trainOffset.z * Mathf.Cos(trainAngle));
        Vector3 groundPos = myPos + new Vector3(groundOffset.x * Mathf.Cos(trainAngle) - groundOffset.z * Mathf.Sin(trainAngle), 0, groundOffset.x * Mathf.Sin(trainAngle) + groundOffset.z * Mathf.Cos(trainAngle));

        // for OnDrawGizmos
        //groundPointLookahead = groundPos; 

        return groundPos; 
    }

    void OnDrawGizmos()
    {
        // train point is green
        Gizmos.color = Color.green;
        if(trainPoint != null)
            Gizmos.DrawWireSphere(trainPoint.position, drawRadius);

        // ground point is magenta
        Gizmos.color = Color.magenta;
        if(groundPoint != null)
            Gizmos.DrawWireSphere(groundPoint.position, drawRadius);

        Gizmos.color = Color.cyan;
        if(groundPoint != null && trainPoint != null)
            Gizmos.DrawLine(trainPoint.position, groundPoint.position);


        Gizmos.color = Color.blue;
        if (groundPointLookahead != Vector3.zero)
            Gizmos.DrawWireSphere(groundPointLookahead, drawRadius);
    }
}
