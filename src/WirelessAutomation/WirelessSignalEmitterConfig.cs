using STRINGS;
using System.Collections.Generic;
using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace WirelessAutomation
{
    public class WirelessSignalEmitterConfig : IBuildingConfig
    {
        public static string Id = "WIRELESSSIGNALEMITTER";

        public override BuildingDef CreateBuildingDef()
        {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                id: Id,
                width: 1,
                height: 1,
                anim: "wifi_emitter_kanim",
                hitpoints: BUILDINGS.HITPOINTS.TIER1,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER1,
                construction_materials: MATERIALS.REFINED_METALS,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.Anywhere,
                decor: DECOR.NONE,
                noise: NOISE_POLLUTION.NONE);

            buildingDef.Overheatable = false;
            buildingDef.Floodable = false;
            buildingDef.Entombable = false;
            buildingDef.ViewMode = OverlayModes.Logic.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.SceneLayer = Grid.SceneLayer.Building;

            buildingDef.LogicInputPorts = new List<LogicPorts.Port>
            {
                LogicPorts.Port.RibbonInputPort(LogicSwitch.PORT_ID, new CellOffset(0, 0),
                    UI.LOGIC_PORTS.CONTROL_OPERATIONAL, STRINGS.BUILDINGS.PREFABS.WIRELESSSIGNALEMITTER.PORT_ACTIVE, STRINGS.BUILDINGS.PREFABS.WIRELESSSIGNALEMITTER.PORT_INACTIVE, true)
            };

            GeneratedBuildings.RegisterWithOverlay(OverlayModes.Logic.HighlightItemIDs, Id);

            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<WIRELESSSIGNALEMITTER>().EmitChannel = 0; 
        }
    }
}