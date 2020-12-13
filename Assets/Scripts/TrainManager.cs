using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public struct InterceptInfo
{
    public Train train;
    public BoardingLink link;
    public Vector3 position;
    public bool successful; // true if the intercept will hit a link.
    // false if there was no intercept and this is just the closest you could get
    public bool allTrainsChecked;
}

public class TrainManager : MonoBehaviour
{
    // Represents the whole train
    // Train Class is one for each car

    public static TrainManager Instance { get; private set; }

    Train[] allTrains;
    public int indexBoardable
    {
        private set;
        get;
    }

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // order the trains.
        // whichever train is at the top of the screen is first
        // assuming the train travels up in reference to the screen
        allTrains = FindObjectsOfType<Train>().OrderBy(t => -t.transform.position.z).ToArray();
        for (int i = 0; i < allTrains.Length; i++)
        {
            allTrains[i].SetTrainIndex(i);
            if(allTrains[i].CanBoard(TrainBoardable, true))
            {
                // can already board the train
                TrainBoardable();
            }
        }
    }

    void TrainBoardable()
    {
        //Debug.Log("A train is boardable");
        // assume trains are boardable in order. 
        indexBoardable++;
    }

    public Train GetTrain(int index)
    {
        // don't check if in bounds.
        // should never be out of bounds so it's ok to throw an error and have the game break. Easier to find bugs
        return allTrains[index];
    }

    public Stockpile[] GetAllStockpiles()
    {
        Stockpile[] allStockpiles = new Stockpile[0];
        for (int i = 0; i < allTrains.Length; i++)
        {
            allStockpiles = allStockpiles.Concat(allTrains[i].GetAllStockpiles()).ToArray();
        }
        return allStockpiles;
    }

    public InterceptInfo TryInterceptTrain(Vector3 startPos)
    {
        Debug.Log("Running base TryIntercept. Shouldn't be run. Defaults are outdated");
        // if no preference, try the first train
        return TryInterceptTrain(allTrains[0], allTrains.Length, 5, 25, 5, startPos, 1f);
    }

    public InterceptInfo TryInterceptTrain(Train targetTrain, int numAdditionalTrains, int timeMin, int timeMax, int timeStep, Vector3 startPos, float moveSpeed, bool oldStyle = false)
    {
        //Debug.Log("Trying to intercept: " +targetTrain.gameObject.name);
        if(oldStyle)
        {
            return TryInterceptTrain(targetTrain, numAdditionalTrains, timeMin, timeMax, timeStep, startPos, moveSpeed);
        }

        InterceptInfo intercept = new InterceptInfo
        {
            successful = false,
            allTrainsChecked = true
        };

        int targetIndex = System.Array.IndexOf(allTrains, targetTrain);

        Vector3[] boardingPoints;
        BoardingLink[] boardingLinks;
        float closestMissDst = 0;
        float currentDst = 0;

        for (int t = timeMin; t < timeMax; t += timeStep)
        {
            if(intercept.successful)
            {
                break;
            }

            for (int i = targetIndex; i < indexBoardable; i++)
            {
                //Debug.LogFormat("i-tI > num. i: {0}, targetIndex: {1}, numAdditionalTrains: {2}", i, targetIndex, numAdditionalTrains);
                if(intercept.successful || ((i - targetIndex) > numAdditionalTrains))
                {
                    break;
                }

                bool rightSide = startPos.x > allTrains[i].transform.position.x;
                (boardingPoints, boardingLinks) = allTrains[i].GetBoardingLocationsInTime(t, rightSide);
                for (int j = 0; j < boardingPoints.Length; j++)
                {
                    currentDst = Vector3.Distance(startPos, boardingPoints[j]);
                    float missDistance = currentDst - moveSpeed * (t / Time.fixedDeltaTime);

                    if (missDistance < 0)
                    {
                        intercept.position = boardingPoints[j];
                        intercept.link = boardingLinks[j];
                        intercept.train = allTrains[i];
                        intercept.successful = true;
                        Debug.Log(gameObject.name + ": Successful Boarding at time = " + t + " at " + intercept.link);

                        //Debug.DrawLine(boardingPoints[j], startPos, Color.green, 15f);

                        break;
                    }
                    else
                    {
                        if (missDistance < closestMissDst || closestMissDst == 0)
                        {
                            closestMissDst = missDistance;
                            // keep looking for a better intercept
                            // but keep track of the best so far
                            intercept.position = boardingPoints[j];
                            intercept.link = boardingLinks[j];
                            intercept.train = allTrains[i];
                        }
                    }
                }
            }
        }

        if (!intercept.successful)
        {
            Debug.Log("No successful board. Moving close as I can get.");
            //Debug.DrawLine(intercept.position, startPos, new Color(1, 0, 1, 1f), 15f); // new Color(1, 1, 0, 0.5f)

            // not a success
            // that means the values are just the closest
            // check if we checked all of the trains or if some of the trains we wanted to check were unboardable
            if (targetIndex + numAdditionalTrains >= indexBoardable)
            {
                // some trains just weren't ready to be boarded
                intercept.allTrainsChecked = false;
            }
        }

        return intercept;
    }

    /// <summary>
    /// Returns a point where you can go to intercept.
    /// Also returns the train you will end up boarding.
    /// And the boarding link you will use.
    /// </summary>
    /// <param name="targetTrain">Starting Train </param>
    /// <param name="numAdditionalTrains">How many more trains you want to check</param>
    /// <param name="timeMin">The soonest you want to intercept. In seconds.</param>
    /// <param name="timeMax">The latest you want to intercept. In seconds.</param>
    /// <param name="timeStep">The granularity of the tests between min and max. In seconds.</param>
    /// <param name="moveSpeed">Your moveSpeed.</param>
    /// <returns></returns>
    public InterceptInfo TryInterceptTrain(Train targetTrain, int numAdditionalTrains, int timeMin, int timeMax, int timeStep, Vector3 startPos, float moveSpeed)
    {
        //Debug.Log("Enter TryInterceptTrain");
        //Debug.LogFormat("Target Train: {0}",targetTrain.gameObject);

        //Debug.Log("Enemy MoveSpeed: " + moveSpeed);

        InterceptInfo intercept = new InterceptInfo
        {
            successful = false,
            allTrainsChecked = true
        };

        int targetIndex = System.Array.IndexOf(allTrains, targetTrain);

        // if you give a targetTrain
        // try that train first and then check other trains behind it
        // don't check trains in front of it. Unlikely you'll be able to get to them. And if you could, you could probably get to the target train
        for (int i = targetIndex; i < indexBoardable; i++)
        {
            if(intercept.successful || (i - targetIndex > numAdditionalTrains))
            {
                // break if already found an intercept
                // or if you've run out of trains you wanted to check (numAdditionalTrains)
                break;
            }

            // iterate through the trains

            Vector3[] boardingPoints;
            BoardingLink[] boardingLinks;
            float closestMissDst = 0;
            float currentDst = 0;


            // Finds A close enough boarding point
            // not THE closest boarding point yet
            for (int t = timeMin; t <= timeMax; t += timeStep)
            {
                if(intercept.successful)
                {
                    break;
                }

                // pick the right boarding links if I'm to the right side of the train. Otherwise, use the left links
                bool rightSide = startPos.x > allTrains[i].transform.position.x;
                (boardingPoints, boardingLinks) = allTrains[i].GetBoardingLocationsInTime(t, rightSide);
                for (int j = 0; j < boardingPoints.Length; j++)
                {
                    currentDst = Vector3.Distance(startPos, boardingPoints[j]);
                    // x/ time.fixedDeltaTime is the number of fixed updates there are in x seconds

                    float missDistance = currentDst - moveSpeed * (t / Time.fixedDeltaTime);
                    // how much you miss your target by

                    // if miss distance < 0, you didn't miss; you'll make it
                    if (missDistance < 0)
                    {
                        intercept.position = boardingPoints[j];
                        intercept.link = boardingLinks[j];
                        intercept.train = allTrains[i];
                        intercept.successful = true;
                        Debug.Log(gameObject.name + ": Successful Boarding at time = " + t + " at "+intercept.link);

                        //Debug.DrawLine(boardingPoints[j], startPos, Color.green, 15f);

                        break;
                    }
                    else
                    {
                        // old way makes the backup the closest boarding position
                        // but what I want is the closest miss
                        // Where if I tried to get there, I'd only be 1m short
                        // the closest boarding pos is not that.

                        if (missDistance < closestMissDst || closestMissDst == 0)
                        {
                            closestMissDst = missDistance;
                            // keep looking for a better intercept
                            // but keep track of the best so far
                            intercept.position = boardingPoints[j];
                            intercept.link = boardingLinks[j];
                            intercept.train = allTrains[i];
                        }
                    }

                    // Visualization lines
                    Vector3 dir = (startPos - boardingPoints[j]).normalized;
                    dir *= (currentDst - moveSpeed * (t / Time.fixedDeltaTime)); // this is the miss distance
                    float percent = (float)t / timeMax;
                    //Debug.DrawRay(boardingPoints[j], dir, new Color(percent, percent, 0, 1), 15f);
                    //Debug.DrawLine(boardingPoints[j])
                }
            }
        }

        if(!intercept.successful)
        {
            Debug.Log("No successful board. Moving close as I can get.");
            //Debug.DrawLine(intercept.position, startPos, new Color(1, 0, 1, 1f), 15f); // new Color(1, 1, 0, 0.5f)
            
            // not a success
            // that means the values are just the closest
            // check if we checked all of the trains or if some of the trains we wanted to check were unboardable
            if (targetIndex+numAdditionalTrains >= indexBoardable)
            {
                // some trains just weren't ready to be boarded
                intercept.allTrainsChecked = false;
            }
        }

        Debug.LogFormat("Info: position: {0}, link: {1}, train: {2}, success: {3}, allChecked: {4}", intercept.position, intercept.link, intercept.train, intercept.successful, intercept.allTrainsChecked);

        //Debug.Break();

        return intercept;
    }
}
