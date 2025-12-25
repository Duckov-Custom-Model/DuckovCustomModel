using System;
using System.Collections.Generic;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers
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

        public static void UpdateAll(CustomAnimatorControl? control)
        {
            if (control == null) return;

            foreach (var updater in RegisteredUpdaters)
                updater.UpdateParameters(control);
        }

        public static void Cleanup()
        {
            foreach (var updater in RegisteredUpdaters)
                if (updater is IDisposable disposable)
                    disposable.Dispose();

            RegisteredUpdaters.Clear();
        }
    }
}
