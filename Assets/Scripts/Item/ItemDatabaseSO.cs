using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemDatabase")]
public class ItemDatabaseSO : ScriptableObject
{
    public List<ItemDataSO> allItems;
}
