using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ScreenMind.Core.Abstractions;

namespace ScreenMind.Platform.Windows.Security;

/// <summary>
/// Stores API keys and tokens as generic credentials in Windows Credential Manager.
/// Returned secrets are immutable strings, so callers should keep their lifetime as short as possible.
/// </summary>
public sealed class WindowsCredentialSecretStore : ISecretStore
{
    private const int _errorNotFound = 1168;
    private const uint _credentialTypeGeneric = 1;
    private const uint _credentialPersistLocalMachine = 2;
    private const int _maxKeyLength = 128;
    private const int _maxSecretBytes = 2560;
    private const string _targetPrefix = "ScreenMind:";

    public Task SetSecretAsync(string key, string value, CancellationToken cancellationToken)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);
        EnsureWindows();
        cancellationToken.ThrowIfCancellationRequested();

        var secretByteCount = Encoding.Unicode.GetByteCount(value);
        if (secretByteCount > _maxSecretBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "The secret is too large for Windows Credential Manager.");
        }

        var secretPointer = Marshal.StringToCoTaskMemUni(value);
        try
        {
            var credential = new NativeCredential
            {
                Type = _credentialTypeGeneric,
                TargetName = GetTargetName(key),
                CredentialBlobSize = (uint)secretByteCount,
                CredentialBlob = secretPointer,
                Persist = _credentialPersistLocalMachine,
                UserName = Environment.UserName,
            };

            if (!CredWrite(ref credential, 0))
            {
                throw CreateOperationException("write");
            }
        }
        finally
        {
            Marshal.ZeroFreeCoTaskMemUnicode(secretPointer);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken)
    {
        ValidateKey(key);
        EnsureWindows();
        cancellationToken.ThrowIfCancellationRequested();

        if (!CredRead(GetTargetName(key), _credentialTypeGeneric, 0, out var credentialPointer))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == _errorNotFound)
            {
                return Task.FromResult<string?>(null);
            }

            throw CreateOperationException("read", error);
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
            var value = credential.CredentialBlobSize == 0
                ? string.Empty
                : Marshal.PtrToStringUni(
                    credential.CredentialBlob,
                    checked((int)credential.CredentialBlobSize / sizeof(char)));
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<string?>(value);
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    public async Task<bool> HasSecretAsync(string key, CancellationToken cancellationToken)
    {
        ValidateKey(key);
        return await GetSecretAsync(key, cancellationToken).ConfigureAwait(false) is not null;
    }

    public Task RemoveSecretAsync(string key, CancellationToken cancellationToken)
    {
        ValidateKey(key);
        EnsureWindows();
        cancellationToken.ThrowIfCancellationRequested();

        if (!CredDelete(GetTargetName(key), _credentialTypeGeneric, 0))
        {
            var error = Marshal.GetLastWin32Error();
            if (error != _errorNotFound)
            {
                throw CreateOperationException("remove", error);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    internal static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length > _maxKeyLength || key is "." or ".." ||
            key.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')))
        {
            throw new ArgumentException(
                "Secret names may contain only ASCII letters, digits, periods, hyphens and underscores.",
                nameof(key));
        }
    }

    private static string GetTargetName(string key) => _targetPrefix + key;

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows Credential Manager is available only on Windows.");
        }
    }

    private static Win32Exception CreateOperationException(string operation, int? error = null) =>
        new(error ?? Marshal.GetLastWin32Error(), $"Windows Credential Manager could not {operation} the secret.");

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(ref NativeCredential credential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }
}
