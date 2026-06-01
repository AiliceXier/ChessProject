using UnityEngine;

public class TileClickHandler : MonoBehaviour
{
    public int tileX;
    public int tileZ;

    private void OnMouseDown()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTileClicked(tileX, tileZ);
    }
}