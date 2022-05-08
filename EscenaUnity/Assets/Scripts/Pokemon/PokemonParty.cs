/*
 * Esta clase representa el grupo de pokémon de un entrenador. Contiene referencias a cada pokémon presente además de métodos para
 * obtener pokémon que aún no están debilitados y también para consumir distintos items.
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokemonParty : MonoBehaviour
{
	[SerializeField] private List<Pokemon> pokemon;

	public List<Pokemon> Pokemons
	{
		get { 
			return pokemon; 
		}
	}
	public void Init()
	{
		foreach(var poke in pokemon)
		{
			poke.Init();
		}
	}

	public Pokemon GetHealthyPokemon()
	{
		return pokemon.Where(x => x.HP > 0).FirstOrDefault();
	}

	public int GetNumHealthyPokemon()
    {
		int num = 0;
		foreach(var p in pokemon)
        {
			if (p.HP > 0) num++;
        }
		return num;
    }

	[SerializeField] private int nFullHeals;

	public float NFullHeals
    {
		get
        {
			return nFullHeals;
        }
    }

	[SerializeField] private int nMaxPotions;

	public float NMaxPotions
	{
		get
		{
			return nMaxPotions;
		}
	}

	public void consumeItem(ItemID item)
    {
		if (item == ItemID.fullHeal) nFullHeals--;
		else if (item == ItemID.maxPotion) nMaxPotions--;

		Debug.Log($"Consumed a {item.ToString()}");
    }
}
