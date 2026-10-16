namespace DuckovCustomModel.Utils
{
    internal static class RadialActionInput
    {
        internal static bool MovementRequested(CharacterMainControl character)
        {
            var level = LevelManager.Instance;
            var input = level != null ? level.InputManager : null;
            return character == CharacterMainControl.Main && input != null &&
                   input.ControllingCharacter == character && InputManager.InputActived &&
                   input.MoveAxisInput.sqrMagnitude > .0004f;
        }
    }
}
