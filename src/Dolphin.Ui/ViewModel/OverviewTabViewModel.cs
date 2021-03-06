﻿using Dolphin.Enum;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Dolphin.Ui.ViewModel
{
    public class OverviewTabViewModel : ViewModelBase, IEventSubscriber
    {
        #region Private Fields

        private readonly IEventBus eventBus;
        private readonly IHandleService handleService;
        private readonly IModelService modelService;
        private readonly IStartProcessService processService;
        private readonly ISettingsService settingsService;
        private ICommand negateSuspendedCommand;

        #endregion Private Fields

        #region Public Constructors

        public OverviewTabViewModel(IEventBus eventBus, IModelService modelService, IHandleService handleService, ISettingsService settingsService, IStartProcessService processService)
        {
            this.eventBus = eventBus;
            this.modelService = modelService;
            this.handleService = handleService;
            this.settingsService = settingsService;
            this.processService = processService;

            var playerInformationChanged = new Subscription<PlayerInformationChangedEvent>(OnPlayerInformationChanged);
            var skillRecognitionChanged = new Subscription<SkillRecognitionChangedEvent>(OnSkillRecognitionChanged);
            var worldInformationChanged = new Subscription<WorldInformationChangedEvent>(OnWorldInformationChanged);
            var skillCanBeCasted = new Subscription<SkillCanBeCastedEvent>(OnSkillCanBeCasted);
            var hotkeyPressed = new Subscription<HotkeyPressedEvent>(OnHotkeyPressed);

            SubscribeBus(playerInformationChanged);
            SubscribeBus(skillRecognitionChanged);
            SubscribeBus(worldInformationChanged);
            SubscribeBus(skillCanBeCasted);
            SubscribeBus(hotkeyPressed);

            handleService.HandleStatusChanged += OnHandleChanged;

            CurrentSkills = new ObservableCollection<string>
            {
                "pack://application:,,,/Resource/Skill/EmptyFrame.png",
                "pack://application:,,,/Resource/Skill/EmptyFrame.png",
                "pack://application:,,,/Resource/Skill/EmptyFrame.png",
                "pack://application:,,,/Resource/Skill/EmptyFrame.png",
                "pack://application:,,,/Resource/Skill/EmptyFrame.png",
                "pack://application:,,,/Resource/Skill/EmptyFrame.png",
            };

            CurrentSkillState = new ObservableCollection<string> { "Cant cast", "Cant cast", "Cant cast", "Cant cast", "Cant cast", "Cant cast" };
            CurrentActiveState = new ObservableCollection<string> { "Not active", "Not active", "Not active", "Not active", "Not active", "Not active", };
            CurrentPlayerClass = "pack://application:,,,/Resource/Skill/EmptyFrame.png";
            CurrentHealth = 0;
            CurrentPrimaryResource = 0;
            CurrentPrimaryResource = 0;

            var handle = handleService.GetHandle("Diablo III64");

            DiabloProcessId = handle != null ? handle.ProcessId : 0;
            DiabloClientRectangle = handle != null ? $"{handle.ClientRectangle.Width}x{handle.ClientRectangle.Height}" : "0x0";

            SkillIndexSuspensionStatus = new ObservableCollection<bool>(settingsService.SmartFeatureSettings.SkillSuspensionStatus);
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<string> CurrentActiveState { get; set; }

        public int CurrentHealth { get; set; }

        public WorldLocation CurrentLocation { get; set; }

        public string CurrentPlayerClass { get; set; }

        public int CurrentPrimaryResource { get; set; }

        public int CurrentSecondaryResource { get; set; }

        public ObservableCollection<string> CurrentSkills { get; set; }

        public ObservableCollection<string> CurrentSkillState { get; set; }

        public string DiabloClientRectangle { get; set; }

        public uint DiabloProcessId { get; set; }

        public string SmartFeaturesSupported { get; set; }

        public ICommand NegateSuspendedCommand
        {
            get
            {
                negateSuspendedCommand = negateSuspendedCommand ?? new RelayCommand((o) => NegateSuspended(int.Parse((string)o)));
                return negateSuspendedCommand;
            }
        }

        public Window OpenWindow { get; set; }

        public ObservableCollection<bool> SkillIndexSuspensionStatus { get; set; }

        public ICommand StartProcessCommand => new RelayCommand(o =>
        {
            settingsService.UiSettings.ExecuteablePaths.TryGetValue((string)o, out var path);

            if (!string.IsNullOrEmpty(path))
            {
                processService.Start(path);
            }
        });

        #endregion Public Properties

        #region Private Methods

        private string GetResourcePath(SkillName skillName)
        {
            if (skillName == SkillName.None)
            {
                return "pack://application:,,,/Resource/Skill/EmptyFrame.png";
            }
            else
            {
                return $"pack://application:,,,/Resource/Skill/{skillName}.png";
            }
        }

        private string GetResourcePath(PlayerClass playerClass)
        {
            if (playerClass == PlayerClass.None)
            {
                return "pack://application:,,,/Resource/Skill/EmptyFrame.png";
            }
            else
            {
                return $"pack://application:,,,/Resource/Class/{playerClass}.png";
            }
        }

        private void NegateSuspended(int index)
        {
            var newStatus = !SkillIndexSuspensionStatus[index];
            SkillIndexSuspensionStatus[index] = newStatus;
            settingsService.SmartFeatureSettings.SkillSuspensionStatus[index] = newStatus;
        }

        private void OnHandleChanged(object o, HandleChangedEventArgs e)
        {
            if (e.ProcessName == "Diablo III64")
            {
                var newHandle = e.NewHandle;
                var rect = newHandle.ClientRectangle;

                PropertySetter(newHandle.ProcessId, nameof(DiabloProcessId));
                PropertySetter($"{rect.Width}x{rect.Height}", nameof(DiabloClientRectangle));

                if (VersionInformation.SupportedResolutions.Contains(rect.Size))
                    PropertySetter("", nameof(SmartFeaturesSupported));
                else
                    PropertySetter("Smart features not supported for resolution.", nameof(SmartFeaturesSupported));
            }
        }

        private void OnHotkeyPressed(object sender, HotkeyPressedEvent e)
        {
            var action = settingsService.GetActionName(e.PressedHotkey);
            if (action.IsSuspensionAction())
            {
                NegateSuspended(int.Parse(action.ToString().Last().ToString()));
            }
        }

        private void OnPlayerInformationChanged(object o, PlayerInformationChangedEvent @event)
        {
            if (@event.ChangedProperties.Contains(nameof(Player.Class)))
            {
                var resource = GetResourcePath(modelService.Player.Class);
                if (CurrentPlayerClass != resource)
                {
                    CurrentPlayerClass = resource;
                    RaisePropertyChanged(nameof(CurrentPlayerClass));
                }
            }

            // TODO: maybe its worth to filter these performance wise

            CurrentHealth = modelService.Player.HealthPercentage;
            CurrentPrimaryResource = modelService.Player.PrimaryResourcePercentage;
            CurrentSecondaryResource = modelService.Player.SecondaryRessourcePercentage;

            PropertySetter(modelService.Player.HealthPercentage, nameof(CurrentHealth));

            RaisePropertyChanged(nameof(CurrentHealth));
            RaisePropertyChanged(nameof(CurrentPrimaryResource));
            RaisePropertyChanged(nameof(CurrentSecondaryResource));
        }

        private void OnSkillCanBeCasted(object o, SkillCanBeCastedEvent @event)
        {
            var skill = modelService.GetSkill(@event.SkillIndex);
            CurrentSkillState[@event.SkillIndex] = "Can cast";
            CurrentActiveState[@event.SkillIndex] = skill.IsActive ? "Active" : "Not active";
        }

        private void OnSkillRecognitionChanged(object o, SkillRecognitionChangedEvent @event)
        {
            var newResourcePath = GetResourcePath(@event.NewSkillName);
            if (CurrentSkills[@event.Index] != newResourcePath)
            {
                CurrentSkills[@event.Index] = newResourcePath;
                CurrentSkillState[@event.Index] = @event.NewSkillName == SkillName.None ? "" : "Cant cast";
            }
        }

        private void OnWorldInformationChanged(object o, WorldInformationChangedEvent @event)
        {
            if (@event.IsLocationChanged)
            {
                PropertySetter(@event.NewLocation, nameof(CurrentLocation));
            }

            if (@event.IsWindowChanged)
            {
                PropertySetter(@event.NewOpenWindow, nameof(OpenWindow));
            }
        }

        private void SubscribeBus<T>(Subscription<T> subscription) where T : IEvent
        {
            eventBus.Subscribe(subscription);
        }

        #endregion Private Methods
    }
}