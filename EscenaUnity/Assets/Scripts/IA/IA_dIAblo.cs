/*
 * Esta clase implementa la IA 'inteligente'. Su funcionamiento está explicado en la documentación del proyecto.
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IA_dIAblo : PokemonIA
{
	[Range(0.0f, 1.0f)]
	[SerializeField] float offensiveThreshold = 0.8f;
	[Range(0.0f, 1.0f)]
	[SerializeField] float defensiveThreshold = 0.3f;
	[SerializeField] int switchThreshold = 3;
	[SerializeField] int pointsOnOffTypeAdvante = 1;
	[SerializeField] int pointsOnKill = 2;
	[SerializeField] int pointsOnKillWithPrio = 99;
	[SerializeField] int pointsOnDefInmunity = 3;
	[SerializeField] int pointsOnDefAdvantage = 1;
	[SerializeField] int pointsOnDefDisadvantage = -1;
	[SerializeField] int maxStatDrops = 2;
	[SerializeField] int maxStatBoosts = 4;

	Pokemon lastSwitch;
	int switchCounter;

	public override Pokemon decideSwitch()
	{
		return null;
	}

	public override ItemID decideItemUse()
	{
		//Si su Pokemon tiene la mitad de la vida o menos y tiene Pociones, tiene un 20% de probabilidades de usarla.
		ItemID item = ItemID.NULL;
		if (myParty.NFullHeals > 0)
		{
			if (myUnit.Pokemon.HP >= offensiveThreshold * myUnit.Pokemon.MaxHP && (myUnit.Pokemon.Status != null || myUnit.Pokemon.VolatileStatus!= null)) item = ItemID.fullHeal;
		}
		if (myParty.NMaxPotions > 0)
		{
			if (myUnit.Pokemon.HP <= defensiveThreshold * myUnit.Pokemon.MaxHP) item = ItemID.maxPotion;
		}
		return item;
	}

	public override Move decideNextMove()
	{
		//Coge el primer movimiento con tipo supereficaz que encuentra
		Move moveChoice = null;
		int maxDamage = 0;
		List<Move> killMoves = new List<Move>();
		List<Move> statusMoves = new List<Move>();

		Pokemon unitToAttack;
		if (switchCounter >= switchThreshold) unitToAttack = lastSwitch;
		else unitToAttack = opposingUnit.Pokemon;

		foreach (Move move in myUnit.Pokemon.Moves)
		{
			if (move.PP == 0) continue;
			int dmg = unitToAttack.SimulateDamage(move, myUnit.Pokemon) * (move.Base.Accuracy / 100);
			if (dmg > unitToAttack.HP) //Si mata al oponente
            {
				if (move.Base.Priority > 0) //Si le matamos Y tiene prioridad, lo mejor es utilizar este ataque
                {
					killMoves.Clear();
					killMoves.Add(move);
					break;
                }
				else killMoves.Add(move);
            }

			if (move.Base.Category == MoveCategory.Status)
            {
				if (myUnit.Pokemon.HP >= offensiveThreshold * myUnit.Pokemon.MaxHP && move.Base.Effects.Status != ConditionID.rest)
                {
					if (unitToAttack.Status == null && move.Base.Effects.Status != ConditionID.none)
                    {
						statusMoves.Add(move);
                    }
					else if (unitToAttack.VolatileStatus == null && move.Base.Effects.VolatileStatus != ConditionID.none)
					{
						statusMoves.Add(move);
					}
					else if (move.Base.Effects.Boosts.Count > 0)
                    {
						if(move.Base.Effects.Boosts.Where(x => x.boost < 0).Count() > 0) //si es un movimiento de estado que baja estadísticas del oponente
                        {
							int statDrops = 0;
							foreach (int drop in opposingUnit.Pokemon.StatBoosts.Values)
								if (drop < 0) statDrops -= drop;
							if (statDrops < maxStatDrops) statusMoves.Add(move); //lo añadimos solo si el oponente no tiene ya las estadísticas muy bajas
                        }
                        else //si no, es que es un movimiento que sube nuestras propias estadísticas
                        {
							int statBoosts = 0;
							foreach (int boost in myUnit.Pokemon.StatBoosts.Values)
								if (boost > 0) statBoosts += boost;
							if(statBoosts < maxStatBoosts) statusMoves.Add(move); //lo añadimos solo si no tenemos ya las estadísticas muy altas
						}
					}
                }

				else if (myUnit.Pokemon.HP <= defensiveThreshold * myUnit.Pokemon.MaxHP)
                {
					if (move.Base.Effects.Status == ConditionID.rest) statusMoves.Add(move);
                }

			}

			else if (dmg > maxDamage)
            {
				maxDamage = dmg;
				moveChoice = move;
            }
		}

		if (killMoves.Count > 0) return killMoves[Random.Range(0, killMoves.Count)];
		if (statusMoves.Count > 0) return statusMoves[Random.Range(0, statusMoves.Count)];
		if (moveChoice != null) return moveChoice;
		return myUnit.Pokemon.GetRandomMove();
	}

	public override Pokemon decideNextPokemon()
	{
		float maxPoints = 0;
		Pokemon MVP = myParty.GetHealthyPokemon();
		//Recorremos los vivos
		foreach (Pokemon myPokemon in myParty.Pokemons)
		{
			float points = 0;
			//Si el pokemon tiene un ataque ofensivo con ventaja de tipo = 1 pto
			//Si el pokemon tiene un ataque que mata al rival = 2 ptos
			//Si el pokemon tiene un ataque que mata y con prioridad = 99 ptos
			//Si el pokemon tiene resistencia contra uno de los tipos = 1 pto
			//Si el pokemon tiene debilidad contra uno de los tipos = -1 pto
			if (myPokemon.HP > 0)
			{
				float maxMovePoints = 0;
				//Miramos capacidad ofensiva (Recorremos los ataques)
				foreach (Move move in myPokemon.Moves)
				{
					float movePoints = 0;
					if (move.Base.Category != MoveCategory.Status)
					{
						//Ventaja de tipo
						if (opposingUnit.Pokemon.SimulateDamage(move, myPokemon) > opposingUnit.Pokemon.HP) //Uno de nuestros ataques mata al enemigo
						{
							if (move.Base.Priority > 0) //Si el ataque le mata y tiene prioridad
							{
								movePoints = pointsOnKillWithPrio;
							}
							else movePoints = pointsOnKill;
						}

						else
						{
							movePoints = TypeChart.GetEffectiveness(move.Base.Type, opposingUnit.Pokemon.Base.Type1) * TypeChart.GetEffectiveness(move.Base.Type, opposingUnit.Pokemon.Base.Type2) / 2 * pointsOnOffTypeAdvante;
						}
					}
					if (movePoints > maxMovePoints) maxMovePoints = movePoints;
				}
				points += maxMovePoints;

				//Miramos capacidad defensiva (Recorremos las resistencias frente al pokemon)

				//0 -> Inmune
				//0.25
				//0.5
				//1 -> Daño neutro
				//2 -> Un tipo es muy efectivo
				//4 -> Ambos tipos son muy efectivos
				float defPoints = 0;

				float defType1 = TypeChart.GetEffectiveness(opposingUnit.Pokemon.Base.Type1, myUnit.Pokemon.Base.Type1) * TypeChart.GetEffectiveness(opposingUnit.Pokemon.Base.Type1, myUnit.Pokemon.Base.Type2);
				if (defType1 == 0) defPoints += pointsOnDefInmunity;
				else if (defType1 == 0.25 || defType1 == 0.5) defPoints += pointsOnDefAdvantage;
				else if (defType1 == 2 || defType1 == 4) defPoints += pointsOnDefDisadvantage;

				float defType2 = TypeChart.GetEffectiveness(opposingUnit.Pokemon.Base.Type2, myUnit.Pokemon.Base.Type1) * TypeChart.GetEffectiveness(opposingUnit.Pokemon.Base.Type2, myUnit.Pokemon.Base.Type2);
				if (defType2 == 0) defPoints += pointsOnDefInmunity;
				else if (defType2 == 0.25 || defType2 == 0.5) defPoints += pointsOnDefAdvantage;
				else if (defType2 == 2 || defType2 == 4) defPoints += pointsOnDefDisadvantage;

				points += defPoints;

				if (points > maxPoints)
				{
					maxPoints = points;
					MVP = myPokemon;
				}
			}
		}

		return MVP;
	}

	public override void onEnemySwitch(Pokemon pokemon)
    {
		lastSwitch = pokemon;
		switchCounter++;
    }

	public override void onEnemyAttack()
    {
		lastSwitch = null;
		switchCounter = 0;
	}
}
