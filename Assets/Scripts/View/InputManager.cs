using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public event Action<Coordinate> OnCubeTapped;

    void Update()
    {
        // TEST 1: Is the Update loop even running?
        // (Uncomment this if you get absolutely NO logs at all)
        // Debug.Log("[Diagnostics] InputManager is alive.");

        if (Input.GetMouseButtonDown(0))
        {
            // TEST 2: Did Unity detect the hardware click?
            Debug.Log(
                $"[Diagnostics] 1. Hardware Click Detected at Screen Position: {Input.mousePosition}"
            );

            if (Camera.main == null)
            {
                Debug.LogError(
                    "[Diagnostics] ERROR: Camera.main is NULL! Check your MainCamera tag."
                );
                return;
            }

            // Convert to world space and force it to a 2D vector (dropping the Z depth)
            Vector3 rawWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(rawWorldPos.x, rawWorldPos.y);

            Debug.Log($"[Diagnostics] 2. Converted to World Space 2D: {mousePos2D}");

            // TEST 3: Visual Debugging - Draw a red cross exactly where the game thinks you clicked
            Debug.DrawLine(
                rawWorldPos - Vector3.up * 0.5f,
                rawWorldPos + Vector3.up * 0.5f,
                Color.red,
                2f
            );
            Debug.DrawLine(
                rawWorldPos - Vector3.right * 0.5f,
                rawWorldPos + Vector3.right * 0.5f,
                Color.red,
                2f
            );

            // TEST 4: The Physics Raycast
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log(
                    $"[Diagnostics] 3. SUCCESS! Raycast hit an object named: {hit.collider.gameObject.name}"
                );

                CubeView clickedCube = hit.collider.GetComponent<CubeView>();
                if (clickedCube != null)
                {
                    Debug.Log(
                        $"[Diagnostics] 4. Object is a Cube! Firing event for Coordinate: {clickedCube.GridCoordinate.x}, {clickedCube.GridCoordinate.y}"
                    );
                    OnCubeTapped?.Invoke(clickedCube.GridCoordinate);
                }
                else
                {
                    Debug.LogWarning(
                        "[Diagnostics] Hit an object, but it doesn't have a CubeView script!"
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    "[Diagnostics] 3. FAILED: Raycast hit absolutely nothing in the 2D physics world."
                );
            }
        }
    }
}
