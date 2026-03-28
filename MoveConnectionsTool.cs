using System.Linq;
using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.CursorToolSystem;
using Timberborn.InputSystem;
using Timberborn.SelectionSystem;
using Timberborn.ToolSystem;
using Timberborn.ToolSystemUI;
using Timberborn.UISound;

namespace Calloatti.MoveConnections
{
  public class MoveConnectionsTool : ITool, IInputProcessor, IToolDescriptor, IGroupIgnoringTool
  {
    private readonly InputService _inputService;
    private readonly SelectableObjectRaycaster _selectableObjectRaycaster;
    private readonly ToolService _toolService;
    private readonly CursorService _cursorService;
    private readonly UISoundController _uiSoundController;

    private Automator _originAutomator;

    public MoveConnectionsTool(
        InputService inputService,
        SelectableObjectRaycaster selectableObjectRaycaster,
        ToolService toolService,
        CursorService cursorService,
        UISoundController uiSoundController)
    {
      _inputService = inputService;
      _selectableObjectRaycaster = selectableObjectRaycaster;
      _toolService = toolService;
      _cursorService = cursorService;
      _uiSoundController = uiSoundController;
    }

    public void SwitchTo(Automator originAutomator)
    {
      _originAutomator = originAutomator;
      _toolService.SwitchTool(this);
    }

    public void Enter()
    {
      _inputService.AddInputProcessor(this);
      // Uses the standard vanilla picking cursor icon
      _cursorService.SetCursor("PickObjectCursor");
    }

    public void Exit()
    {
      _inputService.RemoveInputProcessor(this);
      _cursorService.ResetCursor();
      _originAutomator = null;
    }

    public bool ProcessInput()
    {
      if (_toolService.ActiveTool != this) return false;

      if (_inputService.MainMouseButtonDown && !_inputService.MouseOverUI)
      {
        if (_selectableObjectRaycaster.TryHitSelectableObject(out var hitObject))
        {
          Automator destination = hitObject.GetComponent<Automator>();

          // Ensure we clicked a valid transmitter that isn't the one we started with
          if (destination != null && destination.IsTransmitter && destination != _originAutomator)
          {
            MoveAllConnections(_originAutomator, destination);
            _uiSoundController.PlayClickSound();
            _toolService.SwitchToDefaultTool();
            return true;
          }
        }

        // If they clicked invalid terrain or a non-transmitter, play the error sound
        _uiSoundController.PlayCantDoSound();
      }

      return false;
    }

    private void MoveAllConnections(Automator origin, Automator destination)
    {
      // We MUST ToList() this because calling Connect() modifies the origin's OutputConnections collection mid-loop!
      var connectionsToMove = origin.OutputConnections.ToList();

      foreach (AutomatorConnection connection in connectionsToMove)
      {
        connection.Connect(destination);
      }
    }

    public ToolDescription DescribeTool()
    {
      return new ToolDescription.Builder()
          .AddPrioritizedSection("Select destination building to move connections to.")
          .Build();
    }
  }
}