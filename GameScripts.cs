using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GameScripts : MonoBehaviour
{
    private readonly List<string> lines = new List<string>();
    private readonly int depth = 0;

    private bool doDrugieSetup = true;
    private int drewCount;
    private bool firstPlayerTurn = true;
    private double globalBFactor;
    private double globalTurnCounter;
    private bool isFirstTurn = true;


    // private GameObject firstPlayerLines, secondPlayerLines;
    [SerializeField] private GameObject linePrefFirst, linePrefSecond, linesContainer;
    private double localBFactor;
    private int loopCount;

    [SerializeField] private int loopCounter;

    public List<GameObject> pointsArr;
    private Dictionary<GameObject, GameObject> randomBoards = new Dictionary<GameObject, GameObject>();


    private int secondLegitBoards;

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
#if UNITY_EDITOR
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
#endif

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
            Debug.Log("Ilość poprawnych planszy: " + secondLegitBoards);
            Debug.Log("Liczba prób: " + loopCount);
#if UNITY_EDITOR

            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
#endif
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
            var isLegitBoard = true;
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
                    if (!lines.Contains(keyValuePair.Key.name + keyValuePair.Value.name))
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
                secondLegitBoards += 1;
            else
                Debug.Log("nielegitne");
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

                if (keyValuePair.Value + keyValuePair.Key.ToString() ==
                    normalizedBoard.Key + normalizedBoard.Value.ToString())
                    shouldAdd = false;
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


    private Dictionary<GameObject, GameObject> PosMovesFromPoint(GameObject localPoint)
    {
        var validMovesArr = new Dictionary<GameObject, GameObject>();
        var validMoves = 0;
        var validPoints = new List<GameObject>(pointsArr);
        var pointIndex = validPoints.IndexOf(localPoint);

        var pointsCount = validPoints.Count;
        var invalidPoints = new GameObject[2];
        invalidPoints[1] = validPoints[(pointIndex + 1) % pointsCount];
        invalidPoints[0] = validPoints[(pointsCount + (pointIndex - 1) % pointsCount) % pointsCount];

        validPoints.Remove(invalidPoints[1]);
        validPoints.Remove(invalidPoints[0]);
        validPoints.Remove(localPoint);
        if (isFirstTurn)
        {
            foreach (var validPoint in validPoints) validMovesArr.Add(localPoint, validPoint);

            return validMovesArr;
        }

        foreach (var validPoint in validPoints)
        {
            var possibleM = LinesInFront(localPoint, validPoint);
            if (possibleM == 1)
            {
                validMoves += 1;
                validMovesArr.Add(validPoint, localPoint);
            }
        }

        return validMovesArr;
    }

    private void MiniMaxRoutine()
    {
        var possibleMoves = new List<Dictionary<GameObject, GameObject>>();
        foreach (var o in pointsArr) possibleMoves.Add(PosMovesFromPoint(o));
        var movesValues = new List<double>();
        double allMoves = 0;
        foreach (var possibleMove in possibleMoves)
        {
            movesValues.Add(possibleMove.Count);
            allMoves += possibleMove.Count;
        }
    }

    private List<double> GetMovesValues(Dictionary<GameObject, GameObject> node)
    {
        var movesValues = new List<double>();
        foreach (var keyValuePair in node)
        {
            DrawLine(keyValuePair.Value, keyValuePair.Key);
            movesValues.Add(PosMovesFromPoint(keyValuePair.Key).Count);
            PopLine();
        }

        return movesValues;
    }

    private double NodeValue(Dictionary<GameObject, GameObject> node)
    {
        var movesValues = GetMovesValues(node);
        var allMoves = node.Count;

        for (var i = 0; i < movesValues.Count; i++)
        {
            var movesValue = movesValues[i];
            if (movesValue % 2 == 0)
                movesValues[i] = movesValue / allMoves;
            else
                movesValues[i] = allMoves / movesValue;
        }

        if (movesValues.Count == 1)
        {
            if (movesValues[0] % 2 == 0)
                movesValues[0] = -1;
            else
                movesValues[0] = 1;
        }

        return movesValues.Sum();
    }

    private double TerminalNode(bool maximizing)
    {
        if (maximizing) return double.MinValue;
        return double.MaxValue;
    }

    private double minimax(Dictionary<GameObject, GameObject> node, int depth, bool maximizingPlayer)
    {
        if (node.Count == 0) return TerminalNode(maximizingPlayer);
        if (depth == 0)
            return NodeValue(node);


        if (maximizingPlayer)
        {
            var value = double.MinValue;

            foreach (var keyValuePair in node)
            {
                DrawLine(keyValuePair.Key, keyValuePair.Value);
                value = max(value, minimax(PosMovesFromPoint(keyValuePair.Value), depth - 1, false));
                PopLine();
            }

            return value;
        }
        else
        {
            var value = double.MaxValue;
            foreach (var keyValuePair in node)
            {
                DrawLine(keyValuePair.Key, keyValuePair.Value);
                value = min(value, minimax(PosMovesFromPoint(keyValuePair.Value), depth - 1, true));
                PopLine();
            }

            return value;
        }
    }

    private double max(double value, double child)
    {
        if (value > child)
            return value;
        return child;
    }

    private double min(double value, double child)
    {
        if (value < child)
            return value;
        return child;
    }

    private void PopLine()
    {
        lines.RemoveAt(lines.Count - 1);
        lines.RemoveAt(lines.Count - 1);

        DestroyImmediate(linesContainer.transform.GetChild(linesContainer.transform.childCount - 1).gameObject);
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

    private void moveValuesArr()
    {
        var localPoints = new List<GameObject>(pointsArr);

        foreach (var localPoint in localPoints) CheckPossibleLines();
    }

    public bool CheckPossibleLines()
    {
        var localPoints = new List<GameObject>(pointsArr);


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

        if (isFirstTurn)
        {
            var connectToPointIndex = Random.Range(0, validPoints.Count);
            var connectToPoint = validPoints[connectToPointIndex];
            isFirstTurn = false;
            DrawLine(point, connectToPoint);
            firstPlayerTurn = false;
            return true;
        }

        var movesValues = new List<double>();
        var movesArr = new Dictionary<GameObject, GameObject>();
        GameObject from, to;
        from = to = null;
        var _val = double.MinValue;

        foreach (var validPoint in validPoints)
        {
            var posMoves = PosMovesFromPoint(validPoint);
            foreach (var keyValuePair in posMoves)
                if (_val < minimax(PosMovesFromPoint(keyValuePair.Value), depth, true))
                {
                    _val = minimax(PosMovesFromPoint(keyValuePair.Value), depth, true);
                    to = keyValuePair.Key;
                    from = keyValuePair.Value;
                }
        }

        firstPlayerTurn = !firstPlayerTurn;

        if (to != null)
        {
            DrawLine(from, to);
            return true;
        }

        CountPossibleMoves();
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
