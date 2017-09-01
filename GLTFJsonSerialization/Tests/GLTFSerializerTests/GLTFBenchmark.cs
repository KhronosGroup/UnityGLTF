using UnityEngine;
using UnityEditor;
using System.Collections;
using GLTFJsonSerialization;
using UnityEngine.Networking;
using System.Text;
using System.IO;

public class GLTFBenchmark : MonoBehaviour
{
	public string[] GLTFUrls = new string[]
	{
		"http://localhost:8080/BoomBox/glTF/BoomBox.gltf",
		"http://localhost:8080/Lantern/glTF/Lantern.gltf",
		"http://localhost:8080/Corset/glTF/Corset.gltf"
	};
	public int NumberOfIterations = 5;
	public bool SaveCSV = true;

	IEnumerator Start ()
	{
		var timer = new System.Diagnostics.Stopwatch();

		var csv = new StringBuilder();

		csv.AppendLine("Name, Time (ms)");

		Debug.Log("Start Parsing Benchmark.");

		foreach (var gltfUrl in GLTFUrls)
		{
			var www = UnityWebRequest.Get(gltfUrl);
			yield return www.Send();

			Debug.LogFormat("Benchmarking: {0}", gltfUrl);
			long totalTime = 0;
			for (var i = 0; i < NumberOfIterations; i++)
			{
				timer.Start();
				GLTFParser.ParseJson(Encoding.ASCII.GetBytes(www.downloadHandler.text));
				timer.Stop();

				Debug.LogFormat("Iteration {0} took: {1}ms", i, timer.ElapsedMilliseconds);
				totalTime += timer.ElapsedMilliseconds;
			}

			var avgTime = (float)totalTime / NumberOfIterations;
			Debug.LogFormat("Average parse time {0}ms", avgTime);
			csv.AppendFormat("{0}, {1}\n", gltfUrl, avgTime);
		}



		Debug.Log("End Parsing Benchmark.");

		Debug.Log("Done.");

		if (SaveCSV)
		{
			var fileName = string.Format("glTFBench_{0}iter", NumberOfIterations);
			var path = EditorUtility.SaveFilePanel("Save GLTF Benchmark .csv", "", fileName, "csv");

			if (path != null)
			{
				File.WriteAllText(path, csv.ToString());
				Debug.LogFormat("Benchmark written to: {0}", path);
			}
		}
	}
}
