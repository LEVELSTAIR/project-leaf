using UnityEngine;

[System.Serializable]
public class SaveData
{
    // Position & Rotation (we store floats because Vector3/Quaternion are not serializable by JsonUtility)
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ, rotW;

    // Health
    public int currentHealth;
    public int maxHealth;

    // Oxygen
    public float currentOxygen;
    public float maxOxygen;

    // (Optional) Add more fields as needed, e.g. inventory, score, gender, etc.

    // Helper methods to convert to/from Unity types
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