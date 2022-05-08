/*
 * Esta clase contiene la información de cada entrenador. Su nombre, su sprite, y en caso de que no sea el jugador, la IA que utilizará.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Trainer : MonoBehaviour
{
    [SerializeField] Sprite trainerSprite;
    [SerializeField] string trainerName;
    [SerializeField] PokemonIA trainerAI;

    public Sprite TrainerSprite {
        get { return trainerSprite; }
    }

    public string TrainerName
    {
        get { return trainerName; }
    }

    public PokemonIA TrainerAI
    {
        get { return trainerAI; }
    }

    public PokemonParty TrainerParty
    {
        get { return gameObject.GetComponent<PokemonParty>(); }
    }
}
