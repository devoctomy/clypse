using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace clypse.core.Extensions;

/// <summary>
/// Provides extension methods for SecureString to enable safe conversion to byte arrays.
/// </summary>
public static class SecureStringExtensions
{
    /// <summary>
    /// Converts a SecureString to a UTF-8 encoded byte array in a secure manner.
    /// The SecureString is temporarily converted to an unmanaged BSTR, then to UTF-8 bytes,
    /// with the intermediate memory being securely zeroed out.
    /// </summary>
    /// <param name="secureString">The SecureString to convert.</param>
    /// <returns>A byte array containing the UTF-8 encoded representation of the SecureString.</returns>
    /// <exception cref="ArgumentNullException">Thrown when secureString is null.</exception>
    public static byte[] ToUtf8Bytes(this SecureString secureString)
    {
        ArgumentNullException.ThrowIfNull(secureString);

        IntPtr bstr = IntPtr.Zero;
        try
        {
            bstr = Marshal.SecureStringToBSTR(secureString);
            int length = Marshal.ReadInt32(bstr, -4);
            var unicodeBytes = new byte[length];
            Marshal.Copy(bstr, unicodeBytes, 0, length);
            string plain = Encoding.Unicode.GetString(unicodeBytes);
            return Encoding.UTF8.GetBytes(plain);
        }
        finally
        {
            if (bstr != IntPtr.Zero)
            {
                Marshal.ZeroFreeBSTR(bstr);
            }
        }
    }
}
