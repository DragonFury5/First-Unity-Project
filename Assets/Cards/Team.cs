/// <summary>
/// Central definition of team/ownership IDs.
/// Use these constants everywhere instead of raw 0/1/2.
///   0 = Neutral (unowned / not yet spawned)
///   1 = Player
///   2 = Enemy
/// </summary>
public static class Team
{
    public const int Neutral = 0;
    public const int Player  = 1;
    public const int Enemy   = 2;
}