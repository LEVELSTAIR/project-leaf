using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Utility for converting frustum planes to float4 for Burst jobs.
/// </summary>
public static class FrustumCuller
{
    public static float4 PlaneToFloat4(Plane plane)
    {
        return new float4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
    }

    public static float4[] GetFrustumPlanesFloat4(Camera camera)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        float4[] result = new float4[6];
        for (int i = 0; i < 6; i++)
            result[i] = PlaneToFloat4(planes[i]);
        return result;
    }
}