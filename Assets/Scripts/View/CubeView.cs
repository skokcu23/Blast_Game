using System.Threading.Tasks;
using UnityEngine;

public class CubeView : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer _spriteRenderer;
    private Coordinate _coordinate;
    private GameOrchestrator _orchestrator;

    // This is called by the BoardView when the game starts
    public void Setup(Coordinate coord, Sprite sprite, GameOrchestrator orchestrator)
    {
        _coordinate = coord;
        _orchestrator = orchestrator;
        _spriteRenderer.sprite = sprite;

        // Helpful for debugging in the Hierarchy
        name = $"Cube_{coord.x}_{coord.y}";
    }

    private void OnMouseDown()
    {
        _orchestrator.OnCellTapped(_coordinate);
    }

    public async Task PlayPopAnimation()
    {
        // Use a Tween library like DOTween for "nice animations" [cite: 163, 166]
        transform.localScale = Vector3.one * 1.2f;
        await Task.Delay(100);

        Destroy(gameObject);
    }
}
