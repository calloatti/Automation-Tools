using Bindito.Core;
using System;
using Timberborn.Automation;
using Timberborn.AutomationUI; // Requerido para el acceso directo
using Timberborn.SelectionSystem;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace Calloatti.AutoTools
{
  public class AutoMapHoverService : ILoadableSingleton, IDisposable
  {
    private readonly EntitySelectionService _selectionService;
    private readonly AutoMapService _mapService;
    private readonly TransmitterPickerTool _pickerTool;

    private GameObject _hoverContainer;
    private LineRenderer _dynamicLine;

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
      // Setup del contenedor y la línea
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
      // Acceso directo a los campos anteriormente privados gracias al publicizer
      if (_pickerTool != null && _pickerTool._transmitterPickerToolHighlighter != null)
      {
        return _pickerTool._transmitterPickerToolHighlighter._hoveredTransmitter;
      }
      return null;
    }
  }

  public class HoverLineUpdater : MonoBehaviour
  {
    private AutoMapHoverService _hoverService;
    private EntitySelectionService _selectionService;
    private AutoMapService _mapService;
    private LineRenderer _dynamicLine;
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

        if (hoveredAutomator != _lastHoveredAutomator)
        {
          _lastHoveredAutomator = hoveredAutomator;
          Color previewColor = _mapService.GetPartitionColor(hoveredAutomator.Partition);

          Gradient gradient = new Gradient();
          gradient.SetKeys(
              new GradientColorKey[] { new GradientColorKey(previewColor, 0.0f), new GradientColorKey(previewColor, 1.0f) },
              new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
          );
          _dynamicLine.colorGradient = gradient;

          // Matemáticas movidas aquí adentro para calcular la curva solo cuando cambia el target
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
      }
      else
      {
        _dynamicLine.enabled = false;
        _lastHoveredAutomator = null;
      }
    }
  }
}