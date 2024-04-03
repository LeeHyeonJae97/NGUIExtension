using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IReuseTableCellData
{
    /// <summary>
    /// Size of the cell when updated with this data.
    /// </summary>
    
    Vector2 Size { get; set; }
}
