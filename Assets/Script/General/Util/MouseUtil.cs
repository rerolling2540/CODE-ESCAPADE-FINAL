using UnityEngine;

public static class MouseUtil 
{
    public static Vector3 GetMousePositionInWorldSpace(float zValue = 0f)
    {
        TryGetMousePositionInWorldSpace(out Vector3 position, zValue);
        return position;
    }

    public static bool TryGetMousePositionInWorldSpace(out Vector3 position, float zValue = 0f)
    {
        position = Vector3.zero;
        // Scene changes destroy the old camera; resolve the current one for each request.
        Camera camera = Camera.main;
        if (camera == null || !camera.isActiveAndEnabled) return false;
        Plane dragPlane = new(camera.transform.forward, new Vector3(0, 0, zValue));
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float distance))
        {
            position = ray.GetPoint(distance);
            return true;
        }
        return false;
    }
}
