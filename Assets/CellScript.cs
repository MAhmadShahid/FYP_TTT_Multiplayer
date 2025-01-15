using UnityEngine;
using Mirror;

public class CellScript : MonoBehaviour
{
    [SerializeField] public int cellValue;
    public MatchController matchController;

    [ClientCallback]
    public void OnCellTouched()
    {
        if (matchController.currentPlayer != null && matchController.currentPlayer.isLocalPlayer)
            matchController?.CommandMakePlay_NewRule(cellValue);
    }

}
