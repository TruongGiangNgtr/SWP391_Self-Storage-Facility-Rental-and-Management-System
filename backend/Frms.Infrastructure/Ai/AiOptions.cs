namespace Frms.Infrastructure.Ai;

// Deployment-only knobs. Not exposed in the API or persisted in FRMS.
public sealed class AiOptions
{
    public const string SectionName = "Ai";
    public string? Provider { get; set; }
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public int TimeoutSeconds { get; set; } = 15;
}
