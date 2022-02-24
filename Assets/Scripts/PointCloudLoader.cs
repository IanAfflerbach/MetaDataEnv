using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class PointCloudLoader : MonoBehaviour
{
    public string filename;

    public Shader shader;
    [HideInInspector]
    public bool permute;

    private BaseImporter importer;

    private Action guiEvent;

    private void Start()
    {
        string fullFileName = Path.Combine(Application.streamingAssetsPath, filename);
        if (!File.Exists(fullFileName))
        {
            Debug.Log($"File {filename} does not exist, shutting down...");
            return;
        }

        switch(Path.GetExtension(fullFileName))
        {
            case ".csv":
                importer = CSVImporter.Instance;
                break;
            case ".las":
                importer = LASImporter.Instance;
                break;
            default:
                importer = BaseImporter.Instance;
                break;
        }

        importer.Import(fullFileName);
        guiEvent += OnGUIEvent;
    }

    private void OnGUI()
    {
        guiEvent?.Invoke();
    }

    private bool loaded = false;
    private void OnGUIEvent()
    {

        float x = 15.0f;
        float y = 15.0f;

        if (!loaded)
        {
            if (GUI.Button(new Rect(x, y, 100, 25), "Load Data"))
            {
                StartCoroutine(importer.GenerateMeshes(transform, shader));
                loaded = true;
            }
        }

        if (!importer.Ready)
        {
            GUI.Label(new Rect(x, y + 35, 500, 25), "Header Data Processing...");
        }
    }
}