using System;
using System.Collections.Generic;
using TUNING;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using KSerialization;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Database;
using PeterHan.PLib.PatchManager;
using System.Linq;
using PeterHan.PLib.Detours;
using UnityEngine.UI;
using STRINGS;

namespace Portal_Cubes
{
    
    
    // Token: 0x02000005 RID: 5
    public class DoubleClick : KMonoBehaviour, IPointerClickHandler, IEventSystemHandler
    {
        // Token: 0x06000016 RID: 22 RVA: 0x000028CC File Offset: 0x00000ACC
        public void OnPointerClick(PointerEventData eventData)
        {
            bool flag = this.doubleClickCoroutine != null && this.onDoubleClick != null;
            if (flag)
            {
                this.onDoubleClick();
                this.didDoubleClick = true;
            }
            else
            {
                this.doubleClickCoroutine = this.DoubleClickTimer(eventData);
                base.StartCoroutine(this.doubleClickCoroutine);
            }
        }

        // Token: 0x06000017 RID: 23 RVA: 0x00002922 File Offset: 0x00000B22
        private IEnumerator DoubleClickTimer(PointerEventData eventData)
        {
            float startTime = Time.unscaledTime;
            while (Time.unscaledTime - startTime < 0.22f && !this.didDoubleClick)
            {
                yield return null;
            }
            bool flag = !this.didDoubleClick && this.onClick != null;
            if (flag)
            {
                this.onClick();
            }
            this.doubleClickCoroutine = null;
            this.didDoubleClick = false;
            yield break;
        }

        // Token: 0x04000004 RID: 4
        public System.Action onClick;

        // Token: 0x04000005 RID: 5
        public System.Action onDoubleClick;

        // Token: 0x04000006 RID: 6
        private const float DoubleClickTime = 0.22f;

        // Token: 0x04000007 RID: 7
        private bool didDoubleClick;

        // Token: 0x04000008 RID: 8
        private IEnumerator doubleClickCoroutine;
    }
    
    
    
    public class DupeTeleporter : StateMachineComponent<DupeTeleporter.SMInstance>, IBasicBuilding
	{
		// Token: 0x0600008A RID: 138 RVA: 0x00006AAE File Offset: 0x00004CAE
		protected override void OnPrefabInit()
		{
			base.OnPrefabInit();
			base.gameObject.FindOrAddComponent<Workable>();
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00006AC4 File Offset: 0x00004CC4
		protected override void OnSpawn()
		{
			base.OnSpawn();
			base.smi.StartSM();
			PortalComponents.GetTelepoterInstance<DupeTeleporter>(this.portalType).Add(this);
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00006AEC File Offset: 0x00004CEC
		protected override void OnCleanUp()
		{
			PortalComponents.GetTelepoterInstance<DupeTeleporter>(this.portalType).Remove(this);
			base.OnCleanUp();
		}

		// Token: 0x04000065 RID: 101
		public Element.State portalType;

		// Token: 0x04000066 RID: 102
		private StoppingReact.PortalEnterWorkableReactable reactable;

		// Token: 0x04000067 RID: 103
		private static readonly HashedString Idle_anims = new HashedString("Idle");

		// Token: 0x04000068 RID: 104
		private static readonly HashedString Searching_anims = new HashedString("Searching");

		// Token: 0x02000028 RID: 40
		public class SMInstance : GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.GameInstance
		{
			// Token: 0x060000D9 RID: 217 RVA: 0x00008098 File Offset: 0x00006298
			public SMInstance(DupeTeleporter master) : base(master)
			{
			}
		}

		// Token: 0x02000029 RID: 41
		public class States : GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter>
		{
			// Token: 0x060000DA RID: 218 RVA: 0x000080A4 File Offset: 0x000062A4
			public override void InitializeStates(out StateMachine.BaseState default_state)
			{
				default_state = this.Searching;
				this.Searching.Enter("SetSearching", delegate(DupeTeleporter.SMInstance smi)
				{
					PortalCore component = smi.GetComponent<PortalCore>();
					component.isStoring = false;
					component.isDropping = false;
					smi.GetComponent<KBatchedAnimController>().Play(DupeTeleporter.Searching_anims, KAnim.PlayMode.Paused, 1f, 0f);
				}).ToggleReactable((DupeTeleporter.SMInstance smi) => smi.master.reactable = new StoppingReact.PortalEnterWorkableReactable(smi.master.GetComponent<StoppingReact>(), smi.GetComponent<PortalCore>())).UpdateTransition(this.Teleporting_input, delegate(DupeTeleporter.SMInstance smi, float ft)
				{
					PortalCore component = smi.GetComponent<PortalCore>();
					return component.needPick() && !component.isDropping && !component.needDrop();
				}, UpdateRate.SIM_1000ms, false).UpdateTransition(this.Teleporting_output, delegate(DupeTeleporter.SMInstance smi, float ft)
				{
					PortalCore component = smi.GetComponent<PortalCore>();
					PortalConnection component2 = smi.GetComponent<PortalConnection>();
					return component.needDrop() && !component.isStoring;
				}, UpdateRate.SIM_1000ms, false).EventTransition(GameHashes.ActiveChanged, this.Idle, (DupeTeleporter.SMInstance smi) => !smi.GetComponent<Operational>().IsActive);
				this.Idle.Enter("SetIdle", delegate(DupeTeleporter.SMInstance smi)
				{
					smi.GetComponent<KBatchedAnimController>().Play(DupeTeleporter.Idle_anims, KAnim.PlayMode.Paused, 1f, 0f);
				}).EventTransition(GameHashes.ActiveChanged, this.Searching, (DupeTeleporter.SMInstance smi) => smi.GetComponent<Operational>().IsActive).UpdateTransition(this.Searching, delegate(DupeTeleporter.SMInstance smi, float ft)
				{
					PortalCore component = smi.GetComponent<PortalCore>();
					return !component.blocked(false) && component.isLogicEnabled();
				}, UpdateRate.SIM_1000ms, false);
				this.Teleporting_input.PlayAnim("Teleporting_input").Enter("SetTeleporting_input", delegate(DupeTeleporter.SMInstance smi)
				{
					PortalCore component = smi.GetComponent<PortalCore>();
					component.isStoring = true;
				}).OnAnimQueueComplete(this.Teleporting_part1);
				this.Teleporting_part1.PlayAnim("Teleporting_part1").Enter("SetTeleporting_part1", delegate(DupeTeleporter.SMInstance smi)
				{
				}).Exit("ExitTeleporting_part1", delegate(DupeTeleporter.SMInstance smi)
				{
					PortalCore component = smi.GetComponent<PortalCore>();
					component.doPick();
				}).OnAnimQueueComplete(this.Teleporting_part2);
				this.Teleporting_loop.PlayAnim("Teleporting_loop", KAnim.PlayMode.Loop).Enter("SetTeleporting_loop", delegate(DupeTeleporter.SMInstance smi)
				{
				}).TagTransition(GameTags.Operational, this.Teleporting_part2, true);
				this.Teleporting_part2.PlayAnim("Teleporting_part2").Enter("SetTeleporting_part2", delegate(DupeTeleporter.SMInstance smi)
				{
				}).OnAnimQueueComplete(this.Retracting_input);
				this.Retracting_input.PlayAnim("Retracting_input").Enter("SetRetracting", delegate(DupeTeleporter.SMInstance smi)
				{
				}).OnAnimQueueComplete(this.Searching);
				this.Teleporting_output.PlayAnim("Teleporting_output").Enter("SetTeleporting_output", delegate(DupeTeleporter.SMInstance smi)
				{
					PortalCore component = smi.GetComponent<PortalCore>();
					component.isDropping = true;
				}).OnAnimQueueComplete(this.Dropping_tele_part1);
				this.Dropping_tele_part1.PlayAnim("dropping_tele_part1").Enter("Setdropping_tele_part1", delegate(DupeTeleporter.SMInstance smi)
				{
				}).Exit("Exitdropping_tele_part1", delegate(DupeTeleporter.SMInstance smi)
				{
					PortalCore component = smi.GetComponent<PortalCore>();
					component.doDrop();
				}).OnAnimQueueComplete(this.Dropping_tele_part2);
				this.Dropping_tele_part2.PlayAnim("Dropping_tele_part2").Enter("SetDropping_tele_part2", delegate(DupeTeleporter.SMInstance smi)
				{
				}).OnAnimQueueComplete(this.Retracting_output);
				this.Retracting_output.PlayAnim("Retracting_output").Enter("SetRetracting_output", delegate(DupeTeleporter.SMInstance smi)
				{
				}).OnAnimQueueComplete(this.Searching);
			}

			// Token: 0x040000A5 RID: 165
			public GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.State Idle;

			// Token: 0x040000A6 RID: 166
			public GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.State Searching;

			// Token: 0x040000A7 RID: 167
			public GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.State Teleporting_input;

			// Token: 0x040000A8 RID: 168
			public GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.State Teleporting_part1;

			// Token: 0x040000A9 RID: 169
			public GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.State Teleporting_loop;

			// Token: 0x040000AA RID: 170
			public GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.State Teleporting_part2;

			// Token: 0x040000AB RID: 171
			public GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.State Teleporting_output;

			// Token: 0x040000AC RID: 172
			public GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.State Dropping_tele_part1;

			// Token: 0x040000AD RID: 173
			public GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.State Dropping_tele_part2;

			// Token: 0x040000AE RID: 174
			public GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.State Retracting_input;

			// Token: 0x040000AF RID: 175
			public GameStateMachine<DupeTeleporter.States, DupeTeleporter.SMInstance, DupeTeleporter, object>.State Retracting_output;
		}
	}
    
   
    
    public interface IIndex
    {
	    // Token: 0x17000003 RID: 3
	    // (get) Token: 0x06000031 RID: 49
	    // (set) Token: 0x06000032 RID: 50
	    int ID { get; set; }
    }
    
    
    [SerializationConfig(MemberSerialization.OptIn)]
    public class KGSlider : SideScreen, ISaveLoadable, ISingleSliderControl, ISliderControl
    {
	    // Token: 0x06000027 RID: 39 RVA: 0x00002D6C File Offset: 0x00000F6C
	    public float GetSliderMin(int index)
	    {
		    return 10f;
	    }

	    // Token: 0x06000028 RID: 40 RVA: 0x00002D84 File Offset: 0x00000F84
	    public float GetSliderMax(int index)
	    {
		    return 100000f;
	    }

	    // Token: 0x06000029 RID: 41 RVA: 0x00002D9C File Offset: 0x00000F9C
	    public float GetSliderValue(int index)
	    {
		    return this.gPerTransform;
	    }

	    // Token: 0x0600002A RID: 42 RVA: 0x00002DB4 File Offset: 0x00000FB4
	    public void SetSliderValue(float percent, int index)
	    {
		    this.gPerTransform = percent;
	    }

	    // Token: 0x0600002B RID: 43 RVA: 0x00002DC0 File Offset: 0x00000FC0
	    public string GetSliderTooltipKey(int index)
	    {
		    return PortalCubeStrings.UI.UISIDESCREENS.SLIDERTITLE;
	    }

	    // Token: 0x0600002C RID: 44 RVA: 0x00002DDC File Offset: 0x00000FDC
	    public string GetSliderTooltip()
	    {
		    return PortalCubeStrings.UI.UISIDESCREENS.SLIDERTITLE;
	    }

	    // Token: 0x0600002D RID: 45 RVA: 0x00002DF8 File Offset: 0x00000FF8
	    public int SliderDecimalPlaces(int index)
	    {
		    return 1;
	    }

	    // Token: 0x17000001 RID: 1
	    // (get) Token: 0x0600002E RID: 46 RVA: 0x00002E0C File Offset: 0x0000100C
	    public string SliderTitleKey
	    {
		    get
		    {
			    return PortalCubeStrings.UI.UISIDESCREENS.SLIDERTITLE;
		    }
	    }

	    // Token: 0x17000002 RID: 2
	    // (get) Token: 0x0600002F RID: 47 RVA: 0x00002E28 File Offset: 0x00001028
	    public string SliderUnits
	    {
		    get
		    {
			    return "kg";
		    }
	    }

	    // Token: 0x0400000E RID: 14
	    [Serialize]
	    public float gPerTransform = 10f;
    }
    
    
    
    
    
    
    [SerializationConfig(MemberSerialization.OptIn)]
	[AddComponentMenu("KMonoBehaviour/scripts/MyChoreRangeVisualizer")]
	public class MyChoreRangeVisualizer : KMonoBehaviour
	{
		// Token: 0x06000033 RID: 51 RVA: 0x00002E54 File Offset: 0x00001054
		protected override void OnSpawn()
		{
			base.OnSpawn();
			base.Subscribe<MyChoreRangeVisualizer>(-1503271301, MyChoreRangeVisualizer.OnSelectDelegate);
			bool flag = this.movable;
			if (flag)
			{
				Singleton<CellChangeMonitor>.Instance.RegisterCellChangedHandler(base.transform, new System.Action(this.OnCellChange), "MyChoreRangeVisualizer.OnSpawn");
				base.Subscribe<MyChoreRangeVisualizer>(-1643076535, MyChoreRangeVisualizer.OnRotatedDelegate);
			}
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002EBC File Offset: 0x000010BC
		protected override void OnCleanUp()
		{
			Singleton<CellChangeMonitor>.Instance.UnregisterCellChangedHandler(base.transform, new System.Action(this.OnCellChange));
			base.Unsubscribe<MyChoreRangeVisualizer>(-1503271301, MyChoreRangeVisualizer.OnSelectDelegate, false);
			base.Unsubscribe<MyChoreRangeVisualizer>(-1643076535, MyChoreRangeVisualizer.OnRotatedDelegate, false);
			this.ClearVisualizers();
			base.OnCleanUp();
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00002F1C File Offset: 0x0000111C
		private void OnSelect(object data)
		{
			bool flag = (bool)data;
			if (flag)
			{
				SoundEvent.PlayOneShot(GlobalAssets.GetSound("RadialGrid_form", false), base.transform.position, 1f);
				this.UpdateVisualizers();
			}
			else
			{
				SoundEvent.PlayOneShot(GlobalAssets.GetSound("RadialGrid_disappear", false), base.transform.position, 1f);
				this.ClearVisualizers();
			}
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002F87 File Offset: 0x00001187
		private void OnRotated(object data)
		{
			this.UpdateVisualizers();
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00002F91 File Offset: 0x00001191
		private void OnCellChange()
		{
			this.UpdateVisualizers();
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002F9C File Offset: 0x0000119C
		public void UpdateVisualizers()
		{
			PortalCore component = base.GetComponent<PortalCore>();
			this.newCells.Clear();
			CellOffset rotatedCellOffset = this.vision_offset;
			bool flag = this.rotatable;
			if (flag)
			{
				rotatedCellOffset = this.rotatable.GetRotatedCellOffset(this.vision_offset);
			}
			int cell = Grid.PosToCell(base.transform.gameObject);
			int num;
			int num2;
			Grid.CellToXY(Grid.OffsetCell(cell, rotatedCellOffset), out num, out num2);
			Orientation orientation = this.rotatable.GetOrientation();
			for (int i = 0; i < this.height; i++)
			{
				for (int j = 0; j < this.width; j++)
				{
					int num3 = 0;
					int num4 = 0;
					CellOffset offset = new CellOffset(num3 + j, num4 + i);
					switch (orientation)
					{
					case Orientation.Neutral:
						num3 = 0;
						num4 = 1;
						offset = new CellOffset(num3 + j + this.reduceXX, num4 + i);
						break;
					case Orientation.R90:
						num3 = 1;
						num4 = 0;
						offset = new CellOffset(num3 + i, num4 + j + this.reduceXX);
						break;
					case Orientation.R180:
						num3 = 0;
						num4 = -1;
						offset = new CellOffset(num3 + j + this.reduceXX, num4 - i);
						break;
					case Orientation.R270:
						num3 = -1;
						num4 = 0;
						offset = new CellOffset(num3 - i, num4 + j + this.reduceXX);
						break;
					}
					int num5 = Grid.OffsetCell(cell, offset);
					bool flag2 = Grid.IsValidCell(num5);
					if (flag2)
					{
						int x;
						int y;
						Grid.CellToXY(num5, out x, out y);
						bool flag3 = Grid.TestLineOfSight(num, num2, x, y, this.blocking_cb, this.blocking_tile_visible);
						if (flag3)
						{
							this.newCells.Add(num5);
						}
					}
				}
			}
			for (int k = this.visualizers.Count - 1; k >= 0; k--)
			{
				bool flag4 = this.newCells.Contains(this.visualizers[k].cell);
				if (flag4)
				{
					this.newCells.Remove(this.visualizers[k].cell);
				}
				else
				{
					this.DestroyEffect(this.visualizers[k].controller);
					this.visualizers.RemoveAt(k);
				}
			}
			for (int l = 0; l < this.newCells.Count; l++)
			{
				KBatchedAnimController controller = this.CreateEffect(this.newCells[l]);
				this.visualizers.Add(new MyChoreRangeVisualizer.VisData
				{
					cell = this.newCells[l],
					controller = controller
				});
			}
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00003270 File Offset: 0x00001470
		private void ClearVisualizers()
		{
			for (int i = 0; i < this.visualizers.Count; i++)
			{
				this.DestroyEffect(this.visualizers[i].controller);
			}
			this.visualizers.Clear();
		}

		// Token: 0x0600003A RID: 58 RVA: 0x000032C0 File Offset: 0x000014C0
		private KBatchedAnimController CreateEffect(int cell)
		{
			KBatchedAnimController kbatchedAnimController = FXHelpers.CreateEffect(MyChoreRangeVisualizer.AnimName, Grid.CellToPosCCC(cell, this.sceneLayer), null, false, this.sceneLayer, true);
			kbatchedAnimController.destroyOnAnimComplete = false;
			kbatchedAnimController.visibilityType = KAnimControllerBase.VisibilityType.Always;
			kbatchedAnimController.gameObject.SetActive(true);
			kbatchedAnimController.Play(MyChoreRangeVisualizer.PreAnims, KAnim.PlayMode.Loop);
			return kbatchedAnimController;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x0000331B File Offset: 0x0000151B
		private void DestroyEffect(KBatchedAnimController controller)
		{
			controller.destroyOnAnimComplete = true;
			controller.Play(MyChoreRangeVisualizer.PostAnim, KAnim.PlayMode.Once, 1f, 0f);
		}

		// Token: 0x0400000F RID: 15
		[MyCmpReq]
		private KSelectable selectable;

		// Token: 0x04000010 RID: 16
		[MyCmpGet]
		private Rotatable rotatable;

		// Token: 0x04000011 RID: 17
		[SerializeField]
		[Serialize]
		public int x;

		// Token: 0x04000012 RID: 18
		[SerializeField]
		[Serialize]
		public int y;

		// Token: 0x04000013 RID: 19
		[SerializeField]
		[Serialize]
		public int reduceXX = 0;

		// Token: 0x04000014 RID: 20
		[SerializeField]
		[Serialize]
		public int width;

		// Token: 0x04000015 RID: 21
		[SerializeField]
		[Serialize]
		public int height;

		// Token: 0x04000016 RID: 22
		public bool movable;

		// Token: 0x04000017 RID: 23
		public Grid.SceneLayer sceneLayer = Grid.SceneLayer.FXFront;

		// Token: 0x04000018 RID: 24
		public CellOffset vision_offset;

		// Token: 0x04000019 RID: 25
		public Func<int, bool> blocking_cb = new Func<int, bool>(Grid.PhysicalBlockingCB);

		// Token: 0x0400001A RID: 26
		public bool blocking_tile_visible = true;

		// Token: 0x0400001B RID: 27
		private static readonly string AnimName = "transferarmgrid_kanim";

		// Token: 0x0400001C RID: 28
		private static readonly HashedString[] PreAnims = new HashedString[]
		{
			"grid_pre",
			"grid_loop"
		};

		// Token: 0x0400001D RID: 29
		private static readonly HashedString PostAnim = "grid_pst";

		// Token: 0x0400001E RID: 30
		private List<MyChoreRangeVisualizer.VisData> visualizers = new List<MyChoreRangeVisualizer.VisData>();

		// Token: 0x0400001F RID: 31
		private List<int> newCells = new List<int>();

		// Token: 0x04000020 RID: 32
		private static readonly EventSystem.IntraObjectHandler<MyChoreRangeVisualizer> OnSelectDelegate = new EventSystem.IntraObjectHandler<MyChoreRangeVisualizer>(delegate(MyChoreRangeVisualizer component, object data)
		{
			component.OnSelect(data);
		});

		// Token: 0x04000021 RID: 33
		private static readonly EventSystem.IntraObjectHandler<MyChoreRangeVisualizer> OnRotatedDelegate = new EventSystem.IntraObjectHandler<MyChoreRangeVisualizer>(delegate(MyChoreRangeVisualizer component, object data)
		{
			component.OnRotated(data);
		});

		// Token: 0x0200001B RID: 27
		private struct VisData
		{
			// Token: 0x04000091 RID: 145
			public int cell;

			// Token: 0x04000092 RID: 146
			public KBatchedAnimController controller;
		}
	}
	
	
	public class NavTeleporterNew : KMonoBehaviour
	{
		// Token: 0x0600001C RID: 28 RVA: 0x0000298D File Offset: 0x00000B8D
		protected override void OnPrefabInit()
		{
			base.OnPrefabInit();
			base.GetComponent<KPrefabID>().AddTag(GameTags.NavTeleporters, false);
			base.gameObject.FindOrAddComponent<Rotatable>();
			this.Register();
		}

		// Token: 0x0600001D RID: 29 RVA: 0x000029BC File Offset: 0x00000BBC
		protected override void OnCleanUp()
		{
			base.OnCleanUp();
			int cell = this.GetCell(true);
			bool flag = cell != Grid.InvalidCell;
			if (flag)
			{
				Grid.HasNavTeleporter[cell] = false;
			}
			this.Deregister();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000029FE File Offset: 0x00000BFE
		public void SetOverrideCell(int cell)
		{
			this.overrideCell = cell;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002A08 File Offset: 0x00000C08
		public int GetCell(bool isInputCell)
		{
			Rotatable component = base.gameObject.GetComponent<Rotatable>();
			bool flag = component == null;
			int result;
			if (flag)
			{
				result = Grid.InvalidCell;
			}
			else
			{
				Vector3 position = base.transform.GetPosition();
				Vector3 rotatedOffset = Rotatable.GetRotatedOffset(new Vector3(0f, 1f), component.GetOrientation());
				Vector3 rotatedOffset2 = Rotatable.GetRotatedOffset(new Vector3(0f, -1f), component.GetOrientation());
				switch (component.GetOrientation())
				{
				case Orientation.Neutral:
					rotatedOffset2.y -= 1f;
					break;
				case Orientation.R180:
					rotatedOffset.y -= 1f;
					break;
				}
				int num = Grid.PosToCell(position + rotatedOffset);
				int num2 = Grid.PosToCell(position + rotatedOffset2);
				int num3;
				int num4;
				Grid.CellToXY(num, out num3, out num4);
				int num5;
				int num6;
				Grid.CellToXY(num2, out num5, out num6);
				result = (isInputCell ? num : num2);
			}
			return result;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002B10 File Offset: 0x00000D10
		public void OneWayTarget(NavTeleporterNew nt)
		{
			this.BreakLink();
			bool flag = nt != null;
			if (flag)
			{
				this.SetLink(nt);
			}
			else
			{
				this.BreakLink();
			}
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002B48 File Offset: 0x00000D48
		public void Register()
		{
			int cell = this.GetCell(true);
			bool flag = !Grid.IsValidCell(cell);
			if (flag)
			{
				this.lastRegisteredCell = Grid.InvalidCell;
			}
			else
			{
				Grid.HasNavTeleporter[cell] = true;
				Pathfinding.Instance.GetNavGrid(MinionConfig.MINION_NAV_GRID_NAME);
				Pathfinding.Instance.AddDirtyNavGridCell(cell);
				this.lastRegisteredCell = cell;
				int num;
				int num2;
				Grid.CellToXY(this.lastRegisteredCell, out num, out num2);
			}
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002BBC File Offset: 0x00000DBC
		private void SetLink(NavTeleporterNew nt)
		{
			int cell = nt.GetCell(false);
			int num;
			int num2;
			Grid.CellToXY(this.lastRegisteredCell, out num, out num2);
			int num3;
			int num4;
			Grid.CellToXY(cell, out num3, out num4);
			Pathfinding.Instance.GetNavGrid(MinionConfig.MINION_NAV_GRID_NAME).teleportTransitions[this.lastRegisteredCell] = cell;
			Pathfinding.Instance.AddDirtyNavGridCell(this.lastRegisteredCell);
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002C20 File Offset: 0x00000E20
		public void InputLinkToOutput(bool setOrBreak)
		{
			int cell = this.GetCell(true);
			bool flag = this.lastRegisteredCell != cell;
			if (flag)
			{
				this.Deregister();
				this.Register();
			}
			if (setOrBreak)
			{
				int num;
				int num2;
				Grid.CellToXY(this.lastRegisteredCell, out num, out num2);
				int num3;
				int num4;
				Grid.CellToXY(this.GetCell(false), out num3, out num4);
				Pathfinding.Instance.GetNavGrid(MinionConfig.MINION_NAV_GRID_NAME).teleportTransitions[this.lastRegisteredCell] = this.GetCell(false);
				Pathfinding.Instance.AddDirtyNavGridCell(this.lastRegisteredCell);
			}
			else
			{
				this.BreakLink();
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002CC4 File Offset: 0x00000EC4
		public void Deregister()
		{
			bool flag = this.lastRegisteredCell != Grid.InvalidCell;
			if (flag)
			{
				this.BreakLink();
				Grid.HasNavTeleporter[this.lastRegisteredCell] = false;
				Pathfinding.Instance.AddDirtyNavGridCell(this.lastRegisteredCell);
				this.lastRegisteredCell = Grid.InvalidCell;
			}
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002D1D File Offset: 0x00000F1D
		private void BreakLink()
		{
			Pathfinding.Instance.GetNavGrid(MinionConfig.MINION_NAV_GRID_NAME).teleportTransitions.Remove(this.lastRegisteredCell);
			Pathfinding.Instance.AddDirtyNavGridCell(this.lastRegisteredCell);
		}

		// Token: 0x0400000A RID: 10
		[MyCmpReq]
		private PortalCore pc;

		// Token: 0x0400000B RID: 11
		private int lastRegisteredCell = Grid.InvalidCell;

		// Token: 0x0400000C RID: 12
		public CellOffset offset;

		// Token: 0x0400000D RID: 13
		private int overrideCell = -1;
	}
    
	public class Patches : UserMod2
	{
		// Token: 0x06000099 RID: 153 RVA: 0x0000749A File Offset: 0x0000569A
		public override void OnLoad(Harmony harmony)
		{
			base.OnLoad(harmony);
			PUtil.InitLibrary(true);
			new PLocalization().Register(null);
			new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
		}

		// Token: 0x0200002F RID: 47
		[HarmonyPatch(typeof(Db))]
		[HarmonyPatch("Initialize")]
		public class Db_Initialize_Patch
		{
			// Token: 0x060000FD RID: 253 RVA: 0x00008800 File Offset: 0x00006A00
			public static void Prefix()
			{
				StringUtils.AddBuildingStrings("SolidPortalCube", PortalCubeStrings.BUILDINGS.PREFABS.SOLIDPORTALCUBE.NAME, PortalCubeStrings.BUILDINGS.PREFABS.SOLIDPORTALCUBE.DESC, PortalCubeStrings.BUILDINGS.PREFABS.SOLIDPORTALCUBE.EFFECT);
				StringUtils.AddBuildingStrings("LiquidPortalCube", PortalCubeStrings.BUILDINGS.PREFABS.LIQUIDPORTALCUBE.NAME, PortalCubeStrings.BUILDINGS.PREFABS.LIQUIDPORTALCUBE.DESC, PortalCubeStrings.BUILDINGS.PREFABS.LIQUIDPORTALCUBE.EFFECT);
				StringUtils.AddBuildingStrings("GasPortalCube", PortalCubeStrings.BUILDINGS.PREFABS.GASPORTALCUBE.NAME, PortalCubeStrings.BUILDINGS.PREFABS.GASPORTALCUBE.DESC, PortalCubeStrings.BUILDINGS.PREFABS.GASPORTALCUBE.EFFECT);
				StringUtils.AddBuildingStrings("DupePortalCube", PortalCubeStrings.BUILDINGS.PREFABS.DUPEPORTALCUBE.NAME, PortalCubeStrings.BUILDINGS.PREFABS.DUPEPORTALCUBE.DESC, PortalCubeStrings.BUILDINGS.PREFABS.DUPEPORTALCUBE.EFFECT);
			}

			// Token: 0x060000FE RID: 254 RVA: 0x000088B4 File Offset: 0x00006AB4
			public static void Postfix()
			{
				BuildingUtils.AddBuildingToPlanScreen("SolidPortalCube", "Conveyance", null);
				BuildingUtils.AddBuildingToTech("SolidPortalCube", "SolidSpace");
				BuildingUtils.AddBuildingToPlanScreen("DupePortalCube", "Equipment", null);
				BuildingUtils.AddBuildingToTech("DupePortalCube", "SolidSpace");
				BuildingUtils.AddBuildingToPlanScreen("LiquidPortalCube", "Plumbing", null);
				BuildingUtils.AddBuildingToTech("LiquidPortalCube", "LiquidFiltering");
				BuildingUtils.AddBuildingToPlanScreen("GasPortalCube", "HVAC", null);
				BuildingUtils.AddBuildingToTech("GasPortalCube", "HVAC");
			}
		}

		// Token: 0x02000030 RID: 48
		[HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
		public static class DetailsScreen_OnPrefabInit_Patch
		{
			// Token: 0x06000100 RID: 256 RVA: 0x00008963 File Offset: 0x00006B63
			internal static void Postfix(List<DetailsScreen.SideScreenRef> ___sideScreens, GameObject ___sideScreenContentBody)
			{
				PortalSideScreen.AddSideScreen(___sideScreens, ___sideScreenContentBody);
			}
		}

		// Token: 0x02000031 RID: 49
		[HarmonyPatch(typeof(Reactable), "UpdateLocation")]
		public static class Reactable_UpdateLocation_Patch
		{
			// Token: 0x06000101 RID: 257 RVA: 0x00008970 File Offset: 0x00006B70
			internal static void Postfix(Reactable __instance)
			{
				StoppingReact.PortalEnterWorkableReactable portalEnterWorkableReactable = __instance as StoppingReact.PortalEnterWorkableReactable;
				bool flag = portalEnterWorkableReactable != null;
				if (flag)
				{
					portalEnterWorkableReactable.MyUpdateLocation();
				}
			}
		}
	}
	
	public static class PortalComponents
	{
		// Token: 0x0600007A RID: 122 RVA: 0x00005FDC File Offset: 0x000041DC
		public static Components.Cmps<T> GetTelepoterInstance<T>(Element.State elementState) where T : KMonoBehaviour
		{
			bool flag = typeof(T) == typeof(PortalConnection);
			if (flag)
			{
				switch (elementState)
				{
				case Element.State.Gas:
					return PortalComponents.gas_portal_connection_instance as Components.Cmps<T>;
				case Element.State.Liquid:
					return PortalComponents.liquid_portal_connection_instance as Components.Cmps<T>;
				case Element.State.Solid:
					return PortalComponents.solid_portal_connection_instance as Components.Cmps<T>;
				case Element.State.Unbreakable:
					return PortalComponents.dupe_portal_connection_instance as Components.Cmps<T>;
				}
			}
			bool flag2 = typeof(T) == typeof(PortalCore);
			if (flag2)
			{
				switch (elementState)
				{
				case Element.State.Gas:
					return PortalComponents.gas_portal_core_instance as Components.Cmps<T>;
				case Element.State.Liquid:
					return PortalComponents.liquid_portal_core_instance as Components.Cmps<T>;
				case Element.State.Solid:
					return PortalComponents.solid_portal_core_instance as Components.Cmps<T>;
				}
			}
			bool flag3 = typeof(T) == typeof(DupeTeleporter);
			if (flag3)
			{
				if (elementState == Element.State.Unbreakable)
				{
					return PortalComponents.dupe_portal_teleporter_statemachine_instance as Components.Cmps<T>;
				}
			}
			return null;
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00006110 File Offset: 0x00004310
		public static bool TryGetComponents<T>(this KMonoBehaviour behaviour, out List<T> ts) where T : KMonoBehaviour
		{
			bool result = false;
			try
			{
				ts = behaviour.gameObject.GetComponents<T>().ToList<T>();
				result = true;
			}
			catch
			{
				try
				{
					ts = behaviour.GetComponents<T>().ToList<T>();
					result = true;
				}
				catch
				{
					ts = new List<T>();
				}
			}
			return result;
		}

		// Token: 0x0400004F RID: 79
		private static Components.Cmps<PortalConnection> solid_portal_connection_instance = new Components.Cmps<PortalConnection>();

		// Token: 0x04000050 RID: 80
		private static Components.Cmps<PortalConnection> liquid_portal_connection_instance = new Components.Cmps<PortalConnection>();

		// Token: 0x04000051 RID: 81
		private static Components.Cmps<PortalConnection> gas_portal_connection_instance = new Components.Cmps<PortalConnection>();

		// Token: 0x04000052 RID: 82
		private static Components.Cmps<PortalConnection> dupe_portal_connection_instance = new Components.Cmps<PortalConnection>();

		// Token: 0x04000053 RID: 83
		private static Components.Cmps<PortalCore> solid_portal_core_instance = new Components.Cmps<PortalCore>();

		// Token: 0x04000054 RID: 84
		private static Components.Cmps<PortalCore> liquid_portal_core_instance = new Components.Cmps<PortalCore>();

		// Token: 0x04000055 RID: 85
		private static Components.Cmps<PortalCore> gas_portal_core_instance = new Components.Cmps<PortalCore>();

		// Token: 0x04000056 RID: 86
		private static Components.Cmps<DupeTeleporter> dupe_portal_teleporter_statemachine_instance = new Components.Cmps<DupeTeleporter>();

		// Token: 0x04000057 RID: 87
		public static SortedSet<int>[] senderhs = new SortedSet<int>[]
		{
			new SortedSet<int>(),
			new SortedSet<int>(),
			new SortedSet<int>()
		};

		// Token: 0x04000058 RID: 88
		public static Dictionary<int, int> targetPortalDic = new Dictionary<int, int>();
	}
	
	// Token: 0x0200000B RID: 11
	[SerializationConfig(MemberSerialization.OptIn)]
	public class PortalConnection : KMonoBehaviour, IIndex, IComparable
	{
		// Token: 0x0600003E RID: 62 RVA: 0x00003418 File Offset: 0x00001618
		protected override void OnPrefabInit()
		{
			base.OnPrefabInit();
			base.Subscribe<PortalConnection>(-592767678, PortalConnection.OnOperationalChangedDelegate);
			PortalComponents.GetTelepoterInstance<PortalConnection>(this.portalType)
				.Register(new Action<PortalConnection>(this.OnAdd), new Action<PortalConnection>(this.OnRemove));
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00003468 File Offset: 0x00001668
		protected override void OnSpawn()
		{
			this.OnOperationalChanged(this.operational.IsOperational);
			base.OnSpawn();
			Building component = base.GetComponent<Building>();
			this.ID = component.GetCell();
			PortalComponents.GetTelepoterInstance<PortalConnection>(this.portalType).Add(this);
			bool flag = this.targetPortalDic.Count > 0 && PortalComponents.targetPortalDic.Count == 0;
			if (flag)
			{
				PortalComponents.targetPortalDic = this.targetPortalDic;
			}
		}

		// Token: 0x06000040 RID: 64 RVA: 0x000034E8 File Offset: 0x000016E8
		public int CompareTo(object obj)
		{
			bool flag = obj == null;
			int result;
			if (flag)
			{
				result = 1;
			}
			else
			{
				PortalConnection portalConnection = obj as PortalConnection;
				int num = this.GetMyWorld().GetComponent<ClusterGridEntity>().Name
					.CompareTo(portalConnection.GetMyWorld().GetComponent<ClusterGridEntity>().Name);
				bool flag2 = num != 0;
				if (flag2)
				{
					result = num;
				}
				else
				{
					result = this.ID.CompareTo(portalConnection.ID);
				}
			}

			return result;
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00003558 File Offset: 0x00001758
		protected override void OnCleanUp()
		{
			PortalCore component = base.GetComponent<PortalCore>();
			bool flag = this.portalType == Element.State.Unbreakable;
			if (flag)
			{
				List<PortalConnection> list = new List<PortalConnection>();
				bool flag2 = component.tryGetChildrenPortals(out list);
				if (flag2)
				{
					foreach (PortalConnection portalConnection in list)
					{
						portalConnection.GetComponent<NavTeleporterNew>().OneWayTarget(null);
					}
				}
			}

			this.targetPortalDic.Remove(this.ID);
			PortalComponents.targetPortalDic.Remove(this.ID);
			List<int> children = component.getChildren(this.ID);
			foreach (int key in children)
			{
				this.targetPortalDic.Remove(key);
				PortalComponents.targetPortalDic.Remove(key);
			}

			base.Unsubscribe<PortalConnection>(-592767678, PortalConnection.OnOperationalChangedDelegate, false);
			PortalComponents.GetTelepoterInstance<PortalConnection>(this.portalType).Remove(this);
			base.OnCleanUp();
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003694 File Offset: 0x00001894
		internal List<PortalConnection> GetTarget(int ID)
		{
			bool flag = !PortalComponents.targetPortalDic.ContainsKey(ID);
			List<PortalConnection> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				int targetPortalID = PortalComponents.targetPortalDic[ID];
				result = PortalComponents.GetTelepoterInstance<PortalConnection>(this.portalType).Items
					.FindAll((PortalConnection x) => targetPortalID == x.ID);
			}

			return result;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x000036F8 File Offset: 0x000018F8
		public void OnAdd(PortalConnection t)
		{
			bool flag = t != this;
			if (!flag)
			{
				bool flag2 = PortalComponents.targetPortalDic.ContainsKey(this.ID);
				if (!flag2)
				{
					Components.Cmps<PortalConnection> telepoterInstance =
						PortalComponents.GetTelepoterInstance<PortalConnection>(t.portalType);
					SortedSet<int> sortedSet = PortalComponents.senderhs[(int)((t.portalType - Element.State.Gas) % 3)];
					for (int i = 0; i < telepoterInstance.Count + 10; i++)
					{
						bool flag3 = !sortedSet.Contains(i);
						if (flag3)
						{
							sortedSet.Add(i);
							break;
						}
					}

					this.UpdateName();
				}
			}
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003790 File Offset: 0x00001990
		public void OnRemove(PortalConnection t)
		{
			bool flag = t != this;
			if (!flag)
			{
				PortalComponents.GetTelepoterInstance<PortalConnection>(t.portalType);
			}
		}

		// Token: 0x06000045 RID: 69 RVA: 0x000037B8 File Offset: 0x000019B8
		public void UpdateName()
		{
			UserNameable component = base.GetComponent<UserNameable>();
			bool flag = component != null && component.savedName.Contains(this.id.ToString());
			if (flag)
			{
				component.SetName(((component != null) ? component.savedName : null) + " " + this.id.ToString());
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000046 RID: 70 RVA: 0x00003824 File Offset: 0x00001A24
		// (set) Token: 0x06000047 RID: 71 RVA: 0x0000383C File Offset: 0x00001A3C
		public int ID
		{
			get { return this.id; }
			set { this.id = value; }
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00003848 File Offset: 0x00001A48
		public void OnOperationalChanged(object data)
		{
			bool value = (bool)data;
			this.operational.SetActive(value, false);
		}

		// Token: 0x04000022 RID: 34
		[SerializeField] [Serialize] public Dictionary<int, int> targetPortalDic = new Dictionary<int, int>();

		// Token: 0x04000023 RID: 35
		[Serialize] private int id = -1;

		// Token: 0x04000024 RID: 36
		[SerializeField] [Serialize] public int currentTurn;

		// Token: 0x04000025 RID: 37
		public Element.State portalType;

		// Token: 0x04000026 RID: 38
		[MyCmpGet] public Operational operational;

		// Token: 0x04000027 RID: 39
		private static readonly EventSystem.IntraObjectHandler<PortalConnection> OnOperationalChangedDelegate =
			new EventSystem.IntraObjectHandler<PortalConnection>(delegate(PortalConnection component, object data)
			{
				component.OnOperationalChanged(data);
			});
	}

	
	// Token: 0x0200000C RID: 12
	[SerializationConfig(MemberSerialization.OptIn)]
	public class PortalCore : KMonoBehaviour, IGameObjectEffectDescriptor, ISim4000ms
	{
		// Token: 0x0600004B RID: 75 RVA: 0x000038A4 File Offset: 0x00001AA4
		protected override void OnSpawn()
		{
			base.OnSpawn();
			Rotatable component = base.GetComponent<Rotatable>();
			Vector3 position = base.transform.GetPosition();
			Vector3 rotatedOffset = Rotatable.GetRotatedOffset(new Vector3(0f, 1f), component.GetOrientation());
			Vector3 rotatedOffset2 = Rotatable.GetRotatedOffset(new Vector3(0f, -1f), component.GetOrientation());
			this.myInPutCell = Grid.PosToCell(position + rotatedOffset);
			this.myOutPutCell = Grid.PosToCell(position + rotatedOffset2);
			int num;
			int num2;
			Grid.CellToXY(this.myInPutCell, out num, out num2);
			int num3;
			int num4;
			Grid.CellToXY(this.myOutPutCell, out num3, out num4);
			this.blocked(false);
			this.getReadyCells();
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003955 File Offset: 0x00001B55
		protected override void OnPrefabInit()
		{
			base.OnPrefabInit();
			base.Subscribe<PortalCore>(493375141, PortalCore.OnRefreshUserMenuDelegate);
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00003970 File Offset: 0x00001B70
		public void toggleChildrenNavLines(List<PortalConnection> children, bool ConnectOrDeconnect, bool hasTargetAndChildrenBoth = false, bool forceToToggle = false)
		{
			bool flag = false;
			if (hasTargetAndChildrenBoth)
			{
				children.Add(this.pc);
			}
			bool flag2 = ConnectOrDeconnect && (!this.hasChildrenNavConnected || forceToToggle);
			if (flag2)
			{
				foreach (PortalConnection portalConnection in children)
				{
					NavTeleporterNew component = portalConnection.GetComponent<NavTeleporterNew>();
					bool flag3 = component != null;
					if (flag3)
					{
						PortalCore component2 = portalConnection.GetComponent<PortalCore>();
						PortalConnection portalConnection2;
						component2.tryGetTargetPortal(out portalConnection2);
						PortalCore component3 = portalConnection2.GetComponent<PortalCore>();
						bool flag4 = portalConnection2 != null && component2.isPortalValid() && component3.isPortalValid();
						if (flag4)
						{
							NavTeleporterNew component4 = portalConnection2.GetComponent<NavTeleporterNew>();
							bool flag5 = component4 != null;
							if (flag5)
							{
								flag = true;
								component.OneWayTarget(component4);
							}
						}
					}
				}
				bool flag6 = flag;
				if (flag6)
				{
					this.hasChildrenNavConnected = true;
					this.hasChildrenNavDeconnected = false;
				}
			}
			bool flag7 = !ConnectOrDeconnect && (!this.hasChildrenNavDeconnected || forceToToggle);
			if (flag7)
			{
				foreach (PortalConnection portalConnection3 in children)
				{
					NavTeleporterNew component5 = portalConnection3.GetComponent<NavTeleporterNew>();
					bool flag8 = component5 != null;
					if (flag8)
					{
						component5.OneWayTarget(null);
						flag = true;
					}
				}
				bool flag9 = flag;
				if (flag9)
				{
					this.hasChildrenNavDeconnected = true;
					this.hasChildrenNavConnected = false;
				}
			}
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003B1C File Offset: 0x00001D1C
		public void toggleSelfNavLines(bool ConnectOrDeconnect, bool forceToToggle = false)
		{
			bool flag = ConnectOrDeconnect && (!this.hasSelfConnected || forceToToggle);
			if (flag)
			{
				NavTeleporterNew component = this.pc.GetComponent<NavTeleporterNew>();
				component.InputLinkToOutput(ConnectOrDeconnect);
				this.hasSelfConnected = true;
			}
			bool flag2 = !ConnectOrDeconnect && (this.hasSelfConnected || forceToToggle);
			if (flag2)
			{
				NavTeleporterNew component2 = this.pc.GetComponent<NavTeleporterNew>();
				component2.InputLinkToOutput(ConnectOrDeconnect);
				this.hasSelfConnected = false;
			}
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003B8C File Offset: 0x00001D8C
		public bool isPortalValid()
		{
			return !Grid.Solid[this.myOutPutCell] && this.isLogicEnabled();
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00003BBC File Offset: 0x00001DBC
		public bool blocked(bool forceToToggle = false)
		{
			bool flag = this.myOutPutCell < 0;
			bool result;
			if (flag)
			{
				result = true;
			}
			else
			{
				bool flag2 = this.pc != null && this.pc.portalType == Element.State.Unbreakable;
				if (flag2)
				{
					PortalConnection portalConnection;
					bool flag3 = this.tryGetTargetPortal(out portalConnection);
					List<PortalConnection> list;
					bool flag4 = this.tryGetChildrenPortals(out list);
					bool flag5 = !Grid.Solid[this.myOutPutCell] && this.isLogicEnabled();
					bool flag6 = flag3 && flag4;
					if (flag6)
					{
						bool flag7 = flag5;
						if (flag7)
						{
							this.toggleChildrenNavLines(list, true, true, forceToToggle);
						}
						else
						{
							this.toggleChildrenNavLines(list, false, true, forceToToggle);
						}
					}
					else
					{
						bool flag8 = flag4;
						if (flag8)
						{
							bool flag9 = flag5;
							if (flag9)
							{
								this.toggleChildrenNavLines(list, true, false, forceToToggle);
							}
							else
							{
								this.toggleChildrenNavLines(list, false, false, forceToToggle);
							}
						}
						bool flag10 = !flag3;
						if (flag10)
						{
							bool flag11 = flag5;
							if (flag11)
							{
								this.toggleSelfNavLines(true, forceToToggle);
							}
							else
							{
								this.toggleSelfNavLines(false, forceToToggle);
							}
						}
						bool flag12 = flag3;
						if (flag12)
						{
							list.Clear();
							list.Add(this.pc);
							bool flag13 = flag5;
							if (flag13)
							{
								this.toggleChildrenNavLines(list, true, false, forceToToggle);
							}
							else
							{
								this.toggleChildrenNavLines(list, false, false, forceToToggle);
							}
						}
					}
				}
				bool flag14 = Grid.Solid[this.myOutPutCell];
				if (flag14)
				{
					this.blockedGuid = this.selectable.ToggleStatusItem(Db.Get().BuildingStatusItems.OutputTileBlocked, this.blockedGuid, true, null);
					base.GetComponent<KBatchedAnimController>().TintColour = PortalCore.BLOCKED_TINT;
					this.controller.SetActive(false, false);
					result = true;
				}
				else
				{
					bool flag15 = this.isLogicEnabled();
					if (flag15)
					{
						this.controller.SetActive(true, false);
						base.GetComponent<KBatchedAnimController>().TintColour = PortalCore.NORMAL_TINT;
						this.blockedGuid = this.selectable.ToggleStatusItem(Db.Get().BuildingStatusItems.OutputTileBlocked, this.blockedGuid, false, null);
					}
					result = false;
				}
			}
			return result;
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00003DD0 File Offset: 0x00001FD0
		public bool isLogicEnabled()
		{
			bool result = false;
			LogicPorts component = base.GetComponent<LogicPorts>();
			bool flag = component != null;
			if (flag)
			{
				LogicPorts.Port port = default(LogicPorts.Port);
				bool flag2 = false;
				base.GetComponent<LogicPorts>().TryGetPortAtCell(this.building.GetCell(), out port, out flag2);
				result = (component.GetInputValue(port.id) == 1 || !component.IsPortConnected(port.id));
			}
			return result;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00003E43 File Offset: 0x00002043
		public void Sim4000ms(float dt)
		{
			this.getReadyCells();
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00003E50 File Offset: 0x00002050
		public bool needPick()
		{
			bool flag = this.blocked(false);
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				foreach (int cell in this.objectCells)
				{
					int num;
					int num2;
					Grid.CellToXY(cell, out num, out num2);
					Element.State portalType = this.pc.portalType;
					Element.State state = portalType;
					Element.State state2 = state;
					if (state2 - Element.State.Gas > 1)
					{
						if (state2 - Element.State.Solid > 1)
						{
							return this.doSolidPick();
						}
						bool flag2 = this.TeleportableCellSolid(cell);
						if (flag2)
						{
							return true;
						}
					}
					else
					{
						bool flag3 = this.IsElementTakeable(cell);
						if (flag3)
						{
							return true;
						}
					}
				}
				result = false;
			}
			return result;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00003F1C File Offset: 0x0000211C
		public bool needDrop()
		{
			bool result = false;
			bool flag = this.storage != null && this.storage.MassStored() > 0f;
			if (flag)
			{
				result = true;
			}
			return result;
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00003F5C File Offset: 0x0000215C
		private int getTargetPortalOutPutCell()
		{
			PortalConnection portalConnection = new PortalConnection();
			Rotatable component = base.GetComponent<Rotatable>();
			Vector3 position = base.transform.GetPosition();
			Vector3 rotatedOffset = Rotatable.GetRotatedOffset(new Vector3(0f, 1f), component.GetOrientation());
			Vector3 rotatedOffset2 = Rotatable.GetRotatedOffset(new Vector3(0f, -1f), component.GetOrientation());
			int num = Grid.PosToCell(position + rotatedOffset);
			int num2 = Grid.PosToCell(position + rotatedOffset2);
			bool flag = !this.tryGetTargetPortal(out portalConnection);
			int result;
			if (flag)
			{
				result = num2;
			}
			else
			{
				Building component2 = portalConnection.GetComponent<Building>();
				Rotatable component3 = component2.GetComponent<Rotatable>();
				Vector3 position2 = component2.transform.GetPosition();
				Vector3 rotatedOffset3 = Rotatable.GetRotatedOffset(new Vector3(0f, 1f), component3.GetOrientation());
				Vector3 rotatedOffset4 = Rotatable.GetRotatedOffset(new Vector3(0f, -1f), component3.GetOrientation());
				int num3 = Grid.PosToCell(position2 + rotatedOffset3);
				int num4 = Grid.PosToCell(position2 + rotatedOffset4);
				int num5;
				int num6;
				Grid.CellToXY(num4, out num5, out num6);
				int num7 = num4;
				result = num7;
			}
			return result;
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00004088 File Offset: 0x00002288
		public bool tryGetChildrenPortals(out List<PortalConnection> childrenPortals)
		{
			childrenPortals = new List<PortalConnection>();
			this.pc = base.GetComponent<PortalConnection>();
			bool flag = this.pc == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				List<int> children = this.getChildren(this.pc.ID);
				bool flag2 = children.Count > 0;
				if (flag2)
				{
					using (List<int>.Enumerator enumerator = children.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							int childID = enumerator.Current;
							List<PortalConnection> list = PortalComponents.GetTelepoterInstance<PortalConnection>(this.pc.portalType).Items.FindAll((PortalConnection x) => childID == x.ID);
							bool flag3 = list.Count == 1;
							if (!flag3)
							{
								childrenPortals.Clear();
								return false;
							}
							childrenPortals.Add(list[0]);
						}
					}
					bool flag4 = childrenPortals.Count > 0;
					result = flag4;
				}
				else
				{
					result = false;
				}
			}
			return result;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x000041AC File Offset: 0x000023AC
		public bool tryGetTargetPortal(out PortalConnection targetPortal)
		{
			PortalConnection component = base.GetComponent<PortalConnection>();
			targetPortal = new PortalConnection();
			bool flag = component == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				int targetPortalID;
				bool flag2 = PortalComponents.targetPortalDic.TryGetValue(component.ID, out targetPortalID);
				if (flag2)
				{
					List<PortalConnection> list = PortalComponents.GetTelepoterInstance<PortalConnection>(component.portalType).Items.FindAll((PortalConnection x) => targetPortalID == x.ID);
					bool flag3 = list.Count == 1;
					if (flag3)
					{
						targetPortal = list[0];
						result = true;
					}
					else
					{
						result = false;
					}
				}
				else
				{
					result = false;
				}
			}
			return result;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00004248 File Offset: 0x00002448
		public static bool BlockingCB(int cell)
		{
			return Grid.Foundation[cell];
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00004268 File Offset: 0x00002468
		public bool TeleportableCellSolid(int cell)
		{
			GameObject gameObject = Grid.Objects[cell, 3];
			bool flag = false;
			bool flag2 = gameObject == null;
			bool result;
			if (flag2)
			{
				result = flag;
			}
			else
			{
				flag = true;
				ObjectLayerListItem objectLayerListItem = (gameObject == null) ? null : gameObject.GetComponent<Pickupable>().objectLayerListItem;
				List<ObjectLayerListItem> list = new List<ObjectLayerListItem>();
				bool flag3 = objectLayerListItem != null;
				if (flag3)
				{
					list.Add(objectLayerListItem);
				}
				for (int i = 0; i < list.Count; i++)
				{
					while (list[i] != null)
					{
						GameObject gameObject2 = list[i].gameObject;
						Pickupable component = gameObject2.GetComponent<Pickupable>();
						bool flag4 = component != null;
						bool flag5 = flag4 && !this.IsElementTakeable(component);
						if (!flag5)
						{
							return true;
						}
						flag = false;
						list[i] = list[i].nextItem;
					}
				}
				result = flag;
			}
			return result;
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00004368 File Offset: 0x00002568
		public bool IsElementTakeable(int cell)
		{
			float num = Grid.Mass[cell];
			bool flag = num <= 0f;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				KGSlider component = base.GetComponent<KGSlider>();
				bool flag2 = num >= component.gPerTransform && this.storage.MassStored() + component.gPerTransform > this.storage.capacityKg;
				if (flag2)
				{
					result = false;
				}
				else
				{
					bool flag3 = num < component.gPerTransform && this.storage.MassStored() + num > this.storage.capacityKg;
					if (flag3)
					{
						result = false;
					}
					else
					{
						PortalConnection portalConnection;
						bool flag4 = this.tryGetTargetPortal(out portalConnection);
						if (flag4)
						{
							bool flag5 = num >= component.gPerTransform && portalConnection.GetComponent<Storage>().MassStored() + component.gPerTransform > this.storage.capacityKg;
							if (flag5)
							{
								return false;
							}
							bool flag6 = num < component.gPerTransform && portalConnection.GetComponent<Storage>().MassStored() + num > this.storage.capacityKg;
							if (flag6)
							{
								return false;
							}
						}
						TreeFilterable component2 = base.GetComponent<TreeFilterable>();
						bool flag7 = component2.AcceptedTags.Count <= 0;
						if (flag7)
						{
							result = false;
						}
						else
						{
							bool flag8 = PortalCore.toggleNonFilteredElement ? (!component2.ContainsTag(Grid.Element[cell].tag)) : component2.ContainsTag(Grid.Element[cell].tag);
							result = flag8;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600005B RID: 91 RVA: 0x000044F0 File Offset: 0x000026F0
		public bool IsElementTakeable(Pickupable elementToPick)
		{
			bool flag = false;
			bool flag2 = elementToPick.UnreservedAmount <= 0f;
			bool result;
			if (flag2)
			{
				result = false;
			}
			else
			{
				bool flag3 = elementToPick.UnreservedAmount > this.storage.capacityKg;
				if (flag3)
				{
					result = false;
				}
				else
				{
					bool flag4 = this.pc != null && this.pc.portalType == Element.State.Unbreakable && !elementToPick.HasTag(GameTags.Minion);
					if (flag4)
					{
						global::Debug.Log("只存小人,其余不存");
						result = false;
					}
					else
					{
						KGSlider kgslider = base.GetComponent<KGSlider>();
						bool flag5 = kgslider == null;
						if (flag5)
						{
							kgslider = new KGSlider();
							kgslider.gPerTransform = 100000f;
						}
						bool flag6 = elementToPick.UnreservedAmount >= kgslider.gPerTransform && this.storage.MassStored() + kgslider.gPerTransform > this.storage.capacityKg;
						if (flag6)
						{
							result = false;
						}
						else
						{
							bool flag7 = elementToPick.UnreservedAmount < kgslider.gPerTransform && this.storage.MassStored() + elementToPick.UnreservedAmount > this.storage.capacityKg;
							if (flag7)
							{
								result = false;
							}
							else
							{
								PortalConnection portalConnection;
								bool flag8 = this.tryGetTargetPortal(out portalConnection);
								if (flag8)
								{
									bool flag9 = elementToPick.UnreservedAmount >= kgslider.gPerTransform && portalConnection.GetComponent<Storage>().MassStored() + kgslider.gPerTransform > this.storage.capacityKg;
									if (flag9)
									{
										return false;
									}
									bool flag10 = elementToPick.UnreservedAmount < kgslider.gPerTransform && portalConnection.GetComponent<Storage>().MassStored() + elementToPick.UnreservedAmount > this.storage.capacityKg;
									if (flag10)
									{
										return false;
									}
								}
								bool flag11 = elementToPick.HasTag(GameTags.Minion) && !this.DupeTeleportable;
								if (flag11)
								{
									result = false;
								}
								else
								{
									bool flag12 = elementToPick.HasTag(GameTags.Minion) && this.DupeTeleportable;
									if (flag12)
									{
										result = true;
									}
									else
									{
										TreeFilterable component = base.GetComponent<TreeFilterable>();
										bool flag13 = component != null;
										if (flag13)
										{
											bool flag14 = component.AcceptedTags.Count <= 0;
											if (flag14)
											{
												return false;
											}
											bool flag15 = elementToPick.HasTag(GameTags.Creature);
											bool flag16 = this.storage.GetOnlyFetchMarkedItems() && !flag15;
											if (flag16)
											{
												flag = (elementToPick.GetComponent<KPrefabID>().HasTag(GameTags.Garbage) && (PortalCore.toggleNonFilteredElement ? (!component.ContainsTag(elementToPick.GetComponent<KPrefabID>().PrefabTag)) : component.ContainsTag(elementToPick.GetComponent<KPrefabID>().PrefabTag)));
											}
											else
											{
												flag = (PortalCore.toggleNonFilteredElement ? (!component.ContainsTag(elementToPick.GetComponent<KPrefabID>().PrefabTag)) : component.ContainsTag(elementToPick.GetComponent<KPrefabID>().PrefabTag));
											}
										}
										result = flag;
									}
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600005C RID: 92 RVA: 0x000047EC File Offset: 0x000029EC
		public bool IsDuplicateTakeable(Pickupable dupes)
		{
			bool flag = false;
			bool flag2 = dupes.UnreservedAmount <= 0f;
			bool result;
			if (flag2)
			{
				result = false;
			}
			else
			{
				bool flag3 = dupes.UnreservedAmount > this.storage.capacityKg;
				if (flag3)
				{
					result = false;
				}
				else
				{
					PortalConnection portalConnection;
					bool flag4 = this.tryGetTargetPortal(out portalConnection) && portalConnection.GetComponent<Storage>().MassStored() + dupes.UnreservedAmount > this.storage.capacityKg;
					if (flag4)
					{
						result = false;
					}
					else
					{
						bool flag5 = dupes.HasTag(GameTags.Minion) && !this.DupeTeleportable;
						if (flag5)
						{
							result = false;
						}
						else
						{
							bool flag6 = dupes.HasTag(GameTags.Minion) && this.DupeTeleportable;
							result = (flag6 || flag);
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x000048BC File Offset: 0x00002ABC
		public int getStopperRangeWidth()
		{
			Rotatable component = base.gameObject.GetComponent<Rotatable>();
			bool flag = component == null;
			int result;
			if (flag)
			{
				result = 1;
			}
			else
			{
				switch (component.GetOrientation())
				{
				case Orientation.Neutral:
				case Orientation.R180:
					result = 1;
					break;
				case Orientation.R90:
				case Orientation.R270:
					result = 3;
					break;
				default:
					result = 1;
					break;
				}
			}
			return result;
		}

		// Token: 0x0600005E RID: 94 RVA: 0x0000491C File Offset: 0x00002B1C
		public int getStopperRangeHeight()
		{
			Rotatable component = base.gameObject.GetComponent<Rotatable>();
			bool flag = component == null;
			int result;
			if (flag)
			{
				result = 1;
			}
			else
			{
				switch (component.GetOrientation())
				{
				case Orientation.Neutral:
				case Orientation.R180:
					result = 3;
					break;
				case Orientation.R90:
				case Orientation.R270:
					result = 1;
					break;
				default:
					result = 1;
					break;
				}
			}
			return result;
		}

		// Token: 0x0600005F RID: 95 RVA: 0x0000497C File Offset: 0x00002B7C
		private void getReadyCells()
		{
			this.objectCells.Clear();
			CellOffset rotatedCellOffset = this.vision_offset;
			bool flag = this.rotatable;
			if (flag)
			{
				rotatedCellOffset = this.rotatable.GetRotatedCellOffset(this.vision_offset);
				int cell = Grid.PosToCell(base.transform.gameObject);
				int cell2 = Grid.OffsetCell(cell, rotatedCellOffset);
				int num;
				int num2;
				Grid.CellToXY(cell2, out num, out num2);
				Orientation orientation = this.rotatable.GetOrientation();
				for (int i = 0; i < this.height; i++)
				{
					for (int j = 0; j < this.width; j++)
					{
						int num3 = 0;
						int num4 = 0;
						CellOffset offset = new CellOffset(num3 + j, num4 + i);
						switch (orientation)
						{
						case Orientation.Neutral:
							num3 = 0;
							num4 = 1;
							offset = new CellOffset(num3 + j + this.reduceXX, num4 + i);
							break;
						case Orientation.R90:
							num3 = 1;
							num4 = 0;
							offset = new CellOffset(num3 + i, num4 + j + this.reduceXX);
							break;
						case Orientation.R180:
							num3 = 0;
							num4 = -1;
							offset = new CellOffset(num3 + j + this.reduceXX, num4 - i);
							break;
						case Orientation.R270:
							num3 = -1;
							num4 = 0;
							offset = new CellOffset(num3 - i, num4 + j + this.reduceXX);
							break;
						}
						int num5 = Grid.OffsetCell(cell, offset);
						bool flag2 = Grid.IsValidCell(num5);
						if (flag2)
						{
							int x;
							int y;
							Grid.CellToXY(num5, out x, out y);
							bool flag3 = Grid.IsValidCell(num5) && Grid.TestLineOfSight(num, num2, x, y, this.blocking_cb, this.blocking_tile_visible);
							if (flag3)
							{
								this.objectCells.Add(num5);
							}
						}
					}
				}
			}
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00004B5C File Offset: 0x00002D5C
		public List<Descriptor> GetDescriptors(GameObject go)
		{
			PortalConnection component = base.GetComponent<PortalConnection>();
			bool flag = component == null;
			List<Descriptor> result;
			if (flag)
			{
				result = new List<Descriptor>();
			}
			else
			{
				this.res.Clear();
				this.getPath(component.ID, "");
				string txt = this.readPath(this.res);
				result = new List<Descriptor>
				{
					new Descriptor(txt, "Connection Chain", Descriptor.DescriptorType.Effect, false)
				};
			}
			return result;
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00004BD0 File Offset: 0x00002DD0
		private string readPath(List<string> res)
		{
			string text = "";
			for (int i = 0; i < res.Count; i++)
			{
				text = string.Concat(new string[]
				{
					text,
					(i + 1).ToString(),
					". ",
					res[i],
					"\r\n"
				});
			}
			return text;
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00004C3C File Offset: 0x00002E3C
		private void getPath(int root, string path)
		{
			List<int> children = this.getChildren(root);
			children.ForEach(delegate(int node)
			{
				string text = this.getName(node) + " → " + path;
				bool flag = this.hasChild(node);
				if (flag)
				{
					this.getPath(node, text);
				}
				else
				{
					this.res.Add(text + this.name.ToString());
				}
			});
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00004C7C File Offset: 0x00002E7C
		private string getName(int node)
		{
			List<PortalConnection> list = (from x in PortalComponents.GetTelepoterInstance<PortalConnection>(base.GetComponent<PortalConnection>().portalType).Items
			where x.ID == node
			select x).ToList<PortalConnection>();
			return (list.Count > 0) ? list.First<PortalConnection>().name : "";
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00004CE4 File Offset: 0x00002EE4
		public bool hasChild(int node)
		{
			return (from x in PortalComponents.targetPortalDic
			where x.Value == node && x.Key != this.GetComponent<PortalConnection>().ID
			select x).ToList<KeyValuePair<int, int>>().Count > 0;
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00004D30 File Offset: 0x00002F30
		public List<int> getChildren(int root)
		{
			return (from x in PortalComponents.targetPortalDic
			where x.Value == root && x.Key != this.GetComponent<PortalConnection>().ID
			select x into k
			select k.Key).ToList<int>();
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00004D98 File Offset: 0x00002F98
		public bool doPick()
		{
			Element.State portalType = this.pc.portalType;
			Element.State state = portalType;
			Element.State state2 = state;
			bool result;
			if (state2 - Element.State.Gas > 1)
			{
				if (state2 != Element.State.Solid)
				{
					result = this.doSolidPick();
				}
				else
				{
					result = this.doSolidPick();
				}
			}
			else
			{
				result = this.doElementPick();
			}
			return result;
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00004DE0 File Offset: 0x00002FE0
		private bool doElementPick()
		{
			PortalConnection portalConnection = new PortalConnection();
			bool flag = this.tryGetTargetPortal(out portalConnection);
			KGSlider component = base.GetComponent<KGSlider>();
			float num = 0f;
			float num2 = component.gPerTransform;
			foreach (int num3 in this.objectCells)
			{
				Element element = Grid.Element[num3];
				bool flag2 = this.IsElementTakeable(num3);
				if (flag2)
				{
					float num4 = (Grid.Mass[num3] > num2) ? num2 : Grid.Mass[num3];
					num += num4;
					bool flag3 = false;
					bool flag4 = num >= num2;
					if (flag4)
					{
						flag3 = true;
					}
					bool flag5 = num2 <= 0f;
					if (flag5)
					{
						return false;
					}
					bool flag6 = flag && portalConnection != null;
					if (flag6)
					{
						Storage component2 = portalConnection.GetComponent<Storage>();
						bool flag7 = num > component2.capacityKg || component2.MassStored() >= component2.capacityKg;
						if (flag7)
						{
							return false;
						}
						bool isLiquid = Grid.Element[num3].IsLiquid;
						if (isLiquid)
						{
							component2.AddLiquid(Grid.Element[num3].id, num4, Grid.Temperature[num3], 0, 0, false, true);
						}
						else
						{
							bool isGas = Grid.Element[num3].IsGas;
							if (isGas)
							{
								component2.AddGasChunk(Grid.Element[num3].id, num4, Grid.Temperature[num3], 0, 0, false, true);
							}
						}
					}
					else
					{
						bool flag8 = num > this.storage.capacityKg;
						if (flag8)
						{
							return false;
						}
						bool isLiquid2 = Grid.Element[num3].IsLiquid;
						if (isLiquid2)
						{
							this.storage.AddLiquid(Grid.Element[num3].id, num4, Grid.Temperature[num3], 0, 0, false, true);
						}
						else
						{
							bool isGas2 = Grid.Element[num3].IsGas;
							if (isGas2)
							{
								this.storage.AddGasChunk(Grid.Element[num3].id, num4, Grid.Temperature[num3], 0, 0, false, true);
							}
						}
					}
					num2 -= num4;
					bool flag9 = flag3 && Grid.Mass[num3] - num4 > 0f;
					if (flag9)
					{
						SimMessages.ReplaceElement(num3, element.id, CellEventLogger.Instance.SandBoxTool, Grid.Mass[num3] - num4, Grid.Temperature[num3], Grid.DiseaseIdx[num3], Grid.DiseaseCount[num3], -1);
					}
					else
					{
						SimMessages.ReplaceElement(num3, SimHashes.Vacuum, CellEventLogger.Instance.SandBoxTool, 0f, 0f, 0, 0, -1);
					}
				}
			}
			return true;
		}

		// Token: 0x06000068 RID: 104 RVA: 0x000050FC File Offset: 0x000032FC
		private bool doSolidPick()
		{
			PortalConnection portalConnection = new PortalConnection();
			bool flag = this.tryGetTargetPortal(out portalConnection);
			float num = 0f;
			KGSlider kgslider = base.GetComponent<KGSlider>();
			bool flag2 = kgslider == null;
			if (flag2)
			{
				kgslider = new KGSlider();
				kgslider.gPerTransform = 100000f;
			}
			float num2 = kgslider.gPerTransform;
			foreach (int cell in this.objectCells)
			{
				GameObject gameObject = Grid.Objects[cell, 3];
				int num3 = 0;
				bool flag3 = true;
				while (gameObject != null && flag3)
				{
					bool flag4 = gameObject != null;
					bool flag5 = flag4;
					if (flag5)
					{
						num3++;
						for (ObjectLayerListItem objectLayerListItem = gameObject.GetComponent<Pickupable>().objectLayerListItem; objectLayerListItem != null; objectLayerListItem = objectLayerListItem.nextItem)
						{
							GameObject gameObject2 = objectLayerListItem.gameObject;
							Pickupable component = gameObject2.GetComponent<Pickupable>();
							bool flag6 = component != null;
							bool flag7 = flag6;
							if (flag7)
							{
								flag3 = this.IsElementTakeable(component);
								bool flag8 = flag3;
								if (flag8)
								{
									float num4 = (component.UnreservedAmount > num2) ? num2 : component.UnreservedAmount;
									num += num4;
									bool flag9 = num2 <= 0f;
									if (flag9)
									{
										return false;
									}
									bool flag10 = flag && portalConnection != null;
									if (flag10)
									{
										Storage component2 = portalConnection.GetComponent<Storage>();
										bool flag11 = num > component2.capacityKg && component2.MassStored() >= component2.capacityKg;
										if (flag11)
										{
											return false;
										}
										component2.Store(component.Take(num4).gameObject, true, false, false, false);
										this.storeCarringItems(component, component2);
									}
									else
									{
										bool flag12 = num > this.storage.capacityKg;
										if (flag12)
										{
											return false;
										}
										this.storeCarringItems(component, this.storage);
										this.storage.Store(component.Take(num4).gameObject, true, false, false, false);
									}
									num2 -= num4;
								}
							}
						}
					}
					gameObject = Grid.Objects[cell, 3];
				}
			}
			return true;
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00005380 File Offset: 0x00003580
		public void storeCarringItems(Pickupable component, Storage targetStorage)
		{
			MinionModifiers component2 = component.GetComponent<MinionModifiers>();
			bool flag = component2 != null;
			if (flag)
			{
				Storage component3 = component2.GetComponent<Storage>();
				bool flag2 = component3 == null || component3.items.Count == 0;
				if (!flag2)
				{
					foreach (GameObject gameObject in component3.items)
					{
						Pickupable component4 = gameObject.GetComponent<Pickupable>();
						targetStorage.Store(component4.gameObject, true, false, false, false);
					}
					component3.items.Clear();
				}
			}
		}

		// Token: 0x0600006A RID: 106 RVA: 0x0000543C File Offset: 0x0000363C
		public void doDrop()
		{
			Rotatable component = base.GetComponent<Rotatable>();
			Vector3 position = base.transform.GetPosition();
			Vector3 rotatedOffset = Rotatable.GetRotatedOffset(new Vector3(0f, 1f), component.GetOrientation());
			Vector3 rotatedOffset2 = Rotatable.GetRotatedOffset(new Vector3(0f, -1f), component.GetOrientation());
			int num = Grid.PosToCell(position + rotatedOffset);
			int cell = Grid.PosToCell(position + rotatedOffset2);
			Vector3 vector = Grid.CellToPos(cell);
			List<GameObject> list = new List<GameObject>();
			foreach (GameObject gameObject in this.storage.items)
			{
				bool flag = gameObject.HasTag(GameTags.Minion);
				if (flag)
				{
					list.Add(gameObject);
				}
				bool flag2 = gameObject.HasTag(GameTags.Creature);
				if (flag2)
				{
				}
			}
			vector.y += 0.5f;
			vector.x += 0.5f;
			this.storage.DropAll(vector, this.NeedVentGas, this.NeedDumpLiquid, default(Vector3), true, null);
			bool flag3 = list.Count > 0;
			if (flag3)
			{
				this.transformDupelicates(list, vector);
			}
		}

		// Token: 0x0600006B RID: 107 RVA: 0x0000559C File Offset: 0x0000379C
		private void transformDupelicates(List<GameObject> duplicates, Vector3 posToTransform)
		{
			Orientation orientation = this.rotatable.GetOrientation();
			Orientation orientation2 = orientation;
			Orientation orientation3 = orientation2;
			if (orientation3 != Orientation.Neutral)
			{
				if (orientation3 - Orientation.R90 > 2)
				{
				}
			}
			else
			{
				posToTransform.y -= 1.5f;
			}
			foreach (GameObject gameObject in duplicates)
			{
				gameObject.transform.SetPosition(Grid.CellToPos(Grid.PosToCell(new Vector3(posToTransform.x, posToTransform.y)), CellAlignment.Bottom, Grid.SceneLayer.Move));
			}
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00005648 File Offset: 0x00003848
		private void refreshRangeVisualizer(int x, int y, int height, int width)
		{
			MyChoreRangeVisualizer myChoreRangeVisualizer = base.transform.gameObject.AddOrGet<MyChoreRangeVisualizer>();
			myChoreRangeVisualizer.x = x;
			myChoreRangeVisualizer.reduceXX = this.reduceXX;
			myChoreRangeVisualizer.y = y;
			myChoreRangeVisualizer.width = width;
			myChoreRangeVisualizer.height = height;
			myChoreRangeVisualizer.vision_offset = new CellOffset(0, 0);
			myChoreRangeVisualizer.movable = this.movable;
			myChoreRangeVisualizer.blocking_tile_visible = false;
			base.transform.gameObject.GetComponent<KPrefabID>().instantiateFn += delegate(GameObject go)
			{
				go.GetComponent<StationaryChoreRangeVisualizer>().blocking_cb = new Func<int, bool>(PortalCore.BlockingCB);
			};
			myChoreRangeVisualizer.UpdateVisualizers();
			this.getReadyCells();
		}

		// Token: 0x0600006D RID: 109 RVA: 0x000056F4 File Offset: 0x000038F4
		private void addHeight()
		{
			bool flag = this.height + 1 == 15;
			if (!flag)
			{
				this.height++;
				this.refreshRangeVisualizer(this.x, this.y, this.height, this.width);
			}
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00005744 File Offset: 0x00003944
		private void reduceHeight()
		{
			bool flag = this.height - 1 == 0;
			if (!flag)
			{
				this.height--;
				this.refreshRangeVisualizer(this.x, this.y, this.height, this.width);
			}
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00005790 File Offset: 0x00003990
		private void addWidth()
		{
			bool flag = this.width + 1 == 6;
			if (!flag)
			{
				this.width++;
				bool flag2 = this.needToSizeXAdd;
				if (flag2)
				{
					this.reduceXX--;
					this.needToSizeXAdd = false;
				}
				else
				{
					this.needToSizeXAdd = true;
				}
				this.refreshRangeVisualizer(this.x, this.y, this.height, this.width);
			}
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00005808 File Offset: 0x00003A08
		private void reduceWidth()
		{
			bool flag = this.width - 1 == 0;
			if (!flag)
			{
				this.width--;
				bool flag2 = this.needToSizeXReduce;
				if (flag2)
				{
					this.reduceXX++;
					this.needToSizeXReduce = false;
				}
				else
				{
					this.needToSizeXReduce = true;
				}
				this.refreshRangeVisualizer(this.x, this.y, this.height, this.width);
			}
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00005880 File Offset: 0x00003A80
		private void doToggleFilteredElement()
		{
			PortalCore.toggleNonFilteredElement = !PortalCore.toggleNonFilteredElement;
			this.OnRefreshUserMenu("");
		}

		// Token: 0x06000072 RID: 114 RVA: 0x0000589C File Offset: 0x00003A9C
		private void doToggleDupeTeleportable()
		{
			this.DupeTeleportable = !this.DupeTeleportable;
			this.OnRefreshUserMenu("");
			Navigator component = SelectTool.Instance.selected.GetComponent<Navigator>();
			bool flag = component == null;
			if (!flag)
			{
				int mouseCell = DebugHandler.GetMouseCell();
				bool flag2 = Grid.IsValidCell(mouseCell);
				if (flag2)
				{
					PathFinder.PotentialPath potential_path = new PathFinder.PotentialPath(this.building.GetCell(), NavType.Floor, component.flags);
					PathFinder.Path path = default(PathFinder.Path);
					PathFinder.UpdatePath(component.NavGrid, component.GetCurrentAbilities(), potential_path, PathFinderQueries.cellQuery.Reset(mouseCell), ref path);
					foreach (PathFinder.Path.Node node in path.nodes)
					{
					}
				}
			}
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00005980 File Offset: 0x00003B80
		private void doToggleCreatureTeleportable()
		{
			this.CreaturesTeleportable = !this.CreaturesTeleportable;
			this.OnRefreshUserMenu("");
		}

		// Token: 0x06000074 RID: 116 RVA: 0x0000599E File Offset: 0x00003B9E
		private void doToggleVentGas()
		{
			this.NeedVentGas = !this.NeedVentGas;
			this.OnRefreshUserMenu("");
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000059BC File Offset: 0x00003BBC
		private void doToggleShowPath()
		{
			this.debugPathFinder = !this.debugPathFinder;
			this.OnRefreshUserMenu("");
		}

		// Token: 0x06000076 RID: 118 RVA: 0x000059DA File Offset: 0x00003BDA
		private void doToggleDumpLiquid()
		{
			this.NeedDumpLiquid = !this.NeedDumpLiquid;
			this.OnRefreshUserMenu("");
		}

		// Token: 0x06000077 RID: 119 RVA: 0x000059F8 File Offset: 0x00003BF8
		private void OnRefreshUserMenu(object data)
		{
			KIconButtonMenu.ButtonInfo button = new KIconButtonMenu.ButtonInfo("action_priority", PortalCubeStrings.UI.BUTTONS.ADDHEIGHT, new System.Action(this.addHeight), global::Action.SwitchActiveWorld10, null, null, null, PortalCubeStrings.UI.BUTTONS.ADDHEIGHT_DESC, true);
			Game.Instance.userMenu.AddButton(base.gameObject, button, 0f);
			KIconButtonMenu.ButtonInfo button2 = new KIconButtonMenu.ButtonInfo("action_empty_contents", PortalCubeStrings.UI.BUTTONS.REDUCEHEIGHT, new System.Action(this.reduceHeight), global::Action.SwitchActiveWorld10, null, null, null, PortalCubeStrings.UI.BUTTONS.REDUCEHEIGHT_DESC, true);
			Game.Instance.userMenu.AddButton(base.gameObject, button2, 0f);
			KIconButtonMenu.ButtonInfo button3 = new KIconButtonMenu.ButtonInfo("action_direction_both", PortalCubeStrings.UI.BUTTONS.ADDWIDTH, new System.Action(this.addWidth), global::Action.SwitchActiveWorld10, null, null, null, PortalCubeStrings.UI.BUTTONS.ADDWIDTH_DESC, true);
			Game.Instance.userMenu.AddButton(base.gameObject, button3, 0f);
			KIconButtonMenu.ButtonInfo button4 = new KIconButtonMenu.ButtonInfo("action_direction_both", PortalCubeStrings.UI.BUTTONS.REDUCEWIDTH, new System.Action(this.reduceWidth), global::Action.SwitchActiveWorld10, null, null, null, PortalCubeStrings.UI.BUTTONS.REDUCEWIDTH_DESC, true);
			Game.Instance.userMenu.AddButton(base.gameObject, button4, 0f);
			bool flag = this.pc.portalType == Element.State.Solid;
			if (!flag)
			{
				bool flag2 = this.pc.portalType == Element.State.Unbreakable;
				if (flag2)
				{
					bool flag3 = !this.debugPathFinder;
					if (flag3)
					{
						KIconButtonMenu.ButtonInfo button5 = new KIconButtonMenu.ButtonInfo("action", "Show Path", new System.Action(this.doToggleShowPath), global::Action.SwitchActiveWorld10, null, null, null, "Just for debug", true);
						Game.Instance.userMenu.AddButton(base.gameObject, button5, 0f);
					}
					else
					{
						KIconButtonMenu.ButtonInfo button6 = new KIconButtonMenu.ButtonInfo("action", "Hide Path", new System.Action(this.doToggleShowPath), global::Action.SwitchActiveWorld10, null, null, null, "Just for debug", true);
						Game.Instance.userMenu.AddButton(base.gameObject, button6, 0f);
					}
				}
				else
				{
					bool flag4 = this.pc.portalType == Element.State.Gas;
					if (flag4)
					{
						bool flag5 = !this.NeedVentGas;
						if (flag5)
						{
							KIconButtonMenu.ButtonInfo button7 = new KIconButtonMenu.ButtonInfo("action_rocket_restriction_uncontrolled", PortalCubeStrings.UI.BUTTONS.NEEDVENT_ON, new System.Action(this.doToggleVentGas), global::Action.SwitchActiveWorld10, null, null, null, PortalCubeStrings.UI.BUTTONS.NEEDVENT_ON_DESC, true);
							Game.Instance.userMenu.AddButton(base.gameObject, button7, 0f);
						}
						else
						{
							KIconButtonMenu.ButtonInfo button8 = new KIconButtonMenu.ButtonInfo("action_rocket_restriction_controlled", PortalCubeStrings.UI.BUTTONS.NEEDVENT_OFF, new System.Action(this.doToggleVentGas), global::Action.SwitchActiveWorld10, null, null, null, PortalCubeStrings.UI.BUTTONS.NEEDVENT_OFF_DESC, true);
							Game.Instance.userMenu.AddButton(base.gameObject, button8, 0f);
						}
					}
					else
					{
						bool flag6 = this.pc.portalType == Element.State.Liquid;
						if (flag6)
						{
							bool flag7 = !this.NeedDumpLiquid;
							if (flag7)
							{
								KIconButtonMenu.ButtonInfo button9 = new KIconButtonMenu.ButtonInfo("action_rocket_restriction_uncontrolled", PortalCubeStrings.UI.BUTTONS.DUMPLIQUID_ON, new System.Action(this.doToggleDumpLiquid), global::Action.SwitchActiveWorld10, null, null, null, PortalCubeStrings.UI.BUTTONS.DUMPLIQUID_ON_DESC, true);
								Game.Instance.userMenu.AddButton(base.gameObject, button9, 0f);
							}
							else
							{
								KIconButtonMenu.ButtonInfo button10 = new KIconButtonMenu.ButtonInfo("action_rocket_restriction_controlled", PortalCubeStrings.UI.BUTTONS.DUMPLIQUID_OFF, new System.Action(this.doToggleDumpLiquid), global::Action.SwitchActiveWorld10, null, null, null, PortalCubeStrings.UI.BUTTONS.DUMPLIQUID_OFF_DESC, true);
								Game.Instance.userMenu.AddButton(base.gameObject, button10, 0f);
							}
						}
					}
				}
			}
			bool flag8 = this.pc.portalType != Element.State.Unbreakable;
			if (flag8)
			{
				bool flag9 = !PortalCore.toggleNonFilteredElement;
				if (flag9)
				{
					KIconButtonMenu.ButtonInfo button11 = new KIconButtonMenu.ButtonInfo("action_move_to_storage", PortalCubeStrings.UI.BUTTONS.NONSELECTEDELEMENT_ON, new System.Action(this.doToggleFilteredElement), global::Action.SwitchActiveWorld10, null, null, null, PortalCubeStrings.UI.BUTTONS.NONSELECTEDELEMENT_ON_DESC, true);
					Game.Instance.userMenu.AddButton(base.gameObject, button11, 0f);
				}
				else
				{
					KIconButtonMenu.ButtonInfo button12 = new KIconButtonMenu.ButtonInfo("action_move_to_storage", PortalCubeStrings.UI.BUTTONS.NONSELECTEDELEMENT_OFF, new System.Action(this.doToggleFilteredElement), global::Action.SwitchActiveWorld10, null, null, null, PortalCubeStrings.UI.BUTTONS.NONSELECTEDELEMENT_OFF_DESC, true);
					Game.Instance.userMenu.AddButton(base.gameObject, button12, 0f);
				}
			}
		}

		// Token: 0x04000028 RID: 40
		[MyCmpGet]
		private Building building;

		// Token: 0x04000029 RID: 41
		[MyCmpGet]
		private Storage storage;

		// Token: 0x0400002A RID: 42
		[MyCmpGet]
		public PortalConnection pc;

		// Token: 0x0400002B RID: 43
		[MyCmpGet]
		public KBatchedAnimController kanimController;

		// Token: 0x0400002C RID: 44
		[MyCmpGet]
		public Operational controller;

		// Token: 0x0400002D RID: 45
		[MyCmpGet]
		private Rotatable rotatable;

		// Token: 0x0400002E RID: 46
		public bool isWorking = false;

		// Token: 0x0400002F RID: 47
		public int myOutPutCell = -1;

		// Token: 0x04000030 RID: 48
		public int myInPutCell = -1;

		// Token: 0x04000031 RID: 49
		private List<int> objectCells = new List<int>();

		// Token: 0x04000032 RID: 50
		private Guid blockedGuid;

		// Token: 0x04000033 RID: 51
		public static readonly Color32 BLOCKED_TINT = new Color(0.5019608f, 0.5019608f, 0.5019608f, 1f);

		// Token: 0x04000034 RID: 52
		public static readonly Color32 NORMAL_TINT = Color.white;

		// Token: 0x04000035 RID: 53
		[MyCmpGet]
		private KSelectable selectable;

		// Token: 0x04000036 RID: 54
		[SerializeField]
		[Serialize]
		private static bool toggleNonFilteredElement = false;

		// Token: 0x04000037 RID: 55
		[SerializeField]
		[Serialize]
		public int reduceXX = 0;

		// Token: 0x04000038 RID: 56
		[SerializeField]
		[Serialize]
		private bool needToSizeXAdd = false;

		// Token: 0x04000039 RID: 57
		[SerializeField]
		[Serialize]
		private bool needToSizeXReduce = false;

		// Token: 0x0400003A RID: 58
		[SerializeField]
		[Serialize]
		public bool DupeTeleportable = false;

		// Token: 0x0400003B RID: 59
		[SerializeField]
		[Serialize]
		public bool debugPathFinder = false;

		// Token: 0x0400003C RID: 60
		[SerializeField]
		[Serialize]
		public bool NeedVentGas = false;

		// Token: 0x0400003D RID: 61
		[SerializeField]
		[Serialize]
		public bool NeedDumpLiquid = false;

		// Token: 0x0400003E RID: 62
		[SerializeField]
		[Serialize]
		public bool CreaturesTeleportable = false;

		// Token: 0x0400003F RID: 63
		public int newyy = 0;

		// Token: 0x04000040 RID: 64
		public bool isStoring = false;

		// Token: 0x04000041 RID: 65
		public bool isDropping = false;

		// Token: 0x04000042 RID: 66
		private bool hasChildrenNavConnected = false;

		// Token: 0x04000043 RID: 67
		private bool hasChildrenNavDeconnected = false;

		// Token: 0x04000044 RID: 68
		private bool hasSelfConnected = false;

		// Token: 0x04000045 RID: 69
		[SerializeField]
		public int x = 0;

		// Token: 0x04000046 RID: 70
		[SerializeField]
		public int y = 0;

		// Token: 0x04000047 RID: 71
		[SerializeField]
		[Serialize]
		public int width = 1;

		// Token: 0x04000048 RID: 72
		[SerializeField]
		[Serialize]
		public int height = 1;

		// Token: 0x04000049 RID: 73
		public bool movable;

		// Token: 0x0400004A RID: 74
		public CellOffset vision_offset;

		// Token: 0x0400004B RID: 75
		public Func<int, bool> blocking_cb = new Func<int, bool>(PortalCore.BlockingCB);

		// Token: 0x0400004C RID: 76
		public bool blocking_tile_visible = false;

		// Token: 0x0400004D RID: 77
		private List<string> res = new List<string>();

		// Token: 0x0400004E RID: 78
		private static readonly EventSystem.IntraObjectHandler<PortalCore> OnRefreshUserMenuDelegate = new EventSystem.IntraObjectHandler<PortalCore>(delegate(PortalCore component, object data)
		{
			component.OnRefreshUserMenu(data);
		});
	}
	
	// Token: 0x02000012 RID: 18
	public static class PortalCubeStrings
	{
		// Token: 0x0200002D RID: 45
		public static class BUILDINGS
		{
			// Token: 0x02000037 RID: 55
			public static class PREFABS
			{
				// Token: 0x0200003A RID: 58
				public static class DUPEPORTALCUBE
				{
					// Token: 0x0400010C RID: 268
					public static LocString NAME = STRINGS.UI.FormatAsLink("Dupe Portal Cube", "DupePortalCube");

					// Token: 0x0400010D RID: 269
					public static LocString DESC = "You can select a target portal cube by clicking on the building, you can scan the block, and support multiple pairs of ranges; After built , the duplicates can automatically use the portal to find the nearest path.";

					// Token: 0x0400010E RID: 270
					public static LocString EFFECT = string.Concat(new string[]
					{
						"Dupe portal cube, which can transmit duplicates. The default state is to teleporting the duplicates at the input to the output.The cell below the output requires a floor, and the output cannot be blocked. After the target portal cube is checked, the dupes can be teleported to the output port of the target portal cube, which can be across galaxies and rockets.The building supports 360° rotation. When the default direction is used, the output point is the second grid below the building."
					});
				}

				// Token: 0x0200003B RID: 59
				public static class SOLIDPORTALCUBE
				{
					// Token: 0x0400010F RID: 271
					public static LocString NAME = STRINGS.UI.FormatAsLink("Solid Portal Cube", "SolidPortalCube");

					// Token: 0x04000110 RID: 272
					public static LocString DESC = "You can select a target portal cube by clicking on the building, you can scan the block, and support multiple pairs of ranges; support control selection 'Single transfer quality' ; support filter the required substances, and support the screening of 'not substances';also can transfer duplicates ,default is not enabled, you can click the button to toggle.";

					// Token: 0x04000111 RID: 273
					public static LocString EFFECT = string.Concat(new string[]
					{
						"Portal cube, which can transmit ",
						STRINGS.UI.FormatAsLink("solid", "ELEMENTS_SOLID"),
						" materials and creatures. Maximum mass per transfer: 100 tons. The default state is to store the substance at the input and put it on the output, and the output cannot be blocked. After the target portal cube is checked, the material can be teleported to the output port of the target portal cube, which can be across galaxies and rockets."
					});
				}

				// Token: 0x0200003C RID: 60
				public static class LIQUIDPORTALCUBE
				{
					// Token: 0x04000112 RID: 274
					public static LocString NAME = STRINGS.UI.FormatAsLink("Liquid Portal Cube", "LiquidPortalCube");

					// Token: 0x04000113 RID: 275
					public static LocString DESC = "You can select a target portal cube by clicking on the building, you can scan the block, and support multiple pairs of ranges; support control selection 'Single transfer quality' ; support filter the required substances, and support the screening of 'not substances'";

					// Token: 0x04000114 RID: 276
					public static LocString EFFECT = string.Concat(new string[]
					{
						"Liquid Portal cube, which can transmit ",
						STRINGS.UI.FormatAsLink("Liquid", "ELEMENTS_LIQUID"),
						" elements. Maximum mass per transfer: 100 tons. The default state is to store the substance at the input and put it on the output, and the output cannot be blocked. After the target portal cube is checked, the element can be teleported to the output port of the target portal cube, which can be across galaxies and rockets."
					});
				}

				// Token: 0x0200003D RID: 61
				public static class GASPORTALCUBE
				{
					// Token: 0x04000115 RID: 277
					public static LocString NAME = STRINGS.UI.FormatAsLink("Gas Portal Cube", "GasPortalCube");

					// Token: 0x04000116 RID: 278
					public static LocString DESC = "You can select a target portal cube by clicking on the building, you can scan the block, and support multiple pairs of ranges; support control selection 'Single transfer quality' ; support filter the required substances, and support the screening of 'not substances'";

					// Token: 0x04000117 RID: 279
					public static LocString EFFECT = string.Concat(new string[]
					{
						"Gas Portal cube, which can transmit ",
						STRINGS.UI.FormatAsLink("Gas", "ELEMENTS_Gas"),
						" elements. Maximum mass per transfer: 100 tons. The default state is to store the substance at the input and put it on the output, and the output cannot be blocked. After the target portal cube is checked, the element can be teleported to the output port of the target portal cube, which can be across galaxies and rockets."
					});
				}
			}
		}

		// Token: 0x0200002E RID: 46
		public static class UI
		{
			// Token: 0x02000038 RID: 56
			public static class UISIDESCREENS
			{
				// Token: 0x040000EB RID: 235
				public static LocString TITLE = "Portal Cube Configs";

				// Token: 0x040000EC RID: 236
				public static LocString HEADER = "Target Portal Cube Selector";

				// Token: 0x040000ED RID: 237
				public static LocString NO_TARGET = "No Target Portal Cube.";

				// Token: 0x040000EE RID: 238
				public static LocString NO_TARGET_DESC = "Build more portal cube to transport materials.";

				// Token: 0x040000EF RID: 239
				public static LocString SLIDERTITLE = "Single transfer quality (KG)";
			}

			// Token: 0x02000039 RID: 57
			public static class BUTTONS
			{
				// Token: 0x040000F0 RID: 240
				public static LocString ADDHEIGHT = "Height +1";

				// Token: 0x040000F1 RID: 241
				public static LocString ADDHEIGHT_DESC = "Height of scaning range +1 cell";

				// Token: 0x040000F2 RID: 242
				public static LocString REDUCEHEIGHT = "Height -1";

				// Token: 0x040000F3 RID: 243
				public static LocString REDUCEHEIGHT_DESC = "Height of scaning range -1 cell";

				// Token: 0x040000F4 RID: 244
				public static LocString ADDWIDTH = "Width +1";

				// Token: 0x040000F5 RID: 245
				public static LocString ADDWIDTH_DESC = "Width of scaning range +1 cell";

				// Token: 0x040000F6 RID: 246
				public static LocString REDUCEWIDTH = "Width -1";

				// Token: 0x040000F7 RID: 247
				public static LocString REDUCEWIDTH_DESC = "Width of scaning range -1 cell";

				// Token: 0x040000F8 RID: 248
				public static LocString DUPLICATETELEPORTING_ON = "Dupe Teleport:On";

				// Token: 0x040000F9 RID: 249
				public static LocString DUPLICATETELEPORTING_ON_DESC = "Switch to : The duplicates CAN be teleporting";

				// Token: 0x040000FA RID: 250
				public static LocString DUPLICATETELEPORTING_OFF = "Dupe Teleport:Off";

				// Token: 0x040000FB RID: 251
				public static LocString DUPLICATETELEPORTING_OFF_DESC = "Switch to : The duplicates CAN NOT be teleporting";

				// Token: 0x040000FC RID: 252
				public static LocString CREATURETELEPORTING_ON = "Creature Teleport:On";

				// Token: 0x040000FD RID: 253
				public static LocString CREATURETELEPORTING_ON_DESC = "Switch to : The creatures CAN be teleporting";

				// Token: 0x040000FE RID: 254
				public static LocString CREATURETELEPORTING_OFF = "Creature Teleport:Off";

				// Token: 0x040000FF RID: 255
				public static LocString CREATURETELEPORTING_OFF_DESC = "Switch to : The creatures CAN NOT be teleporting";

				// Token: 0x04000100 RID: 256
				public static LocString NONSELECTEDELEMENT_ON = "Non-Filtered Element";

				// Token: 0x04000101 RID: 257
				public static LocString NONSELECTEDELEMENT_ON_DESC = "Switch to: Transport all the materials not selected";

				// Token: 0x04000102 RID: 258
				public static LocString NONSELECTEDELEMENT_OFF = "Filtered Element";

				// Token: 0x04000103 RID: 259
				public static LocString NONSELECTEDELEMENT_OFF_DESC = "Switch to: Transport the only materials selected";

				// Token: 0x04000104 RID: 260
				public static LocString NEEDVENT_ON = "Vent Gas:On";

				// Token: 0x04000105 RID: 261
				public static LocString NEEDVENT_ON_DESC = "Switch to: Need vent gas";

				// Token: 0x04000106 RID: 262
				public static LocString NEEDVENT_OFF = "Vent Gas:Off";

				// Token: 0x04000107 RID: 263
				public static LocString NEEDVENT_OFF_DESC = "Switch to: Need not to vent gas";

				// Token: 0x04000108 RID: 264
				public static LocString DUMPLIQUID_ON = "Dump Liquid:On";

				// Token: 0x04000109 RID: 265
				public static LocString DUMPLIQUID_ON_DESC = "Switch to: Need to Dump Liquid";

				// Token: 0x0400010A RID: 266
				public static LocString DUMPLIQUID_OFF = "Dump Liquid:Off";

				// Token: 0x0400010B RID: 267
				public static LocString DUMPLIQUID_OFF_DESC = "Switch to: Need not to Dump Liquid";
			}
		}
	}
	
	public class PortalSideScreen : SideScreenContent
	{
		// Token: 0x0600007D RID: 125 RVA: 0x0000620C File Offset: 0x0000440C
		internal static void AddSideScreen(IList<DetailsScreen.SideScreenRef> existing, GameObject parent)
		{
			bool flag = false;
			foreach (DetailsScreen.SideScreenRef sideScreenRef in existing)
			{
				LogicBroadcastChannelSideScreen logicBroadcastChannelSideScreen = sideScreenRef.screenPrefab as LogicBroadcastChannelSideScreen;
				bool flag2 = logicBroadcastChannelSideScreen != null;
				if (flag2)
				{
					DetailsScreen.SideScreenRef sideScreenRef2 = new DetailsScreen.SideScreenRef();
					PortalSideScreen portalSideScreen = PortalSideScreen.CreateScreen(logicBroadcastChannelSideScreen);
					flag = true;
					sideScreenRef2.name = "PortalSideScreen";
					sideScreenRef2.screenPrefab = portalSideScreen;
					sideScreenRef2.screenInstance = portalSideScreen;
					Transform transform = portalSideScreen.gameObject.transform;
					transform.SetParent(parent.transform);
					transform.localScale = Vector3.one;
					existing.Insert(0, sideScreenRef2);
					break;
				}
			}
			bool flag3 = !flag;
			if (flag3)
			{
				PUtil.LogWarning("Unable to find LogicBroadcastChannel side screen!");
			}
		}

		// Token: 0x0600007E RID: 126 RVA: 0x000062F0 File Offset: 0x000044F0
		private static PortalSideScreen CreateScreen(LogicBroadcastChannelSideScreen ss)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(ss.gameObject);
			gameObject.name = "PortalSideScreen";
			bool activeSelf = gameObject.activeSelf;
			gameObject.SetActive(false);
			LogicBroadcastChannelSideScreen component = gameObject.GetComponent<LogicBroadcastChannelSideScreen>();
			PortalSideScreen portalSideScreen = gameObject.AddComponent<PortalSideScreen>();
			portalSideScreen.rowPrefab = PortalSideScreen.ROW_PREFAB.Get(component);
			portalSideScreen.listContainer = PortalSideScreen.LIST_CONTAINER.Get(component);
			portalSideScreen.headerLabel = PortalSideScreen.HEADER_LABEL.Get(component);
			portalSideScreen.noChannelRow = PortalSideScreen.NO_CHANNEL_ROW.Get(component);
			try
			{
				portalSideScreen.noChannelRow.GetComponent<HierarchyReferences>().GetReference<LocText>("Label").SetText(PortalCubeStrings.UI.UISIDESCREENS.NO_TARGET);
				portalSideScreen.noChannelRow.GetComponent<HierarchyReferences>().GetReference<LocText>("DistanceLabel").SetText(PortalCubeStrings.UI.UISIDESCREENS.NO_TARGET_DESC);
			}
			catch (Exception)
			{
				PUtil.LogWarning("PortalSideScreen: replace LocText failed");
			}
			portalSideScreen.emptySpaceRow = PortalSideScreen.EMPTY_SPACE_ROW.Get(component);
			UnityEngine.Object.DestroyImmediate(component);
			gameObject.SetActive(activeSelf);
			return portalSideScreen;
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00006428 File Offset: 0x00004628
		public override bool IsValidForTarget(GameObject target)
		{
			return target.GetComponent<PortalConnection>() != null;
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00006446 File Offset: 0x00004646
		public override void SetTarget(GameObject target)
		{
			base.SetTarget(target);
			this.sensor = target.GetComponent<PortalConnection>();
			this.Build();
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00006464 File Offset: 0x00004664
		private void ClearRows()
		{
			bool flag = this.emptySpaceRow != null;
			if (flag)
			{
				Util.KDestroyGameObject(this.emptySpaceRow);
			}
			foreach (KeyValuePair<PortalConnection, GameObject> keyValuePair in this.tsenderCtrlRows)
			{
				Util.KDestroyGameObject(keyValuePair.Value);
			}
			this.tsenderCtrlRows.Clear();
		}

		// Token: 0x06000082 RID: 130 RVA: 0x000064EC File Offset: 0x000046EC
		public override string GetTitle()
		{
			return PortalCubeStrings.UI.UISIDESCREENS.TITLE;
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00006508 File Offset: 0x00004708
		private void Build()
		{
			this.headerLabel.SetText(PortalCubeStrings.UI.UISIDESCREENS.HEADER);
			this.ClearRows();
			List<PortalConnection> list = PortalComponents.GetTelepoterInstance<PortalConnection>(this.sensor.portalType).Items.FindAll((PortalConnection x) => x != this.sensor);
			bool flag = list.Count > 1;
			if (flag)
			{
				list.Sort();
			}
			foreach (PortalConnection portalConnection in list)
			{
				bool flag2 = portalConnection;
				if (flag2)
				{
					GameObject gameObject = Util.KInstantiateUI(this.rowPrefab, this.listContainer, false);
					gameObject.gameObject.name = portalConnection.gameObject.GetProperName();
					global::Debug.Assert(!this.tsenderCtrlRows.ContainsKey(portalConnection), "Adding two of the same sender to TeleporterSenderSideScreen UI: " + portalConnection.gameObject.GetProperName());
					this.tsenderCtrlRows.Add(portalConnection, gameObject);
					gameObject.SetActive(true);
				}
			}
			this.noChannelRow.SetActive(list.Count == 0);
			this.Refresh();
		}

		// Token: 0x06000084 RID: 132 RVA: 0x0000664C File Offset: 0x0000484C
		internal List<PortalConnection> GetTarget(int ID)
		{
			bool flag = !PortalComponents.targetPortalDic.ContainsKey(ID);
			List<PortalConnection> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				int targetPortalID = PortalComponents.targetPortalDic[ID];
				result = PortalComponents.GetTelepoterInstance<PortalConnection>(this.sensor.portalType).Items.FindAll((PortalConnection x) => targetPortalID == x.ID);
			}
			return result;
		}

		// Token: 0x06000085 RID: 133 RVA: 0x000066B4 File Offset: 0x000048B4
		public void ToggleSenderControllerByStoredSet(PortalConnection currentPortal, PortalConnection targetPortal)
		{
			Dictionary<int, int> targetPortalDic = PortalComponents.targetPortalDic;
			PortalCore component = this.sensor.GetComponent<PortalCore>();
			bool flag = targetPortalDic.ContainsKey(currentPortal.ID) && targetPortalDic[currentPortal.ID] == targetPortal.ID;
			if (flag)
			{
				targetPortalDic.Remove(currentPortal.ID);
				component.blocked(true);
			}
			else
			{
				bool flag2 = targetPortalDic.ContainsKey(currentPortal.ID);
				if (flag2)
				{
					targetPortalDic.Remove(currentPortal.ID);
					targetPortalDic.Add(currentPortal.ID, targetPortal.ID);
					component.blocked(true);
				}
				else
				{
					targetPortal.enabled = true;
					targetPortalDic.Add(currentPortal.ID, targetPortal.ID);
					this.sensor.targetPortalDic = targetPortalDic;
					component.blocked(true);
				}
			}
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00006780 File Offset: 0x00004980
		private void Refresh()
		{
			using (Dictionary<PortalConnection, GameObject>.Enumerator enumerator = this.tsenderCtrlRows.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<PortalConnection, GameObject> kvp = enumerator.Current;
					kvp.Value.GetComponent<HierarchyReferences>().GetReference<LocText>("Label").SetText(kvp.Key.gameObject.GetProperName());
					WorldContainer myWorld = kvp.Key.GetMyWorld();
					kvp.Value.GetComponent<HierarchyReferences>().GetReference<LocText>("DistanceLabel").SetText(myWorld.GetComponent<ClusterGridEntity>().Name);
					kvp.Value.GetComponent<HierarchyReferences>().GetReference<Image>("Icon").sprite = Def.GetUISprite(kvp.Key.gameObject, "ui", false).first;
					kvp.Value.GetComponent<HierarchyReferences>().GetReference<Image>("Icon").color = Def.GetUISprite(kvp.Key.gameObject, "ui", false).second;
					kvp.Value.GetComponent<HierarchyReferences>().GetReference<Image>("WorldIcon").sprite = (myWorld.IsModuleInterior ? Assets.GetSprite("icon_category_rocketry") : Def.GetUISprite(myWorld.GetComponent<ClusterGridEntity>(), "ui", false).first);
					kvp.Value.GetComponent<HierarchyReferences>().GetReference<Image>("WorldIcon").color = (myWorld.IsModuleInterior ? Color.white : Def.GetUISprite(myWorld.GetComponent<ClusterGridEntity>(), "ui", false).second);
					kvp.Value.gameObject.AddOrGet<DoubleClick>().onDoubleClick = delegate()
					{
						try
						{
							CameraController.Instance.ActiveWorldStarWipe(kvp.Key.gameObject.GetMyWorldId(), kvp.Key.transform.GetPosition(), 8f, null);
						}
						catch
						{
							this.Build();
						}
					};
					kvp.Value.gameObject.AddOrGet<DoubleClick>().onClick = delegate()
					{
						try
						{
							this.ToggleSenderControllerByStoredSet(this.sensor, kvp.Key);
							this.Refresh();
						}
						catch
						{
							this.Build();
						}
					};
					bool flag = false;
					bool flag2 = this.GetTarget(this.sensor.ID) != null;
					if (flag2)
					{
						flag = this.GetTarget(this.sensor.ID).Contains(kvp.Key);
					}
					kvp.Value.GetComponent<HierarchyReferences>().GetReference<MultiToggle>("Toggle").ChangeState(flag ? 1 : 0);
				}
			}
		}

		// Token: 0x04000059 RID: 89
		private static readonly IDetouredField<LogicBroadcastChannelSideScreen, GameObject> ROW_PREFAB = PDetours.DetourField<LogicBroadcastChannelSideScreen, GameObject>("rowPrefab");

		// Token: 0x0400005A RID: 90
		private static readonly IDetouredField<LogicBroadcastChannelSideScreen, GameObject> LIST_CONTAINER = PDetours.DetourField<LogicBroadcastChannelSideScreen, GameObject>("listContainer");

		// Token: 0x0400005B RID: 91
		private static readonly IDetouredField<LogicBroadcastChannelSideScreen, LocText> HEADER_LABEL = PDetours.DetourField<LogicBroadcastChannelSideScreen, LocText>("headerLabel");

		// Token: 0x0400005C RID: 92
		private static readonly IDetouredField<LogicBroadcastChannelSideScreen, GameObject> NO_CHANNEL_ROW = PDetours.DetourField<LogicBroadcastChannelSideScreen, GameObject>("noChannelRow");

		// Token: 0x0400005D RID: 93
		private static readonly IDetouredField<LogicBroadcastChannelSideScreen, GameObject> EMPTY_SPACE_ROW = PDetours.DetourField<LogicBroadcastChannelSideScreen, GameObject>("emptySpaceRow");

		// Token: 0x0400005E RID: 94
		private PortalConnection sensor;

		// Token: 0x0400005F RID: 95
		[SerializeField]
		private GameObject rowPrefab;

		// Token: 0x04000060 RID: 96
		[SerializeField]
		private GameObject listContainer;

		// Token: 0x04000061 RID: 97
		[SerializeField]
		private LocText headerLabel;

		// Token: 0x04000062 RID: 98
		[SerializeField]
		private GameObject noChannelRow;

		// Token: 0x04000063 RID: 99
		private GameObject emptySpaceRow;

		// Token: 0x04000064 RID: 100
		private Dictionary<PortalConnection, GameObject> tsenderCtrlRows = new Dictionary<PortalConnection, GameObject>();
	}
	
	
	// Token: 0x02000006 RID: 6
	public class StoppingReact : Workable
	{
		// Token: 0x06000019 RID: 25 RVA: 0x00002944 File Offset: 0x00000B44
		private void ClearReactable()
		{
			bool flag = this.reactable != null;
			if (flag)
			{
				this.reactable.Cleanup();
				this.reactable = null;
			}
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002974 File Offset: 0x00000B74
		private void CreateNewReactable(PortalCore sp)
		{
			this.reactable = new StoppingReact.PortalEnterWorkableReactable(this, sp);
		}

		// Token: 0x04000009 RID: 9
		private StoppingReact.PortalEnterWorkableReactable reactable;

		// Token: 0x0200001A RID: 26
		public class PortalEnterWorkableReactable : Reactable
		{
			// Token: 0x060000B2 RID: 178 RVA: 0x00007940 File Offset: 0x00005B40
			public PortalEnterWorkableReactable(StoppingReact stopper, PortalCore sp) : base(stopper.gameObject, "PortalEnterReactable", Db.Get().ChoreTypes.Checkpoint, 0, 0, true, 0f, 0f, float.PositiveInfinity)
			{
				bool flag = stopper == null;
				if (flag)
				{
					throw new ArgumentNullException("stopper is null");
				}
				bool flag2 = sp == null;
				if (flag2)
				{
					throw new ArgumentNullException("portal Core is null");
				}
				this.stopper = stopper;
				this.sp = sp;
				this.distractedAnim = Assets.GetAnim("anim_interacts_washbasin_kanim");
			}

			// Token: 0x060000B3 RID: 179 RVA: 0x000079D8 File Offset: 0x00005BD8
			public override bool InternalCanBegin(GameObject new_reactor, Navigator.ActiveTransition transition)
			{
				bool flag = this.reactor == null;
				bool flag2 = flag;
				if (flag2)
				{
					flag = this.MustStop(new_reactor, (float)transition.x);
				}
				return flag;
			}

			// Token: 0x060000B4 RID: 180 RVA: 0x00007A10 File Offset: 0x00005C10
			private bool MustStop(GameObject dupe, float x)
			{
				SuffocationMonitor.Instance instance = (dupe == null) ? null : dupe.GetSMI<SuffocationMonitor.Instance>();
				Navigator component = dupe.GetComponent<Navigator>();
				bool needToStop = false;
				bool flag = component != null;
				if (flag)
				{
					bool flag2 = this.sp != null;
					if (flag2)
					{
						PortalConnection portalConnection;
						this.sp.tryGetTargetPortal(out portalConnection);
						bool flag3 = portalConnection != null;
						if (flag3)
						{
							PortalCore component2 = portalConnection.GetComponent<PortalCore>();
							NavTeleporterNew component3 = component2.GetComponent<NavTeleporterNew>();
							int targetOutputCell = component3.GetCell(false);
							component.path.nodes.ForEach(delegate(PathFinder.Path.Node node)
							{
								bool flag4 = node.cell == targetOutputCell;
								if (flag4)
								{
									needToStop = true;
								}
							});
						}
						else
						{
							NavTeleporterNew component4 = this.sp.GetComponent<NavTeleporterNew>();
							int targetOutputCell = component4.GetCell(false);
							component.path.nodes.ForEach(delegate(PathFinder.Path.Node node)
							{
								bool flag4 = node.cell == targetOutputCell;
								if (flag4)
								{
									needToStop = true;
								}
							});
						}
					}
				}
				bool needToStop2 = needToStop;
				if (needToStop2)
				{
					needToStop = true;
				}
				return (instance == null || !instance.IsSuffocating()) & needToStop;
			}

			// Token: 0x060000B5 RID: 181 RVA: 0x00007B58 File Offset: 0x00005D58
			public void MyUpdateLocation()
			{
				GameScenePartitioner.Instance.Free(ref this.partitionerEntry);
				bool flag = this.gameObject != null;
				if (flag)
				{
					this.sourceCell = Grid.PosToCell(this.gameObject);
					this.sp = this.gameObject.GetComponent<PortalCore>();
					Extents extents = default(Extents);
					bool flag2 = this.sp != null;
					if (flag2)
					{
						Rotatable component = this.sp.GetComponent<Rotatable>();
						bool flag3 = component != null;
						if (flag3)
						{
							switch (component.GetOrientation())
							{
							case Orientation.Neutral:
								extents = Extents.OneCell(Grid.CellAbove(this.sourceCell));
								break;
							case Orientation.R90:
								extents = Extents.OneCell(Grid.CellRight(this.sourceCell));
								break;
							case Orientation.R180:
								extents = Extents.OneCell(Grid.CellBelow(Grid.CellBelow(this.sourceCell)));
								break;
							case Orientation.R270:
								extents = Extents.OneCell(Grid.CellLeft(this.sourceCell));
								break;
							}
							this.partitionerEntry = GameScenePartitioner.Instance.Add("Reactable", this, extents, GameScenePartitioner.Instance.objectLayers[0], null);
						}
					}
				}
			}

			// Token: 0x060000B6 RID: 182 RVA: 0x00007C88 File Offset: 0x00005E88
			protected override void InternalBegin()
			{
				this.reactorNavigator = this.reactor.GetComponent<Navigator>();
				bool debugPathFinder = this.sp.debugPathFinder;
				if (debugPathFinder)
				{
					this.reactorNavigator.DrawPath();
				}
				KBatchedAnimController component = this.reactor.GetComponent<KBatchedAnimController>();
				component.AddAnimOverrides(Assets.GetAnim("anim_interacts_washbasin_kanim"), 1f);
				component.Play("idle_pre", KAnim.PlayMode.Once, 1f, 0f);
				component.Queue("idle_default", KAnim.PlayMode.Loop, 1f, 0f);
				this.stopper.CreateNewReactable(this.sp);
			}

			// Token: 0x060000B7 RID: 183 RVA: 0x00007D34 File Offset: 0x00005F34
			public override void Update(float dt)
			{
				bool flag = this.sp == null;
				if (flag)
				{
					base.Cleanup();
				}
				else
				{
					bool flag2 = !this.sp.isPortalValid();
					if (flag2)
					{
						base.Cleanup();
					}
				}
			}

			// Token: 0x060000B8 RID: 184 RVA: 0x00007D7C File Offset: 0x00005F7C
			protected override void InternalEnd()
			{
				GameObject reactor = this.reactor;
				bool flag = reactor == null;
				if (!flag)
				{
					reactor.GetComponent<KBatchedAnimController>().RemoveAnimOverrides(this.distractedAnim);
				}
			}

			// Token: 0x060000B9 RID: 185 RVA: 0x00007DB1 File Offset: 0x00005FB1
			protected override void InternalCleanup()
			{
				this.reactorNavigator = null;
			}

			// Token: 0x04000089 RID: 137
			private Navigator reactorNavigator;

			// Token: 0x0400008A RID: 138
			private PortalCore sp;

			// Token: 0x0400008B RID: 139
			private int rangeHeight;

			// Token: 0x0400008C RID: 140
			private HandleVector<int>.Handle partitionerEntry;

			// Token: 0x0400008D RID: 141
			protected Workable workable;

			// Token: 0x0400008E RID: 142
			private Worker worker;

			// Token: 0x0400008F RID: 143
			private readonly StoppingReact stopper;

			// Token: 0x04000090 RID: 144
			private readonly KAnimFile distractedAnim;

			// Token: 0x02000032 RID: 50
			public enum AllowedDirection
			{
				// Token: 0x040000CF RID: 207
				Any,
				// Token: 0x040000D0 RID: 208
				Left,
				// Token: 0x040000D1 RID: 209
				Right
			}
		}
	}
	
	public static class StringUtils
	{
		// Token: 0x0600009E RID: 158 RVA: 0x0000760C File Offset: 0x0000580C
		public static void AddBuildingStrings(string buildingId, string name, string description, string effect)
		{
			Strings.Add(new string[]
			{
				"STRINGS.BUILDINGS.PREFABS." + buildingId.ToUpperInvariant() + ".NAME",
				UI.FormatAsLink(name, buildingId)
			});
			Strings.Add(new string[]
			{
				"STRINGS.BUILDINGS.PREFABS." + buildingId.ToUpperInvariant() + ".DESC",
				description
			});
			Strings.Add(new string[]
			{
				"STRINGS.BUILDINGS.PREFABS." + buildingId.ToUpperInvariant() + ".EFFECT",
				effect
			});
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00007698 File Offset: 0x00005898
		public static void AddStatusItemStrings(string id, string prefix, string name, string tooltip)
		{
			Strings.Add(new string[]
			{
				string.Concat(new string[]
				{
					"STRINGS.",
					prefix.ToUpperInvariant(),
					".STATUSITEMS.",
					id.ToUpperInvariant(),
					".NAME"
				}),
				name
			});
			Strings.Add(new string[]
			{
				string.Concat(new string[]
				{
					"STRINGS.",
					prefix.ToUpperInvariant(),
					".STATUSITEMS.",
					id.ToUpperInvariant(),
					".TOOLTIP"
				}),
				tooltip
			});
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00007738 File Offset: 0x00005938
		public static void AddSideScreenStrings(string key, string title, string tooltip)
		{
			Strings.Add(new string[]
			{
				"STRINGS.UI.UISIDESCREENS." + key.ToUpperInvariant() + ".TITLE",
				title
			});
			Strings.Add(new string[]
			{
				"STRINGS.UI.UISIDESCREENS." + key.ToUpperInvariant() + ".TOOLTIP",
				tooltip
			});
		}
	}
	
	public class TeleportStateMachine : GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget>
	{
		// Token: 0x0600008F RID: 143 RVA: 0x00006B34 File Offset: 0x00004D34
		public override void InitializeStates(out StateMachine.BaseState defaultState)
		{
			defaultState = this.Searching;
			this.Searching.Enter("SetSearching", delegate(TeleportStateMachine.Instance smi)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				component.isStoring = false;
				component.isDropping = false;
				smi.GetComponent<KBatchedAnimController>().Play(TeleportStateMachine.Searching_anims, KAnim.PlayMode.Paused, 1f, 0f);
			}).UpdateTransition(this.Stretching_input, delegate(TeleportStateMachine.Instance smi, float ft)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				PortalConnection portalConnection;
				return component.needPick() && !component.isDropping && !component.needDrop() && !component.tryGetTargetPortal(out portalConnection);
			}, UpdateRate.SIM_1000ms, false).UpdateTransition(this.Teleporting_input, delegate(TeleportStateMachine.Instance smi, float ft)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				PortalConnection portalConnection;
				return component.needPick() && !component.isDropping && !component.needDrop() && component.tryGetTargetPortal(out portalConnection);
			}, UpdateRate.SIM_1000ms, false).UpdateTransition(this.Stretching_output, delegate(TeleportStateMachine.Instance smi, float ft)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				PortalConnection component2 = smi.GetComponent<PortalConnection>();
				return component.needDrop() && !component.isStoring && !component.hasChild(component2.ID);
			}, UpdateRate.SIM_1000ms, false).UpdateTransition(this.Teleporting_output, delegate(TeleportStateMachine.Instance smi, float ft)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				PortalConnection component2 = smi.GetComponent<PortalConnection>();
				return component.needDrop() && !component.isStoring && component.hasChild(component2.ID);
			}, UpdateRate.SIM_1000ms, false).EventTransition(GameHashes.ActiveChanged, this.Idle, (TeleportStateMachine.Instance smi) => !smi.GetComponent<Operational>().IsActive);
			this.Idle.Enter("SetIdle", delegate(TeleportStateMachine.Instance smi)
			{
				smi.GetComponent<KBatchedAnimController>().Play(TeleportStateMachine.Idle_anims, KAnim.PlayMode.Paused, 1f, 0f);
			}).EventTransition(GameHashes.ActiveChanged, this.Searching, (TeleportStateMachine.Instance smi) => smi.GetComponent<Operational>().IsActive).UpdateTransition(this.Searching, delegate(TeleportStateMachine.Instance smi, float ft)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				return !component.blocked(false) && component.isLogicEnabled();
			}, UpdateRate.SIM_1000ms, false);
			this.Stretching_input.PlayAnim("Stretching_input").Enter("SetStretching", delegate(TeleportStateMachine.Instance smi)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				component.isStoring = true;
			}).OnAnimQueueComplete(this.Storing_input_part1);
			this.Storing_input_part1.PlayAnim("Storing_input_part1").Enter("SetStoring_part1", delegate(TeleportStateMachine.Instance smi)
			{
			}).OnAnimQueueComplete(this.Storing_input_part2);
			this.Storing_input_part2.PlayAnim("Storing_input_part2").Enter("SetStoring_part2", delegate(TeleportStateMachine.Instance smi)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				component.doPick();
			}).OnAnimQueueComplete(this.Retracting_input);
			this.Teleporting_input.PlayAnim("Teleporting_input").Enter("SetTeleporting_input", delegate(TeleportStateMachine.Instance smi)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				component.isStoring = true;
			}).OnAnimQueueComplete(this.Teleporting_part1);
			this.Teleporting_part1.PlayAnim("Teleporting_part1").Enter("SetTeleporting_part1", delegate(TeleportStateMachine.Instance smi)
			{
			}).Exit("ExitTeleporting_part1", delegate(TeleportStateMachine.Instance smi)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				component.doPick();
			}).OnAnimQueueComplete(this.Teleporting_part2);
			this.Teleporting_part2.PlayAnim("Teleporting_part2").Enter("SetTeleporting_part2", delegate(TeleportStateMachine.Instance smi)
			{
			}).OnAnimQueueComplete(this.Retracting_input);
			this.Retracting_input.PlayAnim("Retracting_input").Enter("SetRetracting", delegate(TeleportStateMachine.Instance smi)
			{
			}).OnAnimQueueComplete(this.Searching);
			this.Stretching_output.PlayAnim("Stretching_output").Enter("SetStretching_output", delegate(TeleportStateMachine.Instance smi)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				component.isDropping = true;
			}).OnAnimQueueComplete(this.Dropping_normal);
			this.Dropping_normal.PlayAnim("Dropping_normal").Enter("SetDropping_normal", delegate(TeleportStateMachine.Instance smi)
			{
			}).Exit("ExitDropping", delegate(TeleportStateMachine.Instance smi)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				component.doDrop();
			}).OnAnimQueueComplete(this.Retracting_output);
			this.Dropping_part2.PlayAnim("Dropping_part1").Enter("SetDropping_part2", delegate(TeleportStateMachine.Instance smi)
			{
			}).OnAnimQueueComplete(this.Retracting_output);
			this.Teleporting_output.PlayAnim("Teleporting_output").Enter("SetTeleporting_output", delegate(TeleportStateMachine.Instance smi)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				component.isDropping = true;
			}).OnAnimQueueComplete(this.Dropping_tele_part1);
			this.Dropping_tele_part1.PlayAnim("dropping_tele_part1").Enter("Setdropping_tele_part1", delegate(TeleportStateMachine.Instance smi)
			{
			}).Exit("Exitdropping_tele_part1", delegate(TeleportStateMachine.Instance smi)
			{
				PortalCore component = smi.GetComponent<PortalCore>();
				component.doDrop();
			}).OnAnimQueueComplete(this.Dropping_tele_part2);
			this.Dropping_tele_part2.PlayAnim("Dropping_tele_part2").Enter("SetDropping_tele_part2", delegate(TeleportStateMachine.Instance smi)
			{
			}).OnAnimQueueComplete(this.Retracting_output);
			this.Retracting_output.PlayAnim("Retracting_output").Enter("SetRetracting_output", delegate(TeleportStateMachine.Instance smi)
			{
			}).OnAnimQueueComplete(this.Searching);
		}

		// Token: 0x04000069 RID: 105
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Idle;

		// Token: 0x0400006A RID: 106
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Searching;

		// Token: 0x0400006B RID: 107
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Stretching_input;

		// Token: 0x0400006C RID: 108
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Storing_input_part1;

		// Token: 0x0400006D RID: 109
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Storing_input_part2;

		// Token: 0x0400006E RID: 110
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Teleporting_input;

		// Token: 0x0400006F RID: 111
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Teleporting_part1;

		// Token: 0x04000070 RID: 112
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Teleporting_part2;

		// Token: 0x04000071 RID: 113
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Teleporting_output;

		// Token: 0x04000072 RID: 114
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Dropping_tele_part1;

		// Token: 0x04000073 RID: 115
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Dropping_tele_part2;

		// Token: 0x04000074 RID: 116
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Dropping_normal;

		// Token: 0x04000075 RID: 117
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Dropping_part2;

		// Token: 0x04000076 RID: 118
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Stretching_output;

		// Token: 0x04000077 RID: 119
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Retracting_input;

		// Token: 0x04000078 RID: 120
		public GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.State Retracting_output;

		// Token: 0x04000079 RID: 121
		private static readonly HashedString Idle_anims = new HashedString("Idle");

		// Token: 0x0400007A RID: 122
		private static readonly HashedString Searching_anims = new HashedString("Searching");

		// Token: 0x0200002A RID: 42
		public new class Instance : GameStateMachine<TeleportStateMachine, TeleportStateMachine.Instance, IStateMachineTarget, object>.GameInstance
		{
			// Token: 0x060000DC RID: 220 RVA: 0x000084EF File Offset: 0x000066EF
			public Instance(IStateMachineTarget master) : base(master)
			{
			}
		}
	}
}