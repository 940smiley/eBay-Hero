using System.Text.Json;

namespace eBayHero.Core.Licensing;

public enum ProductEdition
{
    Public,
    Development
}

public sealed record ProductAccessSnapshot(
    ProductEdition Edition,
    bool HasPaidEntitlement,
    DateTimeOffset TrialStartedUtc,
    DateTimeOffset TrialEndsUtc,
    int PremiumActionsUsed,
    int PremiumActionLimit)
{
    public bool IsDeveloper => Edition == ProductEdition.Development;
    public bool IsTrialActive => DateTimeOffset.UtcNow < TrialEndsUtc && PremiumActionsUsed < PremiumActionLimit;
    public int PremiumActionsRemaining => Math.Max(0, PremiumActionLimit - PremiumActionsUsed);
}

public interface IProductAccessService
{
    ProductAccessSnapshot Current { get; }
    bool TryConsumePremiumAction(out string message);
}

public sealed class ProductAccessService : IProductAccessService
{
    public const int TrialDays = 14;
    public const int TrialPremiumActions = 25;

    private readonly string _statePath;
    private readonly ProductEdition _edition;
    private readonly Func<DateTimeOffset> _clock;
    private AccessState _state;

    public ProductAccessService(string operationsRoot, ProductEdition edition, Func<DateTimeOffset>? clock = null)
    {
        _statePath = Path.Combine(operationsRoot, "license", "access-state.json");
        _edition = edition;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _state = LoadOrCreate();
    }

    public ProductAccessSnapshot Current => new(
        _edition, _state.HasPaidEntitlement, _state.TrialStartedUtc,
        _state.TrialStartedUtc.AddDays(TrialDays), _state.PremiumActionsUsed, TrialPremiumActions);

    public bool TryConsumePremiumAction(out string message)
    {
        if (_edition == ProductEdition.Development || _state.HasPaidEntitlement)
        {
            message = "Premium access enabled.";
            return true;
        }

        if (!Current.IsTrialActive)
        {
            message = "Your free trial has ended. Upgrade to continue using OCR, pricing, and exports.";
            return false;
        }

        _state.PremiumActionsUsed++;
        Save();
        message = $"{Current.PremiumActionsRemaining} premium trial actions remain.";
        return true;
    }

    private AccessState LoadOrCreate()
    {
        try
        {
            if (File.Exists(_statePath))
            {
                var loaded = JsonSerializer.Deserialize<AccessState>(File.ReadAllText(_statePath));
                if (loaded is not null && loaded.TrialStartedUtc != default) return loaded;
            }
        }
        catch (JsonException) { }

        var created = new AccessState { TrialStartedUtc = _clock() };
        _state = created;
        Save();
        return created;
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
        var temporaryPath = _statePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, _statePath, true);
    }

    private sealed class AccessState
    {
        public DateTimeOffset TrialStartedUtc { get; set; }
        public int PremiumActionsUsed { get; set; }
        public bool HasPaidEntitlement { get; set; }
    }
}
