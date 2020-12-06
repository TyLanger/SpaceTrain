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

    float timeBetweenGroupSpawns = 15;
    float timeBetweenEnitiesInGroup = 1;
    float timeOfNextGroup = 0;
    int sizeOfGroup = 5;

    int currentGroupSpawns = 0;
    bool groupSpawning = false;

    float spawnMinDist = 35;
    float spawnMaxDist = 55;

    // What to spawn
    public Enemy[] enemyTypes;

    int lastEnemyTypeSpawnedIndex;
    Vector3 lastSpawnPosition;

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
        lastEnemyTypeSpawnedIndex = 0;
        lastSpawnPosition = Vector3.zero;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Time.time > timeOfNextGroup)
        {
            timeOfNextGroup = Time.time + timeBetweenGroupSpawns;
            StartGroupSpawning();
        }
        if (Time.time > timeOfNextSpawn)
        {
            if (groupSpawning)
            {
                timeOfNextSpawn = Time.time + timeBetweenEnitiesInGroup;
                currentGroupSpawns++;

                // ensures you spawn the same enemies in the same general location
                Spawn(lastEnemyTypeSpawnedIndex, lastSpawnPosition + new Vector3(0, 0, 1));

                if(currentGroupSpawns >= sizeOfGroup)
                {
                    groupSpawning = false;
                }
            }
            else
            {
                timeOfNextSpawn = Time.time + timeBetweenSpawns;
                // spawn something
                if (thingsAlive < maxNumEnemies)
                {
                    // spawn something randomly
                    Spawn();
                }
            }
            
        }
    }

    int GetRandomEnemyType()
    {
        return Random.Range(0, enemyTypes.Length);
    }

    Vector3 GetRandomSpawnLocation()
    {
        int trainPathIndex = headTrain.GetTrainPathIndex();
        // spawn the enemy a little ahead of the train
        Vector3 whereToSpawn = path.GetPoint(trainPathIndex + 15);

        // randomize how far away from the train
        float dist = Random.Range(spawnMinDist, spawnMaxDist);
        whereToSpawn.x = -dist * Mathf.Sign(whereToSpawn.x);

        return whereToSpawn;
    }

    /// <summary>
    /// Spawn with given parameters
    /// </summary>
    void Spawn(int enemyIndex, Vector3 spawnLocation)
    {
        lastEnemyTypeSpawnedIndex = enemyIndex;
        lastSpawnPosition = spawnLocation;

        Enemy copy = (Enemy)Instantiate(enemyTypes[enemyIndex], spawnLocation, Quaternion.identity);
        thingsAlive++;
        copy.trainEngine = headTrain; // randomize the train someday
        copy.OnDeath += EntityDies;
        copy.Initialize();
    }

    /// <summary>
    /// Spawn a random enemy at a random location
    /// </summary>
    void Spawn()
    {
        Spawn(GetRandomEnemyType(), GetRandomSpawnLocation());
    }

    void StartGroupSpawning()
    {
        groupSpawning = true;
        currentGroupSpawns = 0;
        timeOfNextSpawn = Time.time;
        lastSpawnPosition = GetRandomSpawnLocation();
        lastEnemyTypeSpawnedIndex = GetRandomEnemyType();
    }

    void EntityDies()
    {
        thingsAlive--;
    }
}
