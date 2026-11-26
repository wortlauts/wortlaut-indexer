using System.Net.Http.Headers;
using System.Text;

namespace Wortlaut.Indexer.Indexing;

public sealed record EsCredentials(string Username, string Password)
{
    public static EsCredentials? From(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        if (string.IsNullOrEmpty(password))
        {
            throw new EsException("A password is required when a username is given.");
        }

        return new EsCredentials(username, password);
    }

    public AuthenticationHeaderValue ToAuthorizationHeader() =>
        new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}")));
}
