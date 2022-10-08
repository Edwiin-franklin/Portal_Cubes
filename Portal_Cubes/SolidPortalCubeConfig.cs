using System;
using System.Collections.Generic;
using TUNING;
using UnityEngine;


namespace Portal_Cubes
{
    public class SolidPortalCubeConfig : IBuildingConfig
	{
		// Token: 0x06000092 RID: 146 RVA: 0x00007150 File Offset: 0x00005350
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
			BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef("SolidPortalCube", 1, 1, "SolidPortalCube_kanim", 30, 200f, construction_mass, construction_materials, 1600f, BuildLocationRule.Tile, BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NONE, 1f);
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

		// Token: 0x06000093 RID: 147 RVA: 0x00007234 File Offset: 0x00005434
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
			List<Tag> list = new List<Tag>();
			list.AddRange(STORAGEFILTERS.NOT_EDIBLE_SOLIDS);
			list.AddRange(STORAGEFILTERS.FOOD);
			list.AddRange(STORAGEFILTERS.BAGABLE_CREATURES);
			list.AddRange(STORAGEFILTERS.SWIMMING_CREATURES);
			list.AddRange(STORAGEFILTERS.GASES);
			list.AddRange(STORAGEFILTERS.LIQUIDS);
			list.AddRange(STORAGEFILTERS.PAYLOADS);
			list.Add(GameTags.Equipped);
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
			PortalCore portalCore = go.AddOrGet<PortalCore>();
			portalCore.vision_offset = new CellOffset(0, 0);
			SolidPortalCubeConfig.AddVisualizer(go, false, portalCore.height, portalCore.width);
			go.AddOrGet<LogicOperationalController>();
			go.AddOrGet<UserNameable>();
			go.AddOrGet<PortalConnection>().portalType = Element.State.Solid;
			go.AddOrGet<KGSlider>();
		}

		// Token: 0x06000094 RID: 148 RVA: 0x000073A4 File Offset: 0x000055A4
		public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
		{
			SolidPortalCubeConfig.AddVisualizer(go, true, 1, 1);
		}

		// Token: 0x06000095 RID: 149 RVA: 0x000073B1 File Offset: 0x000055B1
		public override void DoPostConfigureUnderConstruction(GameObject go)
		{
			SolidPortalCubeConfig.AddVisualizer(go, false, 1, 1);
		}

		// Token: 0x06000096 RID: 150 RVA: 0x000073C0 File Offset: 0x000055C0
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

		// Token: 0x06000097 RID: 151 RVA: 0x00007438 File Offset: 0x00005638
		public override void DoPostConfigureComplete(GameObject go)
		{
			go.AddOrGetDef<OperationalController.Def>();
			go.AddOrGet<LogicOperationalController>();
			go.GetComponent<KPrefabID>().prefabInitFn += delegate(GameObject gameObject)
			{
				new TeleportStateMachine.Instance(gameObject.GetComponent<KPrefabID>()).StartSM();
			};
			go.GetComponent<KPrefabID>().AddTag(GameTags.FloorTiles, false);
		}

		// Token: 0x0400007B RID: 123
		public const string ID = "SolidPortalCube";
	}
}