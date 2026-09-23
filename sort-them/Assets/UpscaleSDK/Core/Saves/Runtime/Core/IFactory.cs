namespace UpscaleSDK.Core.Saves
{
    /// <summary>
    /// Defines the contract for the factory.
    /// </summary>
    public interface IFactory<TResult>
    {
        /// <summary>
        /// Create.
        /// </summary>
        public TResult Create();
    }
    
    /// <summary>
    /// Defines the contract for the factory.
    /// </summary>
    public interface IFactory<TParam, TResult>
    {
        /// <summary>
        /// Create.
        /// </summary>
        /// <param name="param">The param.</param>
        public TResult Create(TParam param);
    }
    
    /// <summary>
    /// Defines the contract for the factory.
    /// </summary>
    public interface IFactory<TParam1, TParam2, TResult>
    {
        /// <summary>
        /// Create.
        /// </summary>
        /// <param name="param1">The param1.</param>
        /// <param name="param2">The param2.</param>
        public TResult Create(TParam1 param1, TParam2 param2);
    }
    
    /// <summary>
    /// Defines the contract for the factory.
    /// </summary>
    public interface IFactory<TParam1, TParam2, TParam3, TResult>
    {
        /// <summary>
        /// Create.
        /// </summary>
        /// <param name="param1">The param1.</param>
        /// <param name="param2">The param2.</param>
        /// <param name="param3">The param3.</param>
        public TResult Create(TParam1 param1, TParam2 param2, TParam3 param3);
    }
    
    /// <summary>
    /// Defines the contract for the factory.
    /// </summary>
    public interface IFactory<TParam1, TParam2, TParam3, TParam4, TResult>
    {
        /// <summary>
        /// Create.
        /// </summary>
        /// <param name="param1">The param1.</param>
        /// <param name="param2">The param2.</param>
        /// <param name="param3">The param3.</param>
        /// <param name="param4">The param4.</param>
        public TResult Create(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4);
    }
    
    /// <summary>
    /// Defines the contract for the factory.
    /// </summary>
    public interface IFactory<TParam1, TParam2, TParam3, TParam4, TParam5, TResult>
    {
        /// <summary>
        /// Create.
        /// </summary>
        /// <param name="param1">The param1.</param>
        /// <param name="param2">The param2.</param>
        /// <param name="param3">The param3.</param>
        /// <param name="param4">The param4.</param>
        /// <param name="param5">The param5.</param>
        public TResult Create(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5);
    }
    
    /// <summary>
    /// Defines the contract for the factory.
    /// </summary>
    public interface IFactory<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TResult>
    {
        /// <summary>
        /// Create.
        /// </summary>
        /// <param name="manualSavesFolderName">The manual saves folder name.</param>
        /// <param name="autoSavesFolderName">The auto saves folder name.</param>
        /// <param name="extension">The extension.</param>
        /// <param name="serializator">The serializator.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="processors">The processors.</param>
        public TResult Create(TParam1 manualSavesFolderName, TParam2 autoSavesFolderName, TParam3 extension, TParam4 serializator, TParam5 fileSystem, TParam6 processors);
    }
    
    /// <summary>
    /// Defines the contract for the factory.
    /// </summary>
    public interface IFactory<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TResult>
    {
        /// <summary>
        /// Create.
        /// </summary>
        /// <param name="param1">The param1.</param>
        /// <param name="param2">The param2.</param>
        /// <param name="param3">The param3.</param>
        /// <param name="param4">The param4.</param>
        /// <param name="param5">The param5.</param>
        /// <param name="param6">The param6.</param>
        /// <param name="param7">The param7.</param>
        public TResult Create(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6, TParam7 param7);
    }
}