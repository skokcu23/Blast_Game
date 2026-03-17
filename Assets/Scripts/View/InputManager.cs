using System;
using UnityEngine;

/// <summary>
/// Converts raw mouse/touch input into grid-level events.
/// Fires OnCubeTapped with the grid Coordinate of the clicked cell.
/// </summary>
public class InputManager : MonoBehaviour
{
    public event Action<Coordinate> OnCubeTapped;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null)
            {
                Debug.LogError("[InputManager] Camera.main is NULL! Check your MainCamera tag.");
                return;
            }

            Vector3 rawWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(rawWorldPos.x, rawWorldPos.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                CubeView clickedCube = hit.collider.GetComponent<CubeView>();
                if (clickedCube != null)
                {
                    OnCubeTapped?.Invoke(clickedCube.GridCoordinate);
                }
            }
        }
    }
}
