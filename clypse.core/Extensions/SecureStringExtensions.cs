using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace clypse.core.Extensions;

public static class SecureStringExtensions
{
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
                Marshal.ZeroFreeBSTR(bstr);
        }
    }
}
