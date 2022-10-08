﻿using System;
using TUNING;
using UnityEngine;

namespace Portal_Cubes
{
    public class DupePortalCubeConfig : IBuildingConfig
	{
		// Token: 0x0600000F RID: 15 RVA: 0x00002610 File Offset: 0x00000810
		public override BuildingDef CreateBuildingDef()
		{
			string[] array = new string[]
			{
				"Diamond",
				"Steel"
			};
			float[] construction_mass = new float[]
			{
				BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0],
				BUILDINGS.CONSTRUCTION_MASS_KG.TIER5[0]
			};
			string[] construction_materials = array;
			BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef("DupePortalCube", 1, 1, "DupePortalCube_kanim", 30, 200f, construction_mass, construction_materials, 1600f, BuildLocationRule.Tile, BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NONE, 1f);
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

		// Token: 0x06000010 RID: 16 RVA: 0x000026F4 File Offset: 0x000008F4
		public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
		{
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
			Storage storage = go.AddOrGet<Storage>();
			storage.capacityKg = 100000f;
			storage.showInUI = true;
			storage.showDescriptor = true;
			storage.allowItemRemoval = false;
			storage.onlyTransferFromLowerPriority = true;
			storage.showCapacityStatusItem = true;
			storage.showCapacityAsMainStatus = true;
			PortalCore portalCore = go.AddOrGet<PortalCore>();
			portalCore.DupeTeleportable = true;
			portalCore.height = 2;
			portalCore.vision_offset = new CellOffset(0, 0);
			DupePortalCubeConfig.AddVisualizer(go, false, portalCore.width, 2);
			go.AddOrGet<LogicOperationalController>();
			go.AddOrGet<UserNameable>();
			go.AddOrGet<PortalConnection>().portalType = Element.State.Unbreakable;
			go.AddOrGet<NavTeleporterNew>();
			go.AddOrGet<StoppingReact>();
			DupeTeleporter dupeTeleporter = go.AddOrGet<DupeTeleporter>();
			dupeTeleporter.portalType = Element.State.Unbreakable;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x0000280C File Offset: 0x00000A0C
		public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
		{
			DupePortalCubeConfig.AddVisualizer(go, true, 1, 2);
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002819 File Offset: 0x00000A19
		public override void DoPostConfigureUnderConstruction(GameObject go)
		{
			DupePortalCubeConfig.AddVisualizer(go, false, 1, 2);
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002828 File Offset: 0x00000A28
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

		// Token: 0x06000014 RID: 20 RVA: 0x0000289F File Offset: 0x00000A9F
		public override void DoPostConfigureComplete(GameObject go)
		{
			go.AddOrGetDef<OperationalController.Def>();
			go.AddOrGet<LogicOperationalController>();
			go.GetComponent<KPrefabID>().AddTag(GameTags.FloorTiles, false);
		}

		// Token: 0x04000003 RID: 3
		public const string ID = "DupePortalCube";
	}
}