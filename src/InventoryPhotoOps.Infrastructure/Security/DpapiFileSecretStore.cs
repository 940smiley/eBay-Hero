#pragma warning disable CA1416
using System.Security.Cryptography;
using System.Text;
using InventoryPhotoOps.Core.Configuration;
using InventoryPhotoOps.Core.Services;

namespace InventoryPhotoOps.Infrastructure.Security;

public sealed class DpapiFileSecretStore(InventoryOptions options) : ISecretStore
{
    private readonly string _root = Path.Combine(options.OperationsRoot, "secrets");

    public async Task SaveAsync(SecretReference reference, string secretValue, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_root);
        var path = GetPath(reference);
        var clearBytes = Encoding.UTF8.GetBytes(secretValue);
        var protectedBytes = ProtectedData.Protect(clearBytes, GetEntropy(reference), DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(path, protectedBytes, cancellationToken);
    }

    public async Task<string?> ReadAsync(SecretReference reference, CancellationToken cancellationToken)
    {
        var path = GetPath(reference);
        if (!File.Exists(path))
        {
            return null;
        }

        var protectedBytes = await File.ReadAllBytesAsync(path, cancellationToken);
        var clearBytes = ProtectedData.Unprotect(protectedBytes, GetEntropy(reference), DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(clearBytes);
    }

    public Task DeleteAsync(SecretReference reference, CancellationToken cancellationToken)
    {
        var path = GetPath(reference);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetPath(SecretReference reference)
    {
        var fileName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(reference.Scope + ":" + reference.Name))) + ".bin";
        return Path.Combine(_root, fileName);
    }

    private static byte[] GetEntropy(SecretReference reference) =>
        SHA256.HashData(Encoding.UTF8.GetBytes("InventoryPhotoOps:" + reference.Scope + ":" + reference.Name));
}
