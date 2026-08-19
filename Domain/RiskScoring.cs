namespace MIC.risk.Domain;

/// <summary>
/// The single definition of how risk is scored. Every threshold, band and formula in the
/// application derives from here — including the SQL of the computed columns, which is kept
/// in step with <see cref="InherentRiskSql"/> and <see cref="ResidualRiskSql"/>.
/// The frontend mirrors these values; change them together.
/// </summary>
public static class RiskScoring
{
    public const int MinRating = 1;
    public const int MaxRating = 5;

    /// <summary>Severity x Frequency, so 1 through 25.</summary>
    public static int InherentRisk(int severity, int frequency) => severity * frequency;

    /// <summary>
    /// Inherent risk carried through the control rating, so 1 through 125.
    /// Control effectiveness runs 1 = very strong to 5 = very weak, which means the rating acts
    /// as a multiplier on exposure rather than a discount: strong controls (1) leave residual
    /// equal to inherent, and weak controls (5) multiply the exposure fivefold.
    /// Residual is therefore always greater than or equal to inherent, never less.
    /// </summary>
    public static int ResidualRisk(int severity, int frequency, int controlEffectiveness) =>
        InherentRisk(severity, frequency) * controlEffectiveness;

    /// <summary>A control rating at or above this counts as a weak control.</summary>
    public const int WeakControlThreshold = 4;

    /// <summary>
    /// Bands apply to both scores on the same thresholds. That works because a perfectly
    /// controlled risk (rating 1) has residual equal to inherent and so lands in the same band;
    /// weaker controls escalate it. Inherent tops out at 25, residual at 125.
    /// </summary>
    public static RiskBand Band(int score) => score switch
    {
        <= 5 => RiskBand.Low,
        <= 10 => RiskBand.Moderate,
        <= 15 => RiskBand.High,
        _ => RiskBand.Critical
    };

    /// <summary>Lowest score that counts as critical, for the early-warning panel.</summary>
    public const int CriticalBandFloor = 16;

    // Mirrors of the two expressions above, for the persisted computed columns.
    public const string InherentRiskSql = "[Severity] * [Frequency]";
    public const string ResidualRiskSql = "[Severity] * [Frequency] * [ControlEffectiveness]";
}

public enum RiskBand
{
    Low,
    Moderate,
    High,
    Critical
}
