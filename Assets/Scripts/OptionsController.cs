using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OptionsController : MonoBehaviour
{
    [Header("Resolution Settings")]

    Resolution[] resolutions;

    public List<string> GetResolutionOptions()
    {
        resolutions = Screen.resolutions;

        List<string> resOptions = new List<string>();

        foreach (Resolution r in resolutions)
        {
            resOptions.Add(r.width + " by " + r.height + " at " + r.refreshRate);
        }

        return resOptions;
    }

    public List<string> GetQualityOptions()
    {
        return QualitySettings.names.ToList();
    }

    public void SetResolution(int value)
    {
        Screen.SetResolution(resolutions[value].width, resolutions[value].height, Screen.fullScreenMode);
    }

    public void SetScreenMode(int value)
    {
        switch (value)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.fullScreen = true;
                break;

            case 1:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.fullScreen = false;
                break;

            default:
                Debug.LogWarning("Unknown OnScreenModeChange value... " + value);
                break;
        }
    }

    public void SetQuality(int value)
    {
        QualitySettings.SetQualityLevel(value, true);
    }

    public void SetMasterAudio(float value)
    {
        AudioListener.volume = value;
    }
}