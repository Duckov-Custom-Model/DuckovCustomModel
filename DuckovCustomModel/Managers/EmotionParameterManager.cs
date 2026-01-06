using System;

namespace DuckovCustomModel.Managers
{
    public static class EmotionParameterManager
    {
        public static event Action<int, int>? OnEmotionParametersChanged;

        public static void NotifyEmotionParametersChanged(int value1, int value2)
        {
            OnEmotionParametersChanged?.Invoke(value1, value2);
        }
    }
}
