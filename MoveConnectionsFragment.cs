using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.TooltipSystem;
using UnityEngine.UIElements;
using Timberborn.CoreUI;

namespace Calloatti.MoveConnections
{
  internal class MoveConnectionsFragment : IEntityPanelFragment
  {
    private readonly MoveConnectionsTool _moveConnectionsTool;
    private readonly ITooltipRegistrar _tooltipRegistrar;
    private VisualElement _root;
    private Automator _automator;

    public MoveConnectionsFragment(MoveConnectionsTool moveConnectionsTool, ITooltipRegistrar tooltipRegistrar)
    {
      _moveConnectionsTool = moveConnectionsTool;
      _tooltipRegistrar = tooltipRegistrar;
    }

    public VisualElement InitializeFragment()
    {
      // We don't need a wrapper here because the header containers in EntityPanel.uxml use Row layout by default.
      Button moveButton = new Button(OnButtonClicked);

      // Apply the exact classes used by the Duplicate Settings button to make it fit seamlessly into the header.
      moveButton.AddToClassList("entity-panel__button");
      moveButton.AddToClassList("entity-panel__button--green");

      // Add the icon visual element
      VisualElement icon = new VisualElement();
      icon.AddToClassList("entity-panel__button");
      // Reuse the game's native copy-settings icon
      icon.AddToClassList("duplicate-settings__icon");

      moveButton.Add(icon);
      _root = moveButton;

      // Register the tooltip
      _tooltipRegistrar.Register(_root, "Move Connections");

      _root.ToggleDisplayStyle(false);

      return _root;
    }

    public void ShowFragment(BaseComponent entity)
    {
      _automator = entity.GetComponent<Automator>();
    }

    public void ClearFragment()
    {
      _automator = null;
      _root.ToggleDisplayStyle(false);
    }

    public void UpdateFragment()
    {
      if (_automator != null && _automator.IsTransmitter)
      {
        _root.ToggleDisplayStyle(true);
      }
      else
      {
        _root.ToggleDisplayStyle(false);
      }
    }

    private void OnButtonClicked()
    {
      if (_automator != null)
      {
        _moveConnectionsTool.SwitchTo(_automator);
      }
    }
  }
}