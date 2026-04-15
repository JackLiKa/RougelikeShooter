using System;
// using System.Runtime.Versioning;
using UnityEngine;

public class UnityTool
{
    private static UnityTool instance;
    public static UnityTool Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UnityTool();
            }
            return instance;
        }
    }
    public T GetComponentFromChildren<T>(GameObject obj, string name)
    {
        foreach (Transform t in obj.GetComponentsInChildren<Transform>())
        {
            if (t.name == name)
            {
                if (t.GetComponent<T>() != null)
                {
                    return t.GetComponent<T>();
                }
            }
        }
        return default;
    }
}