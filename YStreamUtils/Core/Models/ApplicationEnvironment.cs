using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace YStreamUtils.Core.Models;

public static class ApplicationEnvironment
{
    public static ApplicationPlatform CurrentPlatform => GetPlatform();

    private static ApplicationPlatform GetPlatform()
    {
        var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        if (isDocker == "true")
        {
            return ApplicationPlatform.Docker;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ApplicationPlatform.Windows;
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return ApplicationPlatform.LinuxNative;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return ApplicationPlatform.MacOs;
        }

        return ApplicationPlatform.Unknown;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter<ApplicationPlatform>))]
public enum ApplicationPlatform
{
    [EnumMember(Value = "windows")]
    Windows,
    [EnumMember(Value = "docker")]
    Docker,
    [EnumMember(Value = "linux")]
    LinuxNative,
    [EnumMember(Value = "macOS")]
    MacOs,
    [EnumMember(Value = "unknown")]
    Unknown
}