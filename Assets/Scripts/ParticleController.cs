using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour {

    public float timeAlive = 0.5f;

	// Use this for initialization
	void Start () {
        Invoke("DestroyThis", timeAlive);
	}
	
	void DestroyThis()
    {
        Destroy(gameObject);
    }
}
