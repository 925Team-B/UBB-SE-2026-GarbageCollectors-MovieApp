#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Repositories;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Services;
using MovieApp.UI.ViewModels;

namespace MovieApp.UI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// Configures dependency injection for all services and ViewModels.
/// </summary>
public partial class App : Application
{
    private readonly string connString = "Server=.\\SQLEXPRESS;Database=MovieAppDb;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=10;";

    private readonly string mockDataPath;
    private readonly bool useMockData;
    private Window? window;

    public static bool UsingMockData { get; private set; }

    /// <summary>Gets the service provider for dependency injection.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        mockDataPath = Path.Combine(AppContext.BaseDirectory, "Data", "mock-data.json");
        useMockData = !CanConnectToDatabase(connString);
        UsingMockData = useMockData;

        // Configure services
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection, connString, useMockData, mockDataPath);
        Services = serviceCollection.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (!useMockData)
        {
            using var scope = Services.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            initializer.EnsureCreatedAndSeeded();
        }

        window = new MainWindow();
        window.Activate();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Configures all services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    private static void ConfigureServices(IServiceCollection services, string connString, bool useMockData, string mockDataPath)
    {
        if (useMockData)
        {
            services.AddSingleton(sp => new MockAppService(mockDataPath));
            services.AddScoped<ICatalogService>(sp => sp.GetRequiredService<MockAppService>());
            services.AddScoped<IReviewService>(sp => sp.GetRequiredService<MockAppService>());
            services.AddScoped<IPointService>(sp => sp.GetRequiredService<MockAppService>());
            services.AddScoped<IBadgeService>(sp => sp.GetRequiredService<MockAppService>());
            services.AddScoped<IBattleService>(sp => sp.GetRequiredService<MockAppService>());
            services.AddScoped<ICommentService>(sp => sp.GetRequiredService<MockAppService>());
        }
        else
        {
            services.AddTransient<IMovieRepository, MovieRepository>(sp => new MovieRepository(connString));
            services.AddTransient<IUserRepository, UserRepository>(sp => new UserRepository(connString));
            services.AddTransient<IReviewRepository, ReviewRepository>(sp => new ReviewRepository(connString));
            services.AddTransient<ICommentRepository, CommentRepository>(sp => new CommentRepository(connString));
            services.AddTransient<IBattleRepository, BattleRepository>(sp => new BattleRepository(connString));
            services.AddTransient<IBadgeRepository, BadgeRepository>(sp => new BadgeRepository(connString));
            services.AddTransient<IBetRepository, BetRepository>(sp => new BetRepository(connString));
            services.AddTransient<IUserStatsRepository, UserStatsRepository>(sp => new UserStatsRepository(connString));
            services.AddTransient<IUserBadgeRepository, UserBadgeRepository>(sp => new UserBadgeRepository(connString));
            services.AddTransient<DatabaseInitializer>(sp => new DatabaseInitializer(connString));

            // Core services
            services.AddScoped<ICatalogService, CatalogService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IPointService, PointService>();
            services.AddScoped<IBadgeService, BadgeService>();
            services.AddScoped<IBattleService, BattleService>();
            services.AddScoped<ICommentService, CommentService>();
        }

        // External review providers + cache + aggregator service
        services.AddSingleton<ICacheService, LocalFileCacheService>();
        services.AddHttpClient<IExternalReviewProvider, OmdbReviewProvider>();
        services.AddHttpClient<IExternalReviewProvider, NytReviewProvider>();
        services.AddHttpClient<IExternalReviewProvider, GuardianReviewProvider>();
        services.AddTransient<ExternalReviewService>();

        // ViewModels
        services.AddTransient<CatalogViewModel>();
        services.AddTransient<MovieDetailViewModel>();
        services.AddTransient<BattleViewModel>();
        services.AddTransient<ForumViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<MainWindowViewModel>();
    }

    private static bool CanConnectToDatabase(string connectionString)
    {
        try
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            builder.InitialCatalog = "master";
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString);
            connection.Open();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DB CONNECTION FAILED] {ex.Message}");
            return false;
        }
    }
}
