/*
 * Clase que implementa los movimientos de los pok�mon. Contiene el n�mero de veces que se puede utilizar y el movimiento como tal (MoveBase)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
	public MoveBase Base { get; set; }
	public int PP { get; set; }

	public Move(MoveBase pBase)
	{
		Base = pBase;
		PP = pBase.PP;
	}
}
