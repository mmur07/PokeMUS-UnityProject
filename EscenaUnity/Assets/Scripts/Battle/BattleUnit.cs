/*
 * La clase BattleUnit representa a cada entrenador presente en la batalla, y lleva el control de sus atributos: el HUD de batalla,
 * el pokémon que está luchando en ese momento y las animaciones.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BattleUnit : MonoBehaviour
{
	int level;
	[SerializeField] bool isPlayerUnit;
	[SerializeField] BattleHud hud;

	private FMODUnity.StudioEventEmitter eventEmitter;

	public BattleHud Hud
	{
		get { return hud; }
	}

	public bool IsPlayerUnit
	{
		get { return isPlayerUnit; }
	}

	public Pokemon Pokemon { get; set; }

	Image image;
	Vector3 originalPos;
	Color originalColor;

	private void Awake()
	{
		image = GetComponent<Image>();
		originalPos = image.transform.localPosition;
		originalColor= image.color;
	}

	public void Setup(Pokemon pokemon)
	{
		Pokemon = pokemon;
		if (isPlayerUnit)
			image.sprite = Pokemon.Base.BackSprite;
		else 
			image.sprite = Pokemon.Base.FrontSprite;

		hud.gameObject.SetActive(true);
		hud.SetData(pokemon);

		image.color = originalColor;

		eventEmitter = gameObject.GetComponent<FMODUnity.StudioEventEmitter>();
		PlayerEnterAnimation();
	}

	public void Clear()
    {
		hud.gameObject.SetActive(false);
    }

	private int PokemonNameToParamID()
    {
        switch (Pokemon.Base.Name)
        {
			case ("Rapidash"):
				return 0;
			case ("Suicune"):
				return 1;
			case ("Tyranitar"):
				return 2;
			case ("Swampert"):
				return 3;
			case ("Ludicolo"):
				return 4;
			case ("Flygon"):
				return 5;
			case ("Glalie"):
				return 6;
			case ("Metagross"):
				return 7;
			case ("Rayquaza"):
				return 8;
			case ("Mismagius"):
				return 9;
			case ("Lucario"):
				return 10;
			case ("Glaceon"):
				return 11;
			default:
				return 0;
		}
    }

	public void PlayerEnterAnimation()
	{
		eventEmitter.Play();
		eventEmitter.EventInstance.setParameterByName("PokemonName", PokemonNameToParamID());

		if (isPlayerUnit)
			image.transform.localPosition = new Vector3(-500f, originalPos.y);
		else
			image.transform.localPosition = new Vector3(500f, originalPos.y);

		image.transform.DOLocalMoveX(originalPos.x, 1f);
	}

	public void PlayAttackAnimation()
	{
		var sequence = DOTween.Sequence();
		if (isPlayerUnit)
			sequence.Append(image.transform.DOLocalMoveX(originalPos.x + 50f, 0.25f));
		else
			sequence.Append(image.transform.DOLocalMoveX(originalPos.x - 50f, 0.25f));

		sequence.Append(image.transform.DOLocalMoveX(originalPos.x, 0.25f));
	}

	public void PlayHitAnimation()
	{
		var sequence = DOTween.Sequence();
		sequence.Append(image.DOColor(Color.gray, 0.1f));
		sequence.Append(image.DOColor(originalColor, 0.1f));
	}

	public void PlayFaintAnimation()
	{
		eventEmitter.Play();
		eventEmitter.EventInstance.setParameterByName("PokemonName", PokemonNameToParamID());

		var sequence = DOTween.Sequence();
		sequence.Append(image.transform.DOLocalMoveY(originalPos.y - 150f, 0.5f));
		sequence.Join(image.DOFade(0f, 0.5f));
	}
}
