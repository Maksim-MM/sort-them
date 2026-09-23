#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UpscaleSDK.Core.Validator;

/// <summary>
/// Represents a ps4 validator class.
/// </summary>
public class PS4Validator : Validator
{
    /// <summary>
    /// Gets or sets the targets.
    /// </summary>
    public override BuildTarget[] Targets => new[] { BuildTarget.PS4 };
    
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
            
        ValidateIsPackageInstalled(result, "com.unity.inputsystem.ps4");
        ValidateIsPackageInstalled(result, "com.unity.nptoolkit2.ps4");
        ValidateIsPackageInstalled(result, "com.unity.savedata.ps4");
        ValidateIsPackageInstalled(result, "com.unity.render-pipelines.ps4");
        ValidateIsPackageInstalled(result, "com.unity.commondialog.ps4");
        
#if UNITY_PS4
        Validate(result,string.IsNullOrEmpty(PlayerSettings.PS4.BackgroundImagePath) == false, "Background Image is not set");
        Validate(result,string.IsNullOrEmpty(PlayerSettings.PS4.StartupImagePath) == false, "Default Start-up Image is not set");
        Validate(result,string.IsNullOrEmpty(PlayerSettings.PS4.ShareFilePath) == false, "Share Parameter File is not set");
        Validate(result,string.IsNullOrEmpty(PlayerSettings.PS4.npTrophyPackPath) == false, "Trophy Pack (trophy.trp) is not set");
        Validate(result,string.IsNullOrEmpty(PlayerSettings.PS4.NPtitleDatPath) == false, "NP Title ID (nptitle.dat) is not set");
#endif 
        return result;
    }
}
#endif