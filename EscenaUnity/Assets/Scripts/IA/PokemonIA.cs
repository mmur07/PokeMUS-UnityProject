/*
 * Esta clase implementa la IA 'tonta' para las batallas pokémon. Su funcionamiento está explicado en la documentación del proyecto.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokemonIA : MonoBehaviour
{
    protected PokemonParty myParty;
    protected PokemonParty opposingParty;
    protected BattleUnit myUnit;
    protected BattleUnit opposingUnit;

    public virtual void Init(PokemonParty mp, PokemonParty op, BattleUnit mu, BattleUnit ou)
    {
        myParty = mp;
        opposingParty = op;
        myUnit = mu;
        opposingUnit = ou;
    }

    public virtual Pokemon decideSwitch()
    {  
        return null;
    }

    public virtual ItemID decideItemUse()
    {
        return ItemID.NULL;
    }

    public virtual Move decideNextMove()
    {
        return myUnit.Pokemon.GetRandomMove();
    }

    public virtual Pokemon decideNextPokemon()
    {
        return myParty.GetHealthyPokemon();
    }

    public virtual void onEnemySwitch(Pokemon pokemon)
    {

    }

    public virtual void onEnemyAttack()
    {

    }
}
