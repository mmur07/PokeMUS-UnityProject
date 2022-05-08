/*
 * Esta clase se encarga de cada una de las unidades de información de nuestros Pokémon presentes en la clase PartyScreen.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
	[SerializeField] Text nameText;
	[SerializeField] Text levelText;
	[SerializeField] HPBar hpBar;

	[SerializeField] Color highlightColor;

	Pokemon _pokemon;

	public void SetData(Pokemon pokemon)
	{
		_pokemon = pokemon;

		nameText.text = pokemon.Base.Name;
		levelText.text = "Nivel " + pokemon.Level;

		hpBar.SetHP((float)(pokemon.HP / pokemon.MaxHP));
		StartCoroutine(UpdateHP());
	}

	public IEnumerator UpdateHP()
	{
		yield return hpBar.SetHPSmooth((float)_pokemon.HP / _pokemon.MaxHP);
	}

	public void SetSelected(bool selected)
	{
		if (selected)
			nameText.color = highlightColor;
		else
			nameText.color = Color.black;
	}
}
