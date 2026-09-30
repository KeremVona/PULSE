using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Exceptions;

namespace HeartCDSS.Domain.Entities;

/// <summary>
/// Domain entity representing a registered version of the machine learning model deployed to the serving runtime.
/// </summary>
public sealed class MlModel : Entity<Guid>
{
    public string ModelName { get; private set; } = string.Empty;
    public string ModelVersion { get; private set; } = string.Empty;
    public string ModelFamily { get; private set; } = string.Empty;
    public string CalibrationMethod { get; private set; } = string.Empty;
    public decimal DecisionThreshold { get; private set; }
    public decimal TargetSensitivity { get; private set; }
    public decimal? BrierScore { get; private set; }
    public decimal? RocAuc { get; private set; }
    public decimal? PrAuc { get; private set; }
    public string? ArtifactHash { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset DeployedAt { get; private set; }

    private MlModel() { }

    private MlModel(
        Guid id,
        string modelName,
        string modelVersion,
        string modelFamily,
        string calibrationMethod,
        decimal decisionThreshold,
        decimal targetSensitivity,
        decimal? brierScore,
        decimal? rocAuc,
        decimal? prAuc,
        string? artifactHash,
        bool isActive,
        DateTimeOffset deployedAt)
        : base(id)
    {
        ModelName = modelName;
        ModelVersion = modelVersion;
        ModelFamily = modelFamily;
        CalibrationMethod = calibrationMethod;
        DecisionThreshold = decimal.Round(decisionThreshold, 4);
        TargetSensitivity = decimal.Round(targetSensitivity, 4);
        BrierScore = brierScore.HasValue ? decimal.Round(brierScore.Value, 4) : null;
        RocAuc = rocAuc.HasValue ? decimal.Round(rocAuc.Value, 4) : null;
        PrAuc = prAuc.HasValue ? decimal.Round(prAuc.Value, 4) : null;
        ArtifactHash = artifactHash;
        IsActive = isActive;
        DeployedAt = deployedAt;
    }

    public static MlModel Make(
        string modelName,
        string modelVersion,
        string modelFamily,
        string calibrationMethod,
        decimal decisionThreshold,
        decimal targetSensitivity,
        decimal? brierScore = null,
        decimal? rocAuc = null,
        decimal? prAuc = null,
        string? artifactHash = null,
        bool isActive = true,
        DateTimeOffset? deployedAt = null,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            throw new DomainException("Model name cannot be empty.");

        if (string.IsNullOrWhiteSpace(modelVersion))
            throw new DomainException("Model version cannot be empty.");

        return new MlModel(
            id ?? Guid.NewGuid(),
            modelName.Trim(),
            modelVersion.Trim(),
            modelFamily.Trim(),
            calibrationMethod.Trim(),
            decisionThreshold,
            targetSensitivity,
            brierScore,
            rocAuc,
            prAuc,
            artifactHash?.Trim(),
            isActive,
            deployedAt ?? DateTimeOffset.UtcNow
        );
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }
}
