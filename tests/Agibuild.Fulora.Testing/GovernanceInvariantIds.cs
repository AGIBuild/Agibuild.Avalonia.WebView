namespace Agibuild.Fulora.Testing;

/// <summary>
/// Stable identifiers for governance invariants. Referenced in governance test diagnostics
/// and CI evidence artifacts for deterministic failure triage.
/// </summary>
public static class GovernanceInvariantIds
{
    public const string AutomationLaneManifestSchema = "GOV-001";
    public const string RuntimeCriticalPathScenarioPresence = "GOV-002";
    public const string RuntimeCriticalPathEvidenceLinkage = "GOV-003";
    public const string SystemIntegrationCtMatrixSchema = "GOV-004";
    public const string WarningGovernanceBaseline = "GOV-005";
    public const string ShellProductionMatrixSchema = "GOV-006";
    public const string ShellManifestMatrixSync = "GOV-007";
    public const string BenchmarkBaselineSchema = "GOV-008";
    public const string BuildPipelineTargetGraph = "GOV-009";
    public const string PackageMetadata = "GOV-010";
    public const string XunitVersionAlignment = "GOV-011";
    public const string TemplateMetadataSchema = "GOV-012";
    public const string BridgeDxAssets = "GOV-013";
    public const string WebView2ReferenceModel = "GOV-014";
    public const string CoverageThreshold = "GOV-015";
    public const string ReadmeQualitySignals = "GOV-016";
    public const string WindowsBaseConflictGovernance = "GOV-017";
    public const string CiTargetOpenSpecGate = "GOV-018";
    public const string PhaseCloseoutConsistency = "GOV-019";
    public const string EvidenceContractV2Schema = "GOV-020";
    public const string BridgeDistributionParity = "GOV-021";
    public const string PhaseTransitionConsistency = "GOV-022";
}
