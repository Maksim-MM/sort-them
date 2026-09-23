#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

namespace UpscaleSDK.Core.Validator
{
    /// <summary>
    /// Represents a validation result class.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// The errors field.
        /// </summary>
        public readonly List<string> Errors = new();
        /// <summary>
        /// The warnings field.
        /// </summary>
        public readonly List<string> Warnings = new();

        /// <summary>
        /// Gets or sets the is valid.
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// Add error.
        /// </summary>
        /// <param name="message">The message.</param>
        public void AddError(string message)
        {
            Errors.Add(message);
        }

        /// <summary>
        /// Add warning.
        /// </summary>
        /// <param name="message">The message.</param>
        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString()
        {
            return string.Join("\n", Errors.Concat(Warnings));
        }
    }
}
#endif