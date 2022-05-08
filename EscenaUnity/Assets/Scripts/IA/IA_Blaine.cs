/*
 * Esta clase implementa la IA 'tonta' para la batalla contra Blaine. Su funcionamiento está explicado en la documentación del proyecto.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IA_Blaine : PokemonIA
{
	public override Pokemon decideSwitch()
	{
		return null;
	}

	public override ItemID decideItemUse()
	{
		//Si su Pokemon tiene la mitad de la vida o menos y tiene Pociones, tiene un 20% de probabilidades de usarla.
		ItemID item = ItemID.NULL;
		if (myParty.NMaxPotions > 0)
		{
			int random = Random.Range(0, 100);
			if (random < 40) item = ItemID.maxPotion;
		}
		return item;
	}

	public override Move decideNextMove()
	{
		//Coge el primer movimiento con tipo supereficaz que encuentra
		Move moveChoice = null;
		foreach (Move move in myUnit.Pokemon.Moves)
		{
			if (TypeChart.GetEffectiveness(move.Base.Type, opposingUnit.Pokemon.Base.Type1) *
				TypeChart.GetEffectiveness(move.Base.Type, opposingUnit.Pokemon.Base.Type2) > 1f)
			{
				moveChoice = move;
				break;
			}
		}
		//Si no hay, coge uno aleatorio
		if (moveChoice == null) moveChoice = myUnit.Pokemon.GetRandomMove();
		return moveChoice;
	}

	public override Pokemon decideNextPokemon()
	{
		return myParty.GetHealthyPokemon();
	}

}
