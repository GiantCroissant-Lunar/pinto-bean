// Unity MonoBehaviour for setting up aspect runtime via profile assets

using UnityEngine;
using Microsoft.Extensions.DependencyInjection;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Unity
{
    /// <summary>
    /// MonoBehaviour component that configures aspect runtime based on Unity profile assets.
    /// This demonstrates how to use Game/Editor profiles to configure different aspect runtimes
    /// for different Unity execution contexts (Editor vs Play mode).
    /// </summary>
    [AddComponentMenu("PintoBean/Aspect Runtime Service Bootstrap")]
    public class AspectRuntimeServiceBootstrap : MonoBehaviour
    {
        [Header("Profile Assets")]
        [Tooltip("Game profile asset to use when in Play mode")]
        [SerializeField] private GameProfileAsset gameProfile;
        
        [Tooltip("Editor profile asset to use when in Editor mode")]
        [SerializeField] private EditorProfileAsset editorProfile;
        
        [Header("Service Configuration")]
        [Tooltip("Enable verbose logging for bootstrap process")]
        [SerializeField] private bool verboseLogging = true;
        
        [Header("Demo Services")]
        [Tooltip("Register demo services for testing aspect runtime")]
        [SerializeField] private bool registerDemoServices = true;

        private IServiceProvider _serviceProvider;
        private bool _isInitialized = false;

        /// <summary>
        /// Gets whether the service provider has been initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the configured service provider.
        /// </summary>
        public IServiceProvider ServiceProvider => _serviceProvider;

        void Awake()
        {
            InitializeServices();
        }

        /// <summary>
        /// Manually triggers service initialization.
        /// </summary>
        [ContextMenu("Initialize Services")]
        public void InitializeServices()
        {
            if (_isInitialized)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning($"[AspectRuntimeServiceBootstrap] Services already initialized on {gameObject.name}");
                }
                return;
            }

            try
            {
                var services = new ServiceCollection();
                
                // Configure aspect runtime based on current Unity mode
                ConfigureAspectRuntime(services);
                
                // Register demo services if enabled
                if (registerDemoServices)
                {
                    RegisterDemoServices(services);
                }
                
                // Build service provider
                _serviceProvider = services.BuildServiceProvider();
                _isInitialized = true;
                
                if (verboseLogging)
                {
                    var aspectRuntime = _serviceProvider.GetRequiredService<IAspectRuntime>();
                    Debug.Log($"[AspectRuntimeServiceBootstrap] ✅ Services initialized on {gameObject.name}. " +
                             $"Aspect Runtime: {aspectRuntime.GetType().Name}");
                }

                // Demonstrate aspect runtime
                if (registerDemoServices)
                {
                    DemonstrateAspectRuntime();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AspectRuntimeServiceBootstrap] Failed to initialize services on {gameObject.name}: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        private void ConfigureAspectRuntime(IServiceCollection services)
        {
            bool isEditorMode = Application.isEditor && !Application.isPlaying;
            bool isPlayMode = Application.isPlaying;
            
            if (verboseLogging)
            {
                string mode = isEditorMode ? "Editor" : (isPlayMode ? "Play" : "Unknown");
                Debug.Log($"[AspectRuntimeServiceBootstrap] Detected Unity mode: {mode}");
            }

            // Select appropriate profile
            if (isEditorMode && editorProfile != null)
            {
                if (verboseLogging)
                {
                    Debug.Log($"[AspectRuntimeServiceBootstrap] Applying Editor profile: {editorProfile.name}");
                    Debug.Log($"[AspectRuntimeServiceBootstrap] Editor aspect runtime type: {editorProfile.AspectRuntimeType}");
                }
                editorProfile.ApplyAspectRuntimeToServices(services);
            }
            else if (isPlayMode && gameProfile != null)
            {
                if (verboseLogging)
                {
                    Debug.Log($"[AspectRuntimeServiceBootstrap] Applying Game profile: {gameProfile.name}");
                    Debug.Log($"[AspectRuntimeServiceBootstrap] Game aspect runtime type: {gameProfile.AspectRuntimeType}");
                }
                gameProfile.ApplyAspectRuntimeToServices(services);
            }
            else
            {
                // Fallback configuration
                string mode = isEditorMode ? "Editor" : "Game";
                if (verboseLogging)
                {
                    Debug.LogWarning($"[AspectRuntimeServiceBootstrap] No {mode} profile assigned, using fallback Unity aspect runtime");
                }
                services.AddUnityAspectRuntime(enableMetrics: true, verboseLogging: false);
            }
        }

        private void RegisterDemoServices(IServiceCollection services)
        {
            services.AddTransient<IDemoService, DemoService>();
            
            if (verboseLogging)
            {
                Debug.Log($"[AspectRuntimeServiceBootstrap] Registered demo services");
            }
        }

        private void DemonstrateAspectRuntime()
        {
            if (!_isInitialized || _serviceProvider == null) return;

            try
            {
                var aspectRuntime = _serviceProvider.GetRequiredService<IAspectRuntime>();
                var demoService = _serviceProvider.GetRequiredService<IDemoService>();

                if (verboseLogging)
                {
                    Debug.Log($"[AspectRuntimeServiceBootstrap] 🎮 Demonstrating aspect runtime logging...");
                }

                // Simulate service call with aspect runtime tracking
                using (var context = aspectRuntime.EnterMethod(typeof(IDemoService), nameof(IDemoService.DoWork), new object[] { "demo" }))
                {
                    try
                    {
                        var result = demoService.DoWork("demo");
                        aspectRuntime.ExitMethod(context, result);
                        
                        // Record custom metric
                        aspectRuntime.RecordMetric("demo.execution", 1.0, ("mode", Application.isPlaying ? "play" : "editor"));
                    }
                    catch (System.Exception ex)
                    {
                        aspectRuntime.RecordException(context, ex);
                        throw;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AspectRuntimeServiceBootstrap] Failed to demonstrate aspect runtime: {ex.Message}");
            }
        }

        void OnDestroy()
        {
            if (_serviceProvider is System.IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    // Demo service interfaces and implementations
    public interface IDemoService
    {
        string DoWork(string input);
    }

    public class DemoService : IDemoService
    {
        public string DoWork(string input)
        {
            // Simulate some work
            System.Threading.Thread.Sleep(10);
            return $"Processed: {input}";
        }
    }
}