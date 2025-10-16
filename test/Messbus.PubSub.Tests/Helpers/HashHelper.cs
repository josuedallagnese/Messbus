using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Messbus.PubSub.Tests.Helpers;

public static class HashHelper
{
    public static string ToMd5Hash<T>(T obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hashBytes = MD5.HashData(bytes);

        var sb = new StringBuilder();

        foreach (var b in hashBytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
}
