#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Runtime.InteropServices;
using System.Text;
using MediaPortal.Utilities.Exceptions;
using Microsoft.Win32;

namespace MediaPortal.Utilities.SystemAPI
{
  /// <summary>
  /// For calls to the Windows API, this class should be used instead of directly using
  /// the underlaying system's API. This class hides the concrete underlaying system and will
  /// use different system functions depending on the underlaying system.
  /// </summary>
  public static class WindowsAPI
  {
    #region Windows API

    private const int CSIDL_MYMUSIC = 0x000d;     // "My Music" folder
    private const int CSIDL_MYVIDEO = 0x000e;     // "My Videos" folder
    private const int CSIDL_MYPICTURES = 0x0027;  // "My Pictures" folder

    private const int SHGFP_TYPE_CURRENT = 0;

    [DllImport("shell32.dll")]
    [Obsolete("Deprecated in Vista and later. Replaced by SHGetKnownFolderPath")]
    private static extern Int32 SHGetFolderPath(
        IntPtr hwndOwner,        // Handle to an owner window.
        Int32 nFolder,           // A CSIDL value that identifies the folder whose path is to be retrieved.
        IntPtr hToken,           // An access token that can be used to represent a particular user.
        UInt32 dwFlags,          // Flags to specify which path is to be returned. It is used for cases where the folder associated with a CSIDL may be moved or renamed by the user. 
        StringBuilder pszPath);  // Pointer to a null-terminated string which will receive the path.

    [FlagsAttribute]
    public enum EXECUTION_STATE : uint
    {
      ES_AWAYMODE_REQUIRED = 0x00000040,
      ES_CONTINUOUS = 0x80000000,
      ES_DISPLAY_REQUIRED = 0x00000002,
      ES_SYSTEM_REQUIRED = 0x00000001
      // Legacy flag, should not be used.
      // ES_USER_PRESENT = 0x00000004
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    [DllImport("powrprof.dll", SetLastError = true)]
    public static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SystemParametersInfo(uint uiAction, bool uiParam, IntPtr pvParam, uint fWinIni);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref bool result, uint fWinIni);

    #endregion

    public const string AUTOSTART_REGISTRY_KEY = @"Software\Microsoft\Windows\Currentversion\Run";

    public const int S_OK = 0x0;
    public const int S_FALSE = 0x1;

    public const int MAX_PATH = 260;

    public const uint SPI_GETSCREENSAVEACTIVE = 0x0010;
    public const uint SPI_SETSCREENSAVEACTIVE = 0x0011;

    /// <summary>
    /// Use this enum to denote special system folders.
    /// </summary>
    public enum SpecialFolder
    {
      MyMusic,
      MyVideos,
      MyPictures,
    }

    public static bool ScreenSaverEnabled
    {
      get
      {
        bool result = false;
        SystemParametersInfo(SPI_GETSCREENSAVEACTIVE, 0, ref result, 0);
        return result;
      }
      set { SystemParametersInfo(SPI_SETSCREENSAVEACTIVE, value, IntPtr.Zero, 0); }
    }

    /// <summary>
    /// Returns a string which contains the name and version of the operating system.
    /// </summary>
    /// <returns>Operating system name and version.</returns>
    public static string GetOsVersionString()
    {
      OperatingSystem os = Environment.OSVersion;
      return os.Platform + "/" + os.Version;
    }

    /// <summary>
    /// Returns the path of the given system's special folder.
    /// </summary>
    /// <param name="folder">Folder to retrieve.</param>
    /// <param name="folderPath">Will be set to the folder path if the result value is <c>true</c>.</param>
    /// <returns><c>true</c>, if the specified special folder could be retrieved. Else <c>false</c>
    /// will be returned.</returns>
    public static bool GetSpecialFolder(SpecialFolder folder, out string folderPath)
    {
      folderPath = null;
      switch (folder)
      {
        case SpecialFolder.MyMusic:
          folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
          return true;
        case SpecialFolder.MyPictures:
          folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
          return true;
        case SpecialFolder.MyVideos:
          StringBuilder sb = new StringBuilder(MAX_PATH);
          // TODO: .net 4.5 introduces Environment.SpecialFolder.MyVideos. Until we switch to .net 4.5, we need to use this
          // deprecated function
          if (SHGetFolderPath(IntPtr.Zero, CSIDL_MYVIDEO, IntPtr.Zero, SHGFP_TYPE_CURRENT, sb) == S_OK)
          {
            folderPath = sb.ToString();
            return true;
          }
          return false;
        default:
          throw new NotImplementedException(string.Format(
              "The handling for special folder '{0}' isn't implemented yet", folder));
      }
    }

    /// <summary>
    /// Adds the application with the specified <paramref name="applicationPath"/> to the autostart
    /// registry key. The application will be automatically started the next system startup.
    /// </summary>
    /// <param name="applicationPath">Path of the application to be auto-started.</param>
    /// <param name="registerName">The name used in the registry as key for the autostart value.</param>
    /// <param name="user">If set to <c>true</c>, the autostart application will be added to the HCKU
    /// registry hive, else it will be added to the HKLM hive.</param>
    /// <exception cref="EnvironmentException">If the appropriate registry key cannot accessed.</exception>
    public static void AddAutostartApplication(string applicationPath, string registerName, bool user)
    {
      RegistryKey root = user ? Registry.CurrentUser : Registry.LocalMachine;
      using (RegistryKey key = root.OpenSubKey(AUTOSTART_REGISTRY_KEY))
      {
        if (key == null)
          throw new EnvironmentException(@"Unable to access/create registry key '{0}\{1}'",
              user ? "HKCU" : "HKLM", AUTOSTART_REGISTRY_KEY);
        key.SetValue(registerName, applicationPath, RegistryValueKind.ExpandString);
      }
    }

    /// <summary>
    /// Removes an application from the autostart registry key.
    /// </summary>
    /// <param name="registerName">The name used in the registry as key for the autostart value.</param>
    /// <param name="user">If set to <c>true</c>, the autostart application will be removed from the HCKU
    /// registry hive, else it will be removed from the HKLM hive.</param>
    /// <exception cref="EnvironmentException">If the appropriate registry key cannot accessed.</exception>
    public static void RemoveAutostartApplication(string registerName, bool user)
    {
      RegistryKey root = user ? Registry.CurrentUser : Registry.LocalMachine;
      using (RegistryKey key = root.OpenSubKey(AUTOSTART_REGISTRY_KEY))
      {
        if (key == null)
          throw new EnvironmentException(@"Unable to access registry key '{0}\{1}'",
              user ? "HKCU" : "HKLM", AUTOSTART_REGISTRY_KEY);
        key.DeleteValue(registerName, false);
      }
    }

    /// <summary>
    /// Returns the application path for the application registered to be autostarted with the
    /// specified <paramref name="registerName"/>.
    /// </summary>
    /// <param name="registerName">The name used in the registry as key for the autostart value.</param>
    /// <param name="user">If set to <c>true</c>, the autostart application path will be searched in the HCKU
    /// registry hive, else it will be searched in the HKLM hive.</param>
    /// <returns>Application path registered to be autostarted with the specified
    /// <paramref name="registerName"/>.</returns>
    public static string GetAutostartApplicationPath(string registerName, bool user)
    {
      RegistryKey root = user ? Registry.CurrentUser : Registry.LocalMachine;
      using (RegistryKey key = root.OpenSubKey(AUTOSTART_REGISTRY_KEY))
        return key == null ? null : key.GetValue(registerName) as string;
    }
  }
}