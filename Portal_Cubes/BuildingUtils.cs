using System;
using System.Collections.Generic;
using TUNING;

namespace Portal_Cubes
{
   // Token: 0x02000014 RID: 20
    public static class BuildingUtils
    {
        // Token: 0x0600009B RID: 155 RVA: 0x000074D8 File Offset: 0x000056D8
        private static PlanScreen.PlanInfo GetMenu(HashedString category)
        {
            foreach (PlanScreen.PlanInfo planInfo in BUILDINGS.PLANORDER)
            {
                bool flag = planInfo.category == category;
                if (flag)
                {
                    return planInfo;
                }
            }
            throw new Exception("The plan menu was not found in TUNING.BUILDINGS.PLANORDER.");
        }

        // Token: 0x0600009C RID: 156 RVA: 0x00007548 File Offset: 0x00005748
        public static void AddBuildingToPlanScreen(string buildingID, HashedString category, string addAferID = null)
        {
            PlanScreen.PlanInfo menu = BuildingUtils.GetMenu(category);
            List<string> data = menu.data;
            bool flag = data != null;
            if (flag)
            {
                bool flag2 = addAferID != null;
                if (flag2)
                {
                    int num = data.IndexOf(addAferID);
                    bool flag3 = num == -1 || num == data.Count - 1;
                    if (flag3)
                    {
                        data.Add(buildingID);
                    }
                    else
                    {
                        data.Insert(num + 1, buildingID);
                    }
                }
                else
                {
                    data.Add(buildingID);
                }
            }
        }

        // Token: 0x0600009D RID: 157 RVA: 0x000075C0 File Offset: 0x000057C0
        public static void AddBuildingToTech(string buildingID, string techID)
        {
            Tech tech = Db.Get().Techs.Get(techID);
            bool flag = tech != null;
            if (flag)
            {
                tech.unlockedItemIDs.Add(buildingID);
            }
            else
            {
                Debug.LogWarning("AddBuildingToTech() Failed to find tech ID: " + techID);
            }
        }
    }
}