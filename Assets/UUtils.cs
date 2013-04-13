using UnityEngine;
using System.Collections.Generic;

public class UUtils
{

    // Example path: "Assets/Prefabs/myPrefab.prefab"
    public static GameObject GetAssetInstance(string assetPath)
    {
        return GameObject.Instantiate(Resources.LoadAssetAtPath(assetPath, typeof(GameObject))) as GameObject;
    }

    public static GameObject GetAsset(string assetPath)
    {
        return Resources.Load(assetPath, typeof(GameObject)) as GameObject;
    }

    public static System.Random rnd = new System.Random();

    public static T PickRandom<T>(List<T> list)
    {
        return list[rnd.Next(list.Count - 1)];
    }
}
