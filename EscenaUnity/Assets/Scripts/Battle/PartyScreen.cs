/*
 * Esta clase se encarga de la pantalla de selecci�n de Pok�mon cuando queremos cambiar a otro distinto en medio de la partida.
 * Coge la informaci�n de los Pok�mon en nuestro grupo y la muestra por pantalla, permitiendo seleccionarlos indivisualmente.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyScreen : MonoBehaviour
{
	[SerializeField] Text messageText;
	PartyMemberUI[] memberSlots;
	List<Pokemon> pokemon;

	public void Init()
	{
		memberSlots = GetComponentsInChildren<PartyMemberUI>();   
	}

	public void SetPartyData(List<Pokemon> pokemon)
	{
		this.pokemon = pokemon;

		for(int i=0; i<memberSlots.Length; i++)
		{
			if (i < pokemon.Count)
				memberSlots[i].SetData(pokemon[i]);
			else
				memberSlots[i].gameObject.SetActive(false);
		}

		messageText.text = "�Elige un Pok�mon!";
	}

	public void UpdateMemberselection(int selectedMember)
	{
		for(int i=0; i<pokemon.Count; i++)
		{
			if (i == selectedMember)
				memberSlots[i].SetSelected(true);
			else
				memberSlots[i].SetSelected(false);
		}
	}

	public void SetMessageText(string message)
	{
		messageText.text = message;
	}
}
