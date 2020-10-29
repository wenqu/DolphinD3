﻿using Dolphin.Image;
using Dolphin.Service;
using Dolphin.Ui.Dialog;
using Dolphin.Ui.ViewModel;
using MvvmDialogs.DialogFactories;
using MvvmDialogs.FrameworkDialogs;
using Newtonsoft.Json;
using RestoreWindowPlace;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Unity;
using Unity.Lifetime;
using WK.Libraries.HotkeyListenerNS;

namespace Dolphin.Ui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public WindowPlace WindowPlace { get; private set; }

        private IUnityContainer container = new UnityContainer();

        private Settings LoadSettings()
        {
            var contractResolver = new ShouldSerializeContractResolver();
            var serializerSettings = new JsonSerializerSettings { ContractResolver = contractResolver };

            Settings settings;
            try
            {
                var json = File.ReadAllText("settings.json");

                settings = JsonConvert.DeserializeObject<Settings>(json, serializerSettings);
            }
            catch
            {
                settings = new Settings(true);
            }

            return settings;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            WindowPlace = new WindowPlace("placement.config");

            var settings = LoadSettings();

            #region Register
            container.RegisterInstance(settings);

            container.RegisterInstance(new Player());
            container.RegisterInstance(new World());
            container.RegisterInstance(new Log());
            container.RegisterInstance(new HotkeyListener());

            container.RegisterType<IEventBus, EventBus>(new ContainerControlledLifetimeManager());

            container.RegisterType<IEventPublisher<PlayerInformationChangedEvent>, ExtractPlayerInformationService>("extractPlayerInformation");
            container.RegisterType<IEventPublisher<SkillCanBeCastedEvent>, ExtractSkillInformationService>("extractSkillInformation");
            container.RegisterType<IEventPublisher<SkillRecognitionChangedEvent>, ExtractSkillInformationService>("extractSkillInformation");
            container.RegisterType<IEventPublisher<HotkeyPressedEvent>, HotkeyListenerService>("hotkeyListener");
            container.RegisterType<IEventPublisher<WorldInformationChangedEvent>, ExtractWorldInformationService>("extractWorldInformation");
            container.RegisterType<IEventSubscriber, ExecuteActionService>("macro");
            container.RegisterType<IEventSubscriber, SkillCastingService>("skill");

            container.RegisterType<IExtractInformationService, ExtractPlayerInformationService>("extractPlayerInformation");
            container.RegisterType<IExtractInformationService, ExtractSkillInformationService>("extractSkillInformation");
            container.RegisterType<IExtractInformationService, ExtractWorldInformationService>("extractWorldInformation");

            container.RegisterType<IImageCacheService, ImageCacheService>(new ContainerControlledLifetimeManager());
            container.RegisterType<ICaptureWindowService, CaptureWindowService>();
            container.RegisterType<ICropImageService, CropImageService>();
            container.RegisterType<ILogService, LogService>(new ContainerControlledLifetimeManager());
            container.RegisterType<IModelService, ModelService>();
            container.RegisterType<IResourceService, ResourceService>();
            container.RegisterType<IImageCacheService, ImageCacheService>(new ContainerControlledLifetimeManager());
            container.RegisterType<ISettingsService, SettingsService>(new ContainerControlledLifetimeManager());
            container.RegisterType<IHandleService, HandleService>(new ContainerControlledLifetimeManager());
            container.RegisterType<IActionFinderService, ActionFinderService>();
            container.RegisterType<ITransformService, TransformService>();
            container.RegisterType<IPoolSpotService, PoolSpotService>();
            container.RegisterType<ITravelInformationService, TravelInformationService>();
            container.RegisterType<IConditionFinderService, ConditionFinderService>();

            container.RegisterType<IViewModelBase, MainViewModel>("main");
            container.RegisterType<IViewModelBase, HotkeyTabViewModel>("hotkeyTab");
            container.RegisterType<IViewModelBase, FeatureTabViewModel>("featureTab");
            container.RegisterType<IViewModelBase, LogTabViewModel>("logTab");
            container.RegisterType<IViewModelBase, ChangeHotkeyDialogViewModel>("changeHotkey");
            container.RegisterType<IViewModelBase, SettingsTabViewModel>("settingsTab");
            container.RegisterType<IViewModelBase, OverviewTabViewModel>("overviewTab");

            container.RegisterType<IFrameworkDialogFactory, CustomFrameworkDialogFactory>();
            container.RegisterType<MvvmDialogs.IDialogService, MvvmDialogs.DialogService>();

            container.RegisterType<ActionService, ActionService>(); // Does this work?

            #endregion

            container.AddExtension(new Diagnostic());

            var mainVM = container.Resolve<IViewModelBase>("main");
            MainWindow = new MainWindow { DataContext = mainVM };
            MainWindow.Show();

            var handleService = container.Resolve<IHandleService>();
            Trace.WriteLine(handleService.GetHashCode());
            var logService = container.Resolve<ILogService>();
            var random = new Random();

            handleService.MainLoop("Diablo III64");

            var captureService = container.Resolve<ICaptureWindowService>();
            var extractSkillService = container.Resolve<IExtractInformationService>("extractSkillInformation");
            var extractPlayerService = container.Resolve<IExtractInformationService>("extractPlayerInformation");
            var extractWorldService = container.Resolve<IExtractInformationService>("extractWorldInformation");
            var _settings = container.Resolve<Settings>();

            var task = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        var handle = handleService.GetHandle("Diablo III64");
                        if (handle != IntPtr.Zero && !_settings.IsPaused)//!settingsService.Settings.IsPaused)
                        {
                            using (var image = captureService.CaptureWindow("Diablo III64"))
                            {
                                //var task1 = Task.Run(() => extractSkillService.Extract(image));
                                //var task2 = Task.Run(() => extractPlayerService.Extract(image));
                                //var delayTask = Task.Run(() => Thread.Sleep((int)delay));

                                //await task2;
                                //await task1;
                                //await delayTask;
                                extractSkillService.Extract(image);
                                extractPlayerService.Extract(image);
                                extractWorldService.Extract(image);
                            }
                        }
                        Thread.Sleep((int)_settings.UpdateInterval);
                    }
                    catch (Exception ex)
                    {
                        logService.AddEntry(this, "Caught exception in information extraction mmainloop", Enum.LogLevel.Error, ex);
                    }
                }
            }, TaskCreationOptions.LongRunning);

            container.Resolve<IEventSubscriber>("macro");
            container.Resolve<IEventSubscriber>("skill");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (e.ApplicationExitCode != 2)
            {
                var settings = container.Resolve<Settings>();
                var json = JsonConvert.SerializeObject(settings);
                File.WriteAllText("settings.json", json);
                WindowPlace.Save();
            }
        }

        /*
        var container = new UnityContainer();
        container.RegisterType<IUnityContainer, UnityContainer>();

        container.RegisterInstance(new Log());
        container.RegisterInstance(new Player());
        container.RegisterInstance(new World());
        // container.RegisterInstance(settings);
        container.RegisterInstance(new Settings());

        container.RegisterType<IEventChannel, EventChannel>(new ContainerControlledLifetimeManager());
        container.RegisterType<IExtractInformationService, ExtractPlayerInformationService>("player"); // Generator
        container.RegisterType<IExtractInformationService, ExtractSkillInformationService>("skill"); // Generator
        container.RegisterType<IActionAdministrationService, ActionAdministrationService>(); // Subscriber

        container.RegisterType<ILogService, LogService>(new ContainerControlledLifetimeManager());
        container.RegisterType<ICacheService, CacheService>(new ContainerControlledLifetimeManager());

        container.RegisterType<IResourceService, ResourceService>();
        container.RegisterType<ICaptureWindowService, CaptureWindowService>();
        container.RegisterType<IModelService, ModelService>();
        container.RegisterType<ISettingsService, SettingsService>();
        container.RegisterType<IActionExecutionService, ActionExecutionService>();

        container.RegisterType<ViewModelBase, MainWindowViewModel>("main");
        container.RegisterType<TabViewmodelBase, DataTabViewModel>("data");
        container.RegisterType<TabViewmodelBase, LogTabViewModel>("log");

        var mainVM = container.Resolve<ViewModelBase>("main");
        MainWindow = new MainWindow { DataContext = mainVM };
        MainWindow.Title = "Blub";
        MainWindow.Closed += (_, __) =>
        {
            throw new Exception("Main window closed");
        };
        MainWindow.Show();

        var logService = container.Resolve<ILogService>();
        logService.EntryAdded += (o, e) =>
        {
            if (e.LogLevel == LogLevel.Erorr)
                Console.WriteLine(e.Message);
        };

        var extractSkillService = container.Resolve<IExtractInformationService>("skill");
        var extractPlayerService = container.Resolve<IExtractInformationService>("player");
        var captureService = container.Resolve<ICaptureWindowService>();

        var actionAdministrationService = container.Resolve<IActionAdministrationService>();

        var task = Task.Factory.StartNew(async () =>
        {
            try
            {
                var diabloHandle = IntPtr.Zero;
                while (diabloHandle == IntPtr.Zero)
                {
                    logService.AddEntry(this, "Searching for Diablo64 Process", LogLevel.Info);
                    diabloHandle = WindowHelper.GetHWND();
                    Thread.Sleep(1000);
                }

                logService.AddEntry(this, $"Found Diablo Process!", LogLevel.Info);

                while (true)
                {
                    var image = await captureService.CaptureWindow("Diablo III64");

                    var task1 = extractSkillService.Extract(image);
                    var task2 = extractPlayerService.Extract(image);
                    var delay = Task.Delay(60);

                    await task1;
                    await task2;
                    await delay;
                }
            }
            catch (Exception ex)
            {
                logService.AddEntry(this, $"Caught Exception in Mainloop", LogLevel.Erorr, ex);
            }
            finally
            {
                //var _json = JsonConvert.SerializeObject(settings);
                //File.WriteAllText(_json, "settings.json");
                logService.SaveLog("log.txt");
                // MainWindow.Close();
            }
        }, TaskCreationOptions.LongRunning);
    }*/
    }
}