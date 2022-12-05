#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace RAB_Session04ChallengeSolution
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // 1. prompt user to select elements
            TaskDialog.Show("Select lines", "Select some lines to convert to Revit elements.");
            IList<Element> pickList = uidoc.Selection.PickElementsByRectangle("Select some elements");

            // 2. filter selected elements
            List<CurveElement> filteredList = new List<CurveElement>();

            foreach(Element element in pickList)
            {
                if(element is CurveElement)
                {
                    CurveElement curve = element as CurveElement;
                    filteredList.Add(curve);
                }
            }

            // 3. Get level and various types
            Level currentLevel = GetLevelByName(doc, "Level 1");

            // 4. Loop through selected CurveElements
            List<ElementId> collection = new List<ElementId>();
            
            Transaction t = new Transaction(doc);
            t.Start("Create Revit Elements");
            foreach(CurveElement currentCurve in filteredList)
            {
                // 5. Get GraphicStyle and Curve for each CurveElement
                Curve elementCurve = currentCurve.GeometryCurve;
                GraphicsStyle currentStyle = currentCurve.LineStyle as GraphicsStyle;
                WallType wt1 = GetWallTypeByName(doc, "Storefront");
                WallType wt2 = GetWallTypeByName(doc, "Generic - 8\"");
                MEPSystemType ductSystemType = GetMEPSystemTypeByName(doc, "Supply Air");
                DuctType ductType = GetDuctTypeByName(doc, "Default");
                MEPSystemType pipeSystemType = GetMEPSystemTypeByName(doc, "Domestic Hot Water");
                PipeType pipeType = GetPipeTypeByName(doc, "Default");

                if (currentStyle.Name == "<Medium Lines>")
                {
                    collection.Add(currentCurve.Id);
                    continue;
                }

                XYZ startPoint = elementCurve.GetEndPoint(0);
                XYZ endPoint = elementCurve.GetEndPoint(1);

                switch (currentStyle.Name)
                {
                    case "A-GLAZ":
                        Wall currentWall = CreateWall(doc, elementCurve, wt1, currentLevel);
                        break;

                    case "A-WALL":
                        Wall currentWall2 = CreateWall(doc, elementCurve, wt2, currentLevel);
                        break;

                    case "M-DUCT":
                        Duct currentDuct = Duct.Create(doc, ductSystemType.Id,
                            ductType.Id, currentLevel.Id, startPoint,
                            endPoint);
                        break;

                    case "P-PIPE":
                        Pipe currentPipe = Pipe.Create(doc, pipeSystemType.Id,
                            pipeType.Id, currentLevel.Id, startPoint,
                            endPoint);
                        break;

                    default:
                        break;
                }
            }

            // hide lines
            doc.ActiveView.HideElements(collection);

            t.Commit();
            t.Dispose();

            

            // 6. Use Switch statement to create walls, ducts, and pipes


            return Result.Succeeded;
        }

        private PipeType GetPipeTypeByName(Document doc, string typeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(PipeType));

            foreach (PipeType currentType in collector)
            {
                if (currentType.Name == typeName)
                    return currentType;
            }

            return null;
        }

        private DuctType GetDuctTypeByName(Document doc, string typeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(DuctType));

            foreach (DuctType currentType in collector)
            {
                if (currentType.Name == typeName)
                    return currentType;
            }

            return null;
        }

        private MEPSystemType GetMEPSystemTypeByName(Document doc, string typeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(MEPSystemType));

            foreach (MEPSystemType currentType in collector)
            {
                if (currentType.Name == typeName)
                    return currentType;
            }

            return null;
        }

        private Wall CreateWall(Document doc, Curve curve, WallType wt, Level level)
        {
            Wall currentWall = Wall.Create(doc, curve, wt.Id, level.Id, 20, 0, false, false);

            return currentWall;
        }

        private WallType GetWallTypeByName(Document doc, string typeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(WallType));

            foreach (WallType currentType in collector)
            {
                if (currentType.Name == typeName)
                    return currentType;
            }

            return null;
        }

        private Level GetLevelByName(Document doc, string levelName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(Level));
            
            foreach(Level currentLevel in collector)
            {
                if (currentLevel.Name == levelName)
                    return currentLevel;
            }

            return null;
        }
    }
}
