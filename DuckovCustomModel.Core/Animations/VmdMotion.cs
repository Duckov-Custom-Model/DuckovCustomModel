using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace DuckovCustomModel.Core.Animations
{
    public sealed class VmdMotion
    {
        private static readonly byte[] ShiftJis = ReadEncoding();
        public Dictionary<string, BoneKey[]> Bones { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, MorphKey[]> Morphs { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, IkKey[]> Ik { get; } = new(StringComparer.Ordinal);
        public double Duration { get; private set; }

        public interface IFrameKey
        {
            uint Frame { get; }
        }

        public readonly struct BoneKey : IFrameKey
        {
            public uint Frame { get; }
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly byte[] Interpolation;

            public BoneKey(uint frame, Vector3 position, Quaternion rotation, byte[] interpolation)
            {
                Frame = frame;
                Position = position;
                Rotation = rotation;
                Interpolation = interpolation;
            }
        }

        public readonly struct MorphKey : IFrameKey
        {
            public uint Frame { get; }
            public readonly float Weight;

            public MorphKey(uint frame, float weight)
            {
                Frame = frame;
                Weight = weight;
            }
        }

        public readonly struct IkKey : IFrameKey
        {
            public uint Frame { get; }
            public readonly bool Enabled;

            public IkKey(uint frame, bool enabled)
            {
                Frame = frame;
                Enabled = enabled;
            }
        }

        public static VmdMotion Parse(byte[] bytes)
        {
            if (bytes.Length < 34 || bytes.Length > 64 * 1024 * 1024)
                throw new InvalidDataException("VMD file size is invalid.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            var header = Encoding.ASCII.GetString(Read(reader, 30)).TrimEnd('\0');
            if (header != "Vocaloid Motion Data 0002" && header != "Vocaloid Motion Data file")
                throw new InvalidDataException("Unsupported VMD header.");
            Read(reader, header.EndsWith("0002", StringComparison.Ordinal) ? 20 : 10);
            var names = new Dictionary<(ulong, ulong, ulong), string>();
            var nameBuffer = new byte[20];
            var result = new VmdMotion();
            var bones = new Dictionary<string, List<BoneKey>>(StringComparer.Ordinal);
            var morphs = new Dictionary<string, List<MorphKey>>(StringComparer.Ordinal);
            var iks = new Dictionary<string, List<IkKey>>(StringComparer.Ordinal);
            var count = Count(reader, 111);
            for (var i = 0; i < count; i++)
            {
                var name = ReadName(reader, 15, nameBuffer, names);
                var frame = Frame(reader);
                var position = new Vector3(Number(reader), Number(reader), Number(reader));
                var rotation = new Quaternion(Number(reader), Number(reader), Number(reader), Number(reader));
                if (rotation.LengthSquared() < .000001f || float.IsInfinity(rotation.LengthSquared()))
                    throw new InvalidDataException("VMD contains a zero quaternion.");
                var interpolation = Read(reader, 16);
                stream.Position += 48;
                for (var c = 0; c < 16; c++)
                    if (interpolation[c] > 127)
                        throw new InvalidDataException("Invalid VMD Bezier curve.");
                Add(bones, name, new BoneKey(frame, position, Quaternion.Normalize(rotation), interpolation));
                result.Duration = Math.Max(result.Duration, frame / 30.0);
            }

            if (stream.Position < stream.Length)
            {
                count = Count(reader, 23);
                for (var i = 0; i < count; i++)
                {
                    var name = ReadName(reader, 15, nameBuffer, names);
                    var frame = Frame(reader);
                    Add(morphs, name, new MorphKey(frame, Number(reader)));
                    result.Duration = Math.Max(result.Duration, frame / 30.0);
                }
            }

            foreach (var size in new[] { 61, 28, 9 })
                if (stream.Position < stream.Length)
                {
                    count = Count(reader, size);
                    stream.Position += (long)count * size;
                }

            if (stream.Position < stream.Length)
            {
                count = Count(reader, 9);
                for (var i = 0; i < count; i++)
                {
                    var frame = Frame(reader);
                    reader.ReadByte();
                    var ikCount = Count(reader, 21);
                    for (var j = 0; j < ikCount; j++)
                    {
                        var name = ReadName(reader, 20, nameBuffer, names);
                        Add(iks, name, new IkKey(frame, reader.ReadByte() != 0));
                    }

                    result.Duration = Math.Max(result.Duration, frame / 30.0);
                }
            }

            if (stream.Position != stream.Length || bones.Count == 0 && morphs.Count == 0)
                throw new InvalidDataException("VMD has no character motion or contains invalid trailing data.");
            foreach (var pair in bones)
                result.Bones.Add(pair.Key,
                    pair.Value.GroupBy(k => k.Frame).Select(g => g.Last()).OrderBy(k => k.Frame).ToArray());
            foreach (var pair in morphs)
                result.Morphs.Add(pair.Key,
                    pair.Value.GroupBy(k => k.Frame).Select(g => g.Last()).OrderBy(k => k.Frame).ToArray());
            foreach (var pair in iks)
                result.Ik.Add(pair.Key,
                    pair.Value.GroupBy(k => k.Frame).Select(g => g.Last()).OrderBy(k => k.Frame).ToArray());
            return result;
        }

        public static void Sample(BoneKey[] keys, double seconds, out Vector3 position, out Quaternion rotation)
        {
            var frame = Math.Max(0, seconds * 30);
            var index = Lower(keys, frame);
            var a = keys[index];
            if (index == keys.Length - 1 || frame <= a.Frame)
            {
                position = a.Position;
                rotation = a.Rotation;
                return;
            }

            var b = keys[index + 1];
            var t = (float)((frame - a.Frame) / (b.Frame - a.Frame));
            position = new Vector3(
                a.Position.X + (b.Position.X - a.Position.X) * Bezier(t, b.Interpolation, 0),
                a.Position.Y + (b.Position.Y - a.Position.Y) * Bezier(t, b.Interpolation, 1),
                a.Position.Z + (b.Position.Z - a.Position.Z) * Bezier(t, b.Interpolation, 2));
            rotation = Quaternion.Slerp(a.Rotation, b.Rotation, Bezier(t, b.Interpolation, 3));
        }

        public static float Sample(MorphKey[] keys, double seconds)
        {
            var frame = Math.Max(0, seconds * 30);
            var index = Lower(keys, frame);
            var a = keys[index];
            if (index == keys.Length - 1 || frame <= a.Frame) return a.Weight;
            var b = keys[index + 1];
            return a.Weight + (b.Weight - a.Weight) * (float)((frame - a.Frame) / (b.Frame - a.Frame));
        }

        public static bool Sample(IkKey[] keys, double seconds) =>
            seconds * 30 < keys[0].Frame || keys[Lower(keys, Math.Max(0, seconds * 30))].Enabled;

        private static int Lower<T>(T[] keys, double target) where T : struct, IFrameKey
        {
            var low = 0;
            var high = keys.Length - 1;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                if (keys[middle].Frame <= target) low = middle;
                else high = middle - 1;
            }

            return low;
        }

        private static float Bezier(float x, byte[] curve, int channel)
        {
            var x1 = curve[channel] / 127f;
            var y1 = curve[channel + 4] / 127f;
            var x2 = curve[channel + 8] / 127f;
            var y2 = curve[channel + 12] / 127f;
            if (x1 == y1 && x2 == y2) return x;
            var low = 0f;
            var high = 1f;
            var t = x;
            for (var i = 0; i < 16; i++)
            {
                var inverse = 1 - t;
                var sample = 3 * inverse * inverse * t * x1 + 3 * inverse * t * t * x2 + t * t * t;
                if (Math.Abs(sample - x) < .00001f) break;
                if (sample < x) low = t;
                else high = t;
                t = (low + high) * .5f;
            }

            var one = 1 - t;
            return 3 * one * one * t * y1 + 3 * one * t * t * y2 + t * t * t;
        }

        private static uint Frame(BinaryReader reader)
        {
            var frame = reader.ReadUInt32();
            if (frame > 30 * 60 * 60 * 24) throw new InvalidDataException("VMD duration exceeds one day.");
            return frame;
        }

        private static int Count(BinaryReader reader, int size)
        {
            var count = reader.ReadUInt32();
            if (count > 1000000 || (long)count * size > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Invalid VMD section length.");
            return (int)count;
        }

        private static float Number(BinaryReader reader)
        {
            var value = reader.ReadSingle();
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Invalid VMD number.");
            return value;
        }

        private static byte[] Read(BinaryReader reader, int count)
        {
            var data = reader.ReadBytes(count);
            if (data.Length != count) throw new EndOfStreamException("Truncated VMD file.");
            return data;
        }

        private static void Add<T>(Dictionary<string, List<T>> tracks, string name, T key)
        {
            if (!tracks.TryGetValue(name, out var list)) tracks.Add(name, list = new List<T>());
            list.Add(key);
        }

        private static byte[] ReadEncoding()
        {
            using var stream =
                typeof(VmdMotion).Assembly.GetManifestResourceStream("DuckovCustomModel.Core.Animations.ShiftJis.bin")
                ?? throw new InvalidOperationException("Missing Shift-JIS mapping.");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        private static string ReadName(BinaryReader reader, int count, byte[] buffer,
            Dictionary<(ulong, ulong, ulong), string> names)
        {
            if (reader.Read(buffer, 0, count) != count) throw new EndOfStreamException("Truncated VMD name.");
            if (count < buffer.Length) buffer[count] = 0;
            ulong a = 0, b = 0, c = 0;
            for (var i = 0; i < count && buffer[i] != 0; i++)
                if (i < 8) a |= (ulong)buffer[i] << (i * 8);
                else if (i < 16) b |= (ulong)buffer[i] << ((i - 8) * 8);
                else c |= (ulong)buffer[i] << ((i - 16) * 8);
            var key = (a, b, c);
            if (names.TryGetValue(key, out var value)) return value;
            return names[key] = Name(buffer);
        }

        private static string Name(byte[] bytes)
        {
            var result = new StringBuilder(bytes.Length);
            for (var i = 0; i < bytes.Length && bytes[i] != 0; i++)
            {
                var a = bytes[i];
                if (a < 0x80)
                {
                    result.Append((char)a);
                    continue;
                }

                if (a >= 0xa1 && a <= 0xdf)
                {
                    result.Append((char)(0xff61 + a - 0xa1));
                    continue;
                }

                var lead = a >= 0x81 && a <= 0x9f ? a - 0x81 : a >= 0xe0 && a <= 0xfc ? a - 0xe0 + 31 : -1;
                if (lead < 0)
                {
                    result.Append('\ufffd');
                    continue;
                }

                if (++i >= bytes.Length || bytes[i] == 0)
                {
                    result.Append('\ufffd');
                    break;
                }

                var b = bytes[i];
                var trail = b >= 0x40 && b <= 0x7e ? b - 0x40 : b >= 0x80 && b <= 0xfc ? b - 0x80 + 63 : -1;
                if (trail < 0)
                {
                    result.Append('\ufffd');
                    i--;
                    continue;
                }

                var offset = (lead * 188 + trail) * 2;
                result.Append((char)(ShiftJis[offset] | ShiftJis[offset + 1] << 8));
            }

            return result.ToString();
        }
    }
}
