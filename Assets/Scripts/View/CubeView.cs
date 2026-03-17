using System.Threading.Tasks;
using UnityEngine;

public class CubeView : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    private Coordinate _coordinate;

    // Public getter so the InputManager can ask "What coordinate are you?"
    public Coordinate GridCoordinate => _coordinate;

    // Notice we removed the Orchestrator parameter completely!
    public void Setup(Coordinate coord, Sprite sprite)
    {
        _coordinate = coord;
        _spriteRenderer.sprite = sprite;
        name = $"Cube_{coord.x}_{coord.y}";
    }

    public async Task PlayPopAnimation()
    {
        transform.localScale = Vector3.one * 1.2f;
        await Task.Delay(100);
        Destroy(gameObject);
    }
}
