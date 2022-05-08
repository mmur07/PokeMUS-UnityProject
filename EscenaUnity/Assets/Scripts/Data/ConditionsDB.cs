/*
 * Esta clase implementa una base de datos que contiene la informción de cada estado alterado que puede sufrir un pokémon: el texto que se
 * mostrará cuando alguien sea afectado, sus efectos (implementados como funciones lambda) y su duración (en caso de que el estado sea volátil)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionsDB
{
	public static void Init()
	{
		foreach(var kvp in Conditions)
		{
			var conditionId = kvp.Key;
			var condition = kvp.Value;

			condition.Id = conditionId;
		}
	}

public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
	{
		{ConditionID.psn,
			new Condition()
			{
				Name= "Veneno",
				StartMessage="ha sido envenenado",
				OnAfterTurn = (Pokemon pokemon) =>
				{
					pokemon.UpdateHP(pokemon.MaxHP / 8);
					pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} se ha hecho daño por el veneno.");
				}
			} 
		},
		{ConditionID.brn,
			new Condition()
			{
				Name= "Quemadura",
				StartMessage="se ha quemado",
				OnAfterTurn = (Pokemon pokemon) =>
				{
					pokemon.UpdateHP(pokemon.MaxHP / 16);
					pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} se ha hecho daño por la quemadura.");
				}
			}
		},
		{ConditionID.par,
			new Condition()
			{
				Name= "Paralizado",
				StartMessage="ha sido paralizado",
				OnBeforeMove = (Pokemon pokemon) =>
				{
					if (Random.Range(1, 5) == 1){
						pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} está paralizado y no puede moverse.");
						return false;
					}

					return true;
				}
			}
		},
		{ConditionID.frz,
			new Condition()
			{
				Name= "Congelado",
					StartMessage="ha sido congelado",
				OnBeforeMove = (Pokemon pokemon) =>
				{
					if (Random.Range(1, 5) == 1){
						pokemon.CureStatus();
						pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} ya no está congelado.");
						return true;
					}
					pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} está congelado y no puede moverse.");
					return false;
				}
			}
		},
		{ConditionID.slp,
			new Condition()
			{
				Name= "Dormir",
				StartMessage="se ha dormido",
				OnStart = (Pokemon pokemon) =>
				{
					pokemon.StatusTime = Random.Range(1, 4);
					Debug.Log($"Will be asleep for {pokemon.StatusTime} moves");
				},
				OnBeforeMove = (Pokemon pokemon) =>
				{
					if (pokemon.StatusTime <= 0)
					{
						pokemon.CureStatus();
						pokemon.StatusChanges.Enqueue($"¡{pokemon.Base.Name} se ha despertado!");
						return true;
					}

					pokemon.StatusTime--;
					pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} está dormido");
					return false;
				}
			}
		},
		{ConditionID.rest,
			new Condition()
			{
				Name= "Descanso",
				StartMessage="se ha dormido y se ha curado por completo.",
				OnStart = (Pokemon pokemon) =>
				{
					pokemon.StatusTime = 2;
					pokemon.HealToFull();
					Debug.Log($"Will be asleep for {pokemon.StatusTime} moves");
				},
				OnBeforeMove = (Pokemon pokemon) =>
				{
					if (pokemon.StatusTime <= 0)
					{
						pokemon.CureStatus();
						pokemon.StatusChanges.Enqueue($"¡{pokemon.Base.Name} se ha despertado!");
						return true;
					}

					pokemon.StatusTime--;
					pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} está dormido");
					return false;
				}
			}
		},
		{ConditionID.tox,
			new Condition()
			{
				Name= "Tóxico",
				StartMessage="se ha envenenado muy fuerte.",
				OnStart = (Pokemon pokemon) =>
				{
					pokemon.StatusTime = 1;
				},
				OnAfterTurn = (Pokemon pokemon) =>
				{
					pokemon.UpdateHP(Mathf.FloorToInt((pokemon.StatusTime * 6.25f * pokemon.MaxHP) / 100.0f));
					pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} se ha hecho daño por el veneno tóxico.");
					pokemon.StatusTime++;
				}
			}
		},
		//Volatile Status Conditions
		{ConditionID.confusion,
			new Condition()
			{
				Name= "Confusión",
				StartMessage="está confuso.",
				OnStart = (Pokemon pokemon) =>
				{
					//Confused for 1-4 turns
					pokemon.VolatileStatusTime = Random.Range(1, 5);
					Debug.Log($"Will be confused for {pokemon.VolatileStatusTime} moves");
				},
				OnBeforeMove = (Pokemon pokemon) =>
				{
					if (pokemon.VolatileStatusTime <= 0)
					{
						pokemon.CureVolatileStatus();
						pokemon.StatusChanges.Enqueue($"¡{pokemon.Base.Name} ya no está confuso!");
						return true;
					}
					pokemon.VolatileStatusTime--;
					pokemon.StatusChanges.Enqueue($"¡{pokemon.Base.Name} está confuso!");

					//50% chance of taking damage
					if(Random.Range(1,3) == 1) return true;

					//Hurt by confusion
					float damage = 40 * (float)pokemon.Attack/pokemon.Defense;
					pokemon.UpdateHP((int)damage);
					pokemon.StatusChanges.Enqueue("¡Está tan confuso que se ha herido a sí mismo!");
					return false;
				}
			}
		},
	};

}
public enum ConditionID
{
	none, psn, brn, slp, par, frz, rest, tox,
	confusion
}