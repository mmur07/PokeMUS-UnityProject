/*
 * Esta clase implementa a cada Pokémon del juego en combate. Contiene toda la información del pokémon así como su salud actual, 
 * sus estados alterados, y métodos para interactuar con el medio durante la batalla (atacar, hacerse daño, etc).
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Pokemon
{
	[SerializeField] PokemonBase _base;
	[SerializeField] int level;

	public PokemonBase Base
	{
		get
		{
			return _base;
		}
	}

	public int Level
	{
		get
		{
			return level;
		}
	}

	public int HP { get; set; }
	public List<Move> Moves { get; set; }
	public Move CurrentMove { get; set; }
	public Dictionary<Stat, int> Stats { get; private set; }
	public Dictionary<Stat, int> StatBoosts { get; private set; }

	public Condition Status { get; private set; }
	public int StatusTime { get; set; }
	public Condition VolatileStatus { get; private set; }
	public int VolatileStatusTime { get; set; }

	public Queue<string> StatusChanges { get; private set; } = new Queue<string>();
	public bool HpChanged { get; set; }
	public event System.Action OnStatusChanged;

	public void Init()
	{
		//Generate moves
		Moves = new List<Move>();
		foreach (var move in Base.KnownMoves)
		{
			Moves.Add(new Move(move));
		}

		CalculateStats();
		HP = MaxHP;

		ResetStatBoosts();
		Status = null;
		VolatileStatus = null;
	}

	void CalculateStats()
	{
		Stats = new Dictionary<Stat, int>();
		Stats.Add(Stat.Attack, Mathf.FloorToInt((2 * Base.Attack * Level) / 100f) + 5);
		Stats.Add(Stat.Defense, Mathf.FloorToInt((2 * Base.Defense * Level) / 100f) + 5);
		Stats.Add(Stat.SpAttack, Mathf.FloorToInt((2 * Base.SpAttack * Level) / 100f) + 5);
		Stats.Add(Stat.SpDefense, Mathf.FloorToInt((2 * Base.SpDefense * Level) / 100f) + 5);
		Stats.Add(Stat.Speed, Mathf.FloorToInt((2 * Base.Speed * Level) / 100f) + 5);

		MaxHP = Mathf.FloorToInt((2 * Base.MaxHP * Level) / 100f) + Level + 10;
	}

	public void ResetStatBoosts()
	{
		StatBoosts = new Dictionary<Stat, int>()
		{
			{Stat.Attack, 0},
			{Stat.Defense, 0},
			{Stat.SpAttack, 0},
			{Stat.SpDefense, 0},
			{Stat.Speed, 0},
			{Stat.Accuracy, 0},
			{Stat.Evasion, 0}
		};
	}

	int GetStat(Stat stat)
	{
		int statVal = Stats[stat];

		int boost = StatBoosts[stat];
		var boostValues = new float[] { 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f };

		if (boost >= 0)
			statVal = Mathf.FloorToInt(statVal * boostValues[boost]);
		else
			statVal = Mathf.FloorToInt(statVal / boostValues[Mathf.Abs(boost)]);

		return statVal;
	}

	public void ApplyBoosts(List<StatBoost> statBoosts)
	{
		foreach (var statBoost in statBoosts)
		{
			var stat = statBoost.stat;
			var boost = statBoost.boost;

			StatBoosts[stat] = Mathf.Clamp(StatBoosts[stat] + boost, -6, 6);

			if (boost > 0)
			{
				StatusChanges.Enqueue($"¡La estadística {stat} de {Base.Name} ha aumentado!");
			}
			else
			{
				StatusChanges.Enqueue($"¡La estadística {stat} de {Base.Name} ha disminuido!");
			}

			Debug.Log(stat + " has been bosted to " + StatBoosts[stat]);
		}
	}

	public int MaxHP
	{
		get; private set;
	}

	public int Attack
	{
		get { return GetStat(Stat.Attack); }
	}

	public int Defense
	{
		get { return GetStat(Stat.Defense); }
	}

	public int SpAttack
	{
		get { return GetStat(Stat.SpAttack); }
	}

	public int SpDefense
	{
		get { return GetStat(Stat.SpDefense); }
	}

	public int Speed
	{
		get { return GetStat(Stat.Speed); }
	}

	public DamageDetails TakeDamage(Move move, Pokemon attacker)
	{
		float critical = 1f;
		if (Random.value * 100f <= 6.25f)
			critical = 2f;

		float type = TypeChart.GetEffectiveness(move.Base.Type, this.Base.Type1) * TypeChart.GetEffectiveness(move.Base.Type, this.Base.Type2);

		float stab = 1f;
		if (move.Base.Type == this.Base.Type1 || move.Base.Type == this.Base.Type2)
			stab = 1.5f;

		var damageDetails = new DamageDetails()
		{
			TypeEffectiveness = type,
			Critical = critical,
			Fainted = false
		};

		float attack = (move.Base.Category == MoveCategory.Special) ? attacker.SpAttack : attacker.Attack;
		float defense = (move.Base.Category == MoveCategory.Special) ? SpDefense : Defense;

		if (attack == attacker.Attack) attack = (attacker.Status?.Id == ConditionID.brn) ? attack / 2 : attack;

		float modifiers = Random.Range(0.85f, 1f) * type * critical * stab;
		float a = (2 * attacker.Level + 10) / 250f;
		float d = a * move.Base.Power * ((float)attack / defense) + 2;
		int damage = Mathf.FloorToInt(d * modifiers);

		UpdateHP(damage);

		return damageDetails;
	}

	//Esto lo usamos para calcular el daño aproximado que hará un ataque para la IA buena
	public int SimulateDamage(Move move, Pokemon attacker)
	{
		float type = TypeChart.GetEffectiveness(move.Base.Type, this.Base.Type1) * TypeChart.GetEffectiveness(move.Base.Type, this.Base.Type2);

		float stab = 1f;
		if (move.Base.Type == this.Base.Type1 || move.Base.Type == this.Base.Type2)
			stab = 1.5f;

		float attack = (move.Base.Category == MoveCategory.Special) ? attacker.SpAttack : attacker.Attack;
		float defense = (move.Base.Category == MoveCategory.Special) ? SpDefense : Defense;

		if (attack == attacker.Attack) attack = (attacker.Status?.Id == ConditionID.brn) ? attack / 2 : attack;

		float modifiers = 0.9f * type * stab;
		float a = (2 * attacker.Level + 10) / 250f;
		float d = a * move.Base.Power * ((float)attack / defense) + 2;
		int damage = Mathf.FloorToInt(d * modifiers);

		return damage;
	}


	public void UpdateHP(int damage)
	{
		HP = Mathf.Clamp(HP - damage, 0, MaxHP);
		HpChanged = true;
	}

	public void HealHP(int heal)
    {
		int preHealHp = HP;
		HP = Mathf.Clamp(HP + heal, 0, MaxHP);
		HpChanged = (preHealHp != HP);
	}

	public void HealToFull()
    {
		int preHealHp = HP;
		HP = MaxHP;
		HpChanged = (preHealHp != HP);
	}

	public void SetStatus(ConditionID conditionId)
	{
		if (Status != null) return;

		Status = ConditionsDB.Conditions[conditionId];
		Status?.OnStart?.Invoke(this);
		StatusChanges.Enqueue($"{Base.Name} {Status.StartMessage}");
		OnStatusChanged?.Invoke();
	}

	public void SetVolatileStatus(ConditionID conditionId)
	{
		if (VolatileStatus != null) return;

		VolatileStatus = ConditionsDB.Conditions[conditionId];
		VolatileStatus?.OnStart?.Invoke(this);
		StatusChanges.Enqueue($"{Base.Name} {VolatileStatus.StartMessage}");
	}

	public Move GetRandomMove()
	{
		var movesWithPP = Moves.Where(x => x.PP > 0).ToList();

		int r = Random.Range(0, movesWithPP.Count);
		return movesWithPP[r];
	}

	public bool OnBeforeMove()
	{
		bool canPerformMove = true;
		if (Status?.OnBeforeMove != null)
		{
			if (!Status.OnBeforeMove(this))
				canPerformMove = false;
		}

		if (VolatileStatus?.OnBeforeMove != null)
		{
			if (!VolatileStatus.OnBeforeMove(this))
				canPerformMove = false;
		}

		return canPerformMove;
	}

	public void OnAfterTurn()
	{
		Status?.OnAfterTurn?.Invoke(this);
		VolatileStatus?.OnAfterTurn?.Invoke(this);
	}

	public void CureStatus()
	{
		Status = null;
		OnStatusChanged?.Invoke();
	}

	public void CureVolatileStatus()
	{
		VolatileStatus = null;
	}
}

public class DamageDetails
{
	public bool Fainted { get; set; }
	public float Critical { get; set; }
	public float TypeEffectiveness { get; set; }
}
