using System.Collections.Generic;
using Timberborn.Automation;
using Timberborn.BlockObjectModelSystem;
using Timberborn.BlockSystem;
using Timberborn.Coordinates;
using UnityEngine;
using UnityEngine.Rendering;

namespace Calloatti.AutoTools
{
  public partial class AutoMapService
  {
    private GameObject _masterContainer;
    private readonly List<GameObject> _lineObjects = new List<GameObject>();

    private void InitializeVisuals()
    {
      _masterContainer = new GameObject("AutoMap_MasterContainer");
      _masterContainer.SetActive(false);

      _lineMaterial = new Material(Shader.Find("Sprites/Default"));
      _lineMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
      _lineMaterial.renderQueue = 4000;
    }

    private void GenerateSnapshot()
    {
      ClearLines();
      foreach (Automator transmitter in _automatorRegistry.Transmitters)
      {
        DrawTransmitterConnections(transmitter);
      }
    }

    private void GenerateSingleSnapshot(Automator selectedAutomator)
    {
      ClearLines();
      HashSet<Automator> partitionMembers = GetConnectedPartition(selectedAutomator);
      foreach (Automator member in partitionMembers)
      {
        if (member.IsTransmitter) DrawTransmitterConnections(member);
      }
    }

    private HashSet<Automator> GetConnectedPartition(Automator startNode)
    {
      HashSet<Automator> network = new HashSet<Automator>();
      Queue<Automator> queue = new Queue<Automator>();
      queue.Enqueue(startNode);
      network.Add(startNode);
      while (queue.Count > 0)
      {
        Automator current = queue.Dequeue();
        foreach (AutomatorConnection connection in current.OutputConnections)
        {
          if (connection.Receiver != null && network.Add(connection.Receiver)) queue.Enqueue(connection.Receiver);
        }
        foreach (AutomatorConnection connection in current.InputConnections)
        {
          if (connection.Transmitter != null && network.Add(connection.Transmitter)) queue.Enqueue(connection.Transmitter);
        }
      }
      return network;
    }

    public Color GetPartitionColor(AutomatorPartition partition)
    {
      if (partition == null) return Color.white;

      // Use the partition's internal hash code as the seed.
      // Guarantees the whole network shares the exact same color.
      int seed = partition.GetHashCode();

      // Save Unity's random state so we don't mess up game logic
      var oldState = UnityEngine.Random.state;

      UnityEngine.Random.InitState(seed);
      Color baseColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.9f, 1f);

      UnityEngine.Random.state = oldState; // Restore it!

      return baseColor;
    }

    private void DrawTransmitterConnections(Automator transmitter)
    {
      if (transmitter.OutputConnections.Count == 0) return;

      // Fetch the color based on the partition rather than individual coordinates
      Color baseColor = GetPartitionColor(transmitter.Partition);
      Vector3 startPos = GetCenterPosition(transmitter);

      foreach (AutomatorConnection connection in transmitter.OutputConnections)
      {
        if (connection.Receiver == null) continue;
        CreateLine(startPos, GetCenterPosition(connection.Receiver), baseColor);
      }
    }

    private void CreateLine(Vector3 start, Vector3 end, Color color)
    {
      GameObject lineObj = new GameObject("AutoLine");
      lineObj.transform.SetParent(_masterContainer.transform);
      _lineObjects.Add(lineObj);

      LineRenderer lr = lineObj.AddComponent<LineRenderer>();
      lr.material = _lineMaterial;

      // Thinner lines
      lr.startWidth = 0.05f;
      lr.endWidth = 0.05f;

      lr.useWorldSpace = true;
      lr.sortingOrder = 32767;

      // Constant color and 100% solid (alpha 1.0) the entire way
      Gradient gradient = new Gradient();
      gradient.SetKeys(
          new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
          new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
      );
      lr.colorGradient = gradient;

      float distance = Vector3.Distance(start, end);
      float arcHeight = Mathf.Max(1.2f, distance * 0.2f);
      lr.positionCount = 21;

      for (int i = 0; i <= 20; i++)
      {
        float t = i / 20f;
        Vector3 pos = Vector3.Lerp(start, end, t);
        pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        lr.SetPosition(i, pos);
      }
    }

    public Vector3 GetCenterPosition(Automator automator)
    {
      // 1. Get the official game-provided building center
      var centerComponent = automator.GetComponent<BlockObjectCenter>();
      Vector3 pos = (centerComponent != null)
          ? centerComponent.WorldCenterAtBaseZ
          : automator.GameObject.transform.position;

      // 2. Add the logical grid height so it sits on top of the block's bounding box
      //var blockObject = automator.GetComponent<BlockObject>();
      //if (blockObject != null) pos.y += blockObject.Blocks.Size.z;

      // 3. Add exactly 1 meter higher, as requested
      pos.y += 1.0f;

      return pos;
    }

    private void ClearLines()
    {
      foreach (GameObject obj in _lineObjects) if (obj != null) UnityEngine.Object.Destroy(obj);
      _lineObjects.Clear();
    }

    private void SetVisibility(bool visible)
    {
      if (_masterContainer != null) _masterContainer.SetActive(visible);
    }

    private void OnDispose()
    {
      ClearLines();
      if (_masterContainer != null) UnityEngine.Object.Destroy(_masterContainer);
      if (_lineMaterial != null) UnityEngine.Object.Destroy(_lineMaterial);
    }
  }
}