using System.Threading.Tasks;
using UnityEngine;

public class CubeView : MonoBehaviour
{
    private Coordinate _coord;
    private GameOrchestrator _orchestrator;

    public void Setup(Coordinate coord, Gem type)
    {
        _coord = coord;
        _orchestrator = FindFirstObjectByType<GameOrchestrator>();
        // Set sprite color based on Gem type here...
    }

    private void OnMouseDown()
    {
        _orchestrator.OnCellTapped(_coord);
    }

    public async Task PlayPopAnimation()
    {
        // Use a Tween library like DOTween for "nice animations" [cite: 163, 166]
        transform.localScale = Vector3.one * 1.2f;
        await Task.Delay(100);

        Destroy(gameObject);
    }
}
