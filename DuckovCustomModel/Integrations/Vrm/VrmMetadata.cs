using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DuckovCustomModel.Integrations.Vrm
{
    internal sealed class VrmMetadata
    {
        internal const int MaximumBytes = 256 * 1024 * 1024;
        internal string Name = string.Empty, Author = string.Empty, Version = string.Empty;
        internal byte[]? Thumbnail;

        internal static byte[] HashContent(byte[] bytes)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var algorithm = IntPtr.Zero;
                try
                {
                    if (BCryptOpenAlgorithmProvider(out algorithm, "SHA256", null, 0) == 0)
                    {
                        var result = new byte[32];
                        if (BCryptHash(algorithm, IntPtr.Zero, 0, bytes, (uint)bytes.Length,
                                result, (uint)result.Length) == 0) return result;
                    }
                }
                catch (DllNotFoundException)
                {
                }
                catch (EntryPointNotFoundException)
                {
                }
                finally
                {
                    if (algorithm != IntPtr.Zero) BCryptCloseAlgorithmProvider(algorithm, 0);
                }
            }

            using var digest = SHA256.Create();
            return digest.ComputeHash(bytes);
        }

        [DllImport("bcrypt.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int BCryptOpenAlgorithmProvider(out IntPtr algorithm, string algorithmId,
            string? implementation, uint flags);

        [DllImport("bcrypt.dll", ExactSpelling = true)]
        private static extern int BCryptHash(IntPtr algorithm, IntPtr secret, uint secretLength,
            [In] byte[] input, uint inputLength, [Out] byte[] output, uint outputLength);

        [DllImport("bcrypt.dll", ExactSpelling = true)]
        private static extern int BCryptCloseAlgorithmProvider(IntPtr algorithm, uint flags);

        internal static byte[] ReadFile(string path)
        {
            ModelSourcePaths.RejectLinks(path);
            var length = new FileInfo(path).Length;
            if (length < 20 || length > MaximumBytes) throw new InvalidDataException("VRM file size is invalid.");
            using var stream = File.OpenRead(path);
            var bytes = new byte[(int)length];
            var offset = 0;
            while (offset < bytes.Length)
            {
                var read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read == 0) throw new EndOfStreamException("VRM file changed while reading.");
                offset += read;
            }

            if (stream.ReadByte() != -1) throw new IOException("VRM file changed while reading.");
            return bytes;
        }

        internal static VrmMetadata Read(byte[] bytes, bool readThumbnail = true)
        {
            if (bytes.Length < 20 || bytes.Length > MaximumBytes || BitConverter.ToUInt32(bytes, 0) != 0x46546C67 ||
                BitConverter.ToUInt32(bytes, 4) != 2 || BitConverter.ToUInt32(bytes, 8) != bytes.Length ||
                BitConverter.ToUInt32(bytes, 16) != 0x4E4F534A)
                throw new InvalidDataException("Invalid VRM GLB header.");
            var length = checked((int)BitConverter.ToUInt32(bytes, 12));
            if (length < 2 || length > 16 * 1024 * 1024 || length > bytes.Length - 20)
                throw new InvalidDataException("Invalid VRM JSON length.");
            var json = ReadMetadataJson(bytes, length);
            foreach (var item in (json["buffers"] as JArray ?? new()).Concat(json["images"] as JArray ?? new()))
                if (item["uri"] is { } uri && !uri.ToString().StartsWith("data:", StringComparison.Ordinal))
                    throw new InvalidDataException("VRM resources must be embedded.");
            var vrm = json["extensions"]?["VRMC_vrm"];
            var old = json["extensions"]?["VRM"];
            if (vrm == null && old == null) throw new InvalidDataException("No VRM extension found.");
            var meta = (vrm ?? old)?["meta"];
            var result = new VrmMetadata
            {
                Name = (string?)meta?[vrm != null ? "name" : "title"] ?? string.Empty,
                Author = vrm != null
                    ? string.Join(", ", (meta?["authors"] as JArray ?? new()).Select(x => x.ToString()))
                    : (string?)meta?["author"] ?? string.Empty,
                Version = (string?)meta?["version"] ?? string.Empty,
            };
            var imageIndex = (int?)meta?["thumbnailImage"];
            if (vrm == null && (int?)meta?["texture"] is >= 0 and var textureIndex &&
                json["textures"] is JArray textures && textureIndex < textures.Count)
                imageIndex = (int?)textures[textureIndex]?["source"];
            if (imageIndex is not >= 0 || json["images"] is not JArray images || imageIndex >= images.Count)
                return result;
            var image = images[imageIndex.Value];
            var viewIndex = (int?)image["bufferView"];
            if (viewIndex is not >= 0 || json["bufferViews"] is not JArray views || viewIndex >= views.Count)
                return result;
            var view = views[viewIndex.Value];
            var binaryHeader = 20 + length;
            if (binaryHeader > bytes.Length - 8 || BitConverter.ToUInt32(bytes, binaryHeader + 4) != 0x004E4942)
                return result;
            var offset = (long?)view["byteOffset"] ?? 0;
            var size = (long?)view["byteLength"] ?? 0;
            var binaryLength = BitConverter.ToUInt32(bytes, binaryHeader);
            if ((int?)view["buffer"] != 0 || offset < 0 || size <= 0 || size > 16 * 1024 * 1024 ||
                offset + size > binaryLength || binaryHeader + 8L + binaryLength > bytes.Length)
                throw new InvalidDataException("Invalid VRM thumbnail range.");
            if (readThumbnail)
            {
                result.Thumbnail = new byte[(int)size];
                Buffer.BlockCopy(bytes, checked(binaryHeader + 8 + (int)offset), result.Thumbnail, 0, (int)size);
            }

            return result;
        }

        private static JObject ReadMetadataJson(byte[] bytes, int length)
        {
            using var text = new StringReader(Encoding.UTF8.GetString(bytes, 20, length));
            using var reader = new JsonTextReader(text);
            if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
                throw new InvalidDataException("Invalid VRM JSON root.");
            var result = new JObject();
            var ended = false;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.Comment) continue;
                if (reader.TokenType == JsonToken.EndObject)
                {
                    ended = true;
                    break;
                }

                if (reader.TokenType != JsonToken.PropertyName)
                    throw new InvalidDataException("Invalid VRM JSON property.");
                var name = (string)reader.Value!;
                if (!reader.Read()) throw new InvalidDataException("Incomplete VRM JSON property.");
                while (reader.TokenType == JsonToken.Comment)
                    if (!reader.Read())
                        throw new InvalidDataException("Incomplete VRM JSON property.");
                if (name is "buffers" or "images" or "extensions" or "textures" or "bufferViews")
                    result[name] = JToken.ReadFrom(reader);
                else reader.Skip();
            }

            if (!ended) throw new InvalidDataException("Incomplete VRM JSON object.");
            while (reader.Read())
                if (reader.TokenType != JsonToken.Comment)
                    throw new InvalidDataException("Unexpected data after VRM JSON object.");
            return result;
        }
    }
}
