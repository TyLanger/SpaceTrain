using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{

    // Handles spawning enemies
    // spawn something every few seconds
    // figure out where the train is so I know where to spawn stuff
    // Train.frontWheelIndex is where the train is. It's private tho
    // still need to call Path.GetPoint(index) to get a V3
    // don't spawn stuff if there are still things alive?

    // When to spawn
    float timeBetweenSpawns = 3;
    float timeOfNextSpawn = 0;
    int maxNumEnemies = 15; // don't spawn more if this many already spawned

    // What to spawn
    public Enemy[] enemyTypes;

    // What I have spawned
    // reference to stuff I've spawned
    // collection stuffIveSpawned
    int thingsAlive = 0;

    Path path;
    public Train headTrain;
    public Train secondTrain;

    // Start is called before the first frame update
    void Start()
    {
        path = FindObjectOfType<Path>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(Time.time > timeOfNextSpawn)
        {
            timeOfNextSpawn = Time.time + timeBetweenSpawns;
            // spawn something
            if (thingsAlive < maxNumEnemies)
            {
                Spawn();
            }
        }
    }

    void Spawn()
    {
        // where the train is on the tracks
        // maybe I could have just gotten the train's trans.pos?
        //Vector3 trainPos = path.GetPoint(headTrain.GetTrainPathIndex());

        int trainPathIndex = headTrain.GetTrainPathIndex();
        // spawn the enemy a little ahead of the train
        Vector3 whereToSpawn = path.GetPoint(trainPathIndex + 15);

        // move the spawn off to one side
        // if the train is to the left of the origin, spawn to the right.
        // between -100, 100 in x is probably on screen

        whereToSpawn.x = -45 * Mathf.Sign(whereToSpawn.x);

        int r = Random.Range(0, enemyTypes.Length);

        // setting it to false is a hack to do stuff before Enemy.Awake()
        // I should probably just change Awake
        enemyTypes[r].gameObject.SetActive(false);
        Enemy copy = (Enemy) Instantiate(enemyTypes[r], whereToSpawn, Quaternion.identity);
        thingsAlive++;
        copy.trainEngine = headTrain; // randomize the train someday
        copy.OnDeath += EntityDies;
        // add this enemy to the num enemies tracker. And track when it dies
        copy.gameObject.SetActive(true);
    }

    void EntityDies()
    {
        thingsAlive--;
    }
}
