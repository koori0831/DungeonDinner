using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace Work.Cook.Code.Info
{
    public class InfoEnum { }

    public enum TribeEnum
    {
        Human,
        Elf,
        Barbarian,
        Dwarf,
        Furry,
        Fishman,
        None = -1,
    }

    public enum CountryEnum
    {
        Republic_of_Bellator,
        St_Aurelia_Empire,
        Lumen_Elf_Autonomous_Forest,
        Grana_Union,
        Kardum_Underground_Republic,
        Neria_Underwater_Kingdom,
        None = -1,
    }

    public enum MarkerEnum
    {
        Country,
        Tribe,
        Ingredient,
        Monster,
        Recipe,

    }

    [Flags]
    public enum ViewHaveInfoEnum
    {
        Image = 1 << 0,
        Name = 1 << 1,
        Description = 1 << 2,
        Index = 1 << 3,
    }
}
