namespace Plugins.UpscaleSDK.PS4.Runtime.DependencyManagement
{
    /// <summary>
    /// Defines the contract for the dependency.
    /// </summary>
    public interface IDependency<T>
    {
        /// <summary>
        /// Gets or sets the instance.
        /// </summary>
        public static T Instance { get; set; }
    }
}