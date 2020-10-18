﻿using Dolphin.Enum;
using Dolphin.EventBus;
using Dolphin.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Unity;
using Unity.Lifetime;

namespace Dolphin.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly UnityContainer container;

        public App() : base()
        {
            //var json = File.ReadAllText("settings.json");
            // var settings = JsonConvert.DeserializeObject<Settings>(json);
            #region Register
            container = new UnityContainer();
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
            #endregion
        }

        protected override void OnStartup(StartupEventArgs ev)
        {
            base.OnStartup(ev);

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
                    while (true)
                    {
                        var image = await captureService.CaptureWindow("Diablo III64");

                        var task1 = extractSkillService.Extract(image);
                        var task2 = extractPlayerService.Extract(image);
                        var delay = Task.Delay(100);

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
        }
    }
}
