using UnityEngine;

public class PlayerSave : MonoBehaviour
{
    public bool Save(Player player, string saveId)
    {
        return SaveSystem.Save(saveId, player.GetTrapIdGrid(), player.GetTrapRotationGrid(), player.gridRows, player.gridCols);
    }

    public bool Load(Player player, string saveId)
    {
        if (SaveSystem.Load(saveId, out int[,] loadedGrid, out int[,] loadedRotations, out int rows, out int cols))
        {
            if (rows != player.gridRows || cols != player.gridCols)
            {
                Debug.LogWarning($"[PlayerSave] Dimensões do save '{saveId}' ({rows}x{cols}) diferem das atuais ({player.gridRows}x{player.gridCols}). Ajustando...");
                // Optionally resize or reject
            }
            player.SetTrapIdGrid(loadedGrid);
            player.SetTrapRotationGrid(loadedRotations);
            Debug.Log($"[PlayerSave] Save '{saveId}' carregado com sucesso.");
            return true;
        }
        return false;
    }

    public bool LoadAndRebuild(Player player, string saveId)
    {
        if (Load(player, saveId))
        {
            player.RebuildTrapsFromGrid();
            return true;
        }
        return false;
    }
}
