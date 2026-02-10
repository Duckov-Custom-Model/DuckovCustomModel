using System;
using System.Collections.Generic;

namespace DuckovCustomModel.Data
{
    public static class CharacterActionDefinitions
    {
        private static readonly Dictionary<Type, int> ActionTypes = [];

        static CharacterActionDefinitions()
        {
            RegisterActionType<Action_Fishing>(1);
            RegisterActionType<Action_FishingV2>(2);
            RegisterActionType<CA_Attack>(3);
            RegisterActionType<CA_Carry>(4);
            RegisterActionType<CA_Dash>(5);
            RegisterActionType<CA_Interact>(6);
            RegisterActionType<CA_Reload>(7);
            RegisterActionType<CA_Skill>(8);
            RegisterActionType<CA_UseItem>(9);
            RegisterActionType<CA_ControlOtherCharacter>(10);
        }

        public static IReadOnlyDictionary<Type, int> GetActionTypes()
        {
            return ActionTypes;
        }

        public static void RegisterActionType<T>(int id) where T : CharacterActionBase
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id), "Action type ID must be non-negative.");
            ActionTypes[typeof(T)] = id;
        }

        public static void RegisterActionType(Type actionType, int id)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id), "Action type ID must be non-negative.");
            ActionTypes[actionType] = id;
        }

        public static int GetActionTypeId<T>() where T : CharacterActionBase
        {
            return ActionTypes.GetValueOrDefault(typeof(T), -1);
        }

        public static int GetActionTypeId(Type actionType)
        {
            return ActionTypes.GetValueOrDefault(actionType, -1);
        }

        public static void UnregisterActionType<T>() where T : CharacterActionBase
        {
            ActionTypes.Remove(typeof(T));
        }

        public static void UnregisterActionType(Type actionType)
        {
            ActionTypes.Remove(actionType);
        }

        public static void ClearActionTypes()
        {
            ActionTypes.Clear();
        }
    }
}
