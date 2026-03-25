using GerkinDev.WatertightGates.Components.Specs;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockObjectModelSystem;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.Coordinates;
using Timberborn.GameFactionSystem;
using Timberborn.PathSystem;
using Timberborn.TerrainSystem;
using UnityEngine;

namespace GerkinDev.WatertightGates.Components
{
	public class FreePositionedDynamicPathModel : BaseComponent, IAwakableComponent, IModelUpdater
	{
		private readonly IConnectionService _connectionService;
		private readonly FactionService _factionService;
		private readonly StackableBlockService _stackableBlockService;
		private readonly IBlockService _blockService;
		private readonly PreviewBlockService _previewBlockService;
		private readonly ITerrainService _terrainService;
		private BlockObject _blockObject;
		private FreePositionedDynamicPathModelSpec _pathModelSpec;
		private GameObject _currentModel;
		private Orientation _currentModelOrientation;
		private readonly NeighboredValues4<GameObject> _groundModels = new NeighboredValues4<GameObject>();
		private readonly NeighboredValues4<GameObject> _roofModels = new NeighboredValues4<GameObject>();

		public FreePositionedDynamicPathModel(IConnectionService connectionService, FactionService factionService, StackableBlockService stackableBlockService, IBlockService blockService, PreviewBlockService previewBlockService, ITerrainService terrainService)
		{
			_connectionService = connectionService;
			_factionService = factionService;
			_stackableBlockService = stackableBlockService;
			_blockService = blockService;
			_previewBlockService = previewBlockService;
			_terrainService = terrainService;
		}

		#region IAwakableComponent
		public void Awake()
		{
			_blockObject = GetComponent<BlockObject>();
			_pathModelSpec = GetComponent<FreePositionedDynamicPathModelSpec>();
			_InitializeModels();
			_SetMatchingModel(down: false, left: false, up: false, right: false);
		}

		private void _InitializeModels()
		{
			_AddModel("0000", down: false, left: false, up: false, right: false);
			_AddModel("0010", down: false, left: false, up: true, right: false);
			_AddModel("1010", down: true, left: false, up: true, right: false);
			_AddModel("0011", down: false, left: false, up: true, right: true);
			_AddModel("0111", down: false, left: true, up: true, right: true);
			_AddModel("1111", down: true, left: true, up: true, right: true);
		}
		#endregion

		#region IModelUpdater
		public void UpdateModel()
		{
			Vector3Int pathOrigin = _GetPathOrigin();
			_SetMatchingModel(_CanConnectInDirection(pathOrigin, Direction2D.Down), _CanConnectInDirection(pathOrigin, Direction2D.Left), _CanConnectInDirection(pathOrigin, Direction2D.Up), _CanConnectInDirection(pathOrigin, Direction2D.Right));
		}
		#endregion

		private void _AddModel(string variant, bool down, bool left, bool up, bool right)
		{
			if (!string.IsNullOrWhiteSpace(_pathModelSpec.GroundModelPrefix))
			{
				_groundModels.AddVariants(_GetModelVariant(_pathModelSpec.GroundModelPrefix, variant, _factionService.Current.PathMaterial.Asset), down, left, up, right);
			}

			if (!string.IsNullOrWhiteSpace(_pathModelSpec.RoofModelPrefix))
			{
				_roofModels.AddVariants(_GetModelVariant(_pathModelSpec.RoofModelPrefix, variant, _factionService.Current.BaseWoodMaterial.Asset), down, left, up, right);
			}
		}

		private GameObject _GetModelVariant(string prefix, string variant, Material material)
		{
			string childName = prefix + variant;
			GameObject gameObject = GameObject.FindChild(childName);
			gameObject.SetActive(value: false);
			gameObject.GetComponentInChildren<Renderer>().sharedMaterial = material;
			return gameObject;
		}

		private void _SetMatchingModel(bool down, bool left, bool up, bool right)
		{
			NeighboredValues4<GameObject> neighboredValues = _IsValidForGroundModel() ? _groundModels : _roofModels;
			if (!neighboredValues.IsEmpty)
			{
				var (model, orientation2) = neighboredValues.GetMatch(down, left, up, right);
				_SetCurrentModel(model, orientation2);
			}
			else if ((bool)_currentModel)
			{
				_currentModel.SetActive(value: false);
			}
		}

		private bool _IsValidForGroundModel()
		{
			Vector3Int coordinates = _blockObject.TransformCoordinates(_pathModelSpec.Position);
			if (!_IsEnforced(coordinates, PathModelType.Roof))
			{
				Vector3Int vector3Int = coordinates - new Vector3Int(0, 0, 1);
				bool num = _terrainService.OnGround(coordinates) || _stackableBlockService.IsUnfinishedGroundBlockAt(vector3Int);
				bool flag = _IsEnforced(vector3Int, PathModelType.Ground);
				return num || flag;
			}

			return false;
		}

		private bool _IsEnforced(Vector3Int coordinates, PathModelType modelType)
		{
			PathModelTypeEnforcer pathModelTypeEnforcer = _blockService.GetObjectsWithComponentAt<PathModelTypeEnforcer>(coordinates).FirstOrDefault() ?? _previewBlockService.GetObjectsWithComponentAt<PathModelTypeEnforcer>(coordinates).FirstOrDefault();
			if (pathModelTypeEnforcer != null)
			{
				return pathModelTypeEnforcer.PathModelType == modelType;
			}

			return false;
		}

		private void _SetCurrentModel(GameObject model, Orientation orientation)
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

		private Vector3Int _GetPathOrigin()
		{
			return _blockObject.TransformCoordinates(_pathModelSpec.Position);
		}

		private bool _CanConnectInDirection(Vector3Int origin, Direction2D direction2D)
		{
			Direction2D direction2D2 = _blockObject.Orientation.Transform(direction2D);
			return _connectionService.CanConnectInDirection(origin, direction2D2);
		}
	}
}
