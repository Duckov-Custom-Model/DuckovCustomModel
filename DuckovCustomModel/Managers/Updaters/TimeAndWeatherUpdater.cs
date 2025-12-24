using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class TimeAndWeatherUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized) return;

            var timeOfDayController = TimeOfDayController.Instance;
            if (timeOfDayController == null)
            {
                customControl.SetParameterFloat(CustomAnimatorHash.Time, -1f);
                customControl.SetParameterInteger(CustomAnimatorHash.Weather, -1);
                customControl.SetParameterInteger(CustomAnimatorHash.TimePhase, -1);
                return;
            }

            var time = timeOfDayController.Time;
            customControl.SetParameterFloat(CustomAnimatorHash.Time, time);

            var currentWeather = timeOfDayController.CurrentWeather;
            var weatherValue = (int)currentWeather;
            customControl.SetParameterInteger(CustomAnimatorHash.Weather, weatherValue);

            var currentPhase = timeOfDayController.CurrentPhase.timePhaseTag;
            var timePhaseValue = (int)currentPhase;
            customControl.SetParameterInteger(CustomAnimatorHash.TimePhase, timePhaseValue);
        }
    }
}
