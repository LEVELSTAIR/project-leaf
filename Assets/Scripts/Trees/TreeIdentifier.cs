using UnityEngine;
using System;

public class TreeIdentifier : MonoBehaviour
{
    [SerializeField] private string uniqueID;
    private bool isIDLocked = false;

    public string UniqueID => uniqueID;

    public void SetID(string newID, bool lockID = true)
    {
        uniqueID = newID;
        if (lockID) isIDLocked = true;
    }

    private void Awake()
    {
        if (isIDLocked) return;

        if (string.IsNullOrEmpty(uniqueID) || IsDuplicateInScene(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
            Debug.Log($"Generated new ID for {name}: {uniqueID}");
        }
    }

    private bool IsDuplicateInScene(string id)
    {
        TreeIdentifier[] all = FindObjectsOfType<TreeIdentifier>();
        int count = 0;
        foreach (var t in all)
        {
            if (t == this) continue;
            if (t.uniqueID == id) count++;
        }
        return count > 0;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isIDLocked && (string.IsNullOrEmpty(uniqueID) || IsDuplicateInScene(uniqueID)))
        {
            uniqueID = Guid.NewGuid().ToString();
            Debug.Log($"Generated new ID for {name}: {uniqueID}");
        }
    }

    public void GenerateNewID()
    {
        uniqueID = Guid.NewGuid().ToString();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}