using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class TimeAndWeatherUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized) return;

            var timeOfDayController = TimeOfDayController.Instance;
            if (timeOfDayController == null)
            {
                control.SetParameterFloat(CustomAnimatorHash.Time, -1f);
                control.SetParameterInteger(CustomAnimatorHash.Weather, -1);
                control.SetParameterInteger(CustomAnimatorHash.TimePhase, -1);
                return;
            }

            var time = timeOfDayController.Time;
            control.SetParameterFloat(CustomAnimatorHash.Time, time);

            var currentWeather = timeOfDayController.CurrentWeather;
            var weatherValue = (int)currentWeather;
            control.SetParameterInteger(CustomAnimatorHash.Weather, weatherValue);

            var currentPhase = timeOfDayController.CurrentPhase.timePhaseTag;
            var timePhaseValue = (int)currentPhase;
            control.SetParameterInteger(CustomAnimatorHash.TimePhase, timePhaseValue);
        }
    }
}
