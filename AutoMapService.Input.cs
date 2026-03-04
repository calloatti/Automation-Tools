using Timberborn.Automation;

namespace Calloatti.AutoTools
{
  public partial class AutoMapService
  {
    public void ToggleAutoMap()
    {
      // 1. The hotkey is the ONLY thing that changes the state
      switch (_currentState)
      {
        case MapDisplayState.Hidden:
          _currentState = MapDisplayState.Global;
          _notificationService.SendNotification("Auto Map: GLOBAL");
          break;
        case MapDisplayState.Global:
          _currentState = MapDisplayState.Single; // This is the Partition mode
          _notificationService.SendNotification("Auto Map: PARTITION");
          break;
        case MapDisplayState.Single:
          _currentState = MapDisplayState.Hidden;
          _notificationService.SendNotification("Auto Map: OFF");
          break;
      }

      // 2. Redraw the map based on the newly selected state
      RefreshVisuals();
    }

    // 3. A single, unified method to draw the correct lines at any time
    public void RefreshVisuals()
    {
      if (_currentState == MapDisplayState.Hidden)
      {
        SetVisibility(false);
        _singleVisualizedAutomator = null;
      }
      else if (_currentState == MapDisplayState.Global)
      {
        _singleVisualizedAutomator = null;
        GenerateSnapshot();
        SetVisibility(true);
      }
      else if (_currentState == MapDisplayState.Single) // Partition mode
      {
        Automator selectedAutomator = null;
        if (_selectionService.IsAnythingSelected)
        {
          selectedAutomator = _selectionService.SelectedObject.GetComponent<Automator>();
        }

        _singleVisualizedAutomator = selectedAutomator;

        if (selectedAutomator != null)
        {
          GenerateSingleSnapshot(selectedAutomator);
          SetVisibility(true);
        }
        else
        {
          SetVisibility(false); // Hide lines until a building is selected
        }
      }
    }
  }
}