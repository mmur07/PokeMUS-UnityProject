/*
 * Clase que representa un item del juego, para su posterior almacenamiento en ItemsDB.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item
{
    public ItemID Id { get; set; }
    public string Name { get; set; }

    public string StartMessage { get; set; }

    public Action<Pokemon> OnUse { get; set; }
}
