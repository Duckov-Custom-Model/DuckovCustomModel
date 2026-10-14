using System;
using System.Runtime.CompilerServices;
using DuckovCustomModel.Core.Data;

namespace DuckovCustomModel.Managers
{
    internal static class ModelLoadStatus
    {
        private static readonly object Sync = new();
        private static ConditionalWeakTable<ModelInfo, Failure> failures = new();
        private static long version;

        internal static long Version
        {
            get
            {
                lock (Sync)
                {
                    return version;
                }
            }
        }

        internal static string? GetError(ModelInfo model)
        {
            lock (Sync)
            {
                return failures.TryGetValue(model, out var failure) && failure.Revision == model.SourceRevision
                    ? failure.Message
                    : null;
            }
        }

        internal static void Failed(ModelInfo model, string revision, Exception exception)
        {
            lock (Sync)
            {
                if (model.SourceRevision != revision) return;
                var message = exception.GetBaseException().Message;
                if (string.IsNullOrWhiteSpace(message)) message = exception.GetType().Name;
                if (failures.TryGetValue(model, out var current) && current.Revision == revision &&
                    current.Message == message)
                    return;
                failures.Remove(model);
                failures.Add(model, new(revision, message));
                version++;
            }
        }

        internal static void Succeeded(ModelInfo model, string revision)
        {
            lock (Sync)
            {
                if (model.SourceRevision == revision && failures.Remove(model)) version++;
            }
        }

        internal static void Clear()
        {
            lock (Sync)
            {
                failures = new();
                version++;
            }
        }

        private sealed class Failure(string revision, string message)
        {
            internal string Revision { get; } = revision;
            internal string Message { get; } = message;
        }
    }
}
