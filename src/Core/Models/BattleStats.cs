namespace SuperDeck.Core.Models;

public class BattleStats
{
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }

    public void ResetFrom(Character character)
    {
        Attack = character.Attack;
        Defense = character.Defense;
        Speed = character.Speed;
    }

    public BattleStats Clone()
    {
        return new BattleStats
        {
            Attack = Attack,
            Defense = Defense,
            Speed = Speed
        };
    }
}
