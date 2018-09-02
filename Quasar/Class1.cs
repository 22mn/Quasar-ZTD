using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Geometry;
using Autodesk.Revit.DB;
using Autodesk.DesignScript.Runtime;
using RevitServices.Persistence;
using Revit.GeometryConversion;
using Revit.Elements;
using RevitServices.Transactions;
using RevitServices.Persistence;

namespace Quasar
{

    /// <summary>
    /// Utility class for tools methods
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Utility class tool
        /// </summary>
        /// <param name="bbox"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static BoundingBoxXYZ crop_box(BoundingBoxXYZ bbox, double offset)
        {
            var minx = bbox.Min.X - offset;
            var miny = bbox.Min.Y - offset;
            var minz = bbox.Min.Z - offset;
            var maxx = bbox.Max.X + offset;
            var maxy = bbox.Max.Y + offset;
            var maxz = bbox.Max.Z + offset;

            var newbox = new BoundingBoxXYZ();
            newbox.Min = new XYZ(minx, miny, minz);
            newbox.Max = new XYZ(maxx, maxy, maxz);

            return newbox;

        }

    }

    /// <summary>
    /// Quasar RevitNodes Class
    /// All Quaser Revit nodes will be under this class.
    /// </summary>
    public static class RevitNodes
    {
        /// <summary>
        ///     Copy filters and override graphic settings from a view and paste its to views.
        /// </summary>
        /// <param name="ViewToCopy">
        ///     View to Copy
        /// </param>
        /// <param name="ViewToPaste">
        ///     Views to paste
        /// </param>
        /// <returns name="Views">Pasted Views</returns>

        [IsVisibleInDynamoLibrary(true)]
        public static List<Revit.Elements.Element> CopyPasteFilter(Revit.Elements.Views.View ViewToCopy, List<Revit.Elements.Views.View> ViewToPaste)
        {

            var filtersId = new List<ElementId>();
            var settings = new List<OverrideGraphicSettings>();
            var visibility = new List<Boolean>();
            var Views = new List<Revit.Elements.Element>();
            var viewtocopy = (Autodesk.Revit.DB.View)ViewToCopy.InternalElement;
            foreach (var id in viewtocopy.GetFilters())
            {
                filtersId.Add(id);
                settings.Add(viewtocopy.GetFilterOverrides(id));
                visibility.Add(viewtocopy.GetFilterVisibility(id));
            }
            TransactionManager.Instance.EnsureInTransaction(DocumentManager.Instance.CurrentDBDocument);
            foreach (var pview in ViewToPaste)
            {
                var pasteview = (Autodesk.Revit.DB.View)pview.InternalElement;
                foreach (var i in filtersId.Zip(settings, Tuple.Create))
                {
                    pasteview.SetFilterOverrides(i.Item1, i.Item2);
                }
                foreach (var j in filtersId.Zip(visibility, Tuple.Create))
                {
                    pasteview.SetFilterVisibility(j.Item1, j.Item2);
                }
                Views.Add(pview);
            }

            TransactionManager.Instance.TransactionTaskDone();
            return Views;
        }


        /// <summary>
        /// Elements hide/unhide in given view. Default value is true(hide).
        /// </summary>
        /// <param name="Elements">Elements or Element</param>
        /// <param name="Views">Views or View</param>
        /// <param name="HideUnhide">true = hide , false = unhide</param>
        /// <returns>Return message</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static String HideUnHideElement(List<Revit.Elements.Element> Elements, List<Revit.Elements.Element> Views, Boolean HideUnhide = false)
        {
            var ids = new List<ElementId>();
            TransactionManager.Instance.EnsureInTransaction(DocumentManager.Instance.CurrentDBDocument);
            foreach (var elem in Elements)
            {
                var id = (Autodesk.Revit.DB.Element)elem.InternalElement;
                ids.Add(id.Id);
            }
            foreach (var view in Views)
            {
                if (HideUnhide == true)
                {
                    var v = (Autodesk.Revit.DB.View)view.InternalElement;
                    v.HideElements(ids);
                }
                else
                {
                    var v = (Autodesk.Revit.DB.View)view.InternalElement;
                    v.UnhideElements(ids);
                }
            }
            TransactionManager.Instance.TransactionTaskDone();
            return "Done!";
        }

        

        

        

        /// <summary>
        /// Hide/Unhide levels and grids from link documents.
        /// </summary>
        /// <param name="Hide"> Hide = true, Unhide = false</param>
        /// <returns>return message</returns>

        [IsVisibleInDynamoLibrary(true)]
        public static String LinkLevelGrid(Boolean Hide = true)
        {
            String ifilter = "LinkLevelGrid_QuasarPackage";
            var doc = DocumentManager.Instance.CurrentDBDocument;
            var activeView = doc.ActiveView;
            Boolean found = false;
            Boolean hide = Hide == true ? false : true;

            TransactionManager.Instance.EnsureInTransaction(doc);

            var allFilters = new FilteredElementCollector(doc).OfClass(typeof(FilterElement)).ToElements();

            var viewFilters = activeView.GetFilters();
            List<String> viewFiltersName = new List<String>();
            foreach (var v in viewFilters) { viewFiltersName.Add(doc.GetElement(v).Name.ToString()); }

            foreach (var fter in allFilters)
            {
                if (ifilter == fter.Name.ToString() && !viewFiltersName.Contains(ifilter))
                {
                    activeView.AddFilter(fter.Id);
                    activeView.SetFilterVisibility(fter.Id, hide);
                    found = true;
                }
                if (ifilter == fter.Name.ToString() && viewFiltersName.Contains(ifilter))
                {
                    activeView.SetFilterVisibility(fter.Id, hide);
                    found = true;
                }
            }

            if (!found)
            {
                var grids = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Grid)).ToElements();
                var levels = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Level)).ToElements();
                var cateIds = new List<ElementId> { grids.First().Category.Id, levels.First().Category.Id };
                var gridTypeIds = new HashSet<ElementId>();
                var levelTypeIds = new HashSet<ElementId>();
                foreach (var i in grids.Zip(levels, Tuple.Create)) { gridTypeIds.Add(i.Item1.GetTypeId()); levelTypeIds.Add(i.Item2.GetTypeId()); }

                var gtypeElements = new List<Autodesk.Revit.DB.Element>();
                var ltypeElements = new List<Autodesk.Revit.DB.Element>();
                foreach (var i in gridTypeIds.Zip(levelTypeIds, Tuple.Create)) { gtypeElements.Add(doc.GetElement(i.Item1)); ltypeElements.Add(doc.GetElement(i.Item2)); }
                gtypeElements.AddRange(ltypeElements);

                foreach (var e in gtypeElements)
                {
                    if (!e.LookupParameter("Type Name").AsString().Contains("_quasar"))
                    {
                        e.Name = e.LookupParameter("Type Name").AsString() + "_quasar";
                    }
                }
                var paramId = gtypeElements.First().LookupParameter("Type Name").Id;
                var ruleSet = new List<FilterRule>();
                var notEndsWith = ParameterFilterRuleFactory.CreateNotEndsWithRule(paramId, "_quasar", false);
                ruleSet.Add(notEndsWith);
                var paramFilterElem = ParameterFilterElement.Create(doc, ifilter, cateIds, ruleSet);
                var ogs = new OverrideGraphicSettings();
                activeView.SetFilterOverrides(paramFilterElem.Id, ogs);
                activeView.SetFilterVisibility(paramFilterElem.Id, hide);
            }
            TransactionManager.Instance.TransactionTaskDone();

            return "DONE!";
        }

        /// <summary>
        /// Built-In Parameter Name by element and parameter(s).
        /// </summary>
        /// <param name="Element">Element input</param>
        /// <param name="Names">Parameter name or names (string)</param>
        /// <returns name = "NameList">Each list contains [0] ParameterName [1] BuiltIn ParameterName</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static List<List<String>> GetBuiltInParameterName(Revit.Elements.Element Element, List<String> Names)
        {
            var NameList = new List<List<String>>();
            var builtInNames = new HashSet<String>();
            foreach (var i in System.Enum.GetValues(typeof(BuiltInParameter)))
            {
                var sub = new List<String>();
                foreach (var p in Names)
                {

                    if (Element.InternalElement.get_Parameter((BuiltInParameter)i) != null && Element.InternalElement.get_Parameter((BuiltInParameter)i).Definition.Name == p)
                    {
                        sub.Add(p); sub.Add(i.ToString());
                        NameList.Add(sub);
                    }
                }
            }

            return NameList;
        }
        /// <summary>
        /// Collect elements by category and document.
        /// </summary>
        /// <param name="Category">Category object</param>
        /// <param name="Document">Document object</param>
        /// <returns name = "Element">List of elements</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static List<Revit.Elements.Element> GetElementFromLinkDocument(Revit.Elements.Category Category, Autodesk.Revit.DB.Document Document)
        {
            var cate = (BuiltInCategory)Enum.ToObject(typeof(BuiltInCategory), Category.Id);
            var filter = new ElementCategoryFilter(cate);
            var Element = new FilteredElementCollector(Document).WherePasses(filter).WhereElementIsNotElementType().ToElements().Select(x => x.ToDSType(true)).ToList();
            return Element;
        }

        /// <summary>
        /// Get parameter from first input and Set this value to second input
        /// </summary>
        /// <param name="FirstElements">Element to get</param>
        /// <param name="SecondElements">Element to set</param>
        /// <param name="ParamNames">Parameter names</param>
        /// <returns>Parameter value and boolean(true is set , false can't set)</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static object GetAndSetParams(List<Revit.Elements.Element> FirstElements, List<Revit.Elements.Element> SecondElements, List<string> ParamNames)
        {
            var Result = new List<object>();
            var doc = DocumentManager.Instance.CurrentDBDocument;
            TransactionManager.Instance.EnsureInTransaction(doc);
            foreach (var i in FirstElements.Zip(SecondElements, Tuple.Create))
            {
                foreach (var j in ParamNames)
                {
                    var value = i.Item1.InternalElement.LookupParameter(j).AsString().ToString();
                    var assign = i.Item2.InternalElement.LookupParameter(j).Set(value).ToString();
                    var subList = new List<string>();
                    subList.Add(value);
                    subList.Add(assign);
                    Result.Add(subList);
                }
            }
            TransactionManager.Instance.TransactionTaskDone();
            return Result;
        }

        /// <summary>
        /// Create WallSweep by wall.
        /// </summary>
        /// <param name="Walls">Wall Elements</param>
        /// <param name="TypeElement">Wall sweep type element</param>
        /// <param name="SweepOrReveal">String value "Sweep" or "Reveal"</param>
        /// <param name="IsVertical">Is vertical true or false</param>
        /// <param name="Offset">distance from wall base</param>
        /// <returns name="WallSweeps">WallSweep Elements</returns>

        [IsVisibleInDynamoLibrary(true)]
        public static List<Revit.Elements.Element> CreateWallSweep(List<Revit.Elements.Wall> Walls,Revit.Elements.Element TypeElement,string SweepOrReveal, bool IsVertical, double Offset=1000)
        {
            var doc = DocumentManager.Instance.CurrentDBDocument;
            var WallSweeps = new List<Revit.Elements.Element>();
            var wallSweepTypes = new Dictionary<string, Autodesk.Revit.DB.WallSweepType>();
            wallSweepTypes.Add("Sweep",WallSweepType.Sweep);
            wallSweepTypes.Add("Reveal", WallSweepType.Reveal);
            var wallSweepTypeId = TypeElement.InternalElement.Id;

            WallSweepInfo wallSweepInfo = new WallSweepInfo(wallSweepTypes[SweepOrReveal], IsVertical);
            wallSweepInfo.Distance = Offset / 304.8;

            TransactionManager.Instance.EnsureInTransaction(doc);

            foreach (var w in Walls)
            {
                var wall = w.InternalElement as Autodesk.Revit.DB.Wall;
                WallSweep wallSweep = WallSweep.Create(wall, wallSweepTypeId, wallSweepInfo);
                WallSweeps.Add(wallSweep.ToDSType(true));
            }

            TransactionManager.Instance.TransactionTaskDone();
            return WallSweeps;
        }


        /// <summary>
        /// Remove paint from walls.
        /// </summary>
        /// <param name="Walls">Wall Elements</param>
        /// <returns name="WallElements">Wall Elements</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static List<Revit.Elements.Element> WallPaintRemove(List<Revit.Elements.Element> Walls)
        {
            var doc = DocumentManager.Instance.CurrentDBDocument;
            var WallElements = Walls;
            foreach (var wall in Walls)
            {
                var solid = wall.InternalElement.GetGeometryObjectFromReference(new Reference(wall.InternalElement)) as Autodesk.Revit.DB.Solid;

                    foreach( Autodesk.Revit.DB.Face face in solid.Faces)
                    {
                        doc.RemovePaint(wall.InternalElement.Id, face);
                    }
            }

            return WallElements;
        }

        /// <summary>
        /// Get Type Element of input element.
        /// </summary>
        /// <param name="Elements">Element input</param>
        /// <returns name="ElementTypes">Return Type Element of input Element</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static List<Revit.Elements.Element> GetElementType(List<Revit.Elements.Element> Elements)
        {
            var ElementTypes = new List<Revit.Elements.Element>();
            var doc = DocumentManager.Instance.CurrentDBDocument;
            foreach (var elem in Elements)
            {
                var id = elem.InternalElement.GetTypeId();
                var typeElement = doc.GetElement(id);
                ElementTypes.Add(typeElement.ToDSType(true));
            }
            return ElementTypes;
        }
        

    }

}
