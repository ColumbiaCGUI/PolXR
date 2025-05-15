using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class MeasurementExporter : MonoBehaviour
{
    public string exportFileName = "MeasurementLog.csv";

    public void ExportTickMarks(List<Vector3> tickPositions, Vector3 startPoint, Vector3 endPoint, float totalDistance)
    {
        string path = Path.Combine(Application.persistentDataPath, exportFileName);

        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("StartPoint,EndPoint,TotalDistance");
            writer.WriteLine("StartX,StartY,StartZ,EndX,EndY,EndZ,TotalDistance");
            writer.WriteLine($"{startPoint.x:F4},{startPoint.y:F4},{startPoint.z:F4},{endPoint.x:F4},{endPoint.y:F4},{endPoint.z:F4},{totalDistance:F2}");
            writer.WriteLine("\nTickIndex,PositionX,PositionY,PositionZ");

            for (int i = 0; i < tickPositions.Count; i++)
            {
                Vector3 pos = tickPositions[i];
                writer.WriteLine($"{i},{pos.x:F4},{pos.y:F4},{pos.z:F4}");
            }
        }
        Debug.Log($"Measurement log saved to: {path}");

    }
}