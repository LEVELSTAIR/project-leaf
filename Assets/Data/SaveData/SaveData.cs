using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TreeState
{
    public string id;

    // ---------- Position & Rotation (for dynamic trees) ----------
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ, rotW;

    // ---------- TreeCuttable data ----------
    public bool isCutDown;
    public int currentHits;
    public float respawnTimeRemaining;

    // ---------- SeedTree data ----------
    public bool isRegrowing;
    public float regrowTimeRemaining;
    public int harvestCount;
    public int regrowCycleCount;
}

[System.Serializable]
public class SaveData
{
    // ---------- Player ----------
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ, rotW;

    public int currentHealth;
    public int maxHealth;

    public float currentOxygen;
    public float maxOxygen;

    // ---------- Trees ----------
    public List<TreeState> treeStates = new List<TreeState>();

    // Helper methods for player
    public Vector3 GetPosition() => new Vector3(posX, posY, posZ);
    public Quaternion GetRotation() => new Quaternion(rotX, rotY, rotZ, rotW);

    public void SetPosition(Vector3 pos)
    {
        posX = pos.x;
        posY = pos.y;
        posZ = pos.z;
    }

    public void SetRotation(Quaternion rot)
    {
        rotX = rot.x;
        rotY = rot.y;
        rotZ = rot.z;
        rotW = rot.w;
    }
}