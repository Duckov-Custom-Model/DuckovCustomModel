using System.Collections.Generic;
using System.Linq;

namespace DuckovCustomModel.Core.Managers
{
    public static class AnimatorParameterUpdaterManager
    {
        private static readonly List<IAnimatorParameterUpdater> RegisteredUpdaters = [];

        public static void Register(IAnimatorParameterUpdater? updater)
        {
            if (updater == null) return;
            if (!RegisteredUpdaters.Contains(updater))
                RegisteredUpdaters.Add(updater);
        }

        public static void Unregister(IAnimatorParameterUpdater? updater)
        {
            if (updater == null) return;
            RegisteredUpdaters.Remove(updater);
        }

        public static void UpdateAll(object? control, object? context)
        {
            if (control == null || context == null) return;

            foreach (var updater in RegisteredUpdaters.OfType<IAnimatorParameterUpdater>())
                updater.UpdateParameters(control, context);
        }
    }
}
