using System;
using System.Collections.Generic;
using System.Globalization;
using Duckov;
using ItemStatsSystem;
using ModelRuntime;
using ModelRuntime.Adapters;
using UnityEngine;
using NVector3 = System.Numerics.Vector3;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed class DuckovEntityStateProvider : IEntityStateProvider, IDisposable
    {
        private static readonly Func<CharacterMainControl, bool>? ReadInvisible =
            CreateOptionalBooleanGetter("Invisible");

        private static readonly Func<CharacterMainControl, bool>? ReadBodyInWater =
            CreateOptionalBooleanGetter("BodyInWater");

        private static readonly Func<CharacterMainControl, bool>?
            ReadSwimming = CreateOptionalBooleanGetter("Swimming");

        private static readonly Func<CharacterMainControl, bool>? ReadHeadInWater =
            CreateOptionalBooleanGetter("HeadInWater");

        private static readonly Func<CharacterMainControl, bool>? ReadFootInWater =
            CreateOptionalBooleanGetter("FootInWater");

        private static readonly Func<CharacterMainControl, bool>? ReadOnlyAimForward =
            CreateOptionalBooleanGetter("onlyAimForward");

        private readonly Vector3 aimOriginLocal;
        private readonly Transform body;
        private readonly CharacterMainControl character;
        private readonly ModelDocument? document;
        private readonly string entityId;
        private readonly Health? health;
        private readonly Dictionary<int, string> itemIds = new();
        private readonly CharacterModel originalModel;
        private readonly DuckovYsmBindingProfile profile;
        private long capturedDashSequence;
        private long capturedSwingSequence, capturedHurtSequence;
        private string dashClip = string.Empty;
        private string dashState = string.Empty;
        private bool disposed;
        private double elapsed;
        private ItemAgent_Gun? gun;
        private bool hasPreviousPosition;
        private DuckovItemAgent? heldAgent;
        private double hurtStarted = double.NegativeInfinity;
        private Vector3 previousPosition;
        private float previousYaw;
        private long reloadSequence, dashSequence, renderedShotSequence, renderedReloadSequence, renderedDashSequence;
        private double shotStarted = double.NegativeInfinity;
        private bool swingOffHand;
        private long swingSequence, shootSequence, useSequence, hurtSequence, loadedSequence, deathSequence;
        private double swingStarted = double.NegativeInfinity;
        private double walkDistance;

        public DuckovEntityStateProvider(CharacterMainControl character, CharacterModel originalModel,
            DuckovYsmBindingProfile profile, ModelDocument? document = null, Transform? originalAimSocket = null)
        {
            this.character = character != null ? character : throw new ArgumentNullException(nameof(character));
            this.originalModel = originalModel != null
                ? originalModel
                : throw new ArgumentNullException(nameof(originalModel));
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            this.document = document;
            body = character.modelRoot != null ? character.modelRoot : character.transform;
            aimOriginLocal = body.InverseTransformPoint(originalAimSocket != null
                ? originalAimSocket.position
                : character.transform.position);
            entityId = "duckov:instance/" + character.GetInstanceID().ToString(CultureInfo.InvariantCulture);
            health = character.Health;
            character.OnHoldAgentChanged += OnHoldAgentChanged;
            character.OnActionStartEvent += OnActionStarted;
            originalModel.OnAttackOrShootEvent += OnAttackOrShoot;
            if (health != null)
            {
                health.OnHurtEvent.AddListener(OnHurt);
                health.OnDeadEvent.AddListener(OnDeath);
            }

            OnHoldAgentChanged(character.CurrentHoldItemAgent);
            if (character.CurrentAction != null && character.CurrentAction.Running)
                OnActionStarted(character.CurrentAction);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (!ReferenceEquals(character, null))
            {
                character.OnHoldAgentChanged -= OnHoldAgentChanged;
                character.OnActionStartEvent -= OnActionStarted;
            }

            if (!ReferenceEquals(originalModel, null)) originalModel.OnAttackOrShootEvent -= OnAttackOrShoot;
            if (!ReferenceEquals(health, null))
            {
                health!.OnHurtEvent.RemoveListener(OnHurt);
                health.OnDeadEvent.RemoveListener(OnDeath);
            }

            UnsubscribeGun();
            heldAgent = null;
            gun = null;
        }

        public void Capture(EntityState destination, double deltaSeconds)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DuckovEntityStateProvider));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (character == null)
            {
                destination.Flags = EntityFlags.Dead;
                return;
            }

            var dt = double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) ? 0 : Math.Max(0, deltaSeconds);
            elapsed += dt;
            if (swingSequence != capturedSwingSequence) swingStarted = elapsed;
            if (hurtSequence != capturedHurtSequence) hurtStarted = elapsed;
            if (shootSequence != renderedShotSequence) shotStarted = elapsed;
            if (dashSequence != capturedDashSequence)
            {
                SelectDashAnimation();
                capturedDashSequence = dashSequence;
            }

            capturedSwingSequence = swingSequence;
            capturedHurtSequence = hurtSequence;
            var isPlayer = character == CharacterMainControl.Main;
            destination.Id = entityId;
            destination.Uuid = null;
            destination.Type = isPlayer
                ? "player"
                : character.characterPreset != null
                    ? "duckov:" + character.characterPreset.nameKey
                    : "duckov:character";
            CaptureMovement(destination, dt, isPlayer);
            CaptureAim(destination);

            var bodyInWater = GetOptionalBoolean(ReadBodyInWater);
            var swimming = GetOptionalBoolean(ReadSwimming);
            var headInWater = GetOptionalBoolean(ReadHeadInWater);
            var footInWater = GetOptionalBoolean(ReadFootInWater);
            var flags = EntityFlags.None;
            if (character.IsOnGround) flags |= EntityFlags.OnGround;
            if (bodyInWater == true) flags |= EntityFlags.InWater;
            if (swimming == true) flags |= EntityFlags.Swimming;
            if (character.Running) flags |= EntityFlags.Sprinting;
            if (character.Sleeping) flags |= EntityFlags.Sleeping;
            if (health != null && health.IsDead) flags |= EntityFlags.Dead;
            if (elapsed - hurtStarted < profile.HurtDurationSeconds) flags |= EntityFlags.Attacked;
            var invisible = GetOptionalBoolean(ReadInvisible);
            if (invisible == true) flags |= EntityFlags.Invisible;
            destination.Queries["query.duckov_invisible"] = OptionalBoolean(invisible);
            destination.EyeInWater = headInWater == true;
            destination.Health = health != null ? health.CurrentHealth : 20;
            destination.MaxHealth = health != null ? health.MaxHealth : 20;
            destination.SupportsStatusEffects = false;
            destination.EffectLevels.Clear();
            destination.FoodLevel = character.MaxEnergy > 0
                ? Math.Max(0, Math.Min(20, 20.0 * character.CurrentEnergy / character.MaxEnergy))
                : 20;
            destination.Queries.Remove("ysm.food_level");
            destination.Queries["ysm.armor_value"] = default;
            destination.Queries["ysm.frozen_ticks"] = default;
            destination.Queries["ysm.arrow_count"] = default;
            CaptureItems(destination);
            CaptureActions(destination, ref flags);
            CaptureVehicle(destination, ref flags);
            destination.Flags = flags;
            destination.Queries["query.duckov_hidden"] = new(character.Hidden);
            destination.Queries["query.duckov_body_in_water"] = OptionalBoolean(bodyInWater);
            destination.Queries["query.duckov_swimming"] = OptionalBoolean(swimming);
            destination.Queries["query.duckov_head_in_water"] = OptionalBoolean(headInWater);
            destination.Queries["query.duckov_foot_in_water"] = OptionalBoolean(footInWater);
            destination.Queries["query.duckov_ads"] = new(character.IsInAdsInput);
            destination.Queries["query.duckov_ads_value"] = character.AdsValue;
            destination.Queries["query.duckov_dashing"] = new(character.Dashing);
            destination.Queries["query.duckov_energy"] = character.CurrentEnergy;
            destination.Queries["query.duckov_max_energy"] = character.MaxEnergy;
            destination.Queries["query.duckov_water"] = character.CurrentWater;
            destination.Queries["query.duckov_max_water"] = character.MaxWater;
            destination.Queries["query.duckov_hurt_sequence"] = hurtSequence;
            destination.Queries["query.duckov_death_sequence"] = deathSequence;
        }

        private void CaptureMovement(EntityState state, double dt, bool isPlayer)
        {
            var position = character.transform.position;
            var delta = hasPreviousPosition ? position - previousPosition : Vector3.zero;
            var teleported = delta.sqrMagnitude > profile.TeleportDistance * profile.TeleportDistance;
            if (teleported) delta = Vector3.zero;
            var velocity = character.Velocity;
            state.Position = SourceVector(position);
            state.PositionDelta = SourceVector(delta);
            state.Velocity = SourceVector(velocity);
            state.GroundSpeed = Math.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
            state.LimbSwingAmount = character.AnimationMoveSpeedValue * profile.LimbSwingScale;
            walkDistance += Math.Sqrt(delta.x * delta.x + delta.z * delta.z);
            state.WalkDistance = walkDistance;
            var yaw = body.eulerAngles.y;
            state.YawSpeed = hasPreviousPosition && !teleported && dt > 0
                ? Mathf.DeltaAngle(previousYaw, yaw) / dt
                : 0;
            state.BodyRotation = Vector(body.eulerAngles);
            previousYaw = yaw;
            previousPosition = position;
            hasPreviousPosition = true;

            var input = character.MoveInput;
            var level = LevelManager.Instance;
            var inputManager = level != null ? level.InputManager : null;
            var playerInputActive = isPlayer && inputManager != null
                                             && inputManager.ControllingCharacter == character &&
                                             InputManager.InputActived;
            var localInput = playerInputActive
                ? body.InverseTransformDirection(inputManager!.WorldMoveInput)
                : Vector3.zero;
            state.CancelExtraAnimationInput = playerInputActive && inputManager!.MoveAxisInput.sqrMagnitude > .0004f;
            state.Queries["ysm.input_horizontal"] = localInput.x;
            state.Queries["ysm.input_vertical"] = localInput.z;
            state.Queries["query.duckov_move_x"] = input.x;
            state.Queries["query.duckov_move_z"] = input.z;
            state.Queries["query.duckov_moving"] = new(character.movementControl.Moving);
            state.Queries["query.duckov_teleported"] = new(teleported);
        }

        private void CaptureAim(EntityState state)
        {
            var origin = body.TransformPoint(aimOriginLocal);
            var direction = GetOptionalBoolean(ReadOnlyAimForward) == true || !character.IsAiming()
                ? body.forward
                : character.GetCurrentAimPoint() - origin;
            if (direction.sqrMagnitude < .000001f) direction = body.forward;
            var local = body.InverseTransformDirection(direction.normalized);
            var pitch = -Mathf.Atan2(local.y, Mathf.Sqrt(local.x * local.x + local.z * local.z)) * Mathf.Rad2Deg;
            var yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            state.HeadRotation = new(
                Mathf.Clamp(pitch * profile.HeadPitchSign, -profile.HeadPitchLimit, profile.HeadPitchLimit),
                Mathf.Clamp(yaw * profile.HeadYawSign, -profile.HeadYawLimit, profile.HeadYawLimit));
            state.ViewRotation = new(
                -Mathf.Atan2(direction.y, Mathf.Sqrt(direction.x * direction.x + direction.z * direction.z)) *
                Mathf.Rad2Deg,
                Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
        }

        private void CaptureItems(EntityState state)
        {
            if (heldAgent != character.CurrentHoldItemAgent) OnHoldAgentChanged(character.CurrentHoldItemAgent);
            ClearItem(state.MainHand);
            ClearItem(state.OffHand);
            if (heldAgent != null && heldAgent.isActiveAndEnabled)
                SetItem(IsOffHand(heldAgent) ? state.OffHand : state.MainHand, heldAgent.Item);
            var slots = character.CharacterItem != null ? character.CharacterItem.Slots : null;
            SetItem(state.Helmet, slots?.GetSlot(CharacterEquipmentController.helmatHash)?.Content);
            SetItem(state.Chest, slots?.GetSlot(CharacterEquipmentController.armorHash)?.Content);
            ClearItem(state.Legs);
            ClearItem(state.Boots);
            state.Queries["query.duckov_face_item"] =
                ItemId(slots?.GetSlot(CharacterEquipmentController.faceMaskHash)?.Content);
            state.Queries["query.duckov_backpack_item"] =
                ItemId(slots?.GetSlot(CharacterEquipmentController.backpackHash)?.Content);
            state.Queries["query.duckov_headset_item"] =
                ItemId(slots?.GetSlot(CharacterEquipmentController.headsetHash)?.Content);
        }

        private void CaptureActions(EntityState state, ref EntityFlags flags)
        {
            var attackAge = elapsed - swingStarted;
            var swinging = attackAge < profile.AttackDurationSeconds;
            if (swinging) flags |= swingOffHand ? EntityFlags.SwingingOffHand : EntityFlags.SwingingMainHand;
            state.SwingSequence = swingSequence;
            state.SwingProgress = swinging ? Math.Min(1, Math.Max(0, attackAge / profile.AttackDurationSeconds)) : 0;
            state.AttackProgress = state.SwingProgress;
            var action = character.CurrentAction;
            var actionRunning = action != null && action.Running;
            var usingItem = actionRunning && action is CA_UseItem;
            if (usingItem) flags |= IsOffHand(heldAgent) ? EntityFlags.UsingOffHand : EntityFlags.UsingMainHand;
            state.UseSequence = useSequence;
            state.Queries["query.duckov_action_running"] = new(actionRunning);
            state.Queries["query.duckov_action_progress"] = actionRunning && action is IProgress progress
                ? progress.GetProgress().progress
                : 0;
            state.Queries["query.duckov_using_item"] = new(usingItem);
            state.Queries["query.duckov_shoot_sequence"] = shootSequence;
            state.Queries["query.duckov_loaded_sequence"] = loadedSequence;
            state.Queries["query.duckov_reload_sequence"] = reloadSequence;
            state.Queries["query.duckov_dash_sequence"] = dashSequence;
            var activeGun = gun != null && heldAgent != null && heldAgent.isActiveAndEnabled
                            && heldAgent.Item != null && !IsOffHand(heldAgent)
                ? gun
                : null;
            var reloading = activeGun != null && activeGun.IsReloading();
            state.Queries["query.duckov_reload"] = new(reloading);
            state.Queries["query.duckov_ammo"] = activeGun != null ? activeGun.BulletCount : 0;
            state.Queries["query.duckov_ammo_ratio"] = activeGun != null && activeGun.Capacity > 0
                ? (double)activeGun.BulletCount / activeGun.Capacity
                : 0;
            state.Queries["query.duckov_gun_ready"] = new(activeGun != null && activeGun.BulletCount > 0 && !reloading);
            state.Queries["query.duckov_gun_state"] = activeGun != null ? (int)activeGun.GunState : -1;
            state.Queries["query.duckov_shoot_mode"] =
                activeGun != null ? (int)activeGun.GunItemSetting.triggerMode : -1;

            var holdingGun = activeGun != null;
            var gunType = holdingGun && heldAgent!.Item != null
                                     && profile.ItemCategories.TryGetValue(heldAgent.Item.TypeID, out var category)
                                     && (category == "pistol" || category == "rpg")
                ? category
                : "rifle";
            var selectedDash = string.IsNullOrEmpty(profile.DashAnimation) ? dashClip : Clip(profile.DashAnimation);
            var dashActive = (flags & EntityFlags.Dead) == 0 && !string.IsNullOrEmpty(selectedDash)
                                                             && actionRunning && action is CA_Dash;
            var fallbackRoll = dashActive && document != null
                                          && selectedDash.StartsWith("parcool:roll_", StringComparison.Ordinal)
                                          && YsmModelSource.IsFallbackAnimation(document, selectedDash);
            var dashAction = action as CA_Dash;
            var dashDuration = dashAction?.dashTime ?? 0;
            var motionDuration = fallbackRoll && dashDuration > 0
                ? dashDuration - Math.Min(.15, dashDuration * .3)
                : dashDuration;
            var dashPlaying = dashActive && (!fallbackRoll || dashAction!.GetProgress().current < motionDuration);
            var suppressHold = dashActive;
            var aiming = holdingGun && !suppressHold && character.IsInAdsInput;
            var firing = holdingGun && !suppressHold && elapsed - shotStarted < profile.AttackDurationSeconds;
            state.Queries["ctrl.tac_hold_gun"] = new(holdingGun && !suppressHold);
            state.Queries["ctrl.tac_gun_type"] = holdingGun && !suppressHold ? gunType : string.Empty;
            state.Queries["ctrl.tac_gun_id"] = string.Empty;
            state.Queries["ctrl.tac_is_fire"] = new(firing);
            state.Queries["ctrl.tac_is_aim"] = new(aiming);
            state.Queries["ctrl.tac_is_reload"] = new(holdingGun && !suppressHold && reloading);
            state.Queries["ctrl.tac_is_melee"] = new(false);
            state.Queries["ctrl.tac_is_draw"] = new(false);
            state.Queries["ctrl.tac_fire_mode"] = string.Empty;
            state.ControllerTimeScales.Remove("player.parcool");
            state.ControllerTimeScales.Remove("player.main");
            if (fallbackRoll && dashPlaying && motionDuration > 0
                && document != null && document.Animations.TryGetValue(selectedDash, out var dashAnimation)
                && dashAnimation.Length > 0)
            {
                var dashSlot = string.IsNullOrEmpty(profile.DashAnimation) ? "player.parcool" : "player.main";
                state.ControllerTimeScales[dashSlot] = Math.Min(dashAnimation.Length, .5) / motionDuration;
            }

            state.Queries["ctrl.parcool_state"] = dashPlaying && string.IsNullOrEmpty(profile.DashAnimation)
                ? dashState
                : string.Empty;

            Override(state, "player.fire", profile.FireAnimation,
                !suppressHold && elapsed - shotStarted < ClipDuration(profile.FireAnimation, profile.AttackDurationSeconds),
                shootSequence != renderedShotSequence, LoopMode.Once);
            Override(state, "player.use", profile.ReloadAnimation, !suppressHold && reloading,
                reloadSequence != renderedReloadSequence, LoopMode.Loop);
            var hasParcoolController = document != null && document.Controllers.ContainsKey("player.parcool");
            var parcoolOwnsDash = hasParcoolController && string.IsNullOrEmpty(profile.DashAnimation);
            if (string.IsNullOrEmpty(profile.DashAnimation) && !hasParcoolController)
                Override(state, "player.parcool", selectedDash, dashPlaying,
                    dashSequence != renderedDashSequence, fallbackRoll ? LoopMode.Once : null);
            if (string.IsNullOrEmpty(profile.DashAnimation) && dashPlaying)
                state.ControllerCommands["player.main"] = ControllerCommand.Stop;
            else
                Override(state, "player.main", parcoolOwnsDash ? string.Empty : selectedDash, dashPlaying,
                    dashSequence != renderedDashSequence, fallbackRoll ? LoopMode.Once : null);
            var vehicle = character.controlOtherCharacterAction;
            var riding = vehicle != null && vehicle.Running && vehicle.vehicleControl
                         && vehicle.targetCharacter != null;
            state.ControllerCommands.Remove("player.hold_mainhand");
            state.ControllerCommands.Remove("player.hold_offhand");
            if (suppressHold)
            {
                state.ControllerCommands["player.hold_mainhand"] = ControllerCommand.Stop;
                state.ControllerCommands["player.hold_offhand"] = ControllerCommand.Stop;
            }
            if (holdingGun && !suppressHold && !riding && (flags & (EntityFlags.Dead | EntityFlags.Sleeping)) == 0)
            {
                var grounded = (flags & EntityFlags.OnGround) != 0;
                if (grounded && !dashPlaying)
                {
                    var gait = (flags & EntityFlags.Sprinting) != 0 ? "run"
                        : state.GroundSpeed > .05 ? "walk" : "idle";
                    Override(state, "player.main", Clip("tac:" + gait), true, false, LoopMode.Loop);
                }

                var hold = aiming
                    ? "tac:aim:"
                    : grounded && (flags & EntityFlags.Sprinting) != 0
                        ? "tac:run:"
                        : "tac:hold:";
                Override(state, "player.hold_mainhand", GunClip(hold, gunType), true, false, LoopMode.Loop);
                if (string.IsNullOrEmpty(profile.FireAnimation))
                {
                    var once = reloading ? string.IsNullOrEmpty(profile.ReloadAnimation)
                            ? GunClip("tac:reload:", gunType)
                            : string.Empty
                        : firing ? GunClip(aiming ? "tac:aim:fire:" : "tac:hold:fire:", gunType) : string.Empty;
                    Override(state, "player.fire", once, reloading || firing,
                        reloading ? reloadSequence != renderedReloadSequence : shootSequence != renderedShotSequence,
                        LoopMode.Once);
                }
            }

            renderedShotSequence = shootSequence;
            renderedReloadSequence = reloadSequence;
            renderedDashSequence = dashSequence;
        }

        private void CaptureVehicle(EntityState state, ref EntityFlags flags)
        {
            var control = character.controlOtherCharacterAction;
            var target = control != null ? control.targetCharacter : null;
            state.VehicleId = string.Empty;
            if (control != null && control.Running && control.vehicleControl && target != null)
            {
                flags |= EntityFlags.Riding;
                var preset = target.characterPreset;
                state.VehicleId = preset != null ? "duckov:" + preset.nameKey : "duckov:vehicle";
            }

            state.VehicleCategory = string.Empty;
            state.Queries["query.duckov_is_vehicle"] = new(character.isVehicle);
            state.Queries["query.duckov_riding_vehicle_type"] = character.ridingVehicleType;
        }

        private void Override(EntityState state, string slot, string animation, bool active, bool reload, LoopMode? loop)
        {
            state.ControllerCommands.Remove(slot);
            if (active && !string.IsNullOrEmpty(animation) && document != null &&
                document.Animations.ContainsKey(animation))
                state.ControllerCommands[slot] = new(animation, loop: loop, reload: reload);
        }

        private double ClipDuration(string animation, double fallback)
        {
            return !string.IsNullOrEmpty(animation)
                   && document != null && document.Animations.TryGetValue(animation, out var clip) && clip.Length > 0
                ? clip.Length
                : fallback;
        }

        private string Clip(string name)
        {
            return document != null && document.Animations.ContainsKey(name)
                ? name
                : string.Empty;
        }

        private string GunClip(string prefix, string type)
        {
            return Clip(prefix + type);
        }

        private void SelectDashAnimation()
        {
            var direction = character.Velocity;
            direction.y = 0;
            if (direction.sqrMagnitude < .01f) direction = character.MoveInput;
            var local = body.InverseTransformDirection(direction);
            var suffix = Mathf.Abs(local.x) > Mathf.Abs(local.z)
                ? local.x < 0 ? "left" : "right"
                : local.z < 0
                    ? "back"
                    : "front";
            dashClip = Clip("parcool:roll_" + suffix);
            dashState = "roll_" + suffix;
            if (string.IsNullOrEmpty(dashClip))
            {
                dashClip = Clip("parcool:dodge_" + suffix);
                dashState = "dodge_" + suffix;
            }
        }

        private bool IsOffHand(DuckovItemAgent? agent)
        {
            return agent != null
                   && agent.handheldSocket == HandheldSocketTypes.leftHandSocket &&
                   originalModel.LefthandSocket != null;
        }

        private string ItemId(Item? item)
        {
            if (item == null) return string.Empty;
            if (!itemIds.TryGetValue(item.TypeID, out var id))
                itemIds[item.TypeID] = id = "duckov:item/" + item.TypeID.ToString(CultureInfo.InvariantCulture);
            return id;
        }

        private void SetItem(ItemState state, Item? item)
        {
            ClearItem(state);
            if (item == null) return;
            state.Id = ItemId(item);
            state.Count = item.StackCount;
            state.Durability = item.Durability;
            state.MaxDurability = item.MaxDurability;
            if (profile.ItemCategories.TryGetValue(item.TypeID, out var category)) state.Category = category;
            if (profile.ItemUseAnimations.TryGetValue(item.TypeID, out var useAnimation))
                state.UseAnimation = useAnimation;
            if (profile.ItemTags.TryGetValue(item.TypeID, out var tags) && tags != null)
                foreach (var tag in tags)
                    if (!string.IsNullOrWhiteSpace(tag))
                        state.Tags.Add(tag);
        }

        private static void ClearItem(ItemState state)
        {
            state.Id = state.Category = state.UseAnimation = string.Empty;
            state.Count = 0;
            state.Durability = state.MaxDurability = 0;
            state.Charged = state.Fishing = false;
            state.Tags.Clear();
        }

        private void OnActionStarted(CharacterActionBase action)
        {
            if (action is CA_UseItem) useSequence++;
            if (action is CA_Reload) reloadSequence++;
            if (action is CA_Dash) dashSequence++;
        }

        private void OnAttackOrShoot()
        {
            if (heldAgent is ItemAgent_Gun) return;
            swingSequence++;
            swingStarted = elapsed;
            swingOffHand = IsOffHand(heldAgent);
        }

        private void OnShoot()
        {
            shootSequence++;
            shotStarted = elapsed;
        }

        private void OnLoaded()
        {
            loadedSequence++;
        }

        private void OnHurt(DamageInfo info)
        {
            hurtSequence++;
            hurtStarted = elapsed;
        }

        private void OnDeath(DamageInfo info)
        {
            deathSequence++;
        }

        private void OnHoldAgentChanged(DuckovItemAgent? agent)
        {
            UnsubscribeGun();
            heldAgent = agent;
            gun = agent as ItemAgent_Gun;
            if (gun == null) return;
            gun.OnShootEvent += OnShoot;
            gun.OnLoadedEvent += OnLoaded;
        }

        private void UnsubscribeGun()
        {
            if (ReferenceEquals(gun, null)) return;
            gun!.OnShootEvent -= OnShoot;
            gun.OnLoadedEvent -= OnLoaded;
        }

        private static NVector3 Vector(Vector3 value)
        {
            return new(value.x, value.y, value.z);
        }

        private static NVector3 SourceVector(Vector3 value)
        {
            return new(value.x, value.y, -value.z);
        }

        private static Func<CharacterMainControl, bool>? CreateOptionalBooleanGetter(string name)
        {
            var type = typeof(CharacterMainControl);
            var getter = type.GetProperty(name)?.GetGetMethod();
            if (getter != null && getter.ReturnType == typeof(bool))
                return (Func<CharacterMainControl, bool>)Delegate.CreateDelegate(
                    typeof(Func<CharacterMainControl, bool>), getter);
            var field = type.GetField(name);
            return field != null && field.FieldType == typeof(bool) ? target => (bool)field.GetValue(target)! : null;
        }

        private bool? GetOptionalBoolean(Func<CharacterMainControl, bool>? getter)
        {
            if (getter == null) return null;
            try
            {
                return getter(character);
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        private static MolangValue OptionalBoolean(bool? value)
        {
            return value.HasValue ? new MolangValue(value.Value) : default;
        }
    }
}
