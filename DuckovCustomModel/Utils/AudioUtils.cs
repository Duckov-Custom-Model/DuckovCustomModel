using System;
using Cysharp.Threading.Tasks;
using Duckov;
using FMOD.Studio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Utils
{
    public static class AudioUtils
    {
        public static EventInstance? PlayAudioWithTempObject(string soundPath, Transform parentTransform)
        {
            if (string.IsNullOrEmpty(soundPath)) return null;
            if (parentTransform == null) return null;

            var tempObject = new GameObject("DuckovCustomModel_TempAudioObject")
            {
                transform =
                {
                    position = parentTransform.position,
                },
            };

            var eventInstance = AudioManager.PostCustomSFX(soundPath, tempObject);
            if (eventInstance == null || !eventInstance.Value.isValid())
            {
                Object.Destroy(tempObject);
                return null;
            }

            UniTask.Void(async () =>
            {
                try
                {
                    while (eventInstance.Value.isValid()) await UniTask.NextFrame();
                }
                catch (OperationCanceledException)
                {
                    // Task was canceled, likely due to game shutdown. Safe to ignore.
                }
                catch (Exception ex)
                {
                    ModLogger.LogError("Error while playing audio:");
                    ModLogger.LogException(ex);
                }
                finally
                {
                    if (tempObject != null)
                        Object.Destroy(tempObject);
                }
            });

            return eventInstance;
        }
    }
}
