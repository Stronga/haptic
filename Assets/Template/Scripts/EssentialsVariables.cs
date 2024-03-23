using UnityEngine;
[System.Serializable]
public class EssentialsVariables
{
	public bool disableMediaControls = false;
	public bool kioskGazeMenu = true;
	public string backgroundPano = null;
	public bool backgroundStereo = false;
	public string logoOnTop = null;
	public Color color1, color2, color3;
	public bool kioskAutoPlay = false;
	public bool alwaysShowStream = false;
	public float kioskFillTime = 3f;
	public TabSwitcher.Tab currentTab = TabSwitcher.Tab.Grid;
	public bool disableBackgroundAudio = false;
}
