using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class DuckovYsmShaderBuild
{
    public static void Build()
    {
        if (!Application.unityVersion.StartsWith("2022.3.", StringComparison.Ordinal))
            throw new InvalidOperationException("Build this bundle with Unity 2022.3 for the Duckov player.");
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/DuckovYsm.shader");
        if (!shader || ShaderUtil.ShaderHasError(shader))
        {
            if (shader) foreach (var message in ShaderUtil.GetShaderMessages(shader)) Debug.LogError(message.message);
            throw new InvalidOperationException("YSM shader has import errors.");
        }
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../DuckovCustomModel/Resources"));
        Directory.CreateDirectory(output);
        var build = new AssetBundleBuild { assetBundleName = "ysm-rendering.bundle", assetNames = new[] { "Assets/DuckovYsm.shader", "Assets/DuckovYsmPreview.shader" } };
        // URP's scriptable stripper removes every variant when the build has no active URP asset.
        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        var pipeline = UniversalRenderPipelineAsset.Create(renderer);
        SetInternalPipelineProperty(pipeline, "supportsMainLightShadows", true);
        SetInternalPipelineProperty(pipeline, "supportsAdditionalLightShadows", true);
        SetInternalPipelineProperty(pipeline, "supportsSoftShadows", true);
        SetInternalPipelineProperty(pipeline, "mainLightRenderingMode", LightRenderingMode.PerPixel);
        SetInternalPipelineProperty(pipeline, "additionalLightsRenderingMode", LightRenderingMode.PerPixel);
        pipeline.shadowCascadeCount = 4;
        var previousDefault = GraphicsSettings.renderPipelineAsset;
        var previousQuality = QualitySettings.renderPipeline;
        try
        {
            GraphicsSettings.renderPipelineAsset = pipeline;
            QualitySettings.renderPipeline = pipeline;
            YsmShaderVariantGuard.RetainedVariants = 0;
            YsmShaderVariantGuard.ForwardModes.Clear();
            var manifest = BuildPipeline.BuildAssetBundles(output, new[] { build }, BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows64);
            if (!manifest || ShaderUtil.ShaderHasError(shader) || YsmShaderVariantGuard.RetainedVariants == 0)
                throw new InvalidOperationException("YSM shader bundle compilation failed or retained no executable variants.");
            if (!YsmShaderVariantGuard.ForwardModes.Contains(""))
                throw new InvalidOperationException("YSM shader bundle stripped its forward program.");
            if (!YsmShaderVariantGuard.ForwardModes.Contains("_FORWARD_PLUS"))
                throw new InvalidOperationException("YSM shader bundle stripped its Forward+ lighting program.");
            var compatibility = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", BindingFlags.NonPublic | BindingFlags.Static);
            if (compatibility == null) throw new InvalidOperationException("Cannot verify local shader batcher compatibility.");
            int compatibilityCode = (int)compatibility.Invoke(null, new object[] { shader, 0 });
            if (compatibilityCode == 0) throw new InvalidOperationException("The cross-version shader must use the regular named-uniform path, not SRP Batcher.");
            Debug.Log("YSM shader bundle: " + output + "; retained variants: " + YsmShaderVariantGuard.RetainedVariants + "; SRP Batcher compatibility code: " + compatibilityCode);
        }
        finally
        {
            GraphicsSettings.renderPipelineAsset = previousDefault;
            QualitySettings.renderPipeline = previousQuality;
            UnityEngine.Object.DestroyImmediate(pipeline);
            UnityEngine.Object.DestroyImmediate(renderer);
        }
    }

    private static void SetInternalPipelineProperty(UniversalRenderPipelineAsset pipeline, string name, object value)
    {
        var property = typeof(UniversalRenderPipelineAsset).GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property?.SetMethod == null) throw new InvalidOperationException("Missing URP build setting: " + name);
        property.SetValue(pipeline, value);
    }
}
public sealed class YsmShaderVariantGuard : IPreprocessShaders
{
    public static int RetainedVariants;
    public static readonly HashSet<string> ForwardModes = new HashSet<string>();
    public int callbackOrder => int.MaxValue;
    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (shader.name != "DuckovCustomModel/Ysm Lit") return;
        if (data.Count == 0) throw new BuildFailedException("The YSM shader was entirely stripped; a URP build asset must be active.");
        if (snippet.passName == "YsmForward")
        {
            var forwardPlus = new ShaderKeyword(shader, "_FORWARD_PLUS");
            var additionalLights = new ShaderKeyword(shader, "_ADDITIONAL_LIGHTS");
            int original = data.Count;
            for (int i = 0; i < original; i++)
            {
                var variant = data[i];
                if (!variant.shaderKeywordSet.IsEnabled(additionalLights)) continue;
                var keywords = variant.shaderKeywordSet;
                keywords.Enable(forwardPlus);
                variant.shaderKeywordSet = keywords;
                data.Add(variant);
            }
        }
        foreach (var variant in data)
        {
            var keywords = variant.shaderKeywordSet.GetShaderKeywords();
            if (snippet.passName == "YsmForward")
            {
                var mode = "";
                foreach (var keyword in keywords)
                    if (keyword.name == "_FORWARD_PLUS" || keyword.name == "_MAIN_LIGHT_SHADOWS" || keyword.name == "_MAIN_LIGHT_SHADOWS_CASCADE" || keyword.name == "_MAIN_LIGHT_SHADOWS_SCREEN")
                        mode = keyword.name;
                ForwardModes.Add(mode);
            }
            Debug.Log("YSM shader variant: " + snippet.passName + " " + string.Join(",", Array.ConvertAll(
                keywords, keyword => keyword.name)));
        }
        RetainedVariants += data.Count;
    }
}
