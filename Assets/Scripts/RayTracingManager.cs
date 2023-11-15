using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using System.IO;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingManager : MonoBehaviour
{
	// Raytracer is currently *very* slow, so limit the number of triangles allowed per mesh
	public const int TriangleLimit = 3000;

	[Header("Ray Tracing Settings")]
	[SerializeField, Range(0, 32)] int maxBounceCount = 4;
	[SerializeField, Range(0, 64)] int numRaysPerPixel = 2;
	[SerializeField, Min(0)] float defocusStrength = 0;
	[SerializeField, Min(0)] float divergeStrength = 0.3f;
	[SerializeField, Min(0)] float focusDistance = 1;
	[SerializeField, Min(0)] float lightdst = 1;
	[SerializeField] EnvironmentSettings environmentSettings;

	[Header("View Settings")]
	[SerializeField] bool useShaderInSceneView;
	[SerializeField] bool visMode;
	[Header("References")]
	[SerializeField] Shader rayTracingShader;
	[SerializeField, HideInInspector] Shader accumulateShader;

	[Header("Info")]
	[SerializeField] int numRenderedFrames;
	[SerializeField] int numMeshChunks;
	[SerializeField] int numTriangles;

	// Materials and render textures
	Material rayTracingMaterial;
	Material accumulateMaterial;
	RenderTexture resultTexture;

	// Buffers
	ComputeBuffer sphereBuffer;
	ComputeBuffer triangleBuffer;
	ComputeBuffer meshInfoBuffer;

	List<Triangle> allTriangles;
	List<MeshInfo> allMeshInfo;

	RenderTexture target;

	ComputeBuffer compute_buffer;
	Vector3[] data;

	public struct Values
    {
		public float dst;
		public float bounces;
		public Vector3 hitPoint1;
		public Vector3 hitPoint2;
		public Vector3 hitPoint3;
		public Vector3 hitPoint4;
		public Vector3 hitPoint5;
		public Vector3 hitPoint6;
		public Vector3 hitPoint7;
		public Vector3 hitPoint8;
		public Vector3 hitPoint9;
		public Vector3 hitPoint10;
	}

	Values[] data2;

	StreamWriter sr;
	StreamWriter sr2;

	void Start()
	{
		data2 = new Values[Screen.width * Screen.height];
		compute_buffer = new ComputeBuffer(data2.Length, sizeof(float) * 32, ComputeBufferType.Default);
		numRenderedFrames = 0;
	}

    // Called after any camera (e.g. game or scene camera) has finished rendering into the src texture
    void OnRenderImage(RenderTexture src, RenderTexture target)
	{
		bool isSceneCam = Camera.current.name == "SceneCamera";

		if (isSceneCam)
        {
			if (useShaderInSceneView)
			{
				InitFrame();
				Graphics.Blit(null, target, rayTracingMaterial);
			}
			else
			{
				Graphics.Blit(src, target); // Draw the unaltered camera render to the screen
			}
		}
        else if(Application.isPlaying)
        {
			//InitFrame();
			//Graphics.Blit(null, target, rayTracingMaterial);

			for (int i = 0; i < 1; i++)
			{
				InitFrame();

				if (!System.IO.File.Exists(Application.dataPath + "/data2.txt"))
				{
					Graphics.ClearRandomWriteTargets();
					rayTracingMaterial.SetPass(0);
					rayTracingMaterial.SetBuffer("data", compute_buffer);
					Graphics.SetRandomWriteTarget(1, compute_buffer, false);
				}

				// Create copy of prev frame
				RenderTexture prevFrameCopy = RenderTexture.GetTemporary(src.width, src.height, 0, ShaderHelper.RGBA_SFloat);
				Graphics.Blit(resultTexture, prevFrameCopy);

				// Run the ray tracing shader and draw the result to a temp texture
				rayTracingMaterial.SetInt("Frame", numRenderedFrames);
				RenderTexture currentFrame = RenderTexture.GetTemporary(src.width, src.height, 0, ShaderHelper.RGBA_SFloat);
				Graphics.Blit(null, currentFrame, rayTracingMaterial);

				if (!System.IO.File.Exists(Application.dataPath + "/data2.txt"))
				{
					compute_buffer.GetData(data2);

					float maxdst = 0;
					float maxbounces = 0;
					Vector3[] hitPoints = new Vector3[10];

					foreach (Values item in data2)
					{
						if (maxdst < item.dst)
						{
							maxdst = item.dst;
							maxbounces = item.bounces;
							hitPoints[0] = item.hitPoint1;
							hitPoints[1] = item.hitPoint2;
							hitPoints[2] = item.hitPoint3;
							hitPoints[3] = item.hitPoint4;
							hitPoints[4] = item.hitPoint5;
							hitPoints[5] = item.hitPoint6;
							hitPoints[6] = item.hitPoint7;
							hitPoints[7] = item.hitPoint8;
							hitPoints[8] = item.hitPoint9;
							hitPoints[9] = item.hitPoint10;
						}
					}

					sr = new StreamWriter(Application.dataPath + "/data2.txt", true);

					sr.WriteLine(maxdst + " " + maxbounces);

					for (int j = 0; j < 10; j++)
					{
						sr.WriteLine(hitPoints[j].x + " " + hitPoints[j].y + " " + hitPoints[j].z);
					}

					sr.WriteLine("");

					foreach (Values item in data2)
					{
						sr.WriteLine(item.dst + " " + item.bounces);
					}

					sr.Close();

					compute_buffer.Release();
					compute_buffer.Dispose();
				}

				if (!System.IO.File.Exists(Application.dataPath + "/points.txt"))
				{
					sr2 = new StreamWriter(Application.dataPath + "/points.txt", true);

					foreach (Values item in data2)
					{
						sr2.WriteLine(item.hitPoint1 + "/" + item.hitPoint2 + "/" + item.hitPoint3 + "/" +
							item.hitPoint4 + "/" + item.hitPoint5 + "/" + item.hitPoint6 + "/" + item.hitPoint7 + "/" +
							item.hitPoint8 + "/" + item.hitPoint9 + "/" + item.hitPoint10);
					}

					sr2.Close();
				}

				// Accumulate
				accumulateMaterial.SetInt("_Frame", numRenderedFrames);
				accumulateMaterial.SetTexture("_PrevFrame", prevFrameCopy);
				Graphics.Blit(currentFrame, resultTexture, accumulateMaterial);

				// Draw result to screen
				Graphics.Blit(resultTexture, target);

				// Release temps
				RenderTexture.ReleaseTemporary(currentFrame);
				RenderTexture.ReleaseTemporary(prevFrameCopy);
				RenderTexture.ReleaseTemporary(currentFrame);

				numRenderedFrames++;
			}
			numRenderedFrames = 0;
		}
        else
        {
			Graphics.Blit(src, target);
		}

		/*if (isSceneCam)
		{
			if (useShaderInSceneView)
			{
				InitFrame();
				Graphics.Blit(null, target, rayTracingMaterial);
			}
			else
			{
				Graphics.Blit(src, target); // Draw the unaltered camera render to the screen
			}
		}
		else
		{
			InitFrame();

			// Create copy of prev frame
			RenderTexture prevFrameCopy = RenderTexture.GetTemporary(src.width, src.height, 0, ShaderHelper.RGBA_SFloat);
			Graphics.Blit(resultTexture, prevFrameCopy);

			// Run the ray tracing shader and draw the result to a temp texture
			rayTracingMaterial.SetInt("Frame", numRenderedFrames);
			RenderTexture currentFrame = RenderTexture.GetTemporary(src.width, src.height, 0, ShaderHelper.RGBA_SFloat);
			Graphics.Blit(null, currentFrame, rayTracingMaterial);

			// Accumulate
			accumulateMaterial.SetInt("_Frame", numRenderedFrames);
			accumulateMaterial.SetTexture("_PrevFrame", prevFrameCopy);
			Graphics.Blit(currentFrame, resultTexture, accumulateMaterial);

			// Draw result to screen
			Graphics.Blit(resultTexture, target);

			// Release temps
			RenderTexture.ReleaseTemporary(currentFrame);
			RenderTexture.ReleaseTemporary(prevFrameCopy);
			RenderTexture.ReleaseTemporary(currentFrame);

			numRenderedFrames += Application.isPlaying ? 1 : 0;
		}*/
	}

	void InitFrame()
	{
		// Create materials used in blits
		ShaderHelper.InitMaterial(rayTracingShader, ref rayTracingMaterial);
		ShaderHelper.InitMaterial(accumulateShader, ref accumulateMaterial);
		// Create result render texture
		ShaderHelper.CreateRenderTexture(ref resultTexture, Screen.width, Screen.height, FilterMode.Bilinear, ShaderHelper.RGBA_SFloat, "Result");

		// Update data
		UpdateCameraParams(Camera.current);
		CreateSpheres();
		CreateMeshes();
		SetShaderParams();
	}

	void SetShaderParams()
	{
		rayTracingMaterial.SetInt("MaxBounceCount", maxBounceCount);
		rayTracingMaterial.SetInt("NumRaysPerPixel", numRaysPerPixel);
		rayTracingMaterial.SetFloat("DefocusStrength", defocusStrength);
		rayTracingMaterial.SetFloat("DivergeStrength", divergeStrength);
		rayTracingMaterial.SetFloat("LightDistance", lightdst);

		rayTracingMaterial.SetInteger("EnvironmentEnabled", environmentSettings.enabled ? 1 : 0);
		rayTracingMaterial.SetColor("GroundColour", environmentSettings.groundColour);
		rayTracingMaterial.SetColor("SkyColourHorizon", environmentSettings.skyColourHorizon);
		rayTracingMaterial.SetColor("SkyColourZenith", environmentSettings.skyColourZenith);
		rayTracingMaterial.SetFloat("SunFocus", environmentSettings.sunFocus);
		rayTracingMaterial.SetFloat("SunIntensity", environmentSettings.sunIntensity);
	}

	void UpdateCameraParams(Camera cam)
	{
		float planeHeight = focusDistance * Tan(cam.fieldOfView * 0.5f * Deg2Rad) * 2;
		float planeWidth = planeHeight * cam.aspect;
		// Send data to shader
		rayTracingMaterial.SetVector("ViewParams", new Vector3(planeWidth, planeHeight, focusDistance));
		rayTracingMaterial.SetMatrix("CamLocalToWorldMatrix", cam.transform.localToWorldMatrix);
	}

	void CreateMeshes()
	{
		RayTracedMesh[] meshObjects = FindObjectsOfType<RayTracedMesh>();

		allTriangles ??= new List<Triangle>();
		allMeshInfo ??= new List<MeshInfo>();
		allTriangles.Clear();
		allMeshInfo.Clear();

		for (int i = 0; i < meshObjects.Length; i++)
		{
			MeshChunk[] chunks = meshObjects[i].GetSubMeshes();
			foreach (MeshChunk chunk in chunks)
			{
				RayTracingMaterial material = meshObjects[i].GetMaterial(chunk.subMeshIndex);
				allMeshInfo.Add(new MeshInfo(allTriangles.Count, chunk.triangles.Length, material, chunk.bounds));
				allTriangles.AddRange(chunk.triangles);

			}
		}

		numMeshChunks = allMeshInfo.Count;
		numTriangles = allTriangles.Count;

		ShaderHelper.CreateStructuredBuffer(ref triangleBuffer, allTriangles);
		ShaderHelper.CreateStructuredBuffer(ref meshInfoBuffer, allMeshInfo);
		rayTracingMaterial.SetBuffer("Triangles", triangleBuffer);
		rayTracingMaterial.SetBuffer("AllMeshInfo", meshInfoBuffer);
		rayTracingMaterial.SetInt("NumMeshes", allMeshInfo.Count);
	}


	void CreateSpheres()
	{
		// Create sphere data from the sphere objects in the scene
		RayTracedSphere[] sphereObjects = FindObjectsOfType<RayTracedSphere>();
		Sphere[] spheres = new Sphere[sphereObjects.Length];

		for (int i = 0; i < sphereObjects.Length; i++)
		{
			spheres[i] = new Sphere()
			{
				position = sphereObjects[i].transform.position,
				radius = sphereObjects[i].transform.localScale.x * 0.5f,
				material = sphereObjects[i].material
			};
		}

		// Create buffer containing all sphere data, and send it to the shader
		ShaderHelper.CreateStructuredBuffer(ref sphereBuffer, spheres);
		rayTracingMaterial.SetBuffer("Spheres", sphereBuffer);
		rayTracingMaterial.SetInt("NumSpheres", sphereObjects.Length);
	}


	void OnDisable()
	{
		ShaderHelper.Release(sphereBuffer, triangleBuffer, meshInfoBuffer);
		ShaderHelper.Release(resultTexture);
	}

	void OnValidate()
	{
		maxBounceCount = Mathf.Max(0, maxBounceCount);
		numRaysPerPixel = Mathf.Max(1, numRaysPerPixel);
		environmentSettings.sunFocus = Mathf.Max(1, environmentSettings.sunFocus);
		environmentSettings.sunIntensity = Mathf.Max(0, environmentSettings.sunIntensity);

	}
}