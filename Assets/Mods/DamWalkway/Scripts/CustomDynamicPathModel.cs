using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockObjectModelSystem;
using Timberborn.BlockSystem;
using Timberborn.Buildings;
using Timberborn.Common;
using Timberborn.Coordinates;
using Timberborn.GameFactionSystem;
using Timberborn.PathSystem;
using Timberborn.TerrainSystem;
using UnityEngine;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts
{
	public class CustomDynamicPathModel : BaseComponent, IAwakableComponent, IModelUpdater
	{
		public readonly IConnectionService _connectionService;

		public readonly FactionService _factionService;

		public readonly StackableBlockService _stackableBlockService;

		public readonly IBlockService _blockService;

		public readonly PreviewBlockService _previewBlockService;

		public readonly ITerrainService _terrainService;

		public BuildingSpec _buildingSpec;

		public BlockObject _blockObject;

		public CustomDynamicPathModelSpec _dynamicPathModelSpec;

		public GameObject _currentModel;

		public Orientation _currentModelOrientation;

		public readonly NeighboredValues4<GameObject> _groundModels = new NeighboredValues4<GameObject>();

		public readonly NeighboredValues4<GameObject> _roofModels = new NeighboredValues4<GameObject>();

		public CustomDynamicPathModel(IConnectionService connectionService, FactionService factionService, StackableBlockService stackableBlockService, IBlockService blockService, PreviewBlockService previewBlockService, ITerrainService terrainService)
		{
			_connectionService = connectionService;
			_factionService = factionService;
			_stackableBlockService = stackableBlockService;
			_blockService = blockService;
			_previewBlockService = previewBlockService;
			_terrainService = terrainService;
		}

		public void Awake()
		{
			_buildingSpec = GetComponent<BuildingSpec>();
			_blockObject = GetComponent<BlockObject>();
			_dynamicPathModelSpec = GetComponent<CustomDynamicPathModelSpec>();
			InitializeModels();
			SetMatchingModel(down: false, left: false, up: false, right: false);
		}

		public void UpdateModel()
		{
			Vector3Int pathOrigin = GetPathOrigin();
			SetMatchingModel(CanConnectInDirection(pathOrigin, Direction2D.Down), CanConnectInDirection(pathOrigin, Direction2D.Left), CanConnectInDirection(pathOrigin, Direction2D.Up), CanConnectInDirection(pathOrigin, Direction2D.Right));
		}

		public void InitializeModels()
		{
			AddModel("0000", down: false, left: false, up: false, right: false);
			AddModel("0010", down: false, left: false, up: true, right: false);
			AddModel("1010", down: true, left: false, up: true, right: false);
			AddModel("0011", down: false, left: false, up: true, right: true);
			AddModel("0111", down: false, left: true, up: true, right: true);
			AddModel("1111", down: true, left: true, up: true, right: true);
		}

		public void AddModel(string variant, bool down, bool left, bool up, bool right)
		{
			if (!string.IsNullOrWhiteSpace(_dynamicPathModelSpec.GroundModelPrefix))
			{
				_groundModels.AddVariants(GetModelVariant(_dynamicPathModelSpec.GroundModelPrefix, variant, _factionService.Current.PathMaterial.Asset), down, left, up, right);
			}

			if (!string.IsNullOrWhiteSpace(_dynamicPathModelSpec.RoofModelPrefix))
			{
				_roofModels.AddVariants(GetModelVariant(_dynamicPathModelSpec.RoofModelPrefix, variant, _factionService.Current.BaseWoodMaterial.Asset), down, left, up, right);
			}
		}

		public GameObject GetModelVariant(string prefix, string variant, Material material)
		{
			string childName = prefix + variant;
			GameObject gameObject = base.GameObject.FindChild(childName);
			gameObject.SetActive(value: false);
			gameObject.GetComponentInChildren<Renderer>().sharedMaterial = material;
			return gameObject;
		}

		public void SetMatchingModel(bool down, bool left, bool up, bool right)
		{
			NeighboredValues4<GameObject> neighboredValues = (IsValidForGroundModel() ? _groundModels : _roofModels);
			if (!neighboredValues.IsEmpty)
			{
				var (model, orientation2) = (OrientedValue<GameObject>)(neighboredValues.GetMatch(down, left, up, right));
				SetCurrentModel(model, orientation2);
			}
			else if ((bool)_currentModel)
			{
				_currentModel.SetActive(value: false);
			}
		}

		public bool IsValidForGroundModel()
		{
			Vector3Int coordinates = _blockObject.TransformCoordinates(_dynamicPathModelSpec.Position);
			if (!IsEnforced(coordinates, PathModelType.Roof))
			{
				Vector3Int vector3Int = coordinates - new Vector3Int(0, 0, 1);
				bool num = _terrainService.OnGround(coordinates) || _stackableBlockService.IsUnfinishedGroundBlockAt(vector3Int);
				bool flag = IsEnforced(vector3Int, PathModelType.Ground);
				return num || flag;
			}

			return false;
		}

		public bool IsEnforced(Vector3Int coordinates, PathModelType modelType)
		{
			PathModelTypeEnforcer pathModelTypeEnforcer = _blockService.GetObjectsWithComponentAt<PathModelTypeEnforcer>(coordinates).FirstOrDefault() ?? _previewBlockService.GetObjectsWithComponentAt<PathModelTypeEnforcer>(coordinates).FirstOrDefault();
			if (pathModelTypeEnforcer != null)
			{
				return pathModelTypeEnforcer.PathModelType == modelType;
			}

			return false;
		}

		public void SetCurrentModel(GameObject model, Orientation orientation)
		{
			if (!(_currentModel != model) && _currentModelOrientation == orientation)
			{
				GameObject currentModel = _currentModel;
				if ((object)currentModel == null || currentModel.activeSelf)
				{
					return;
				}
			}

			if ((bool)_currentModel)
			{
				_currentModel.SetActive(value: false);
			}

			Vector3 localPosition = CoordinateSystem.GridToWorld(orientation.ToPivotOffset());
			Quaternion localRotation = orientation.ToWorldSpaceRotation();
			model.transform.SetLocalPositionAndRotation(localPosition, localRotation);
			_currentModel = model;
			_currentModelOrientation = orientation;
			_currentModel.SetActive(value: true);
		}

		public Vector3Int GetPathOrigin()
		{
			return _blockObject.TransformCoordinates(_dynamicPathModelSpec.Position);
		}

		public bool CanConnectInDirection(Vector3Int origin, Direction2D direction2D)
		{
			Direction2D direction2D2 = _blockObject.Orientation.Transform(direction2D);
			return _connectionService.CanConnectInDirection(origin, direction2D2);
		}
	}
}
