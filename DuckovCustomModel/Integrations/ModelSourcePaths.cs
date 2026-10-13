using System;
using System.IO;
using DuckovCustomModel.Core.Data;

namespace DuckovCustomModel.Integrations
{
    internal static class ModelSourcePaths
    {
        internal static bool IsFileSource(ModelBundleInfo bundle)
        {
            return ModelSourceDefinition.TryGet(bundle.SourceKind, out var definition) && !definition.UsesPrefabPath;
        }

        internal static string SourcePath(ModelBundleInfo bundle, ModelInfo model)
        {
            var directory = IsFileSource(bundle) || string.IsNullOrWhiteSpace(bundle.BundlePath)
                ? bundle.DirectoryPath
                : Path.GetDirectoryName(Path.GetFullPath(Path.Combine(bundle.DirectoryPath, bundle.BundlePath)))!;
            return Resolve(directory, model.SourcePath);
        }

        internal static string Resolve(string directory, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
                throw new InvalidDataException("Model source must use a relative path.");
            var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var path = Path.GetFullPath(Path.Combine(root, relative));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Model source escapes its directory.");
            RejectLinks(path);
            return path;
        }

        internal static void RejectLinks(string path)
        {
            for (var current = Path.GetFullPath(path);
                 !string.IsNullOrEmpty(current);
                 current = Path.GetDirectoryName(current))
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Model sources may not traverse symbolic links or junctions.");
        }
    }
}
