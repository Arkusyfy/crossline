using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class GameScripts : MonoBehaviour
{
    private readonly List<string> lines = new List<string>();

    private bool doDrugieSetup = true;
    private int drewCount;
    private bool firstPlayerTurn = true;
    private double globalBFactor;
    private double globalTurnCounter;
    private bool isFirstTurn = true;

    
    private int secondLegitBoards = 0;

    
    // private GameObject firstPlayerLines, secondPlayerLines;
    [SerializeField] private GameObject linePrefFirst, linePrefSecond, linesContainer;
    private double localBFactor;
    private int loopCount;

    [SerializeField] private int loopCounter;

    public List<GameObject> pointsArr;
    private Dictionary<GameObject, GameObject> randomBoards = new Dictionary<GameObject, GameObject>();

    private bool validTurn = true;

    [SerializeField] private bool zadaniePierwsze = true;

    // Start is called before the first frame update
    private void Start()
    {
        loopCount = loopCounter;
        validTurn = true;
    }

    // Update is called once per frame
    private void GameEnd()
    {
        localBFactor /= drewCount;


        globalBFactor += localBFactor;

        globalTurnCounter += drewCount;


        Debug.Log("Wygrał gracz " + (!firstPlayerTurn ? "pierwszy" : "drugi.") + " \r\nNa planszy jest " + drewCount +
                  " linii." + "\tLokalny Branching Factor to: " + localBFactor);
        if (loopCounter-- - 1 > 0)
        {
            ResetGame();
        }
        else
        {
            globalBFactor /= loopCount;
            globalTurnCounter /= loopCount;
            Debug.Log("Global Branching Factor: " + globalBFactor + "\r\nLiczba prób: " + loopCount);
            Debug.Log("Średnia ilość ruchów: " + globalTurnCounter +
                      "\r\nGłębokość drzewa: " + Mathf.Pow((float) globalBFactor, (float) globalTurnCounter));
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            Application.Quit();
        }
    }

    private void GameEndDwa()
    {
        Debug.Log("Wygrał gracz " + (!firstPlayerTurn ? "pierwszy" : "drugi.") + " \r\nNa planszy jest " + drewCount +
                  " linii.");
        if (loopCounter-- - 1 > 0)
        {
            ResetGame();
        }
        else
        {   
            Debug.Log("Ilość poprawnych planszy: "+secondLegitBoards);
            Debug.Log("Liczba prób: " + loopCount);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            Application.Quit();
        }
    }

    
    private IEnumerator resDelat()
    {
        yield return new WaitForSeconds(7.5f);
        ResetGame();
    }

    private void ResetGame()
    {
        localBFactor = 0;
        firstPlayerTurn = true;
        drewCount = 0;
        lines.Clear();
        foreach (Transform child in linesContainer.transform) Destroy(child.gameObject);
        isFirstTurn = true;
        validTurn = true;
        doDrugieSetup = true;
        randomBoards.Clear();
    }

    private void Update()
    {
        if (zadaniePierwsze)
        {
            if (validTurn && !CheckPossibleLines())
            {
                validTurn = false;
                GameEnd();
            }
            else if (validTurn)
            {
                localBFactor += CountPossibleMoves();
            }
        }
        else if (doDrugieSetup)
        {
            doDrugieSetup = false;
            SetupDrugie();
        }
        else
        {
            bool isLegitBoard = true;
            foreach (var keyValuePair in randomBoards)
            {
                if (isFirstTurn)
                {
                    isFirstTurn = false;
                    DrawLine(keyValuePair.Key, keyValuePair.Value);
                    firstPlayerTurn = false;
                    continue;
                }

                
                if (LinesInFront(keyValuePair.Key, keyValuePair.Value) == 1)
                {
                    if(!lines.Contains(keyValuePair.Key.name + keyValuePair.Value.name))
                    {
                        DrawLine(keyValuePair.Key, keyValuePair.Value);
                        firstPlayerTurn = !firstPlayerTurn;
                    }
                }
                else
                {
                    isLegitBoard = false;
                }
                
            }

            if (isLegitBoard)
            {
                secondLegitBoards += 1;
            }
            else
            {
                Debug.Log("nielegitne");
            }
            GameEndDwa();
            
        }


        // Debug.Break();
    }

    
    private void SetupDrugie()
    {
        var localPoints = new List<GameObject>(pointsArr);
        foreach (var localPoint in localPoints)
        {
            var pointsCount = localPoints.Count;
            var validPoints = new List<GameObject>(localPoints);
            var pointIndex = localPoints.IndexOf(localPoint);

            var invalidPoints = new GameObject[2];
            invalidPoints[1] = validPoints[(pointIndex + 1) % pointsCount];
            invalidPoints[0] = validPoints[(pointsCount + (pointIndex - 1) % pointsCount) % pointsCount];

            validPoints.Remove(invalidPoints[1]);
            validPoints.Remove(invalidPoints[0]);
            validPoints.Remove(localPoint);


            var randomMove = Random.Range(0, 8);
            if (randomMove != 7)
                randomBoards.Add(localPoint, validPoints[randomMove]);

        }

        var normalizedBoards = new Dictionary<GameObject, GameObject>();
        foreach (var keyValuePair in randomBoards)
        {
            var shouldAdd = true;
            foreach (var normalizedBoard in normalizedBoards)
            {
                if (keyValuePair.Value == null || normalizedBoard.Value == null)
                {
                    shouldAdd = false;
                    continue;
                }
                if (keyValuePair.Value.ToString() + keyValuePair.Key.ToString() ==
                    normalizedBoard.Key.ToString() + normalizedBoard.Value.ToString())
                {
                    shouldAdd = false;
                    continue;
                }
            }

            if (shouldAdd) normalizedBoards.Add(keyValuePair.Key, keyValuePair.Value);
        }

        randomBoards.Clear();
        randomBoards = new Dictionary<GameObject, GameObject>(normalizedBoards);
        var rand = new System.Random();
        
        randomBoards = randomBoards.OrderBy(x => rand.Next())
            .ToDictionary(item => item.Key, item => item.Value);

        
        
    }

    private double CountPossibleMoves()
    {
        if (isFirstTurn) return 35;
        var validMovesCnt = 0;
        var localPoints = new List<GameObject>(pointsArr);
        foreach (var localPoint in localPoints)
        {
            var pointsCount = localPoints.Count;
            var validPoints = new List<GameObject>(localPoints);
            var pointIndex = localPoints.IndexOf(localPoint);

            var invalidPoints = new GameObject[2];
            invalidPoints[1] = validPoints[(pointIndex + 1) % pointsCount];
            invalidPoints[0] = validPoints[(pointsCount + (pointIndex - 1) % pointsCount) % pointsCount];

            validPoints.Remove(invalidPoints[1]);
            validPoints.Remove(invalidPoints[0]);
            validPoints.Remove(localPoint);


            foreach (var validPoint in validPoints)
                if (LinesInFront(localPoint, validPoint) == 1 &&
                    !lines.Contains(localPoint.name + validPoint.name))
                    validMovesCnt += 1;
        }

        return validMovesCnt / 2;
    }

    private void DrawLine(GameObject point, GameObject connectToPoint)
    {
        var posA = point.transform.position;
        var posB = connectToPoint.transform.position;
        var linePos = (posB - posA) * 0.5F + posA;
        var lineLenght = (posB - posA).magnitude - .5f;

        var linePref = firstPlayerTurn ? linePrefFirst : linePrefSecond;
        var line = Instantiate(linePref, linePos, Quaternion.identity);
        line.transform.localScale = new Vector3(lineLenght, .1f, .1f);

        // line.transform.LookAt(posB+posA);

        point.transform.LookAt(connectToPoint.transform);

        var lineRot = point.transform.rotation.eulerAngles;
        lineRot.y += 90;
        line.transform.rotation = Quaternion.Euler(lineRot);


        line.name = "Origin: " + point.name + ", Desired: " + connectToPoint.name;
        //(LinesInFront(point, connectToPoint)-1

        drewCount += 1;

        line.transform.parent = linesContainer.transform;

        lines.Add(point.name + connectToPoint.name);
        lines.Add(connectToPoint.name + point.name);
        // Thread.Sleep(1000);
    }

    public bool CheckPossibleLines()
    {
        var localPoints = new List<GameObject>(pointsArr);
        do
        {
            var pointsCount = localPoints.Count;
            var randomIndex = Random.Range(0, pointsCount);
            var validPoints = new List<GameObject>(localPoints);
            var point = localPoints[randomIndex];

            var invalidPoints = new GameObject[2];
            invalidPoints[1] = validPoints[(randomIndex + 1) % pointsCount];
            invalidPoints[0] = validPoints[(pointsCount + (randomIndex - 1) % pointsCount) % pointsCount];

            // if (invalidPoints[0] == null || invalidPoints[1] == null)
            // {
            //     localPoints.Remove(point);
            // }

            validPoints.Remove(invalidPoints[1]);
            validPoints.Remove(invalidPoints[0]);
            validPoints.Remove(point);


            while (validPoints.Count > 0)
            {
                var connectToPointIndex = Random.Range(0, validPoints.Count);
                var connectToPoint = validPoints[connectToPointIndex];
                if (isFirstTurn)
                {
                    isFirstTurn = false;
                    DrawLine(point, connectToPoint);
                    firstPlayerTurn = false;
                    return true;
                }

                if (LinesInFront(point, connectToPoint) == 1 &&
                    !lines.Contains(point.name + connectToPoint.name))
                {
                    DrawLine(point, connectToPoint);
                    firstPlayerTurn = !firstPlayerTurn;
                    return true;
                }


                validPoints.Remove(connectToPoint);
            }


            localPoints.Remove(point);
            // Debug.Log("Origin: " + point.name + ", Desired: " + connectToPoint.name);
        } while (localPoints.Count > 0);

        return false;
    }

    private int LinesInFront(GameObject origin, GameObject target)
    {
        int originIndex, targetIndex;
        originIndex = pointsArr.IndexOf(origin);
        targetIndex = pointsArr.IndexOf(target);

        List<GameObject> leftPoints, rightPoints;
        leftPoints = new List<GameObject>();
        rightPoints = new List<GameObject>();

        for (var temp = originIndex + 1; temp % 10 != targetIndex; temp++) rightPoints.Add(pointsArr[temp % 10]);

        for (var temp = targetIndex + 1; temp % 10 != originIndex; temp++) leftPoints.Add(pointsArr[temp % 10]);

        var cnt = 0;

        foreach (var leftPoint in leftPoints)
        foreach (var rightPoint in rightPoints)
            if (lines.Contains(leftPoint.name + rightPoint.name))
            {
                cnt++;
                if (cnt > 1)
                    return cnt;
            }


        // RaycastHit[] hits;
        //
        //
        //
        // Debug.DrawRay(origin.transform.position,Vector3.Normalize(target.transform.position - origin.transform.position)*20,color);
        // hits = Physics.RaycastAll(origin.transform.position,
        //     Vector3.Normalize(target.transform.position - origin.transform.position),30f);
        // foreach (var raycastHit in hits)
        // {
        //     Debug.Log(raycastHit.collider.gameObject.name);
        // }
        // Debug.Log(hits.Length);
        // return hits.Length;
        leftPoints.Clear();
        rightPoints.Clear();
        leftPoints = null;
        rightPoints = null;
        return cnt;
    }
}