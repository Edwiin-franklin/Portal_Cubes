using System;
using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace Portal_Cubes
{
    public class LiquidPortalCubeConfig : IBuildingConfig
	{
		// Token: 0x06000008 RID: 8 RVA: 0x00002330 File Offset: 0x00000530
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
			BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef("LiquidPortalCube", 1, 1, "LiquidPortalCube_kanim", 30, 200f, construction_mass, construction_materials, 1600f, BuildLocationRule.Tile, BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NONE, 1f);
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

		// Token: 0x06000009 RID: 9 RVA: 0x00002400 File Offset: 0x00000600
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
			list.AddRange(STORAGEFILTERS.LIQUIDS);
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
			go.AddOrGet<PortalConnection>().portalType = Element.State.Liquid;
			go.AddOrGet<KGSlider>();
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000024F0 File Offset: 0x000006F0
		public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
		{
			LiquidPortalCubeConfig.AddVisualizer(go, true, 1, 1);
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000024FD File Offset: 0x000006FD
		public override void DoPostConfigureUnderConstruction(GameObject go)
		{
			LiquidPortalCubeConfig.AddVisualizer(go, false, 1, 1);
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000250C File Offset: 0x0000070C
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

		// Token: 0x0600000D RID: 13 RVA: 0x00002584 File Offset: 0x00000784
		public override void DoPostConfigureComplete(GameObject go)
		{
			go.AddOrGetDef<OperationalController.Def>();
			go.AddOrGet<LogicOperationalController>();
			PortalCore portalCore = go.AddOrGet<PortalCore>();
			portalCore.vision_offset = new CellOffset(0, 0);
			LiquidPortalCubeConfig.AddVisualizer(go, false, portalCore.height, portalCore.width);
			go.GetComponent<KPrefabID>().prefabInitFn += delegate(GameObject gameObject)
			{
				new TeleportStateMachine.Instance(gameObject.GetComponent<KPrefabID>()).StartSM();
			};
			go.GetComponent<KPrefabID>().AddTag(GameTags.FloorTiles, false);
		}

		// Token: 0x04000002 RID: 2
		public const string ID = "LiquidPortalCube";
	}
}