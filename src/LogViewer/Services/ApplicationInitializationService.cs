﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationInitializationService.cs" company="Wild Gums">
//   Copyright (c) 2008 - 2015 Wild Gums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace LogViewer.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using Catel;
    using Catel.Data;
    using Catel.IoC;
    using Catel.Logging;
    using Catel.MVVM;
    using Catel.Services;
    using Catel.Windows.Controls;
    using Configuration;
    using MethodTimer;
    using Models;
    using Orc.Analytics;
    using Orc.FilterBuilder.Services;
    using Orc.Squirrel;
    using Orc.WorkspaceManagement;
    using Orchestra.Markup;
    using Orchestra.Services;
    using Settings = LogViewer.Settings;

    public class ApplicationInitializationService : ApplicationInitializationServiceBase
    {
        #region Fields
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IServiceLocator _serviceLocator;
        private readonly ICommandManager _commandManager;
        private readonly ITypeFactory _typeFactory;
        #endregion

        #region Constructors
        public ApplicationInitializationService(ITypeFactory typeFactory, IServiceLocator serviceLocator, ICommandManager commandManager)
        {
            Argument.IsNotNull(() => typeFactory);
            Argument.IsNotNull(() => serviceLocator);
            Argument.IsNotNull(() => commandManager);

            _typeFactory = typeFactory;
            _serviceLocator = serviceLocator;
            _commandManager = commandManager;
        }
        #endregion

        #region Methods
        public override async Task InitializeBeforeCreatingShell()
        {
            // Non-async first
            await RegisterTypes();
            await InitializeFonts();
            await InitializeSettings();

            await RunAndWaitAsync(new Func<Task>[]
            {
                ImprovePerformance,
                InitializeAnalytics,
                //InitializeAutomaticSupport,
                //InitializeFeedback,
                InitializeFilters,
                InitializeWorkspaces,
                CheckForUpdates
            });
        }

        public override async Task InitializeAfterCreatingShell()
        {
            await InitializeCommands();
            await base.InitializeAfterCreatingShell();
        }

        private async Task InitializeCommands()
        {
            _commandManager.CreateCommand(Commands.File.Exit, throwExceptionWhenCommandIsAlreadyCreated: false);

            _commandManager.CreateCommand(Commands.Filter.ResetSearchTemplate, throwExceptionWhenCommandIsAlreadyCreated: false);

            _commandManager.CreateCommand(Commands.Settings.General, throwExceptionWhenCommandIsAlreadyCreated: false);

            _commandManager.CreateCommand(Commands.Help.About, throwExceptionWhenCommandIsAlreadyCreated: false);
        }

        private async Task RegisterTypes()
        {
            var serviceLocator = ServiceLocator.Default;

            serviceLocator.RegisterType<IManageUserDataService, ManageUserDataService>();

            serviceLocator.RegisterType<IConfigurationInitializationService, ConfigurationInitializationService>();
            serviceLocator.RegisterType<IFilterCustomizationService, FilterCustomizationService>();

            serviceLocator.RegisterType<ILogReaderService, LogReaderService>();
            serviceLocator.RegisterType<IFileNodeService, FileNodeService>();
            serviceLocator.RegisterType<IFilterService, FilterService>();
            serviceLocator.RegisterType<IRegexService, RegexService>();
            serviceLocator.RegisterType<IFileBrowserConfigurationService, FileBrowserConfigurationService>();
            serviceLocator.RegisterType<IFileSystemService, FileSystemService>();
            serviceLocator.RegisterType<IFileBrowserService, FileBrowserService>();
            serviceLocator.RegisterType<IFileSystemWatchingService, FileSystemWatchingService>();
            serviceLocator.RegisterType<ILogTableService, LogTableService>();
            serviceLocator.RegisterType<INavigationNodeCacheService, NavigationNodeCacheService>();
            serviceLocator.RegisterType<ILogTableConfigurationService, LogTableConfigurationService>();

            serviceLocator.RegisterTypeAndInstantiate<FileBrowserModel>();

            serviceLocator.RegisterTypeAndInstantiate<UnhandledExceptionWatcher>();
        }

        [Time]
        private async Task ImprovePerformance()
        {
            Log.Info("Improving performance");

            ModelBase.DefaultSuspendValidationValue = true;
            UserControl.DefaultCreateWarningAndErrorValidatorForViewModelValue = false;
            UserControl.DefaultSkipSearchingForInfoBarMessageControlValue = true;
        }

        [Time]
        private async Task InitializeAnalytics()
        {
            Log.Info("Initializing analytics");

            var analyticsConfigurationSynchronizer = _typeFactory.CreateInstance<AnalyticsConfigurationSynchronizer>();
            _serviceLocator.RegisterInstance(analyticsConfigurationSynchronizer);

            var googleAnalyticsService = _serviceLocator.ResolveType<IGoogleAnalyticsService>();
            googleAnalyticsService.AccountId = Analytics.AccountId;

            _serviceLocator.RegisterTypeAndInstantiate<NavigatorConfigurationSynchronizer>();
            _serviceLocator.RegisterTypeAndInstantiate<TimestampVisibilityConfigurationSynchronizer>();
        }

        [Time]
        private async Task InitializeFonts()
        {
            FontImage.RegisterFont("FontAwesome", new FontFamily(new Uri("pack://application:,,,/LogViewer;component/Resources/Fonts/", UriKind.RelativeOrAbsolute), "./#FontAwesome"));

            FontImage.DefaultFontFamily = "FontAwesome";

            FontImage.DefaultBrush = new SolidColorBrush(Color.FromArgb(255, 87, 87, 87));
        }

        [Time]
        private async Task InitializeSettings()
        {
            Log.Info("Initializing settings");

            var configurationInitializationService = _serviceLocator.ResolveType<IConfigurationInitializationService>();
            configurationInitializationService.Initialize();
        }

        [Time]
        private async Task CheckForUpdates()
        {
            Log.Info("Checking for updates");

            var maximumReleaseDate = DateTime.MaxValue;

            var updateService = _serviceLocator.ResolveType<IUpdateService>();
            updateService.Initialize(Settings.Application.AutomaticUpdates.AvailableChannels, Settings.Application.AutomaticUpdates.DefaultChannel,
                Settings.Application.AutomaticUpdates.CheckForUpdatesDefaultValue);

#pragma warning disable 4014
            // Not dot await, it's a background thread
            updateService.HandleUpdates(maximumReleaseDate);
#pragma warning restore 4014
        }

        [Time]
        private async Task InitializeFilters()
        {
            Log.Info("Initializing filters");

            var filterSchemeManager = _serviceLocator.ResolveType<IFilterSchemeManager>();
            filterSchemeManager.Load();
        }

        [Time]
        private async Task InitializeWorkspaces()
        {
            Log.Info("Initializing workspaces");

            var workspaceManager = _serviceLocator.ResolveType<IWorkspaceManager>();
            await workspaceManager.Initialize(defaultWorkspaceName: Workspaces.DefaultWorkspaceName);

            var defaultWorkspace = (from workspace in workspaceManager.Workspaces
                where string.Equals(workspace.Title, Workspaces.DefaultWorkspaceName)
                select workspace).FirstOrDefault();
            if (defaultWorkspace != null)
            {
                defaultWorkspace.CanDelete = false;
                defaultWorkspace.CanEdit = false;
            }
        }
        #endregion
    }
}