using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MakerBot
{
    /// <summary>MakerBot's HTTP authentication and auxiliary API.</summary>
    public static class FastCGI
    {
        public enum AccessTokenContexts { jsonrpc, put, camera }

        public static Task<string> GetAccessToken(IPAddress address, string authCode, string clientId, string clientSecret, AccessTokenContexts context = AccessTokenContexts.jsonrpc)
            => GetAccessToken(address, authCode, clientId, clientSecret, context, CancellationToken.None);

        public static async Task<string> GetAccessToken(IPAddress address, string authCode, string clientId, string clientSecret, AccessTokenContexts context, CancellationToken cancellationToken)
        {
            var response = await Send(address, "auth", new
            {
                response_type = "token",
                client_id = clientId,
                client_secret = clientSecret,
                auth_code = authCode,
                context = context.ToString()
            }, cancellationToken).ConfigureAwait(false);
            var access = Parse(response, "access token");
            if (!string.Equals(access.Value<string>("status"), "success", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException(access.Value<string>("message") ?? "MakerBot rejected the access token request.");
            return access.Value<string>("access_token") ?? throw new InvalidDataException("MakerBot access token response did not contain a token.");
        }

        public static Task<string> GetAccessToken(IPAddress address, string clientId, string clientSecret, AccessTokenContexts context = AccessTokenContexts.jsonrpc)
            => GetAccessTokenAfterPairing(address, clientId, clientSecret, context, CancellationToken.None);

        private static async Task<string> GetAccessTokenAfterPairing(IPAddress address, string clientId, string clientSecret, AccessTokenContexts context, CancellationToken cancellationToken)
        {
            var authCode = await GetAuthCode(address, clientId, clientSecret, cancellationToken).ConfigureAwait(false);
            return await GetAccessToken(address, authCode, clientId, clientSecret, context, cancellationToken).ConfigureAwait(false);
        }

        public static Task<string> GetAuthCode(IPAddress address, string clientId, string clientSecret)
            => GetAuthCode(address, clientId, clientSecret, CancellationToken.None);

        public static async Task<string> GetAuthCode(IPAddress address, string clientId, string clientSecret, CancellationToken cancellationToken)
        {
            var accessCode = await GetAccessCode(address, clientId, clientSecret, cancellationToken).ConfigureAwait(false);
            return await GetAuthCode(address, accessCode, clientId, clientSecret, cancellationToken).ConfigureAwait(false);
        }

        public static Task<string> GetAuthCode(IPAddress address, string accessCode, string clientId, string clientSecret)
            => GetAuthCode(address, accessCode, clientId, clientSecret, CancellationToken.None);

        public static async Task<string> GetAuthCode(IPAddress address, string accessCode, string clientId, string clientSecret, CancellationToken cancellationToken)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(120));
                while (true)
                {
                    await Task.Delay(1000, timeout.Token).ConfigureAwait(false);
                    var answer = Parse(await Send(address, "auth", new
                    {
                        response_type = "answer",
                        client_id = clientId,
                        client_secret = clientSecret,
                        answer_code = accessCode
                    }, timeout.Token).ConfigureAwait(false), "authorization");
                    var status = answer.Value<string>("answer");
                    if (string.Equals(status, "accepted", StringComparison.OrdinalIgnoreCase))
                        return answer.Value<string>("code") ?? throw new InvalidDataException("Accepted authorization did not contain a code.");
                    if (string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase))
                        throw new UnauthorizedAccessException("MakerBot pairing was rejected at the printer.");
                    if (!string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Unrecognized MakerBot authorization response: " + status);
                }
            }
        }

        public static Task<string> GetAccessCode(IPAddress address, string clientId, string clientSecret)
            => GetAccessCode(address, clientId, clientSecret, CancellationToken.None);

        public static async Task<string> GetAccessCode(IPAddress address, string clientId, string clientSecret, CancellationToken cancellationToken)
        {
            var access = Parse(await Send(address, "auth", new
            {
                response_type = "code",
                client_id = clientId,
                client_secret = clientSecret
            }, cancellationToken).ConfigureAwait(false), "pairing code");
            return access.Value<string>("answer_code") ?? throw new InvalidDataException("MakerBot pairing response did not contain an answer code.");
        }

        public static Task<string> Send(IPAddress address, string path, object parameters)
            => Send(address, path, parameters, CancellationToken.None);

        public static Task<string> Send(IPAddress address, string path, object parameters, CancellationToken cancellationToken)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            var properties = new List<string>();
            foreach (var property in parameters.GetType().GetProperties())
            {
                var value = property.GetValue(parameters);
                if (value != null) properties.Add(Uri.EscapeDataString(property.Name) + "=" + Uri.EscapeDataString(Convert.ToString(value)));
            }
            return Send(address, path, string.Join("&", properties), cancellationToken);
        }

        public static Task<string> Send(IPAddress address, string path, string queryArgs)
            => Send(address, path, queryArgs, CancellationToken.None);

        public static async Task<string> Send(IPAddress address, string path, string queryArgs, CancellationToken cancellationToken)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A MakerBot HTTP path is required.", nameof(path));

            // Explicit address-family selection is intentional. Some Windows configurations reject
            // the IPv4-mapped IPv6 socket selected by HttpClient for these embedded-printer addresses.
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                using (timeout.Token.Register(() => { try { socket.Dispose(); } catch { } }))
                {
                    var connect = Task.Factory.FromAsync(
                        (callback, state) => socket.BeginConnect(new IPEndPoint(address, 80), callback, state),
                        socket.EndConnect,
                        null);
                    await connect.ConfigureAwait(false);

                    using (var stream = new NetworkStream(socket, ownsSocket: false))
                    {
                        var target = "/" + path.TrimStart('/') + "?" + (queryArgs ?? string.Empty);
                        var request = Encoding.ASCII.GetBytes(
                            $"GET {target} HTTP/1.1\r\nHost: {(address.AddressFamily == AddressFamily.InterNetworkV6 ? "[" + address + "]" : address.ToString())}\r\nAccept: application/json\r\nConnection: close\r\n\r\n");
                        await stream.WriteAsync(request, 0, request.Length, timeout.Token).ConfigureAwait(false);
                        await stream.FlushAsync(timeout.Token).ConfigureAwait(false);

                        using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true))
                        {
                            var raw = await reader.ReadToEndAsync().ConfigureAwait(false);
                            var separator = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                            if (separator < 0) throw new InvalidDataException("MakerBot HTTP response did not contain headers.");
                            var headers = raw.Substring(0, separator);
                            if (!headers.StartsWith("HTTP/1.1 200", StringComparison.OrdinalIgnoreCase) &&
                                !headers.StartsWith("HTTP/1.0 200", StringComparison.OrdinalIgnoreCase))
                                throw new WebException("MakerBot HTTP request failed: " + headers.Split('\r', '\n')[0]);
                            var data = raw.Substring(separator + 4);
                            if (headers.IndexOf("Transfer-Encoding: chunked", StringComparison.OrdinalIgnoreCase) >= 0)
                                data = DecodeChunked(data);
                            if (string.IsNullOrWhiteSpace(data)) throw new InvalidDataException("MakerBot HTTP response was empty.");
                            return data;
                        }
                    }
                }
            }
        }

        private static string DecodeChunked(string encoded)
        {
            var decoded = new StringBuilder();
            var position = 0;
            while (position < encoded.Length)
            {
                var lineEnd = encoded.IndexOf("\r\n", position, StringComparison.Ordinal);
                if (lineEnd < 0) throw new InvalidDataException("MakerBot returned an incomplete chunk header.");
                var sizeText = encoded.Substring(position, lineEnd - position).Split(';')[0];
                if (!int.TryParse(sizeText, System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out var size))
                    throw new InvalidDataException("MakerBot returned an invalid HTTP chunk size.");
                position = lineEnd + 2;
                if (size == 0) return decoded.ToString();
                if (position + size > encoded.Length) throw new InvalidDataException("MakerBot returned an incomplete HTTP chunk.");
                decoded.Append(encoded, position, size);
                position += size;
                if (position + 2 > encoded.Length || encoded[position] != '\r' || encoded[position + 1] != '\n')
                    throw new InvalidDataException("MakerBot returned an invalid HTTP chunk terminator.");
                position += 2;
            }
            throw new InvalidDataException("MakerBot chunked response did not terminate.");
        }

        private static JObject Parse(string response, string description)
        {
            try { return JObject.Parse(response); }
            catch (Exception ex) { throw new InvalidDataException($"Failed to parse MakerBot {description} response.", ex); }
        }
    }
}
