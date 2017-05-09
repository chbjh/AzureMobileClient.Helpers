namespace AzureMobileClient.Helpers
{
    /// <summary>
    /// Provides an abstraction for determining the App Service Uri
    /// </summary>
    public interface IAzureCloudServiceOptions
    {
        /// <summary>
        /// The App Service Uri (https://myappservice.azurewebsites.net)
        /// </summary>
        string AppServiceEndpoint { get; }
    }
}