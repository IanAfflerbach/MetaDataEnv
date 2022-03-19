using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerController : MonoBehaviour
{
    public bool isVR;

    public GameObject keyboardPlayerPrefab;

    private void Start()
    {
        if (isVR) Debug.Log("FIXME");
        else SpawnPlayer(keyboardPlayerPrefab);
    }

    private void SpawnPlayer(GameObject prefab)
    {
        Instantiate(prefab, Vector3.zero, Quaternion.identity);
    }
}
