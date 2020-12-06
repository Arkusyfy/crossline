using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using Random = UnityEngine.Random;
using Rand = System.Random;

public class GameScripts : MonoBehaviour
{
    private readonly int depth = 0;
    private readonly List<string> lines = new List<string>();

    private uint[] zobristKey = new uint[10 * 7];
    private uint zHash;



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
    void RandomizeZorbist()
    {
        for (var i = 0; i < zobristKey.Length; i++)
        {
            zobristKey[i] = GetRandomUInt();
        }
    }

    Rand rand = new Rand();

    private Dictionary<uint, double> hashboards = new Dictionary<uint, double>();

    private uint Hashboard()
    {
        uint result = 0;
        for (int i = 0; i < 10; i++)
        {
            // if occupied
            result ^= zobristKey[i * 7];
        }

        foreach (var o in pointsArr)
        {
            foreach (var m in pointsArr)
            {
                if (lines.Contains(o.name + m.name))
                {
                    int parsedO = Int32.Parse(o.name);
                    int parsedM = Int32.Parse(m.name);
                    if (parsedM < parsedO)
                    {
                        if (parsedO == 9)
                        {
                            result ^= zobristKey[parsedO * 7 + parsedM - 1];
                        }
                        else
                        {
                            result ^= zobristKey[parsedO * 7 + parsedM];
                        }
                    }
                    else
                    {
                        if (parsedO == 0)
                        {
                            result ^= zobristKey[parsedO * 7 + parsedM - 2];

                        }
                        else
                        {
                            result ^= zobristKey[parsedO * 7 + parsedM - 3];
                        }
                    }
                }

            }
        }


        return result;
    }

    public uint GetRandomUInt()
    {
        var buffer = new byte[sizeof(uint)];
        rand.NextBytes(buffer);
        return BitConverter.ToUInt32(buffer, 0);
    }

    private void Start()

    {
        loopCount = loopCounter;
        validTurn = true;
        RandomizeZorbist();

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

    private double TerminalNodeNegamax(int sign)
    {
        if (sign == 1) return double.MinValue;
        return double.MaxValue;
    }

    private double NegaMax(Dictionary<GameObject, GameObject> node, int depth, int sign = 1)
    {
        if (node.Count == 0) return TerminalNodeNegamax(sign);
        if (depth == 0)
            return sign * NodeValue(node);
        var value = double.MinValue;
        foreach (var keyValuePair in node)
        {
            DrawLine(keyValuePair.Key, keyValuePair.Value);
            value = max(value, -NegaMax(PosMovesFromPoint(keyValuePair.Value), depth - 1, -sign));
            PopLine();
        }

        return value;
    }


    private double AlphaBeta(Dictionary<GameObject, GameObject> node, int depth, bool maximizingPlayer,
        double alpha = double.MinValue,
        double beta = double.MaxValue)
    {
        if (node.Count == 0) return TerminalNode(maximizingPlayer);
        if (depth == 0)
            return NodeValue(node);
        if (maximizingPlayer)
        {
            foreach (var keyValuePair in node)
            {
                DrawLine(keyValuePair.Key, keyValuePair.Value);
                alpha = max(alpha, AlphaBeta(PosMovesFromPoint(keyValuePair.Value), depth - 1, false, alpha, beta));
                PopLine();
                if (alpha >= beta) return beta;
            }

            return alpha;
        }

        foreach (var keyValuePair in node)
        {
            DrawLine(keyValuePair.Key, keyValuePair.Value);
            beta = min(beta, AlphaBeta(PosMovesFromPoint(keyValuePair.Value), depth - 1, true, alpha, beta));
            PopLine();
            if (alpha >= beta) return alpha;
        }

        return beta;
    }

    private double ABNegaMax(Dictionary<GameObject, GameObject> node, int depth, double alpha = double.MinValue,
        double beta = double.MaxValue, int sign = 1)
    {
        if (node.Count == 0) return TerminalNodeNegamax(sign);
        if (depth == 0)
            return sign * NodeValue(node);
        foreach (var keyValuePair in node)
        {
            DrawLine(keyValuePair.Key, keyValuePair.Value);

            alpha = max(alpha, -ABNegaMax(PosMovesFromPoint(keyValuePair.Value), depth - 1, -beta, -alpha, -sign));
            PopLine();

            if (alpha >= beta) return alpha;
        }

        return alpha;
    }




    public class LineTuple<T1, T2>
    {
        public T1 from { get; private set; }
        public T2 to { get; private set; }

        public LineTuple(T1 arg1, T2 arg2)
        {
            from = arg1;
            to = arg2;
        }
    }

    void SetProofAndDisproof(Dictionary<GameObject, GameObject> node)
    {
        RootProof _node = _rootProofs[node];
        if (_node.expanded)
        {
            if (_node.type) // if AND
            {
                _node.proof = 0;
                _node.disproof = Double.MaxValue;
                foreach (var keyValuePair in node)
                {
                    DrawLine(keyValuePair.Key, keyValuePair.Value);
                    var child = PosMovesFromPoint(keyValuePair.Key);
                    var _child = _rootProofs[child];
                    _node.proof += _child.proof;
                    _node.disproof = min(_node.disproof, _child.disproof);
                    PopLine();
                }
            }

            if (!_node.type) // if OR
            {
                _node.proof = Double.MaxValue;
                _node.disproof = 0;
                foreach (var keyValuePair in node)
                {
                    DrawLine(keyValuePair.Key, keyValuePair.Value);
                    var child = PosMovesFromPoint(keyValuePair.Key);
                    var _child = _rootProofs[child];
                    _node.proof = min(_node.proof, _child.proof);
                    _node.disproof += _child.disproof;
                    PopLine();
                }
            }
        }
        else
        {
            switch (_node.value)
            {
                case 0:
                    _node.proof = 0;
                    _node.disproof = Double.MaxValue;
                    break;
                case 1:
                    _node.proof = Double.MaxValue;
                    _node.disproof = 0;
                    break;
                case 2:
                    _node.proof = 1;
                    _node.disproof = 1;
                    break;
            }
        }

        _rootProofs[node] = _node;
    }

    void Evaluate(Dictionary<GameObject, GameObject> node)
    {
        _rootProofs[node].value = _Evaluate(node);
    }

    int _Evaluate(Dictionary<GameObject, GameObject> node)
    {

        int totalmoves = 0;
        foreach (var keyValuePair in node)
        {
            DrawLine(keyValuePair.Key, keyValuePair.Value);

            totalmoves += PosMovesFromPoint(keyValuePair.Key).Count;

            PopLine();
        }

        if (totalmoves == 0)
        {
            if (firstPlayerTurn)
            {
                return 0;
            }

            return 1;
        }

        return 2;
    }

    Dictionary<GameObject, GameObject> ExpandNode(Dictionary<GameObject, GameObject> node)
    {
        RootProof _node = _rootProofs[node];
        GenerateChildren(node);
        foreach (var keyValuePair in node)
        {
            DrawLine(keyValuePair.Key, keyValuePair.Value);
            Dictionary<GameObject, GameObject> child =
                new Dictionary<GameObject, GameObject>(PosMovesFromPoint(keyValuePair.Key));
            Evaluate(child);
            SetProofAndDisproof(child);
            RootProof _child = _rootProofs[child];
            if (_node.type)
                if (_child.disproof == 0)
                {
                    break;
                }

            if (!_node.type)
                if (_child.proof == 0)
                {
                    break;
                }

            PopLine();
        }

        _rootProofs[node].expanded = true;
        return node;
    }

    bool CompareDictionaries<T1, T2>(Dictionary<T1, T2> x1, Dictionary<T1, T2> x2)
    {
        return (x2 ?? new Dictionary<T1, T2>())
            .OrderBy(kvp => kvp.Key)
            .SequenceEqual((x1 ?? new Dictionary<T1, T2>())
                .OrderBy(kvp => kvp.Key));
    }

    Dictionary<GameObject, GameObject> UpdateAncestors(Dictionary<GameObject, GameObject> node,
        Dictionary<GameObject, GameObject> root)
    {
        while (!CompareDictionaries(root, node))
        {
            RootProof _node = _rootProofs[node];
            double oldProof = _node.proof;
            double oldDisproof = _node.disproof;
            SetProofAndDisproof(node);
            if (_node.proof == oldProof && _node.disproof == oldDisproof)
                return node;
            node = _node.parent;
        }

        SetProofAndDisproof(root);
        return root;
    }

    void GenerateChildren(Dictionary<GameObject, GameObject> node)

    {
        bool nodetype = _rootProofs[node].type;
        foreach (var keyValuePair in node)
        {
            DrawLine(keyValuePair.Key, keyValuePair.Value);
            RootProof child = new RootProof();
            child.type = !nodetype;
            child.parent = node;
            _rootProofs.Add(PosMovesFromPoint(keyValuePair.Key), child);
            PopLine();
        }
    }

    Dictionary<GameObject, GameObject> SelectMostProvingNode(Dictionary<GameObject, GameObject> node)
    {
        RootProof pair = _rootProofs[node];
        while (pair.expanded)
        {
            double value = Double.MaxValue;
            Dictionary<GameObject, GameObject> best = new Dictionary<GameObject, GameObject>(node);
            if (pair.type)
            {
                foreach (var keyValuePair in node)
                {
                    DrawLine(keyValuePair.Key, keyValuePair.Value);
                    var child = PosMovesFromPoint(keyValuePair.Key);
                    var _child =
                        _rootProofs[child];


                    if (value > _child.disproof)
                    {
                        best = child;
                        value = _child.disproof;
                    }

                    PopLine();
                }

            }

            if (!pair.type)
            {
                foreach (var keyValuePair in node)
                {

                    DrawLine(keyValuePair.Key, keyValuePair.Value);
                    var child = PosMovesFromPoint(keyValuePair.Key);
                    var _child =
                        _rootProofs[child];


                    DrawLine(keyValuePair.Key, keyValuePair.Value);
                    if (value > _child.proof)
                    {
                        best = child;
                        value = _child.proof;
                    }

                    PopLine();
                }

            }

            node = best;
        }


        return node;
    }

    bool IsNoMoreMoves(Dictionary<GameObject, GameObject> node)
    {
        foreach (var keyValuePair in node)
        {
            DrawLine(keyValuePair.Key, keyValuePair.Value);
            if (PosMovesFromPoint(keyValuePair.Key).Count > 0)
            {
                PopLine();
                return false;
            }
           PopLine(); 
        }
        return true;
    }

    Dictionary<GameObject, GameObject> RandomMoveNode(Dictionary<GameObject, GameObject> node)
    {
        
        System.Random random = new System.Random();
        List<GameObject> keys = new List<GameObject>(node.Keys);
        GameObject randomPoint = keys[random.Next(keys.Count)];
        DrawLine(randomPoint, node[randomPoint]);

        return PosMovesFromPoint(randomPoint);


    }

    private int resource_counter = 1000;
    bool ResourcesAvailable()
    {
        return
            resource_counter-- > 0;
    }

    private int reward = 0;
    
    void UCTSearch(Dictionary<GameObject, GameObject> root)
    {
        resource_counter = 1000;
        Dictionary<GameObject, GameObject> current = root;
        while (ResourcesAvailable())
        {
            current = TreePolicy(current);
            reward = DefaultPolicy(current);
            Backup(current, reward);
        }

    }



    Dictionary<GameObject, GameObject> Expand(Dictionary<GameObject, GameObject> node)
    {
        
        RootProof _node = _rootProofs[node];
        GenerateChildren(node);
        
        foreach (var keyValuePair in node)
        {
            DrawLine(keyValuePair.Key, keyValuePair.Value);
            Dictionary<GameObject, GameObject> child =
                new Dictionary<GameObject, GameObject>(PosMovesFromPoint(keyValuePair.Key));
            
            

            PopLine();
        }


        do
        {
            Dictionary<GameObject, GameObject> randNode = RandomMoveNode(node);
            PopLine();

        } while (!_rootProofs[node].expanded);
        _rootProofs[node].expanded = true;
        return node;
    }

    int DefaultPolicy(Dictionary<GameObject, GameObject> node)
    {
        Dictionary<GameObject, GameObject> a = node;
        while (!IsNoMoreMoves(node))
        {
            a=  RandomMoveNode(node);
            PopLine();
        }

        return reward;
    }
    
    Dictionary<GameObject, GameObject> TreePolicy(Dictionary<GameObject, GameObject> node)
    {
        while (!IsNoMoreMoves(node))
        {
            if (!_rootProofs[node].expanded)
            {
                return Expand(node);
            }
            else
            {
                node = BestChild(node);
            }
        }

        return node;
    }

    void Backup(Dictionary<GameObject, GameObject> node, int reward)
    {

        while (node != null)
        {
            RootProof _node = _rootProofs[node];
            _node.n++;
            _node.value += reward;
            node = _node.parent;
            _node = _rootProofs[_node.parent];
            reward = 1 - reward;
        }
    } 
    private double c = 1;
    Dictionary<GameObject, GameObject> BestChild(Dictionary<GameObject, GameObject> node)
    {
        Dictionary<GameObject, GameObject> best = node;
        double value = Double.MinValue;
        foreach (var keyValuePair in node)
        {
            DrawLine(keyValuePair.Key, keyValuePair.Value);
            Dictionary<GameObject, GameObject> child = PosMovesFromPoint(keyValuePair.Key);
            RootProof _node = _rootProofs[child];

            double childValue = _node.value / _node.n+c * Mathf.Sqrt(Mathf.Log10(_rootProofs[ _node.parent].n) / _node.n);

            if (childValue > value)
            {
                best = child;
                value = childValue;
            }
            
            PopLine();
        }

        return best;
    }
    


    Dictionary<GameObject, GameObject> MonteCarloEvaluation(Dictionary<GameObject, GameObject> node, int nofSimulations)
    {
        Dictionary<GameObject, GameObject> bestChild = null;
        double bestProbability = -1;
        foreach (var keyValuePair in node)
        {
            DrawLine(keyValuePair.Key, keyValuePair.Value);
            Dictionary<GameObject, GameObject> child = PosMovesFromPoint(keyValuePair.Key);
            int r = 0;
            for (int i = 1; i < nofSimulations; i++)
            {
                var _child = child;
                int popcnt = 0;
                while (!IsNoMoreMoves(_child))
                {
                    _child = RandomMoveNode(_child);
                    popcnt++;
                }

                while (popcnt-->0)
                {
                    PopLine();
                }

                if (!firstPlayerTurn)
                {
                    r++;
                }
            }
            
            double probability = r / nofSimulations;
            if (probability > bestProbability)
            {
                bestChild = child;
                bestProbability = probability;
            }
            PopLine();
        }
        return bestChild;
    }


    public class RootProof
    {
        public double proof, disproof;
        public bool expanded, 
            type; // 0 = OR, 1 = AND
        public int value, // 0 = win, 1 = lose, 2 = unknown
            n; 
        public Dictionary<GameObject, GameObject> parent;

        public RootProof()
        {
            this.type = false;
            this.value = 2;
            this.n = 0;
        }
    }
    private Dictionary<Dictionary<GameObject, GameObject>, RootProof> _rootProofs  = new
        Dictionary<Dictionary<GameObject, GameObject>, RootProof>();
    
    void PNS(Dictionary<GameObject, GameObject> root)
    {
        _rootProofs.Add(root, new RootProof());
        
        Evaluate(root);
        SetProofAndDisproof(root);
        Dictionary<GameObject, GameObject> current = root;
        while (_rootProofs[root].proof != 0 && _rootProofs[root].disproof != 0)
        {
            Dictionary<GameObject,GameObject> mostProving = SelectMostProvingNode(current);
            ExpandNode(mostProving);
            current = UpdateAncestors(mostProving, root);
        }
    }
    
    private double minimax(Dictionary<GameObject, GameObject> node, int depth, bool maximizingPlayer)
    {
        if (node.Count == 0) return TerminalNode(maximizingPlayer);
        if (depth == 0)
        {

            double thisval =  NodeValue(node);
            hashboards.Add(Hashboard(),thisval);
            return thisval;
        }


        uint thisBoard = Hashboard();
        if (hashboards.ContainsKey(thisBoard))
        {
            return hashboards[thisBoard];
        }

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

    private bool CheckPossibleLines()
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