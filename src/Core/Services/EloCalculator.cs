using SuperDeck.Core.Settings;

namespace SuperDeck.Core.Services;

public class EloCalculator
{
    private readonly int _kFactor;
    private readonly double _divisorConstant;

    public EloCalculator(EloSettings settings)
    {
        _kFactor = settings.KFactor;
        _divisorConstant = settings.DivisorConstant;
    }

    public EloCalculator(int kFactor = 30, double divisorConstant = 400.0)
    {
        _kFactor = kFactor;
        _divisorConstant = divisorConstant;
    }

    public double CalculateExpectedResult(int playerMMR, int opponentMMR)
    {
        return 1.0 / (1.0 + Math.Pow(10, (opponentMMR - playerMMR) / _divisorConstant));
    }

    public int CalculateMMRChange(int playerMMR, int opponentMMR, bool won)
    {
        double expected = CalculateExpectedResult(playerMMR, opponentMMR);
        double actual = won ? 1.0 : 0.0;
        return (int)Math.Round(_kFactor * (actual - expected));
    }

    public (int playerChange, int opponentChange) CalculateBothMMRChanges(
        int playerMMR, int opponentMMR, bool playerWon)
    {
        int playerChange = CalculateMMRChange(playerMMR, opponentMMR, playerWon);
        int opponentChange = CalculateMMRChange(opponentMMR, playerMMR, !playerWon);
        return (playerChange, opponentChange);
    }
}
