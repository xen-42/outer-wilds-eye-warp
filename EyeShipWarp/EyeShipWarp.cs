using HarmonyLib;
using NewHorizons;
using NewHorizons.Components;
using NewHorizons.Utility;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EyeShipWarp;

[HarmonyPatch]
public class EyeShipWarp : ModBehaviour
{
	private GameObject _ship;
	private bool _isWarpingToEye;
	public static EyeShipWarp Instance { get; private set; }

	private void Awake()
    {
		Instance = this;
		Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
	}

    private void Start()
    {
		var api = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
		api.LoadConfigs(this);

		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	public void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		switch (scene.name)
		{
			case "SolarSystem":
				OnLoadSolarSystem();
				break;
			case "EyeOfTheUniverse":
				OnLoadEye();
				break;
		}
	}

	private void OnLoadSolarSystem()
	{
		if (_ship == null)
		{
			var ship = SearchUtilities.Find("Ship_Body", false);
			if (ship != null)
			{
				_ship = ship.InstantiateInactive();
				_ship.name = ship.name;
				_ship.AddComponent<ShipWarpController>().Init();
				DontDestroyOnLoad(_ship);
			}
		}

		_isWarpingToEye = false;
	}

	private void OnLoadEye()
	{
		if (_isWarpingToEye && _ship != null)
		{
			var eyeShip = GameObject.Instantiate(_ship);
			eyeShip.name = "Ship_Body";
			SceneManager.MoveGameObjectToScene(eyeShip, SceneManager.GetActiveScene());
			eyeShip.transform.position = SearchUtilities.Find("Vessel_Body").transform.position + Vector3.up * 300f;
			eyeShip.SetActive(true);

			Delay.FireOnNextUpdate(TeleportToShip);
		}

		_isWarpingToEye = false;
	}

	private void TeleportToShip()
	{
		var playerSpawner = GameObject.FindObjectOfType<PlayerSpawner>();
		playerSpawner.DebugWarp(playerSpawner.GetSpawnPoint(SpawnLocation.Ship));

		Locator.GetShipBody().GetComponentInChildren<ShipCockpitController>().OnPressInteract();

		GlobalMessenger.FireEvent("EnterShip");
		PlayerState.OnEnterShip();
	}

	#region Patches
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Main), nameof(Main.ChangeCurrentStarSystem))]
	private static bool NewHorizons_ChangeCurrentStarSystem(string newStarSystem)
	{
		if (newStarSystem == "xen.Eye")
		{
			Main.Instance.ChangeCurrentStarSystem("EyeOfTheUniverse", true, false);
			Instance._isWarpingToEye = true;
			return false;
		}
		else
		{
			return true;
		}
	}
	#endregion
}
