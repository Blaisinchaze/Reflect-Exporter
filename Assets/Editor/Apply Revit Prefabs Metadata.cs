using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Reflect;

public class ApplyRevitPrefabsMetadata : EditorWindow
{
    Object myBasePrefab = null;
    [SerializeField]
    private string exportedObjectPath;
    bool specificExport = false;
    int numberOfExports;
    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Reflect/Apply Prefab Metadata")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        ApplyRevitPrefabsMetadata window = (ApplyRevitPrefabsMetadata)EditorWindow.GetWindow(typeof(ApplyRevitPrefabsMetadata));
        window.Show();
    }

    void OnGUI()
    {

        GUILayout.Label("Exported object path", EditorStyles.boldLabel);
        GUILayout.Label((exportedObjectPath != null ? exportedObjectPath : "Press browse to select an export folder"), EditorStyles.label);
        if (GUILayout.Button("Browse"))
            exportedObjectPath = EditorUtility.OpenFolderPanel("exportedObjectPath","","");

        GUILayout.Space(25);

        GUILayout.Label("Base Revit Export Prefab", EditorStyles.boldLabel);
        myBasePrefab = EditorGUILayout.ObjectField(myBasePrefab, typeof(Object), true);

        GUILayout.Space(25);

        specificExport = EditorGUILayout.Toggle("Only export a specific number of objects? (Used mainly for testing)", specificExport);
        if(specificExport)
        {
            numberOfExports = EditorGUILayout.IntField("How many exports? ", numberOfExports);
        }

        GUILayout.Space(25);

        if (GUILayout.Button("Apply Metadata"))
        {
            if (exportedObjectPath == "" || exportedObjectPath == null)
            {
                Debug.LogWarning("No exported object path set");
                return;
            }
            if (myBasePrefab == null)
            {
                Debug.LogWarning("No prefab to export set");
                return;
            }
            if (specificExport && numberOfExports <= 0)
            {
                Debug.LogWarning("Please select a number greater than 0 for exports or turn off specific number of exports");
                return;
            }
            ApplyMetadata();
        }

        GUILayout.Space(15);

        if (GUILayout.Button("Clear Import Folder"))
        {
            ClearImportFolder();
        }


    }

    public void ClearImportFolder()
    {
        
        var info = new DirectoryInfo(exportedObjectPath);
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo)
        {
            file.Delete();
        }
        AssetDatabase.Refresh();
    }

    public void ApplyMetadata()
    {
        int debugChildrenCount = 0;
        Debug.Log("ApplyingMetadata");
        string prefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(myBasePrefab)); 
        if(prefabPath == null || prefabPath == "")
            prefabPath = AssetDatabase.GetAssetPath(myBasePrefab);

        GameObject basePrefabGameObject;
        try
        {
            basePrefabGameObject = LoadPrefab(prefabPath);
        }
        catch (System.Exception)
        {
            Debug.LogError("Asset Cannot be found at : " + prefabPath);
            return;
        }


        List<GameObject> childPrefabs = new List<GameObject>();
        for (int i = 0; i < basePrefabGameObject.transform.childCount; i++)
        {
            childPrefabs.Add(basePrefabGameObject.transform.GetChild(i).gameObject);
        }
        Debug.Log("child count = " + childPrefabs.Count);
        foreach (var item in childPrefabs)
        {
            string childPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(item));
            childPath = ReformatChildren(LoadPrefab(childPath));
            var childPrefabGameObject = LoadPrefab(childPath);
            //ReformatChildren(childPrefabGameObject);
            CreateAndAddMetadata(childPrefabGameObject,item);
            CreateAndRecentreBounds(childPrefabGameObject);
            SavePrefabData(childPrefabGameObject, childPath);

            debugChildrenCount++;
            if (specificExport && debugChildrenCount >= numberOfExports)
                break;
        }
        SavePrefabData(basePrefabGameObject, prefabPath);
        Debug.Log("Metadata Application Complete");
    }        
    void CopyValues(Metadata _from, Metadata_Plus _to)
    {
        foreach (var item in _from.parameters)
        {
            _to.parameters.Add(item.Key, item.Value.value);
            Debug.Log("logging " + item.Key + " - " + item.Value.value);
        }
        //var json = JsonUtility.ToJson(_from);
        //JsonUtility.FromJsonOverwrite(json, _to);
    }

    void SavePrefabData(GameObject _prefab, string _path)
    {
        EditorUtility.SetDirty(_prefab);
        PrefabUtility.SaveAsPrefabAsset(_prefab, _path);
        PrefabUtility.UnloadPrefabContents(_prefab);
    }

    GameObject LoadPrefab(string _prefabPath)
    {
        return PrefabUtility.LoadPrefabContents(_prefabPath);
    }
    string ReformatChildren(GameObject _childPrefab)
    {
        _childPrefab.transform.position = Vector3.zero;
        string localPath;

        localPath = exportedObjectPath+"/" + _childPrefab.name + ".prefab";
        //localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);  
        
        if (_childPrefab.transform.childCount > 0)
        {
            PrefabUtility.SaveAsPrefabAsset(_childPrefab, localPath);
        }
        else
        {
            GameObject tempGameObject = new GameObject(_childPrefab.name);
            PrefabUtility.SaveAsPrefabAsset(tempGameObject, localPath);
            GameObject formattedObject = LoadPrefab(localPath);
            Instantiate(_childPrefab, formattedObject.transform);
            SavePrefabData(formattedObject, localPath);
            DestroyImmediate(tempGameObject);
        }
        PrefabUtility.UnloadPrefabContents(_childPrefab);

        return localPath;

    }

    void CreateAndAddMetadata(GameObject _childPrefab, GameObject _childGameObject)
    {
        Metadata[] metadatas;
        try
        {
            metadatas = _childGameObject.GetComponents<Metadata>();
            if (metadatas.Length > 0)
            {
                for (int i = 1; i < metadatas.Length; i++)
                {
                    DestroyImmediate(metadatas[i]);
                }
            }
        }
        catch (System.Exception)
        {
            return;
        }

        if (_childPrefab.GetComponent<Metadata>() != null) return;

        Metadata addedMetadata = metadatas[0];

        Metadata_Plus prefabMetadata = (Metadata_Plus)_childPrefab.AddComponent(typeof(Metadata_Plus));

        CopyValues(addedMetadata, prefabMetadata);
        //DestroyImmediate(addedMetadata);
    }

    void CreateAndRecentreBounds(GameObject _childPrefab)
    {
        //if (_childPrefab.GetComponent<BoxCollider>() != null || _childPrefab.GetComponentInChildren<BoxCollider>() != null) return;
        if (_childPrefab.GetComponentInChildren<MeshRenderer>() == null) return;

        GameObject meshHolder = _childPrefab.GetComponentInChildren<MeshRenderer>().gameObject;

        BoxCollider boxCollider = meshHolder.AddComponent<BoxCollider>();
        Vector3 movementBounds;
        if (boxCollider.size.x < boxCollider.size.z)
        {
            Vector3 currentRotation = meshHolder.transform.rotation.eulerAngles;
            currentRotation.y += 90;
            meshHolder.transform.rotation = Quaternion.Euler(currentRotation);
            movementBounds = new Vector3(boxCollider.center.z, -((boxCollider.size.y / 2) - boxCollider.center.y), -boxCollider.center.x);
        }
        else
        {
            movementBounds = new Vector3(boxCollider.center.x, -((boxCollider.size.y / 2) - boxCollider.center.y), boxCollider.center.z);
        }
        //Vector3 movementBounds = new Vector3(boxCollider.center.x, -((boxCollider.size.y/2)-boxCollider.center.y ), boxCollider.center.z);

        Vector3 newPosition = meshHolder.transform.position - movementBounds;

        meshHolder.transform.position = newPosition;

        DestroyImmediate(boxCollider);
        MeshCollider meshCollider = meshHolder.AddComponent<MeshCollider>();

        meshCollider.convex = true;
    }

}