using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    public int tileX;
    public int tileZ;
    public bool isWhite;
    public PieceType pieceType;
    public bool hasMoved;   // 新增：记录棋子是否移动过

    private void OnMouseDown()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPieceClicked(this);
    }
}