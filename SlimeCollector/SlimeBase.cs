using UnityEngine;

public class SlimeBase : MonoBehaviour
{
    public static SlimeBase Instance;

    public enum Types
    {
        WhiteSlime,
        RedSlime,
        OrangeSlime,
        YellowSlime,
        GreenSlime,
        BlueSlime,
        sodomySlime,
        PurpleSlime,
        ClearSlime,

        KingSlime,
        RockSlime,
        GhostSlime,
        PoopSlime,
        TiredSlime,
        GoldSlime,
        SadSlime,
        AngrySlime,
        BabySlime,

        StupidSlime,
        DogSlime,
        CatSlime,
        rabbitSlime,
        hamsterSlime,
        chickSlime,
        FoxSlime,
        PigSlime,
        DragonSlime,
    }
    public Types Slimetype;

    private void Awake()
    {
        Instance = this;
    }
}
