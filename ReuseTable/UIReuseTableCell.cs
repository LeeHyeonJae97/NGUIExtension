using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UIReuseTableCell : MonoBehaviour
{
    /// <summary>
    /// Update cell with data.
    /// </summary>
    /// <param name="data">Data to update with</param>

    public abstract void UpdateData(IReuseTableCellData data);
}
