using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class BuildVrmResources
{
    private const string AnimationPath = "Assets/Animations/UAL1_Standard.fbx";

    private static readonly string[] AnimationNames =
    {
        "Idle_Loop", "Walk_Loop", "Jog_Fwd_Loop", "Roll", "Death01", "Hit_Chest",
        "Sword_Attack", "Pistol_Idle_Loop", "Pistol_Reload", "Pistol_Shoot", "Interact",
    };

    private static Dictionary<string, AnimationClip> _animations;

    public static void ProbeAnimations()
    {
        ImportAnimations();
        foreach (var pair in _animations)
            Debug.Log($"VRM_ANIMATION {pair.Key} length={pair.Value.length:F3} human={pair.Value.humanMotion}");
    }

    private static void ImportAnimations()
    {
        AssetDatabase.ImportAsset(AnimationPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(AnimationPath) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Missing CC0 animation source: " + AnimationPath);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.SaveAndReimport();
        var clips = new List<ModelImporterClipAnimation>();
        foreach (var clip in importer.defaultClipAnimations)
        {
            var name = clip.name[(clip.name.LastIndexOf('|') + 1)..];
            if (!AnimationNames.Contains(name)) continue;
            clip.name = name;
            clip.loopTime = name.EndsWith("_Loop", StringComparison.Ordinal);
            clip.loopPose = clip.loopTime;
            clip.keepOriginalOrientation = false;
            clip.keepOriginalPositionXZ = true;
            clip.keepOriginalPositionY = true;
            clip.lockRootRotation = true;
            clip.lockRootPositionXZ = true;
            clip.lockRootHeightY = true;
            clips.Add(clip);
        }

        importer.clipAnimations = clips.ToArray();
        importer.SaveAndReimport();
        _animations = AssetDatabase.LoadAllAssetsAtPath(AnimationPath).OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
            .ToDictionary(clip => clip.name, StringComparer.Ordinal);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(AnimationPath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException("CC0 animation source has no valid Humanoid avatar.");
        foreach (var name in AnimationNames)
            if (!_animations.TryGetValue(name, out var clip) || !clip.humanMotion || clip.length <= 0)
                throw new InvalidOperationException("Invalid Humanoid animation: " + name);
    }

    private static AnimationClip Animation(string name)
    {
        var source = _animations[name];
        if (AssetDatabase.GetAssetPath(source) != AnimationPath) return source;
        var clip = Object.Instantiate(source);
        clip.name = name;
        var path = "Assets/Generated/" + name + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        _animations[name] = clip;
        return clip;
    }

    public static void Build()
    {
        var output = Environment.GetEnvironmentVariable("DCM_VRM_BUILD_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("DCM_VRM_BUILD_OUTPUT is required.");
        Directory.CreateDirectory(output);
        PlayerBuildInterface.CompilePlayerScripts(new()
        {
            group = BuildTargetGroup.Standalone, target = BuildTarget.StandaloneWindows64,
        }, Path.Combine(output, "Assemblies"));
        Directory.CreateDirectory("Assets/Generated");
        const string mtoonPath = "Packages/com.vrmc.vrm/MToon10/Shaders/vrmc_materials_mtoon_urp.shader";
        var source = File.ReadAllText(mtoonPath);
        var forwardOnly = source.Replace("\"LightMode\" = \"UniversalForward\"",
            "\"LightMode\" = \"UniversalForwardOnly\"");
        forwardOnly = Regex.Replace(forwardOnly,
            "\\s*Pass\\s*\\{\\s*(?:PackageRequirements\\s*\\{[^}]*\\}\\s*)?Name \"XRMotionVectors\".*?ENDHLSL\\s*\\}",
            "", RegexOptions.Singleline);
        if (forwardOnly != source)
        {
            File.WriteAllText(mtoonPath, forwardOnly);
            AssetDatabase.ImportAsset(mtoonPath, ImportAssetOptions.ForceUpdate);
        }

        const string vertexPath = "Packages/com.vrmc.vrm/MToon10/Shaders/vrmc_materials_mtoon_forward_vertex.hlsl";
        var vertex = File.ReadAllText(vertexPath);
        var preciseVertex = vertex.Replace("MToon_TransformObjectToWorldDir(v.tangentOS)",
            "MToon_TransformObjectToWorldDir(v.tangentOS.xyz)");
        if (preciseVertex != vertex)
        {
            File.WriteAllText(vertexPath, preciseVertex);
            AssetDatabase.ImportAsset(vertexPath, ImportAssetOptions.ForceUpdate);
        }

        ImportAnimations();
        var controller = CreateController();
        var assets = new List<string> { AssetDatabase.GetAssetPath(controller) };
        foreach (var name in new[]
                     { "VRM10/Universal Render Pipeline/MToon10", "Universal Render Pipeline/Lit", "UniGLTF/UniUnlit" })
        {
            var shader = Shader.Find(name);
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Invalid shader: " + name);
            assets.Add(AssetDatabase.GetAssetPath(shader));
        }

        var variants = new ShaderVariantCollection();
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var options = new[]
        {
            "_NORMALMAP", "_EMISSION", "_METALLICSPECGLOSSMAP", "_OCCLUSIONMAP", "_ALPHATEST_ON",
            "_SURFACE_TYPE_TRANSPARENT",
        };
        for (var mask = 0; mask < 1 << options.Length; mask++)
        {
            var keywords = options.Where((_, i) => (mask & (1 << i)) != 0).ToArray();
            variants.Add(new(lit, PassType.ScriptableRenderPipeline, keywords));
        }

        const string variantsPath = "Assets/Generated/VrmVariants.shadervariants";
        if (AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(variantsPath))
            AssetDatabase.DeleteAsset(variantsPath);
        AssetDatabase.CreateAsset(variants, variantsPath);
        assets.Add(variantsPath);
        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        var pipeline = UniversalRenderPipelineAsset.Create(renderer);
        foreach (var name in new[]
                     { "supportsMainLightShadows", "supportsAdditionalLightShadows", "supportsSoftShadows" })
            typeof(UniversalRenderPipelineAsset).GetProperty(name).SetValue(pipeline, true);
        typeof(UniversalRenderPipelineAsset).GetProperty("additionalLightsRenderingMode")
            .SetValue(pipeline, LightRenderingMode.PerPixel);
        pipeline.shadowCascadeCount = 4;
        var original = GraphicsSettings.renderPipelineAsset;
        var quality = QualitySettings.renderPipeline;
        try
        {
            GraphicsSettings.renderPipelineAsset = pipeline;
            QualitySettings.renderPipeline = pipeline;
            var manifest = BuildPipeline.BuildAssetBundles(output, new[]
                {
                    new AssetBundleBuild { assetBundleName = "vrm-runtime.bundle", assetNames = assets.ToArray() },
                }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                BuildTarget.StandaloneWindows64);
            if (!manifest) throw new InvalidOperationException("VRM resource build failed.");
        }
        finally
        {
            GraphicsSettings.renderPipelineAsset = original;
            QualitySettings.renderPipeline = quality;
            Object.DestroyImmediate(pipeline);
            Object.DestroyImmediate(renderer);
        }

        Debug.Log("VRM_RESOURCES_BUILT");
    }

    private static AnimatorController CreateController()
    {
        const string path = "Assets/Generated/VrmLocomotion.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path)) AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Dashing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("GunReady", AnimatorControllerParameterType.Bool);
        controller.AddParameter("HandState", AnimatorControllerParameterType.Int);
        controller.AddParameter("RollPose", AnimatorControllerParameterType.Bool);
        controller.AddParameter("RollProgress", AnimatorControllerParameterType.Float);
        controller.AddParameter("Reloading", AnimatorControllerParameterType.Bool);
        controller.AddParameter("UsingItem", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Die", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackProgress", AnimatorControllerParameterType.Float);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        var movement = machine.AddState("Locomotion");
        var tree = new BlendTree { name = "Locomotion", blendParameter = "MoveSpeed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree, controller);
        tree.AddChild(Animation("Idle_Loop"), 0);
        tree.AddChild(Animation("Walk_Loop"), 1);
        tree.AddChild(Animation("Jog_Fwd_Loop"), 2);
        movement.motion = tree;
        machine.defaultState = movement;
        var roll = machine.AddState("Roll");
        roll.motion = Animation("Roll");
        roll.timeParameterActive = true;
        roll.timeParameter = "RollProgress";
        Transition(movement, roll, "RollPose", true, .08f);
        Transition(roll, movement, "RollPose", false, .12f);
        var dead = machine.AddState("Dead");
        dead.motion = Animation("Death01");
        var death = machine.AddAnyStateTransition(dead);
        death.canTransitionToSelf = false;
        death.hasExitTime = false;
        death.duration = .15f;
        death.AddCondition(AnimatorConditionMode.If, 0, "Die");
        Transition(dead, movement, "Die", false, .15f);
        var hurt = machine.AddState("Hurt");
        hurt.motion = Animation("Hit_Chest");
        var recoil = machine.AddAnyStateTransition(hurt);
        recoil.canTransitionToSelf = false;
        recoil.hasExitTime = false;
        recoil.duration = .03f;
        recoil.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        recoil.AddCondition(AnimatorConditionMode.IfNot, 0, "Dashing");
        recoil.AddCondition(AnimatorConditionMode.IfNot, 0, "Die");
        var recover = hurt.AddTransition(movement);
        recover.hasExitTime = true;
        recover.exitTime = 1;
        recover.duration = .08f;
        Transition(hurt, roll, "RollPose", true, .05f);
        AddHoldingLayer(controller);
        AddAttackLayer(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void AddHoldingLayer(AnimatorController controller)
    {
        var mask = new AvatarMask { name = "Arms" };
        for (var i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,
                i == (int)AvatarMaskBodyPart.LeftArm || i == (int)AvatarMaskBodyPart.RightArm ||
                i == (int)AvatarMaskBodyPart.LeftFingers || i == (int)AvatarMaskBodyPart.RightFingers);
        AssetDatabase.AddObjectToAsset(mask, controller);
        var machine = new AnimatorStateMachine { name = "Holding" };
        AssetDatabase.AddObjectToAsset(machine, controller);
        controller.AddLayer(new AnimatorControllerLayer
            { name = "Holding", defaultWeight = 1, avatarMask = mask, stateMachine = machine });
        var free = machine.AddState("Free");
        free.writeDefaultValues = false;
        machine.defaultState = free;
        var hold = machine.AddState("Hold");
        hold.writeDefaultValues = false;
        hold.motion = Animation("Pistol_Idle_Loop");
        var enter = free.AddTransition(hold);
        enter.hasExitTime = false;
        enter.duration = .12f;
        enter.AddCondition(AnimatorConditionMode.NotEqual, 0, "HandState");
        enter.AddCondition(AnimatorConditionMode.IfNot, 0, "Dashing");
        var exit = hold.AddTransition(free);
        exit.hasExitTime = false;
        exit.duration = .12f;
        exit.AddCondition(AnimatorConditionMode.Equals, 0, "HandState");
        Transition(hold, free, "Dashing", true, .05f);
        var recoil = machine.AddState("Shoot");
        recoil.writeDefaultValues = false;
        recoil.motion = Animation("Pistol_Shoot");
        var shot = machine.AddAnyStateTransition(recoil);
        shot.canTransitionToSelf = true;
        shot.hasExitTime = false;
        shot.duration = .02f;
        shot.AddCondition(AnimatorConditionMode.If, 0, "Shoot");
        shot.AddCondition(AnimatorConditionMode.IfNot, 0, "Dashing");
        var settle = recoil.AddTransition(hold);
        settle.hasExitTime = true;
        settle.exitTime = 1;
        settle.duration = .04f;
        Transition(recoil, free, "Dashing", true, .05f);
        foreach (var parameter in new[] { "Reloading", "UsingItem" })
        {
            var action = machine.AddState(parameter);
            action.writeDefaultValues = false;
            action.motion = Animation(parameter == "Reloading" ? "Pistol_Reload" : "Interact");
            var enterAction = machine.AddAnyStateTransition(action);
            enterAction.canTransitionToSelf = false;
            enterAction.hasExitTime = false;
            enterAction.duration = .1f;
            enterAction.AddCondition(AnimatorConditionMode.If, 0, parameter);
            enterAction.AddCondition(AnimatorConditionMode.IfNot, 0, "Dashing");
            Transition(action, free, parameter, false, .1f);
            Transition(action, free, "Dashing", true, .05f);
        }
    }

    private static void AddAttackLayer(AnimatorController controller)
    {
        var source = controller.layers[1];
        var mask = Object.Instantiate(source.avatarMask);
        mask.name = "UpperBody";
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
        AssetDatabase.AddObjectToAsset(mask, controller);
        var machine = new AnimatorStateMachine { name = "MeleeAttack" };
        AssetDatabase.AddObjectToAsset(machine, controller);
        controller.AddLayer(new AnimatorControllerLayer
            { name = "MeleeAttack", defaultWeight = 0, avatarMask = mask, stateMachine = machine });
        var idle = machine.AddState("Idle");
        idle.writeDefaultValues = false;
        machine.defaultState = idle;
        var swing = machine.AddState("Swing");
        swing.writeDefaultValues = false;
        swing.motion = Animation("Sword_Attack");
        swing.timeParameterActive = true;
        swing.timeParameter = "AttackProgress";
        var enter = machine.AddAnyStateTransition(swing);
        enter.canTransitionToSelf = true;
        enter.hasExitTime = false;
        enter.duration = .03f;
        enter.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        enter.AddCondition(AnimatorConditionMode.IfNot, 0, "Dashing");
        var exit = swing.AddTransition(idle);
        exit.hasExitTime = true;
        exit.exitTime = 1;
        exit.duration = .1f;
        Transition(swing, idle, "Dashing", true, .05f);
    }

    private static void Transition(AnimatorState from, AnimatorState to, string parameter, bool value, float duration)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameter);
    }
}
