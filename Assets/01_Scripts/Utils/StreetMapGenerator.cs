using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class StreetMapGenerator : MonoBehaviour
{
    [Header("Sidewalk Configuration")]
    [Tooltip("The sidewalk prefab to tile along the street edges.")]
    public GameObject sidewalkPrefab;
    public float stepX = 2.5f;
    public float startX = -20f;
    public float endX = 25f;
    public float sidewalkZNorth = 10f;
    public float sidewalkZSouth = 0f;

    [Header("Building Configuration")]
    [Tooltip("List of background building prefabs to spawn.")]
    public GameObject[] buildingPrefabs;
    [Tooltip("List of store neon/signs prefabs to attach to building fronts.")]
    public GameObject[] signPrefabs;
    public float buildingZNorth = 12.5f;
    public float buildingZSouth = -2.5f;
    public float[] buildingPositionsX = { -16f, -8f, 0f, 8f, 16f, 23f };

    [Header("Street Lights")]
    public GameObject doubleLampPrefab;
    public float[] lampPositionsX = { -12f, 4f, 20f };

    [Header("Street Props")]
    public GameObject benchPrefab;
    public GameObject trashBinPrefab;
    public GameObject trashBagPrefab;
    public GameObject flowerPrefab;

    [Header("Environment")]
    public GameObject[] treePrefabs;
    public float[] treePositionsX = { -20f, 11f, -20f, 25f };
    public float[] treePositionsZ = { 11.5f, 11.5f, -3.5f, -3.5f };

    [Header("Pedestrians")]
    public GameObject[] pedestrianPrefabs;

    [Header("References")]
    [Tooltip("The parent transform under which to spawn the street elements.")]
    public Transform targetParent;

    [ContextMenu("Generate Street Map")]
    public void GenerateMap()
    {
#if UNITY_EDITOR
        if (targetParent == null)
        {
            targetParent = transform;
        }

        // Clear existing generated elements inside targetParent
        ClearExistingMap();

        // Create groups
        GameObject sidewalkGroup = new GameObject("Sidewalks");
        sidewalkGroup.transform.SetParent(targetParent, false);

        GameObject buildingGroup = new GameObject("Shops_Buildings");
        buildingGroup.transform.SetParent(targetParent, false);

        GameObject propGroup = new GameObject("StreetProps_Decorations");
        propGroup.transform.SetParent(targetParent, false);

        GameObject pedestrianGroup = new GameObject("Pedestrians");
        pedestrianGroup.transform.SetParent(targetParent, false);

        // Register container creations
        Undo.RegisterCreatedObjectUndo(sidewalkGroup, "Generate Map: Sidewalks");
        Undo.RegisterCreatedObjectUndo(buildingGroup, "Generate Map: Buildings");
        Undo.RegisterCreatedObjectUndo(propGroup, "Generate Map: Props");
        Undo.RegisterCreatedObjectUndo(pedestrianGroup, "Generate Map: Pedestrians");

        // 1. Spawning Sidewalks
        if (sidewalkPrefab != null)
        {
            // North Sidewalk
            for (float x = startX + stepX; x <= endX; x += stepX)
            {
                GameObject sw = (GameObject)PrefabUtility.InstantiatePrefab(sidewalkPrefab, sidewalkGroup.transform);
                sw.transform.localPosition = new Vector3(x, 0f, sidewalkZNorth);
                sw.transform.localRotation = Quaternion.identity;
                Undo.RegisterCreatedObjectUndo(sw, "Spawn Sidewalk");
            }

            // South Sidewalk
            for (float x = startX; x <= endX - stepX; x += stepX)
            {
                GameObject sw = (GameObject)PrefabUtility.InstantiatePrefab(sidewalkPrefab, sidewalkGroup.transform);
                sw.transform.localPosition = new Vector3(x, 0f, sidewalkZSouth);
                sw.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                Undo.RegisterCreatedObjectUndo(sw, "Spawn Sidewalk");
            }
        }

        // 2. Spawning Buildings and attaching Signs
        if (buildingPrefabs != null && buildingPrefabs.Length > 0)
        {
            // Spawn North Buildings
            for (int i = 0; i < buildingPositionsX.Length; i++)
            {
                float x = buildingPositionsX[i];
                GameObject bldPref = buildingPrefabs[i % buildingPrefabs.Length];
                if (bldPref != null)
                {
                    GameObject bld = (GameObject)PrefabUtility.InstantiatePrefab(bldPref, buildingGroup.transform);
                    bld.transform.localPosition = new Vector3(x, 0f, buildingZNorth);
                    bld.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    Undo.RegisterCreatedObjectUndo(bld, "Spawn North Building");

                    // Attach Sign
                    if (signPrefabs != null && signPrefabs.Length > 0)
                    {
                        GameObject signPref = signPrefabs[i % signPrefabs.Length];
                        if (signPref != null)
                        {
                            GameObject sign = (GameObject)PrefabUtility.InstantiatePrefab(signPref, bld.transform);
                            sign.transform.localPosition = new Vector3(0f, 3.8f, -4.2f);
                            sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                            Undo.RegisterCreatedObjectUndo(sign, "Spawn Building Sign");
                        }
                    }
                }
            }

            // Spawn South Buildings
            for (int i = 0; i < buildingPositionsX.Length; i++)
            {
                float x = buildingPositionsX[i];
                GameObject bldPref = buildingPrefabs[(i + 3) % buildingPrefabs.Length];
                if (bldPref != null)
                {
                    GameObject bld = (GameObject)PrefabUtility.InstantiatePrefab(bldPref, buildingGroup.transform);
                    bld.transform.localPosition = new Vector3(x, 0f, buildingZSouth);
                    bld.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    Undo.RegisterCreatedObjectUndo(bld, "Spawn South Building");

                    // Attach Sign
                    if (signPrefabs != null && signPrefabs.Length > 0)
                    {
                        GameObject signPref = signPrefabs[(i + 4) % signPrefabs.Length];
                        if (signPref != null)
                        {
                            GameObject sign = (GameObject)PrefabUtility.InstantiatePrefab(signPref, bld.transform);
                            sign.transform.localPosition = new Vector3(0f, 3.8f, -4.2f);
                            sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                            Undo.RegisterCreatedObjectUndo(sign, "Spawn Building Sign");
                        }
                    }
                }
            }
        }

        // 3. Spawning Street Lights
        if (doubleLampPrefab != null)
        {
            // North Lights
            foreach (float lx in lampPositionsX)
            {
                GameObject lamp = (GameObject)PrefabUtility.InstantiatePrefab(doubleLampPrefab, propGroup.transform);
                lamp.transform.localPosition = new Vector3(lx, 0f, sidewalkZNorth + 0.1f);
                lamp.transform.localRotation = Quaternion.identity;
                Undo.RegisterCreatedObjectUndo(lamp, "Spawn Lamp");
            }

            // South Lights
            foreach (float lx in lampPositionsX)
            {
                GameObject lamp = (GameObject)PrefabUtility.InstantiatePrefab(doubleLampPrefab, propGroup.transform);
                lamp.transform.localPosition = new Vector3(lx, 0f, sidewalkZSouth - 0.1f);
                lamp.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                Undo.RegisterCreatedObjectUndo(lamp, "Spawn Lamp");
            }
        }

        // 4. Spawning Benches and Trash Bins
        if (benchPrefab != null && trashBinPrefab != null)
        {
            // North Set
            GameObject benchN = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, propGroup.transform);
            benchN.transform.localPosition = new Vector3(-4f, 0f, sidewalkZNorth + 1.0f);
            benchN.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Undo.RegisterCreatedObjectUndo(benchN, "Spawn North Bench");

            GameObject binN = (GameObject)PrefabUtility.InstantiatePrefab(trashBinPrefab, propGroup.transform);
            binN.transform.localPosition = new Vector3(-5.5f, 0f, sidewalkZNorth + 1.0f);
            Undo.RegisterCreatedObjectUndo(binN, "Spawn North Bin");

            // South Set
            GameObject benchS = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, propGroup.transform);
            benchS.transform.localPosition = new Vector3(12f, 0f, sidewalkZSouth - 1.0f);
            benchS.transform.localRotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(benchS, "Spawn South Bench");

            GameObject binS = (GameObject)PrefabUtility.InstantiatePrefab(trashBinPrefab, propGroup.transform);
            binS.transform.localPosition = new Vector3(13.5f, 0f, sidewalkZSouth - 1.0f);
            Undo.RegisterCreatedObjectUndo(binS, "Spawn South Bin");
        }

        // 5. Spawning Trash Bags
        if (trashBagPrefab != null)
        {
            GameObject tb1 = (GameObject)PrefabUtility.InstantiatePrefab(trashBagPrefab, propGroup.transform);
            tb1.transform.localPosition = new Vector3(-12.0f, 0f, sidewalkZNorth + 2.8f);
            Undo.RegisterCreatedObjectUndo(tb1, "Spawn Trash Bag");

            GameObject tb2 = (GameObject)PrefabUtility.InstantiatePrefab(trashBagPrefab, propGroup.transform);
            tb2.transform.localPosition = new Vector3(4.0f, 0f, sidewalkZSouth - 2.8f);
            Undo.RegisterCreatedObjectUndo(tb2, "Spawn Trash Bag");
        }

        // 6. Spawning Trees and Flowers
        if (treePrefabs != null && treePrefabs.Length > 0)
        {
            for (int i = 0; i < treePositionsX.Length; i++)
            {
                GameObject treePref = treePrefabs[i % treePrefabs.Length];
                if (treePref != null)
                {
                    GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePref, propGroup.transform);
                    tree.transform.localPosition = new Vector3(treePositionsX[i], 0f, treePositionsZ[i]);
                    Undo.RegisterCreatedObjectUndo(tree, "Spawn Tree");
                }
            }
        }
        if (flowerPrefab != null)
        {
            GameObject fl1 = (GameObject)PrefabUtility.InstantiatePrefab(flowerPrefab, propGroup.transform);
            fl1.transform.localPosition = new Vector3(-3.0f, 0f, sidewalkZNorth + 0.8f);
            Undo.RegisterCreatedObjectUndo(fl1, "Spawn Flower");

            GameObject fl2 = (GameObject)PrefabUtility.InstantiatePrefab(flowerPrefab, propGroup.transform);
            fl2.transform.localPosition = new Vector3(11.0f, 0f, sidewalkZSouth - 0.8f);
            Undo.RegisterCreatedObjectUndo(fl2, "Spawn Flower");
        }

        // 7. Spawning Pedestrians
        if (pedestrianPrefabs != null && pedestrianPrefabs.Length > 0)
        {
            if (pedestrianPrefabs[0] != null)
            {
                GameObject chr1 = (GameObject)PrefabUtility.InstantiatePrefab(pedestrianPrefabs[0], pedestrianGroup.transform);
                chr1.transform.localPosition = new Vector3(-1.0f, 0f, sidewalkZNorth + 1.2f);
                chr1.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                Undo.RegisterCreatedObjectUndo(chr1, "Spawn Pedestrian");
            }
            if (pedestrianPrefabs.Length > 1 && pedestrianPrefabs[1] != null)
            {
                GameObject chr2 = (GameObject)PrefabUtility.InstantiatePrefab(pedestrianPrefabs[1], pedestrianGroup.transform);
                chr2.transform.localPosition = new Vector3(10.5f, 0f, sidewalkZSouth - 0.8f);
                chr2.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                Undo.RegisterCreatedObjectUndo(chr2, "Spawn Pedestrian");
            }
        }

        Debug.Log("Street Shop Map generated successfully! You can press Ctrl+Z to undo if needed.");
#else
        Debug.LogWarning("GenerateMap can only be executed in Editor Mode.");
#endif
    }

    [ContextMenu("Clear Map")]
    public void ClearExistingMap()
    {
#if UNITY_EDITOR
        if (targetParent == null) targetParent = transform;

        // Collect all children of targetParent
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in targetParent)
        {
            if (child == transform) continue;
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (var child in childrenToDestroy)
        {
            Undo.DestroyObjectImmediate(child);
        }
#endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(StreetMapGenerator))]
public class StreetMapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StreetMapGenerator generator = (StreetMapGenerator)target;

        GUILayout.Space(20);
        if (GUILayout.Button("Generate Street Map", GUILayout.Height(35)))
        {
            generator.GenerateMap();
        }

        if (GUILayout.Button("Clear Map", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear Map?", "Are you sure you want to delete all spawned street objects?", "Yes", "No"))
            {
                generator.ClearExistingMap();
            }
        }
    }
}
#endif