using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class PlatformRunnerConfiguration
{
    public List<string> buildScenes;
    public List<string> testScenes;
    public BuildTarget buildTarget;
    public bool runInEditor;
    public string projectName = SceneManager.GetActiveScene().path;

    public string resultsDir = null;
    public bool sendResultsOverNetwork;
    public List<string> ipList;
    public int port;

    public PlatformRunnerConfiguration(BuildTarget buildTarget)
    {
        this.buildTarget = buildTarget;
        projectName = SceneManager.GetActiveScene().path;
    }

    public PlatformRunnerConfiguration()
        : this(BuildTarget.StandaloneWindows)
    {
    }

    public string GetTempPath()
    {
        if (string.IsNullOrEmpty(projectName))
            projectName = Path.GetTempFileName();

        var path = Path.Combine("Temp", projectName);
        switch (buildTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return path + ".exe";
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
            case BuildTarget.StandaloneLinuxUniversal:
                return path + ".app";
            case BuildTarget.Android:
                return path + ".apk";
            default:
                return path;
        }
    }

    public string[] GetConnectionIPs()
    {
        return ipList.Select(ip => ip + ":" + port).ToArray();
    }

    public static int TryToGetFreePort()
    {
        var port = -1;
        try
        {
            var l = new TcpListener(IPAddress.Any, 0);
            l.Start();
            port = ((IPEndPoint)l.Server.LocalEndPoint).Port;
            l.Stop();
        }
        catch (SocketException e)
        {
            Debug.LogException(e);
        }
        return port;
    }
}
