using Bindito.Core;
using System;
using System.Reflection;
using Timberborn.Automation;
using Timberborn.AutomationUI;
using Timberborn.SelectionSystem;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace Calloatti.AutoTools
{
  public class AutoMapHoverService : ILoadableSingleton, IDisposable
  {
    private readonly EntitySelectionService _selectionService;
    private readonly AutoMapService _mapService;

    // Inject the PUBLIC tool instead of the internal highlighter
    private readonly TransmitterPickerTool _pickerTool;

    private GameObject _hoverContainer;
    private LineRenderer _dynamicLine;

    // Cached reflection data
    private object _highlighterInstance;
    private FieldInfo _hoveredTransmitterField;

    [Inject]
    public AutoMapHoverService(
        EntitySelectionService selectionService,
        AutoMapService mapService,
        TransmitterPickerTool pickerTool)
    {
      _selectionService = selectionService;
      _mapService = mapService;
      _pickerTool = pickerTool;
    }

    public void Load()
    {
      // 1. Double-Jump Reflection
      FieldInfo highlighterField = typeof(TransmitterPickerTool).GetField(
          "_transmitterPickerToolHighlighter",
          BindingFlags.NonPublic | BindingFlags.Instance);

      if (highlighterField != null)
      {
        _highlighterInstance = highlighterField.GetValue(_pickerTool);

        if (_highlighterInstance != null)
        {
          _hoveredTransmitterField = _highlighterInstance.GetType().GetField(
              "_hoveredTransmitter",
              BindingFlags.NonPublic | BindingFlags.Instance);
        }
      }

      // 2. Setup the container & line
      _hoverContainer = new GameObject("AutoMap_HoverLineContainer");
      _dynamicLine = _hoverContainer.AddComponent<LineRenderer>();

      Material lineMat = new Material(Shader.Find("Sprites/Default"));
      lineMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
      lineMat.renderQueue = 4000;

      _dynamicLine.material = lineMat;
      _dynamicLine.startWidth = 0.05f;
      _dynamicLine.endWidth = 0.05f;
      _dynamicLine.useWorldSpace = true;
      _dynamicLine.sortingOrder = 32767;
      _dynamicLine.enabled = false;

      // 3. Attach the Update loop
      var updater = _hoverContainer.AddComponent<HoverLineUpdater>();
      updater.Setup(this, _selectionService, _mapService, _dynamicLine);
    }

    public void Dispose()
    {
      if (_dynamicLine != null && _dynamicLine.material != null)
      {
        UnityEngine.Object.Destroy(_dynamicLine.material);
      }
      if (_hoverContainer != null)
      {
        UnityEngine.Object.Destroy(_hoverContainer);
      }
    }

    public Automator GetHoveredAutomator()
    {
      if (_highlighterInstance != null && _hoveredTransmitterField != null)
      {
        return _hoveredTransmitterField.GetValue(_highlighterInstance) as Automator;
      }
      return null;
    }
  }

  // --- The MonoBehaviour that runs every frame ---
  public class HoverLineUpdater : MonoBehaviour
  {
    private AutoMapHoverService _hoverService;
    private EntitySelectionService _selectionService;
    private AutoMapService _mapService;
    private LineRenderer _dynamicLine;

    // Track what we hovered over last so we don't rebuild the color every single frame
    private Automator _lastHoveredAutomator;

    public void Setup(
        AutoMapHoverService hoverService,
        EntitySelectionService selectionService,
        AutoMapService mapService,
        LineRenderer dynamicLine)
    {
      _hoverService = hoverService;
      _selectionService = selectionService;
      _mapService = mapService;
      _dynamicLine = dynamicLine;
    }

    void Update()
    {
      if (!_selectionService.IsAnythingSelected)
      {
        _dynamicLine.enabled = false;
        return;
      }

      Automator selectedAutomator = _selectionService.SelectedObject.GetComponent<Automator>();
      if (selectedAutomator == null)
      {
        _dynamicLine.enabled = false;
        return;
      }

      Automator hoveredAutomator = _hoverService.GetHoveredAutomator();

      if (hoveredAutomator != null && hoveredAutomator != selectedAutomator)
      {
        _dynamicLine.enabled = true;

        // ONLY update the color gradient if we are looking at a different sensor
        if (hoveredAutomator != _lastHoveredAutomator)
        {
          _lastHoveredAutomator = hoveredAutomator;

          // Grab the color based on the partition ID!
          Color previewColor = _mapService.GetPartitionColor(hoveredAutomator.Partition);

          Gradient gradient = new Gradient();
          gradient.SetKeys(
              new GradientColorKey[] { new GradientColorKey(previewColor, 0.0f), new GradientColorKey(previewColor, 1.0f) },
              new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
          );
          _dynamicLine.colorGradient = gradient;
        }

        Vector3 start = _mapService.GetCenterPosition(selectedAutomator);
        Vector3 end = _mapService.GetCenterPosition(hoveredAutomator);

        float distance = Vector3.Distance(start, end);
        float arcHeight = Mathf.Max(1.2f, distance * 0.2f);
        _dynamicLine.positionCount = 21;

        for (int i = 0; i <= 20; i++)
        {
          float t = i / 20f;
          Vector3 pos = Vector3.Lerp(start, end, t);
          pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
          _dynamicLine.SetPosition(i, pos);
        }
      }
      else
      {
        _dynamicLine.enabled = false;
        _lastHoveredAutomator = null; // Clear it when mouse leaves
      }
    }
  }
}