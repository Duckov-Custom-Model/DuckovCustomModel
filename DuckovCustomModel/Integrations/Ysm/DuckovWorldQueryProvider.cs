using System;
using ModelRuntime;
using ModelRuntime.Adapters;
using UnityEngine.SceneManagement;
using NVector3 = System.Numerics.Vector3;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed class DuckovWorldQueryProvider : IWorldQueryProvider
    {
        public bool TryGetBlock(NVector3 worldPosition, out BlockState block)
        {
            block = default;
            return false;
        }

        public bool TryQuery(string name, ReadOnlySpan<MolangValue> arguments, out MolangValue result)
        {
            result = default;
            if (arguments.Length != 0) return false;
            if (name == "query.duckov_scene")
            {
                result = SceneManager.GetActiveScene().name;
                return true;
            }

            var time = TimeOfDayController.Instance;
            if (time == null) return false;
            switch (name)
            {
                case "query.time_of_day":
                    result = (time.Time % 24 + 24) % 24 / 24.0;
                    return true;
                case "query.duckov_time_hours":
                    result = time.Time;
                    return true;
                case "query.duckov_weather":
                    result = time.CurrentWeather.ToString();
                    return true;
                default: return false;
            }
        }
    }
}
