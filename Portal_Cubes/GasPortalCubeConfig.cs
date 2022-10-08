using System;
using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace Portal_Cubes
{
     public class GasPortalCubeConfig : IBuildingConfig
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public override BuildingDef CreateBuildingDef()
		{
			string[] array = new string[]
			{
				"Metal"
			};
			float[] construction_mass = new float[]
			{
				BUILDINGS.CONSTRUCTION_MASS_KG.TIER5[0]
			};
			string[] construction_materials = array;
			BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef("GasPortalCube", 1, 1, "GasPortalCube_kanim", 30, 200f, construction_mass, construction_materials, 1600f, BuildLocationRule.Tile, BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NONE, 1f);
			buildingDef.ThermalConductivity = 1f;
			buildingDef.Overheatable = false;
			buildingDef.Floodable = false;
			buildingDef.Entombable = false;
			buildingDef.IsFoundation = true;
			buildingDef.ObjectLayer = ObjectLayer.Building;
			buildingDef.AudioCategory = "Metal";
			buildingDef.SceneLayer = Grid.SceneLayer.TileMain;
			buildingDef.ForegroundLayer = Grid.SceneLayer.TileMain;
			buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
			buildingDef.PermittedRotations = PermittedRotations.R360;
			buildingDef.DragBuild = true;
			return buildingDef;
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002120 File Offset: 0x00000320
		public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
		{
			GeneratedBuildings.MakeBuildingAlwaysOperational(go);
			KBatchedAnimController kbatchedAnimController = go.AddOrGet<KBatchedAnimController>();
			kbatchedAnimController.defaultAnim = "searching";
			BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
			SimCellOccupier simCellOccupier = go.AddOrGet<SimCellOccupier>();
			simCellOccupier.setLiquidImpermeable = true;
			simCellOccupier.setGasImpermeable = true;
			simCellOccupier.doReplaceElement = false;
			simCellOccupier.notifyOnMelt = true;
			go.AddOrGet<Insulator>();
			go.AddOrGet<TileTemperature>();
			go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
			List<Tag> list = new List<Tag>();
			list.AddRange(STORAGEFILTERS.GASES);
			Storage storage = go.AddOrGet<Storage>();
			storage.capacityKg = 100000f;
			storage.showInUI = true;
			storage.showDescriptor = true;
			storage.storageFilters = list;
			storage.allowItemRemoval = false;
			storage.onlyTransferFromLowerPriority = true;
			storage.showCapacityStatusItem = true;
			storage.showCapacityAsMainStatus = true;
			go.AddOrGet<TreeFilterable>();
			go.AddOrGet<UserNameable>();
			go.AddOrGet<PortalConnection>().portalType = Element.State.Gas;
			go.AddOrGet<KGSlider>();
		}

		// Token: 0x06000003 RID: 3 RVA: 0x00002210 File Offset: 0x00000410
		public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
		{
			GasPortalCubeConfig.AddVisualizer(go, true, 1, 1);
		}

		// Token: 0x06000004 RID: 4 RVA: 0x0000221D File Offset: 0x0000041D
		public override void DoPostConfigureUnderConstruction(GameObject go)
		{
			GasPortalCubeConfig.AddVisualizer(go, false, 1, 1);
		}

		// Token: 0x06000005 RID: 5 RVA: 0x0000222C File Offset: 0x0000042C
		public static void AddVisualizer(GameObject prefab, bool movable, int width, int height)
		{
			MyChoreRangeVisualizer myChoreRangeVisualizer = prefab.AddOrGet<MyChoreRangeVisualizer>();
			myChoreRangeVisualizer.x = 0;
			myChoreRangeVisualizer.y = 0;
			myChoreRangeVisualizer.width = width;
			myChoreRangeVisualizer.height = height;
			myChoreRangeVisualizer.vision_offset = new CellOffset(0, 0);
			myChoreRangeVisualizer.movable = movable;
			myChoreRangeVisualizer.blocking_tile_visible = false;
			prefab.GetComponent<KPrefabID>().instantiateFn += delegate(GameObject go)
			{
				go.GetComponent<MyChoreRangeVisualizer>().blocking_cb = new Func<int, bool>(PortalCore.BlockingCB);
			};
		}

		// Token: 0x06000006 RID: 6 RVA: 0x000022A4 File Offset: 0x000004A4
		public override void DoPostConfigureComplete(GameObject go)
		{
			go.AddOrGetDef<OperationalController.Def>();
			go.AddOrGet<LogicOperationalController>();
			PortalCore portalCore = go.AddOrGet<PortalCore>();
			portalCore.vision_offset = new CellOffset(0, 0);
			GasPortalCubeConfig.AddVisualizer(go, false, portalCore.height, portalCore.width);
			go.GetComponent<KPrefabID>().prefabInitFn += delegate(GameObject gameObject)
			{
				new TeleportStateMachine.Instance(gameObject.GetComponent<KPrefabID>()).StartSM();
			};
			go.GetComponent<KPrefabID>().AddTag(GameTags.FloorTiles, false);
		}

		// Token: 0x04000001 RID: 1
		public const string ID = "GasPortalCube";
	}
}