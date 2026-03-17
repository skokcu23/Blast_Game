using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // This is the event the Orchestrator will listen to
    public event Action<Coordinate> OnCubeTapped;

    void Update()
    {
        // Detect left mouse click (works for mobile touch as well!)
        if (Input.GetMouseButtonDown(0))
        {
            // Convert screen click to 2D world space
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Shoot a tiny raycast exactly at the mouse position
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                CubeView clickedCube = hit.collider.GetComponent<CubeView>();
                if (clickedCube != null)
                {
                    // If we hit a cube, broadcast its coordinate!
                    // The '?.Invoke' means "if anyone is listening, trigger the event"
                    OnCubeTapped?.Invoke(clickedCube.GridCoordinate);
                }
            }
        }
    }
}
