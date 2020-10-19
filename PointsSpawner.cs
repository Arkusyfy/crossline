using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointsSpawner : MonoBehaviour
{
    [SerializeField] private GameObject PointPrefab, parent, gameScripts;
    private float radius = 10;
    private int pointsCount = 10;
    public List<GameObject> pointsArr = new List<GameObject>();
    public GameScripts scriptsRef;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < pointsCount; i++)
        {
            float rad = 2 * Mathf.PI /  pointsCount * i;
            float vert = Mathf.Sin(rad);
            float hor = Mathf.Cos(rad);
            Vector3 spawnDir = new Vector3(vert, 0, hor);
            Vector3 spawnPos = Vector3.zero + spawnDir * radius;
            GameObject point = Instantiate(PointPrefab, spawnPos, Quaternion.identity);
            point.transform.LookAt(Vector3.zero);
            // point.transform.Translate(new Vector3(0, point.transform.localScale.y/2),0);
            point.transform.parent = parent.transform;
            point.name = "Point" + i;
            pointsArr.Add(point);
        }

        scriptsRef.pointsArr = this.pointsArr;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
