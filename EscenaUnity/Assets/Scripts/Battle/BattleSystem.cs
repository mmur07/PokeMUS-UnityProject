/*
 * Esta clase implementa la lógica de la batalla. Al principio, inicializa todos los elementos que formarán parte de esta, mientras
 * muestra sus respectivos textos en la caja de texto. Tiene una máquina de estados que indica el punto de la batalla en el que nos
 * encontramos (comienzo, el jugador está seleccionando acción o seleccionando movimiento, el turno está en curso, el juego está ocupado,
 * estamos en la pantalla de selección de Pokémon, se ha elegido una opción de batalla, o la batalla ha acabado). Para cualquiera de estos
 * estados, se llamará a la función correspondiente que haga la acción necesaria. Se activará el menú de selección si es el turno del
 * jugador, y después se invocará la función RunTurns para ejecutar el movimiento en cuestión. Tras cada turno se aplicarán los efectos
 * alterados que tenga cada pokémon (si los hay) y se comprobará si la batalla se ha acabado (si uno de los dos bandos se ha quedado sin
 * ningún pokémon). Si no, el jugador volverá a elegir una acción, y así sucesivamente.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, AboutToUse, BattleOver }
public enum BattleAction { Move, SwitchPokemon, UseItem, Run }
public class BattleSystem : MonoBehaviour
{

	[SerializeField] BattleUnit playerUnit;
	[SerializeField] BattleUnit enemyUnit;
	[SerializeField] BattleDialogBox dialogBox;
	[SerializeField] PartyScreen partyScreen;
	[SerializeField] Trainer player;
	[SerializeField] Trainer rival;

	BattleState state;
	BattleState? previState;
	int currentAction;
	int currentMove;
	int currentMember;
	bool aboutToUseChoice = true;
	Pokemon aboutToUsePokemon = null;

	PokemonParty playerParty;
	PokemonParty rivalParty;

	Image playerImage;
	Image rivalImage;

	PokemonIA enemyAI;

	FMODUnity.StudioEventEmitter eventEmitter;

	private void Start()
	{
		playerParty = player.TrainerParty;
		rivalParty = rival.TrainerParty;
		ConditionsDB.Init();
		ItemsDB.Init();

		playerImage = gameObject.transform.Find("BattleCanvas").transform.Find("PlayerImage").GetComponent<Image>();
		rivalImage = gameObject.transform.Find("BattleCanvas").transform.Find("RivalImage").GetComponent<Image>();
		playerImage.sprite = player.TrainerSprite;
		rivalImage.sprite = rival.TrainerSprite;

		enemyAI = rival.TrainerAI;

		eventEmitter = gameObject.GetComponent<FMODUnity.StudioEventEmitter>();
	}

	public void StartBattle()
	{
		StartCoroutine(SetupBattle());
	}

	public IEnumerator SetupBattle()
	{
		playerUnit.Clear();
		enemyUnit.Clear();

		playerParty.Init();
		rivalParty.Init();

		playerUnit.gameObject.SetActive(false);
		enemyUnit.gameObject.SetActive(false);

		playerImage.gameObject.SetActive(true);
		rivalImage.gameObject.SetActive(true);
		yield return dialogBox.TypeDialog($"¡{rival.TrainerName} te desafía!");

		rivalImage.gameObject.SetActive(false);
		enemyUnit.gameObject.SetActive(true);
		var enemyPokemon = rivalParty.GetHealthyPokemon();
		enemyUnit.Setup(enemyPokemon);
		yield return dialogBox.TypeDialog($"¡{rival.TrainerName} saca a {enemyPokemon.Base.Name}!");

		playerImage.gameObject.SetActive(false);
		playerUnit.gameObject.SetActive(true);
		var playerPokemon = playerParty.GetHealthyPokemon();
		playerUnit.Setup(playerPokemon);
		yield return dialogBox.TypeDialog($"¡Vamos, {playerPokemon.Base.Name}!");

		dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);

		partyScreen.Init();

		enemyAI.Init(rivalParty, playerParty, enemyUnit, playerUnit);

		ActionSelection();
	}

	IEnumerator BattleOver(bool won)
	{
		state = BattleState.BattleOver;
		yield return OnBattleOver(won);
	}

	void ActionSelection()
	{
		state = BattleState.ActionSelection;
		StartCoroutine(dialogBox.TypeDialog("Elige una acción"));
		dialogBox.EnableActionSelector(true);
	}

	void MoveSelection()
	{
		state = BattleState.MoveSelection;
		dialogBox.EnableActionSelector(false);
		dialogBox.EnableDialogText(false);
		dialogBox.EnableMoveSelector(true);
	}

	IEnumerator AboutToUse(Pokemon newPokemon)
	{
		state = BattleState.Busy;
		yield return dialogBox.TypeDialog($"{rival.TrainerName} va a sacar a {newPokemon.Base.name}. ¿Quieres cambiar de Pokémon?");

		state = BattleState.AboutToUse;
		dialogBox.EnableChoiceBox(true);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene("MainMenu");
		if (state == BattleState.ActionSelection)
		{
			HandleActionSelection();
		}
		else if (state == BattleState.MoveSelection)
		{
			HandleMoveSelection();
		}
		else if (state == BattleState.PartyScreen)
		{
			HandlePartySelection();
		}
		else if (state == BattleState.AboutToUse)
		{
			HandleAboutToUse();
		}
	}

	void HandleActionSelection()
	{
		if (Input.GetKeyDown(KeyCode.RightArrow)) ++currentAction;
		else if (Input.GetKeyDown(KeyCode.LeftArrow)) --currentAction;
		else if (Input.GetKeyDown(KeyCode.DownArrow)) currentAction += 2;
		else if (Input.GetKeyDown(KeyCode.UpArrow)) currentAction -= 2;

		currentAction = Mathf.Clamp(currentAction, 0, 3);

		dialogBox.UpdateActionSelection(currentAction);

		if (Input.GetKeyDown(KeyCode.Z))
		{
			if (currentAction == 0)
			{
				//Fight
				MoveSelection();
			}

			else if (currentAction == 1)
			{
				//Bag
				//forbiddenSound.Play();
			}

			if (currentAction == 2)
			{
				//Pokemon
				previState = state;
				OpenPartyScreen();
			}

			else if (currentAction == 3)
			{
				//Run
				SceneManager.LoadScene("MainMenu");
			}
		}
	}

	void OpenPartyScreen()
	{
		state = BattleState.PartyScreen;
		partyScreen.gameObject.SetActive(true);
		eventEmitter.EventInstance.setParameterByName("OnMenu", 1);
		partyScreen.SetPartyData(playerParty.Pokemons);
	}

	void HandleMoveSelection()
	{
		if (Input.GetKeyDown(KeyCode.RightArrow)) ++currentMove;
		else if (Input.GetKeyDown(KeyCode.LeftArrow)) --currentMove;
		else if (Input.GetKeyDown(KeyCode.DownArrow)) currentMove += 2;
		else if (Input.GetKeyDown(KeyCode.UpArrow)) currentMove -= 2;

		currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Pokemon.Moves.Count - 1);

		dialogBox.UpdateMoveSelection(currentMove, playerUnit.Pokemon.Moves[currentMove]);

		if (Input.GetKeyDown(KeyCode.Z))
		{
			var move = playerUnit.Pokemon.Moves[currentMove];
			if (move.PP == 0) return;

			dialogBox.EnableMoveSelector(false);
			dialogBox.EnableDialogText(true);
			StartCoroutine(RunTurns(BattleAction.Move));
		}
		else if (Input.GetKeyDown(KeyCode.X))
		{
			dialogBox.EnableMoveSelector(false);
			dialogBox.EnableDialogText(true);
			ActionSelection();
		}
	}

	void HandlePartySelection()
	{
		if (Input.GetKeyDown(KeyCode.RightArrow)) ++currentMember;
		else if (Input.GetKeyDown(KeyCode.LeftArrow)) --currentMember;
		else if (Input.GetKeyDown(KeyCode.DownArrow)) currentMember += 2;
		else if (Input.GetKeyDown(KeyCode.UpArrow)) currentMember -= 2;

		currentMember = Mathf.Clamp(currentMember, 0, playerParty.Pokemons.Count - 1);

		partyScreen.UpdateMemberselection(currentMember);

		if (Input.GetKeyDown(KeyCode.Z))
		{
			var selectedMember = playerParty.Pokemons[currentMember];
			if (selectedMember.HP <= 0)
			{
				partyScreen.SetMessageText("¡No puedes sacar a un pokémon debilitado!");
				return;
			}
			if (selectedMember == playerUnit.Pokemon)
			{
				partyScreen.SetMessageText("¡Ese Pokémon ya está en el campo de batalla!");
				return;
			}

			partyScreen.gameObject.SetActive(false);
			eventEmitter.EventInstance.setParameterByName("OnMenu", 0);

			if (previState == BattleState.ActionSelection)
			{
				previState = null;
				StartCoroutine(RunTurns(BattleAction.SwitchPokemon));
			}
			else
			{
				state = BattleState.Busy;
				StartCoroutine(SwitchPokemon(selectedMember, playerUnit));
			}
			if (playerParty.GetNumHealthyPokemon() <= 2 && rivalParty.GetNumHealthyPokemon() > 2)
			{
				eventEmitter.EventInstance.setParameterByName("GameCondition", 2); //Pierdes
			}
		}
		else if (Input.GetKeyDown(KeyCode.X))
		{
			if (playerUnit.Pokemon.HP <= 0)
			{
				partyScreen.SetMessageText("Debes elegir un Pokémon para continuar");
				return;
			}

			partyScreen.gameObject.SetActive(false);
			eventEmitter.EventInstance.setParameterByName("OnMenu", 0);

			if (previState == BattleState.AboutToUse)
			{
				previState = null;
				StartCoroutine(SendNextTrainerPokemon());
			}
			else
				ActionSelection();
		}
	}

	void HandleAboutToUse()
	{
		if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
			aboutToUseChoice = !aboutToUseChoice;

		dialogBox.UpdateChoiceBox(aboutToUseChoice);

		if (Input.GetKeyDown(KeyCode.Z))
		{
			dialogBox.EnableChoiceBox(false);
			if (aboutToUseChoice)
			{
				previState = BattleState.AboutToUse;
				OpenPartyScreen();

			}
			else
			{
				StartCoroutine(SendNextTrainerPokemon());
			}
		}
		else if (Input.GetKeyDown(KeyCode.X))
		{
			dialogBox.EnableChoiceBox(false);
			StartCoroutine(SendNextTrainerPokemon());
		}
	}

	IEnumerator SwitchPokemon(Pokemon newPokemon, BattleUnit unit)
	{
		if (unit.Pokemon.HP > 0)
		{
			yield return dialogBox.TypeDialog($"¡Vuelve, {unit.Pokemon.Base.Name}!");
			unit.PlayFaintAnimation();
			yield return new WaitForSeconds(2f);
			unit.Pokemon.CureVolatileStatus();
			unit.Pokemon.ResetStatBoosts();
		}

		unit.Setup(newPokemon);
		if (unit == playerUnit)
		{
			dialogBox.EnableActionSelector(false);
			dialogBox.SetMoveNames(newPokemon.Moves);

			if(playerUnit.Pokemon.HP <= playerUnit.Pokemon.MaxHP * 0.2) eventEmitter.EventInstance.setParameterByName("LowHealth", 1);
			else eventEmitter.EventInstance.setParameterByName("LowHealth", 0);
		}
		yield return dialogBox.TypeDialog($"¡Vamos, {newPokemon.Base.Name}!");

		if (previState == null)
		{
			state = BattleState.RunningTurn;
		}
		else if (previState == BattleState.AboutToUse)
		{
			previState = null;
			StartCoroutine(SendNextTrainerPokemon());
		}
	}

	IEnumerator RunTurns(BattleAction playerAction)
	{
		state = BattleState.RunningTurn;

		playerUnit.Pokemon.CurrentMove = playerUnit.Pokemon.Moves[currentMove];
		enemyUnit.Pokemon.CurrentMove = enemyAI.decideNextMove();

		bool enemyCanMove = true;
		bool playerCanMove = true;

		var enemySwitch = enemyAI.decideSwitch();
		if (enemySwitch != null)
		{
			state = BattleState.Busy;
			yield return SwitchPokemon(enemySwitch, enemyUnit);
			enemyCanMove = false;
		}

		if (playerAction == BattleAction.SwitchPokemon)
		{
			var selectedPokemon = playerParty.Pokemons[currentMember];
			state = BattleState.Busy;
			enemyAI.onEnemySwitch(playerUnit.Pokemon);
			yield return SwitchPokemon(selectedPokemon, playerUnit);
			playerCanMove = false;
		}

		if (enemyCanMove)
		{
			ItemID item = enemyAI.decideItemUse();
			if (item != ItemID.NULL)
			{ //Null implica que no quiere usar un objeto.
				ItemsDB.Items[item].OnUse.Invoke(enemyUnit.Pokemon); //Se aplica el objeto.
				rivalParty.consumeItem(item);

				yield return ShowStatusChanges(enemyUnit.Pokemon);
				yield return enemyUnit.Hud.UpdateHP();
			}

			if (item != ItemID.NULL)
			{
				enemyCanMove = false;
			}
		}

		bool playerGoesFirst = true;

		if (enemyCanMove && playerCanMove)
		{
			int playerMovePriority = playerUnit.Pokemon.CurrentMove.Base.Priority;
			int enemyMovePriority = enemyUnit.Pokemon.CurrentMove.Base.Priority;

			int playerSpeed = (playerUnit.Pokemon.Status?.Id == ConditionID.par) ? playerUnit.Pokemon.Speed / 4 : playerUnit.Pokemon.Speed;
			int enemySpeed = (enemyUnit.Pokemon.Status?.Id == ConditionID.par) ? enemyUnit.Pokemon.Speed / 4 : enemyUnit.Pokemon.Speed;

			//Comprobar quién va primero
			if (enemyMovePriority > playerMovePriority)
				playerGoesFirst = false;
			else if (enemyMovePriority == playerMovePriority)
				playerGoesFirst = playerSpeed >= enemySpeed;
		}

		var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
		var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

		var secondPokemon = secondUnit.Pokemon;

		//Primer turno

		if (playerCanMove)
		{
			enemyAI.onEnemyAttack();
			yield return RunMove(firstUnit, secondUnit, firstUnit.Pokemon.CurrentMove);
		}
		yield return RunAfterTurn(firstUnit);
		if (state == BattleState.BattleOver) yield break;

		if (secondPokemon.HP > 0)
		{
			//Segundo turno (si no se ha acabado la batalla)
			if (enemyCanMove) yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
			yield return RunAfterTurn(secondUnit);
			if (state == BattleState.BattleOver) yield break;
		}

		if (state != BattleState.BattleOver)
			ActionSelection();
	}

	IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
	{
		bool canRunMove = sourceUnit.Pokemon.OnBeforeMove();

		if (!canRunMove)
		{
			yield return ShowStatusChanges(sourceUnit.Pokemon);
			yield return sourceUnit.Hud.UpdateHP();
			yield break;
		}

		move.PP--;
		yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} usó {move.Base.Name}");

		if (CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon))
		{
			sourceUnit.PlayAttackAnimation();
			yield return new WaitForSeconds(1f);
			targetUnit.PlayHitAnimation();

			if (move.Base.Category == MoveCategory.Status)
			{
				if (move.Base.Name == "Descanso") { sourceUnit.Pokemon.CureStatus(); sourceUnit.Pokemon.CureVolatileStatus(); }
				yield return RunMoveEffects(move.Base.Effects, sourceUnit.Pokemon, targetUnit.Pokemon, move.Base.Target);
				if (move.Base.Name == "Descanso") yield return targetUnit.Hud.UpdateHP();
			}
			else
			{
				var damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
				yield return targetUnit.Hud.UpdateHP();
				yield return ShowDamageDetails(damageDetails);
			}

			if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Pokemon.HP > 0)
			{
				foreach (var secondary in move.Base.Secondaries)
				{
					var rnd = UnityEngine.Random.Range(1, 101);
					if (rnd <= secondary.Chance)
						yield return RunMoveEffects(secondary, sourceUnit.Pokemon, targetUnit.Pokemon, secondary.Target);
				}
			}

			if(targetUnit == playerUnit)
            {
				if(playerUnit.Pokemon.HP <= targetUnit.Pokemon.MaxHP * 0.5) eventEmitter.EventInstance.setParameterByName("LowHealth", 1);
				else eventEmitter.EventInstance.setParameterByName("LowHealth", 0);
			}

			if (targetUnit.Pokemon.HP <= 0)
			{
				if (targetUnit == playerUnit) enemyAI.onEnemyAttack(); //Se rompe la cadena de intercambios
				yield return dialogBox.TypeDialog($"¡{targetUnit.Pokemon.Base.Name} se ha debilitado!");
				targetUnit.PlayFaintAnimation();

				yield return new WaitForSeconds(1f);

				yield return CheckForBattleOver(targetUnit);
			}
		}
		else
		{
			yield return dialogBox.TypeDialog($"¡El ataque de {sourceUnit.Pokemon.Base.name} ha fallado!");
		}

	}

	IEnumerator RunAfterTurn(BattleUnit sourceUnit)
	{
		if (state == BattleState.BattleOver) yield break;
		yield return new WaitUntil(() => state == BattleState.RunningTurn);

		//Para condiciones de estado tipo veneno o quemado que dañan al final del turno.
		sourceUnit.Pokemon.OnAfterTurn();
		yield return ShowStatusChanges(sourceUnit.Pokemon);
		yield return sourceUnit.Hud.UpdateHP();
		if (sourceUnit.Pokemon.HP <= 0)
		{
			yield return dialogBox.TypeDialog($"¡{sourceUnit.Pokemon.Base.Name} se ha debilitado!");
			sourceUnit.PlayFaintAnimation();

			yield return new WaitForSeconds(1f);

			yield return CheckForBattleOver(sourceUnit);
			yield return new WaitUntil(() => state == BattleState.RunningTurn);
		}
	}

	IEnumerator RunMoveEffects(MoveEffects effects, Pokemon source, Pokemon target, MoveTarget moveTarget)
	{
		//Aumento de stats
		if (effects.Boosts != null)
		{
			if (moveTarget == MoveTarget.Self)
				source.ApplyBoosts(effects.Boosts);
			else
				target.ApplyBoosts(effects.Boosts);
		}

		//Condiciones de estado
		if (effects.Status != ConditionID.none)
		{
			if (moveTarget == MoveTarget.Self) source.SetStatus(effects.Status);
			else target.SetStatus(effects.Status);
		}

		//Condiciones de estado volátiles
		if (effects.VolatileStatus != ConditionID.none)
		{
			if (moveTarget == MoveTarget.Self) source.SetVolatileStatus(effects.VolatileStatus);
			else target.SetVolatileStatus(effects.VolatileStatus);

		}

		yield return ShowStatusChanges(source);
		yield return ShowStatusChanges(target);
	}

	bool CheckIfMoveHits(Move move, Pokemon source, Pokemon target)
	{
		if (move.Base.AlwaysHits) return true;

		float moveAccuracy = move.Base.Accuracy;

		int accuracy = source.StatBoosts[Stat.Accuracy];
		int evasion = target.StatBoosts[Stat.Evasion];

		var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

		if (accuracy > 0)
			moveAccuracy *= boostValues[accuracy];
		else
			moveAccuracy /= boostValues[-accuracy];

		if (evasion > 0)
			moveAccuracy /= boostValues[evasion];
		else
			moveAccuracy *= boostValues[-evasion];

		return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
	}
	IEnumerator ShowStatusChanges(Pokemon pokemon)
	{
		while (pokemon.StatusChanges.Count > 0)
		{
			var message = pokemon.StatusChanges.Dequeue();
			yield return dialogBox.TypeDialog(message);
		}
	}

	IEnumerator CheckForBattleOver(BattleUnit faintedUnit)
	{
		if (faintedUnit.IsPlayerUnit)
		{
			var nextPokemon = playerParty.GetHealthyPokemon();
			if (nextPokemon != null)
			{
				OpenPartyScreen();
			}
			else
			{
				yield return BattleOver(false);
			}
		}
		else
		{
			var nextPokemon = enemyAI.decideNextPokemon();
			if (nextPokemon != null)
			{
				aboutToUsePokemon = nextPokemon;
				StartCoroutine(AboutToUse(nextPokemon));
			}
			else
			{
				yield return BattleOver(true);
			}
		}
	}

	IEnumerator OnBattleOver(bool won)
	{
		if (won)
		{
			yield return dialogBox.TypeDialog("¡Has ganado la batalla!");
			yield return new WaitForSeconds(3f);
			SceneManager.LoadScene("MainMenu");
		}
		else
		{
			yield return dialogBox.TypeDialog("Has perdido la batalla...");
			yield return new WaitForSeconds(5f);
			SceneManager.LoadScene("MainMenu");
		}
	}
	IEnumerator ShowDamageDetails(DamageDetails damageDetails)
	{
		if (damageDetails.Critical > 1f)
			yield return dialogBox.TypeDialog("¡Un golpe crítico!");

		if (damageDetails.TypeEffectiveness > 1f)
			yield return dialogBox.TypeDialog("¡Es súper efectivo!");
		else if (damageDetails.TypeEffectiveness == 0f)
			yield return dialogBox.TypeDialog("No afecta al Pokémon rival...");
		else if (damageDetails.TypeEffectiveness < 1f)
			yield return dialogBox.TypeDialog("No es muy efectivo...");
	}

	IEnumerator SendNextTrainerPokemon()
	{
		state = BattleState.Busy;

		var nextPokemon = aboutToUsePokemon;
		enemyUnit.Setup(nextPokemon);
		yield return dialogBox.TypeDialog($"{rival.TrainerName} saca a {nextPokemon.Base.Name}");
		state = BattleState.RunningTurn;

		if (rivalParty.GetNumHealthyPokemon() <= 2)
		{
			eventEmitter.EventInstance.setParameterByName("GameCondition", 1); //Ganas
		}
	}
}
