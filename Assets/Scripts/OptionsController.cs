using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OptionsController : MonoBehaviour
{
    [Header("Resolution Settings")]

    Resolution[] resolutions;

    void Awake()
    {
        GetResolutionOptions();
        GetQualityOptions();

        SetResolution(LoadIntSetting("resolution", 0));
        SetQuality(LoadIntSetting("quality", GetQualityOptions().Count - 1));
        SetScreenMode(LoadIntSetting("screen_mode", 0));
        SetMasterAudio(LoadFloatSetting("master_audio", 1.0f));
    }

    void SaveSetting(string name, int value)
    {
        PlayerPrefs.SetInt(name, value);
    }

    void SaveSetting(string name, float value)
    {
        PlayerPrefs.SetFloat(name, value);
    }

    int LoadIntSetting(string name, int defaultValue)
    {
        return PlayerPrefs.GetInt(name, defaultValue);
    }

    float LoadFloatSetting(string name, float defaultValue)
    {
        return PlayerPrefs.GetFloat(name, defaultValue);
    }

    void SaveSettings()
    {
        PlayerPrefs.Save();
    }

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

        SaveSetting("resolution", value);
        SaveSettings();
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

        SaveSetting("screen_mode", value);
        SaveSettings();
    }

    public void SetQuality(int value)
    {
        QualitySettings.SetQualityLevel(value, true);

        SaveSetting("quality", value);
        SaveSettings();
    }

    public void SetMasterAudio(float value)
    {
        AudioListener.volume = value;

        SaveSetting("master_audio", value);
        SaveSettings();
    }
}