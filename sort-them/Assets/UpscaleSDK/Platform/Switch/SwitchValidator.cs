#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UpscaleSDK.Core.Validator;

/// <summary>
/// Represents a switch validator class.
/// </summary>
public class SwitchValidator : Validator
{
    /// <summary>
    /// Gets or sets the targets.
    /// </summary>
    public override BuildTarget[] Targets => new[] { BuildTarget.Switch, BuildTarget.Switch2 };
    
    /// <summary>
    /// Validate.
    /// </summary>
    public override ValidationResult Validate()
    {
        ValidationResult result = new();
        ValidateCompanyName(result);
        ValidateAppVersion(result);
        ValidateIsPackageInstalled(result, "com.unity.inputsystem");
        ValidateIsNewInputEnabled(result);
        ValidateIfSplashScreenEnabled(result);
        ValidateIfGcIncremental(result);
        ValidateScenes(result);
        ValidateSDKConfiguration(result);
            
        ValidateIsPackageInstalled(result, "com.unity.inputsystem.switch");
        
#if UNITY_SWITCH
        ValidateSupportedNpadCount(result);
        ValidateNintendoMetadata(result);
#endif
        return result;
    }
#if UNITY_SWITCH
    private void ValidateSupportedNpadCount(ValidationResult result)
    {
        Validate(result,
            PlayerSettings.Switch.supportedNpadCount == 1,
            "Supported npad count must be set to 1");
    }
    
    private void ValidateNintendoMetadata(ValidationResult result)
    {
        if (string.IsNullOrEmpty(PlayerSettings.Switch.NMETAOverride))
        {
#if !UNITY_6000
            Validate(result,
                PlayerSettings.Switch.applicationID != "0x01004b9000490000",
                "Default application ID mismatch");

            Validate(result,
                PlayerSettings.Switch.startupUserAccount ==
                PlayerSettings.Switch.StartupUserAccount.Required,
                "Startup User Account must be set to Required");

            Validate(result,
                PlayerSettings.Switch.userAccountSaveDataSize > 0,
                "Incorrect save data size (use at least 262144 bytes)");

            Validate(result,
                PlayerSettings.Switch.userAccountSaveDataJournalSize > 0,
                "Incorrect save data journal size (use at least 262144 bytes)");
#else
            Validate(result,
                string.IsNullOrEmpty(PlayerSettings.Switch.NMETAOverride) == false,
                "NMETA file is not set");
#endif
        }
        else
        {
            string path = System.IO.Path.GetFullPath(PlayerSettings.Switch.NMETAOverride);
            string content = System.IO.File.ReadAllText(path);

            Validate(result,
                content.Contains("<ApplicationId>0x01004b9000490000</ApplicationId>") == false,
                "Default application ID mismatch");

            Validate(result,
                content.Contains("<StartupUserAccount>Required</StartupUserAccount>"),
                "Startup User Account must be set to Required");

            Validate(result,
                content.Contains("<UserAccountSaveDataSize>0x0000000000000000</UserAccountSaveDataSize>") == false,
                "Incorrect save data size (use at least 262144 bytes)");

            Validate(result,
                content.Contains("<UserAccountSaveDataJournalSize>0x0000000000000000</UserAccountSaveDataJournalSize>") == false,
                "Incorrect save data journal size (use at least 262144 bytes)");
        }
    }
#endif
    
}
#endif