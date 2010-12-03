#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.Core;
using MediaPortal.Core.Configuration.ConfigurationClasses;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UiComponents.Media.Settings.Configuration
{
  public class ClosePlayersWhenFinished : YesNo
  {
    public override void Load()
    {
      _yes = SettingsManager.Load<MediaModelSettings>().ClosePlayerWhenFinished;
    }

    public override void Save()
    {
      base.Save();
      MediaModelSettings settings = SettingsManager.Load<MediaModelSettings>();
      settings.ClosePlayerWhenFinished = _yes;
      SettingsManager.Save(settings);
      UpdatePlayerContexts();
    }

    protected void UpdatePlayerContexts()
    {
      IPlayerContext pc = ServiceRegistration.Get<IPlayerContextManager>().GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
      if (pc != null)
        pc.CloseWhenFinished = _yes;
      pc = ServiceRegistration.Get<IPlayerContextManager>().GetPlayerContext(PlayerManagerConsts.SECONDARY_SLOT);
      if (pc != null)
        pc.CloseWhenFinished = _yes;
    }
  }
}