using System;
using System.Collections.Generic;
using System.Linq;
using Timberborn.Automation;
using Timberborn.AutomationBuildings;
using Timberborn.BatchControl;
using Timberborn.BlockSystem;
using Timberborn.ConstructionSites;
using Timberborn.CoreUI;
using Timberborn.EntityNaming;
using Timberborn.EntitySystem;
using Timberborn.Illumination;
using Timberborn.Localization;
using Timberborn.SelectionSystem;
using Timberborn.SingletonSystem;
using Timberborn.TooltipSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calloatti.AutomationUI
{
  public class AutomationBatchControlTab : BatchControlTab
  {
    private readonly BatchControlRowGroupFactory _rowGroupFactory;
    private readonly VisualElementLoader _visualElementLoader;
    private readonly AutomatorRegistry _automatorRegistry;
    private readonly EntitySelectionService _entitySelectionService;
    private readonly ITooltipRegistrar _tooltipRegistrar;
    private readonly ILoc _loc;

    // Dictionary to instantly map a selected 3D building to its UI row
    private readonly Dictionary<EntityComponent, VisualElement> _rowRoots = new Dictionary<EntityComponent, VisualElement>();

    public override string TabNameLocKey => "Calloatti.AutoTools.Automation.TabName";
    public override string TabImage => "automation";
    public override string BindingKey => "Calloatti.AutoTools.KeyBind.AutomationTab";

    public AutomationBatchControlTab(
        VisualElementLoader visualElementLoader,
        BatchControlDistrict batchControlDistrict,
        EventBus eventBus,
        BatchControlRowGroupFactory rowGroupFactory,
        AutomatorRegistry automatorRegistry,
        EntitySelectionService entitySelectionService,
        ITooltipRegistrar tooltipRegistrar,
        ILoc loc)
        : base(visualElementLoader, batchControlDistrict, eventBus)
    {
      _visualElementLoader = visualElementLoader;
      _rowGroupFactory = rowGroupFactory;
      _automatorRegistry = automatorRegistry;
      _entitySelectionService = entitySelectionService;
      _tooltipRegistrar = tooltipRegistrar;
      _loc = loc;
    }

    protected override IEnumerable<BatchControlRowGroup> GetRowGroups(IEnumerable<EntityComponent> entities)
    {
      // Clear our lookup dictionary every time the tab repopulates
      _rowRoots.Clear();

      HashSet<EntityComponent> validEntities = new HashSet<EntityComponent>(entities);

      var automationBuildings = _automatorRegistry.Automators.Where(automator => {
        EntityComponent entityComponent = automator.GetComponent<EntityComponent>();

        if (!validEntities.Contains(entityComponent)) return false;

        bool hasAnyConnectedInput = automator.InputConnections.Any(c => c.IsConnected);

        if (!automator.IsTransmitter && !hasAnyConnectedInput)
        {
          return false;
        }

        return true;
      });

      var groupedBuildings = automationBuildings.GroupBy(GetPartitionId);

      foreach (var group in groupedBuildings)
      {
        int itemCount = group.Count();

        BatchControlRow headerRow = CreateHeaderRow($"Network ID: {group.Key} ({itemCount})");
        BatchControlRowGroup rowGroup = _rowGroupFactory.CreateUnsorted(headerRow);

        foreach (Automator automator in group)
        {
          EntityComponent entityComponent = automator.GetComponent<EntityComponent>();

          List<IBatchControlRowItem> rowItems = new List<IBatchControlRowItem>();

          // 1. Add the Building Icon 
          rowItems.Add(CreateIsolatedBuildingIcon(entityComponent));

          // 2. Add Lights
          rowItems.AddRange(CreateStateLights(automator));

          // 3. Add Name Label
          rowItems.Add(CreateNameLabel(entityComponent));

          // 4. Add Custom Right-Side Controls (Warnings, Buttons, Usages)
          rowItems.Add(CreateRightSideControls(automator));

          VisualElement rowRoot = _visualElementLoader.LoadVisualElement("Game/BatchControl/BatchControlRow");
          BatchControlRow row = new BatchControlRow(rowRoot, entityComponent, rowItems.ToArray());

          // Save the UI element so we can auto-scroll to it later
          _rowRoots[entityComponent] = rowRoot;

          rowGroup.AddRow(row);
        }

        yield return rowGroup;
      }
    }

    [OnEvent]
    // Listens for building clicks in the 3D world to auto-scroll the UI
    // Listens for building clicks in the 3D world to auto-scroll the UI
    public void OnSelectableObjectSelected(SelectableObjectSelectedEvent evt)
    {
      if (evt.SelectableObject == null) return;

      EntityComponent entity = evt.SelectableObject.GetComponent<EntityComponent>();

      if (entity != null && _rowRoots.TryGetValue(entity, out VisualElement rowRoot))
      {
        VisualElement current = rowRoot.parent;
        while (current != null)
        {
          if (current is ScrollView scrollView)
          {
            // Check if the row is currently pushed outside the visible viewport
            bool isCutOffTop = rowRoot.worldBound.yMin < scrollView.worldBound.yMin;
            bool isCutOffBottom = rowRoot.worldBound.yMax > scrollView.worldBound.yMax;

            // Only hijack the scrollbar if the user actually can't see the row
            if (isCutOffTop || isCutOffBottom)
            {
              float deltaY = rowRoot.worldBound.center.y - scrollView.worldBound.center.y;

              // Unity will safely clamp this if it hits the top or bottom limits!
              scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, scrollView.scrollOffset.y + deltaY);
            }
            break;
          }
          current = current.parent;
        }
      }
    }
    private IBatchControlRowItem CreateIsolatedBuildingIcon(EntityComponent entity)
    {
      VisualElement visualElement = _visualElementLoader.LoadVisualElement("Game/BatchControl/BuildingBatchControlRowItem");
      Button selectButton = visualElement.Q<Button>("Select");
      Image image = selectButton.Q<Image>("Image");

      LabeledEntity labeledEntity = entity.GetComponent<LabeledEntity>();
      if (labeledEntity != null)
      {
        image.sprite = labeledEntity.Image;
        _tooltipRegistrar.Register(image, labeledEntity.DisplayName);
      }

      selectButton.RegisterCallback<ClickEvent>(evt => {
        _entitySelectionService.SelectAndFocusOn(entity);
      });

      return new BuildingIconRowItem(selectButton, entity);
    }

    private IBatchControlRowItem CreateNameLabel(EntityComponent entity)
    {
      string uniqueName = "Unknown Building";
      string baseName = string.Empty;
      bool isEditable = false;

      var namedEntity = entity.GetComponent<NamedEntity>();
      if (namedEntity != null && !string.IsNullOrEmpty(namedEntity.EntityName))
      {
        uniqueName = namedEntity.EntityName;
        isEditable = namedEntity.IsEditable;
      }

      var labeledEntity = entity.GetComponent<LabeledEntity>();
      if (labeledEntity != null && !string.IsNullOrEmpty(labeledEntity.DisplayName))
      {
        baseName = labeledEntity.DisplayName;
      }

      VisualElement nameContainer = new VisualElement();
      nameContainer.style.flexDirection = FlexDirection.Column;
      nameContainer.style.justifyContent = Justify.Center;
      nameContainer.style.marginLeft = 10;
      nameContainer.style.flexGrow = 1;

      Label primaryLabel = new Label(uniqueName);
      primaryLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
      primaryLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
      nameContainer.Add(primaryLabel);

      if (isEditable && !string.IsNullOrEmpty(baseName) && uniqueName != baseName)
      {
        Label secondaryLabel = new Label(baseName);
        secondaryLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        secondaryLabel.style.fontSize = 12f;
        secondaryLabel.style.color = new Color(0.75f, 0.65f, 0.50f);
        secondaryLabel.style.marginTop = -2f;
        nameContainer.Add(secondaryLabel);
      }

      return new GenericVisualElementRowItem(nameContainer);
    }

    private IEnumerable<IBatchControlRowItem> CreateStateLights(Automator automator)
    {
      CustomizableIlluminator selfColorSource = automator.GetComponent<CustomizableIlluminator>();

      Func<bool> selfIsOnGetter;
      string tooltipText;

      if (automator.IsTransmitter)
      {
        selfIsOnGetter = () => automator.UnfinishedState == AutomatorState.On;
        tooltipText = "Output Broadcast";
      }
      else
      {
        Automatable automatable = automator.GetComponent<Automatable>();
        selfIsOnGetter = () => automatable != null && automatable.State == ConnectionState.On;
        tooltipText = "Device State";
      }

      yield return BuildSingleLight(
          () => automator.Enabled,
          selfIsOnGetter,
          selfColorSource,
          tooltipText,
          automator
      );

      foreach (var connection in automator.InputConnections)
      {
        if (connection.IsConnected && connection.Transmitter != null)
        {
          Automator transmitter = connection.Transmitter;
          CustomizableIlluminator colorSource = transmitter.GetComponent<CustomizableIlluminator>();

          string inputTooltip = $"Input: {transmitter.AutomatorName}";

          yield return BuildSingleLight(
              () => transmitter.Enabled,
              () => transmitter.UnfinishedState == AutomatorState.On,
              colorSource,
              inputTooltip,
              transmitter
          );
        }
      }
    }

    private IBatchControlRowItem BuildSingleLight(Func<bool> isEnabledGetter, Func<bool> isOnGetter, CustomizableIlluminator colorSource, string tooltipText, Automator clickTarget)
    {
      VisualElement root = _visualElementLoader.LoadVisualElement("Game/BatchControl/AutomatableBatchControlRowItem");
      Image stateIcon = root.Q<Image>("StateIcon");

      _tooltipRegistrar.Register(stateIcon, tooltipText);

      stateIcon.AddToClassList("clickable");
      stateIcon.RegisterCallback<ClickEvent>(evt => {
        if (clickTarget != null)
        {
          _entitySelectionService.SelectAndFocusOn(clickTarget);
        }
      });

      return new CustomAutomationLightRowItem(root, stateIcon, isOnGetter, isEnabledGetter, colorSource);
    }

    private IBatchControlRowItem CreateRightSideControls(Automator automator)
    {
      Lever lever = automator.GetComponent<Lever>();
      return new RightSideControlsRowItem(automator, lever, _tooltipRegistrar, _loc, _visualElementLoader);
    }

    private string GetPartitionId(Automator automator)
    {
      if (automator.Partition != null)
      {
        return automator.Partition.DebuggingId;
      }
      return "Unpartitioned";
    }

    private BatchControlRow CreateHeaderRow(string headerText)
    {
      VisualElement headerElement = _visualElementLoader.LoadVisualElement("Game/BatchControl/BatchControlHeaderRow");
      headerElement.Q<Label>("Text").text = headerText;
      return new BatchControlRow(headerElement);
    }

    private class GenericVisualElementRowItem : IBatchControlRowItem
    {
      public VisualElement Root { get; }
      public GenericVisualElementRowItem(VisualElement root) { Root = root; }
    }

    private class BuildingIconRowItem : IBatchControlRowItem, IUpdatableBatchControlRowItem
    {
      public VisualElement Root { get; }

      private readonly ConstructionSite _constructionSite;
      private readonly BlockObject _blockObject;
      private readonly VisualElement _constructionWrapper;
      private readonly Label _progressLabel;
      private readonly VisualElement _progressBar;

      public BuildingIconRowItem(Button selectButton, EntityComponent entity)
      {
        Root = selectButton;
        _constructionSite = entity.GetComponent<ConstructionSite>();
        _blockObject = entity.GetComponent<BlockObject>();

        _constructionWrapper = selectButton.Q<VisualElement>("ConstructionWrapper");
        _progressLabel = selectButton.Q<Label>("ProgressText");
        _progressBar = selectButton.Q<VisualElement>("Progress");
      }

      public void UpdateRowItem()
      {
        if (_constructionWrapper == null) return;

        if (_blockObject != null && _blockObject.IsUnfinished && _constructionSite != null)
        {
          _constructionWrapper.style.display = DisplayStyle.Flex;

          float progress = _constructionSite.BuildTimeProgress;
          _progressLabel.text = $"{Mathf.FloorToInt(progress * 100f)}%";
          _progressBar.style.width = new StyleLength(Length.Percent(progress * 100f));
        }
        else
        {
          _constructionWrapper.style.display = DisplayStyle.None;
        }
      }
    }

    private class CustomAutomationLightRowItem : IBatchControlRowItem, IUpdatableBatchControlRowItem
    {
      public VisualElement Root { get; }

      private readonly Image _icon;
      private readonly Func<bool> _isOnGetter;
      private readonly Func<bool> _isEnabledGetter;
      private readonly CustomizableIlluminator _colorSource;

      public CustomAutomationLightRowItem(VisualElement root, Image icon, Func<bool> isOnGetter, Func<bool> isEnabledGetter, CustomizableIlluminator colorSource)
      {
        Root = root;
        _icon = icon;
        _isOnGetter = isOnGetter;
        _isEnabledGetter = isEnabledGetter;
        _colorSource = colorSource;
      }

      public void UpdateRowItem()
      {
        Root.ToggleDisplayStyle(true);
        _icon.visible = true;

        bool isOn = _isOnGetter != null && _isOnGetter();
        bool isEnabled = _isEnabledGetter != null && _isEnabledGetter();

        _icon.EnableInClassList("automation-state-icon--on", isOn);
        _icon.EnableInClassList("automation-state-icon--unfinished", !isEnabled);

        if (_colorSource != null)
        {
          _icon.style.unityBackgroundImageTintColor = _colorSource.IconColor;
        }
        else
        {
          _icon.style.unityBackgroundImageTintColor = Color.white;
        }
      }
    }

    private class RightSideControlsRowItem : IBatchControlRowItem, IUpdatableBatchControlRowItem
    {
      public VisualElement Root { get; }

      private readonly Automator _automator;
      private readonly Lever _lever;
      private readonly Label _warningLabel;
      private readonly Label _usagesLabel;
      private readonly Button _leverButton;
      private readonly ILoc _loc;

      public RightSideControlsRowItem(Automator automator, Lever lever, ITooltipRegistrar tooltipRegistrar, ILoc loc, VisualElementLoader visualElementLoader)
      {
        _automator = automator;
        _lever = lever;
        _loc = loc;

        Root = new VisualElement();
        Root.style.flexDirection = FlexDirection.Row;
        Root.style.alignItems = Align.Center;
        Root.style.marginLeft = 10;
        Root.style.marginRight = 10;

        _warningLabel = new Label("⚠️");
        _warningLabel.style.color = new Color(0.9f, 0.2f, 0.2f);
        _warningLabel.style.marginRight = 10;
        _warningLabel.style.fontSize = 14f;
        _warningLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        tooltipRegistrar.Register(_warningLabel, "Network Conflict / Infinite Loop Detected!");
        Root.Add(_warningLabel);

        if (_lever != null)
        {
          _leverButton = new Button();

          // Apply the exact vanilla CSS classes - these only work if the Tab already has the style sheets loaded
          _leverButton.AddToClassList("entity-panel__text");
          _leverButton.AddToClassList("entity-fragment__button");
          _leverButton.AddToClassList("entity-fragment__button--red");

          // Inline style overrides to fix the width and mimic the 9-slice look
          _leverButton.style.width = 100f;
          _leverButton.style.height = 25f;
          _leverButton.style.minHeight = 25f;
          _leverButton.style.fontSize = 12f;
          _leverButton.style.marginRight = 10f;
          _leverButton.style.marginLeft = 10f;

          // These are the fallback styles in case the USS classes aren't being picked up by your tab
          _leverButton.style.backgroundColor = new Color(0.48f, 0.11f, 0.11f); // Dark red
          _leverButton.style.borderTopColor = new Color(0.74f, 0.63f, 0.40f); // Gold/tan
          _leverButton.style.borderBottomColor = new Color(0.74f, 0.63f, 0.40f);
          _leverButton.style.borderLeftColor = new Color(0.74f, 0.63f, 0.40f);
          _leverButton.style.borderRightColor = new Color(0.74f, 0.63f, 0.40f);
          _leverButton.style.borderTopWidth = 1f;
          _leverButton.style.borderBottomWidth = 1f;
          _leverButton.style.borderLeftWidth = 1f;
          _leverButton.style.borderRightWidth = 1f;

          _leverButton.RegisterCallback<ClickEvent>(evt => {
            _lever.SwitchState(!_lever.IsOn);
          });
          Root.Add(_leverButton);
        }

        _usagesLabel = new Label();
        _usagesLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        _usagesLabel.style.fontSize = 12f;
        _usagesLabel.style.minWidth = 30f;
        _usagesLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        tooltipRegistrar.Register(_usagesLabel, "Usages");
        Root.Add(_usagesLabel);
      }
      public void UpdateRowItem()
      {
        _warningLabel.style.display = _automator.IsCyclicOrBlocked ? DisplayStyle.Flex : DisplayStyle.None;

        if (_automator.IsTransmitter)
        {
          _usagesLabel.text = $"→ {_automator.Usages}";
          _usagesLabel.style.display = DisplayStyle.Flex;
        }
        else
        {
          _usagesLabel.style.display = DisplayStyle.None;
        }

        if (_leverButton != null)
        {
          _leverButton.text = _lever.IsOn ? _loc.T("Building.Lever.SwitchOff") : _loc.T("Building.Lever.SwitchOn");
          _leverButton.SetEnabled(true);
        }
      }
    }
  }
}