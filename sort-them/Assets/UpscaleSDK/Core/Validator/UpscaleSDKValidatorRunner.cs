#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UpscaleSDK.Core.Validator
{
    /// <summary>
    /// Represents a upscale sdk validator runner class.
    /// </summary>
    public static class UpscaleSDKValidatorRunner
    {
        /// <summary>
        /// Validate.
        /// </summary>
        /// <param name="target">The target.</param>
        public static ValidationResult Validate(BuildTarget target)
        {
            ValidationResult finalResult = new();

            IEnumerable<Validator> validators =
                TypeCache.GetTypesDerivedFrom<Validator>()
                    .Where(type => !type.IsAbstract && !type.IsInterface)
                    .Select(type => Activator.CreateInstance(type) as Validator)
                    .Where(validator =>
                        validator != null &&
                        validator.Targets.Contains(target));

            var enumerable = validators as Validator[] ?? validators.ToArray();
            foreach (Validator validator in enumerable)
            {
                ValidationResult result = validator.Validate();

                finalResult.Errors.AddRange(result.Errors);
                finalResult.Warnings.AddRange(result.Warnings);
            }

            PrintResult(target, enumerable, finalResult);

            return finalResult;
        }

        private static void PrintResult(BuildTarget target, Validator[] validators, ValidationResult result)
        {
            if(validators.Length == 0)
            {
                Debug.LogError("Can't find any validator for this platform");
                return;
            }
            StringBuilder builder = new();

            builder.AppendLine("========== UpscaleSDK Validation ==========");
            builder.AppendLine($"Platform: {target}");

            builder.AppendLine("Validators:");

            foreach (Validator validator in validators)
            {
                builder.AppendLine($"   • {validator.GetType().Name}");
            }
        
            builder.AppendLine();

            if (result.IsValid && result.Warnings.Count == 0)
            {
                builder.AppendLine("✅ Validation passed successfully");
            }

            if (result.Errors.Count > 0)
            {
                builder.AppendLine($"❌ Errors ({result.Errors.Count})");

                foreach (string error in result.Errors)
                {
                    builder.AppendLine($"   • {error}");
                }

                builder.AppendLine();
            }

            if (result.Warnings.Count > 0)
            {
                builder.AppendLine($"⚠️ Warnings ({result.Warnings.Count})");

                foreach (string warning in result.Warnings)
                {
                    builder.AppendLine($"   • {warning}");
                }

                builder.AppendLine();
            }
        
            builder.AppendLine("=====================================");

            string message = builder.ToString();

            if (result.Errors.Count > 0)
            {
                Debug.LogError(message);
            }
            else if (result.Warnings.Count > 0)
            {
                Debug.LogWarning(message);
            }
            else
            {
                Debug.Log(message);
            }
        }
    }
}
#endif