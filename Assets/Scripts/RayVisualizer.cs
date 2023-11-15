using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Dreamteck.Splines;
using System.Globalization;

public class RayVisualizer : MonoBehaviour
{
    List<Vector3[]> data;
    public GameObject lightObj;
    public GameObject cameraObj;
    public GameObject inst;
    StreamReader sr;

    float time = 0.001f;

    public List<SplineRenderer> renderers;

    // Start is called before the first frame update
    void Start()
    {
        sr = new StreamReader(Application.dataPath + "/points.txt", true);

        data = new List<Vector3[]>();

        while (sr.ReadLine() != null)
        {
                string[] point = sr.ReadLine().Split("/");
                Vector3[] points = new Vector3[10];

                for (int i = 0; i < 10; i++)
                {
                    if (point[i].StartsWith("(") && point[i].EndsWith(")"))
                    {
                        point[i] = point[i].Substring(1, point[i].Length - 2);
                    }

                    string[] sArray = point[i].Split(", ");

                    Vector3 result = new Vector3(
                        float.Parse(sArray[0], CultureInfo.InvariantCulture),
                        float.Parse(sArray[1], CultureInfo.InvariantCulture),
                        float.Parse(sArray[2], CultureInfo.InvariantCulture));

                    points[i] = result;
                }

                data.Add(points);
        }

        int index = 0;

        foreach (var item in data)
        {
            index++;
            if (index % 1000 == 0)
            {
                GameObject copy = GameObject.Instantiate(inst.gameObject);
                SplineComputer spline = copy.AddComponent<SplineComputer>();
                SplinePoint[] points = new SplinePoint[12];

                for (int i = 10; i > 0; i--)
                {
                    points[i] = new SplinePoint();
                    points[i].normal = Vector3.up;
                    points[i].size = 1f;
                    points[i].color = Color.white;
                    points[i].position = item[i-1];
                }
                points[11].position = cameraObj.transform.position;
                points[0].position = lightObj.transform.position;

                spline.SetPoints(points);
                spline.type = Spline.Type.Linear;

                SplineRenderer renderer = copy.AddComponent<SplineRenderer>();
                renderer.spline = spline;
                renderer.size = 0.02f;
                renderers.Add(renderer);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var item in renderers)
        {
            float length = item.spline.CalculateLength();
            item.clipTo = time/length;
        }

        time += Time.deltaTime / 100;
    }
}
