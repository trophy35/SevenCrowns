using SevenCrowns.Map.Resources;

namespace SevenCrowns.UI.Cities
{
    /// <summary>
    /// UI-facing provider to expose the authoritative city wallet.
    /// Implement in Core (e.g., CityHudInitializer) so UI can bind consistently
    /// when multiple IResourceWallet instances exist (world vs city).
    /// </summary>
    public interface ICityWalletProvider
    {
        bool TryGetWallet(out IResourceWallet wallet);
    }
}

