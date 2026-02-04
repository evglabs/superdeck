namespace SuperDeck.Core.Settings;

public class GameSettings
{
    public CharacterSettings Character { get; set; } = new();
    public BattleSettings Battle { get; set; } = new();
    public XPSettings XP { get; set; } = new();
    public MMRSettings MMR { get; set; } = new();
    public CardPackSettings CardPack { get; set; } = new();
    public RarityWeightSettings RarityWeights { get; set; } = new();
    public Dictionary<string, double> SuitWeights { get; set; } = new();
    public AuthSettings Auth { get; set; } = new();
    public RateLimitSettings RateLimit { get; set; } = new();
    public AutoBattleSettings AutoBattle { get; set; } = new();
}

public class RateLimitSettings
{
    public bool Enabled { get; set; } = false;
    public int GlobalPermitLimit { get; set; } = 100;
    public int GlobalWindowSeconds { get; set; } = 60;
    public AuthRateLimitSettings Auth { get; set; } = new();
    public BattleRateLimitSettings Battle { get; set; } = new();
}

public class AuthRateLimitSettings
{
    public int PermitLimit { get; set; } = 5;
    public int WindowSeconds { get; set; } = 60;
}

public class BattleRateLimitSettings
{
    public int TokenLimit { get; set; } = 30;
    public int TokensPerPeriod { get; set; } = 10;
    public int ReplenishmentPeriodSeconds { get; set; } = 10;
}

public class CharacterSettings
{
    public int BaseHP { get; set; } = 100;
    public int HPPerLevel { get; set; } = 10;
    public int StartingAttack { get; set; } = 0;
    public int StartingDefense { get; set; } = 0;
    public int StartingSpeed { get; set; } = 0;
    public int StatPointsPerLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 10;
    public int MinSpeed { get; set; } = 0;
    public int AttackPerStatPoint { get; set; } = 1;
    public int DefensePerStatPoint { get; set; } = 1;
    public int SpeedPerStatPoint { get; set; } = 1;
    public int HPPerStatPoint { get; set; } = 5;
}

public class BattleSettings
{
    public int BaseQueueSlots { get; set; } = 3;
    public int MaxQueueSlots { get; set; } = 5;
    public int StartingHandSize { get; set; } = 5;
    public int CardsDrawnPerTurn { get; set; } = 3;
    public int MinDeckSize { get; set; } = 9;
    public int SystemDamageStartRound { get; set; } = 10;
    public double SystemDamageBase { get; set; } = 1.5;
    public int SystemDamagePerRound { get; set; } = 0;
    public int DefaultOpponentDeckSize { get; set; } = 9;
    public int GhostSearchRange { get; set; } = 200;
    public int GhostCandidateCount { get; set; } = 10;
}

public class XPSettings
{
    public int BaseXPForLevelUp { get; set; } = 50;
    public int XPIncreasePerLevel { get; set; } = 25;
    public int XPForWin { get; set; } = 50;
    public int XPForLoss { get; set; } = 25;
}

public class MMRSettings
{
    public int StartingMMR { get; set; } = 1000;
    public int MMRGainOnWin { get; set; } = 25;
    public int MMRLossOnLoss { get; set; } = 25;
    public int MinimumMMR { get; set; } = 100;
    public bool UseEloFormula { get; set; } = true;
    public EloSettings Elo { get; set; } = new();
}

public class EloSettings
{
    public int KFactor { get; set; } = 30;
    public double DivisorConstant { get; set; } = 400.0;
}

public class CardPackSettings
{
    public int BoosterPackSize { get; set; } = 10;
    public int StarterPackSize { get; set; } = 10;
    public int StarterDeckPunchCount { get; set; } = 3;
    public int StarterDeckBlockCount { get; set; } = 3;
    public int RarityRollMax { get; set; } = 1000;
    public double SuitBonusPerOwnedCard { get; set; } = 2.0;
}

public class RarityWeightSettings
{
    // Booster pack rarity thresholds (cumulative out of 1000)
    public int CommonThreshold { get; set; } = 600;      // 0-599 = 60%
    public int UncommonThreshold { get; set; } = 900;    // 600-899 = 30%
    public int RareThreshold { get; set; } = 990;        // 900-989 = 9%
    // 990-999 = 1% Legendary

    // Starter pack weighted selection
    public int StarterCommonWeight { get; set; } = 50;
    public int StarterUncommonWeight { get; set; } = 30;
    public int StarterRareWeight { get; set; } = 15;
    public int StarterEpicWeight { get; set; } = 4;
    public int StarterLegendaryWeight { get; set; } = 1;
}

public class AuthSettings
{
    public int UsernameMinLength { get; set; } = 3;
    public int UsernameMaxLength { get; set; } = 20;
    public int PasswordMinLength { get; set; } = 6;
    public int SessionTimeoutHours { get; set; } = 24;
    public int SaltSizeBytes { get; set; } = 32;
    public int TokenSizeBytes { get; set; } = 32;
    public bool UseJwt { get; set; } = false;
    public JwtSettings Jwt { get; set; } = new();
}

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "SuperDeck";
    public string Audience { get; set; } = "SuperDeck";
    public int ExpirationMinutes { get; set; } = 1440; // 24 hours
}

public class AutoBattleSettings
{
    public bool Enabled { get; set; } = true;
    public int WatchModeDelayMs { get; set; } = 500;
    public bool AllowMidBattleToggle { get; set; } = true;
}
