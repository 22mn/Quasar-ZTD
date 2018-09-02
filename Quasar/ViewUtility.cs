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
    /// This class contains view related nodes.
    /// </summary>
    public static class ViewUtility
    {
        /// <summary>
        /// Current Document Active View
        /// </summary>
        /// <returns name = "ActiveView">Return ActiveView</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static Revit.Elements.Element ActiveView()
        {
            var ActiveView = DocumentManager.Instance.CurrentDBDocument.ActiveView.ToDSType(true);
            return ActiveView;
        }

        /// <summary>
        /// Create floor plan views by rooms, names and offset.
        /// </summary>
        /// <param name="Level">Level element</param>
        /// <param name="Rooms"></param>
        /// <param name="Names">List of names for new views</param>
        /// <param name="Offset">Cropbox offset from room</param>
        /// <returns name="FloorPlanView">Created Ceiling Views</returns>
        public static List<Revit.Elements.Element> FloorPlanViewByRoom(Revit.Elements.Element Level, List<Revit.Elements.Room> Rooms, List<String> Names, double Offset = 500)
        {
            var FloorPlanView = new List<Revit.Elements.Element>();
            var doc = DocumentManager.Instance.CurrentDBDocument;
            var fViews = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Where(x => x.ViewType == ViewType.FloorPlan).ToList();
            var fview = from c in fViews where c.LookupParameter("Associated Level").AsString() == Level.Name.ToString() select c;
            var view = fview.First();
            TransactionManager.Instance.EnsureInTransaction(doc);
            foreach (var elem in Rooms.Zip(Names, Tuple.Create))
            {
                var v = view.Duplicate(ViewDuplicateOption.WithDetailing);
                BoundingBoxXYZ bbox = elem.Item1.InternalElement.get_BoundingBox(doc.ActiveView);
                var newbbox = Utility.crop_box(bbox, Offset / 304.8);
                var dupview = (Autodesk.Revit.DB.View)doc.GetElement(v);
                dupview.Name = elem.Item2;
                dupview.CropBox = newbbox;
                dupview.CropBoxActive = true;
                dupview.CropBoxVisible = true;
                dupview.Scale = view.Scale;
                FloorPlanView.Add(dupview.ToDSType(true));

            }
            TransactionManager.Instance.TransactionTaskDone();

            return FloorPlanView;
        }

        /// <summary>
        /// Create 3D Views for given room.
        /// </summary>
        /// <param name="Rooms">Rooms elements</param>
        /// <param name="Names">Name for new views</param>
        /// <param name="Offset">Offset value for section box. Default is 500.</param>
        /// <returns name="ThreeDView">New 3D Views</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static List<Revit.Elements.Element> ThreeDViewByRoom(List<Revit.Elements.Room> Rooms, List<String> Names, double Offset = 500)
        {
            var ThreeDViews = new List<Revit.Elements.Element>();
            var doc = DocumentManager.Instance.CurrentDBDocument;
            var vtype = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().FirstOrDefault(a => a.ViewFamily == ViewFamily.ThreeDimensional);
            TransactionManager.Instance.EnsureInTransaction(doc);
            foreach (var elem in Rooms.Zip(Names, Tuple.Create))
            {
                BoundingBoxXYZ bbox = elem.Item1.InternalElement.get_BoundingBox(doc.ActiveView);
                var newbbox = Utility.crop_box(bbox, Offset / 304.8);
                View3D ThreeDView = View3D.CreateIsometric(doc, vtype.Id);
                ThreeDView.Name = elem.Item2;
                ThreeDView.SetSectionBox(newbbox);
                ThreeDView.CropBoxActive = true;
                ThreeDView.CropBoxVisible = true;
                ThreeDView.Scale = 50;
                ThreeDViews.Add(ThreeDView.ToDSType(true));

            }
            TransactionManager.Instance.TransactionTaskDone();

            return ThreeDViews;
        }

        /// <summary>
        /// Create ceiling views by rooms, names and offset.
        /// </summary>
        /// <param name="Level">Level element</param>
        /// <param name="Rooms"></param>
        /// <param name="Names">List of names for new views</param>
        /// <param name="Offset">Cropbox offset from room</param>
        /// <returns name="CeilingView">Created Ceiling Views</returns>
        public static List<Revit.Elements.Element> CeilingViewByRoom(Revit.Elements.Element Level, List<Revit.Elements.Room> Rooms, List<String> Names, double Offset = 500)
        {
            var CeilingView = new List<Revit.Elements.Element>();
            var doc = DocumentManager.Instance.CurrentDBDocument;
            var CViews = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Where(x => x.ViewType == ViewType.CeilingPlan).ToList();
            var ceiling = from c in CViews where c.LookupParameter("Associated Level").AsString() == Level.Name.ToString() select c;
            var view = ceiling.First();
            TransactionManager.Instance.EnsureInTransaction(doc);
            foreach (var elem in Rooms.Zip(Names, Tuple.Create))
            {
                var v = view.Duplicate(ViewDuplicateOption.WithDetailing);
                BoundingBoxXYZ bbox = elem.Item1.InternalElement.get_BoundingBox(doc.ActiveView);
                var newbbox = Utility.crop_box(bbox, Offset / 304.8);
                var dupview = (Autodesk.Revit.DB.View)doc.GetElement(v);
                dupview.Name = elem.Item2;
                dupview.CropBox = newbbox;
                dupview.CropBoxActive = true;
                dupview.CropBoxVisible = true;
                dupview.Scale = view.Scale;
                CeilingView.Add(dupview.ToDSType(true));

            }
            TransactionManager.Instance.TransactionTaskDone();

            return CeilingView;
        }

        /// <summary>
        /// Create elevation views in room with crop offset by rooms , floorplan and offset.
        /// Default naming is - "RoomName_RoomNumber - A", "RoomName_RoomNumber - B",
        /// "RoomName_RoomNumber - C", "RoomName_RoomNumber - D".
        /// </summary>
        /// <param name="Rooms">Room elements and make sure all room are bounding</param>
        /// <param name="FloorPlan">Floor plan view</param>
        /// <param name="Offset">Offset from room , default is 500</param>
        /// <returns name="ElevationView"> New Elevation Views</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static List<List<Revit.Elements.Element>> ElevationInRoom(List<Revit.Elements.Room> Rooms, Revit.Elements.Element FloorPlan, double Offset = 500)
        {
            var doc = DocumentManager.Instance.CurrentDBDocument;
            var vtype = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().FirstOrDefault(a => a.ViewFamily == ViewFamily.Elevation);
            var ElevationView = new List<List<Revit.Elements.Element>>();
            TransactionManager.Instance.EnsureInTransaction(doc);
            foreach (var r in Rooms)
            {
                var list = new List<Revit.Elements.Element>();
                var elevViews = new List<Revit.Elements.Element>();
                String rname = r.InternalElement.LookupParameter("Number").AsString() + "_" + r.InternalElement.LookupParameter("Name").AsString();

                LocationPoint elevPoint = (Autodesk.Revit.DB.LocationPoint)r.InternalElement.Location;
                XYZ point = elevPoint.Point;
                BoundingBoxXYZ bbox = r.InternalElement.get_BoundingBox(doc.ActiveView);
                ElevationMarker marker = Autodesk.Revit.DB.ElevationMarker.CreateElevationMarker(doc, vtype.Id, point, 50);

                BoundingBoxXYZ bcrop = Utility.crop_box(bbox, Offset / 304.8);
                var surfaces = bcrop.ToProtoType(true).ToPolySurface().Surfaces().Skip(2).Take(4);

                var westElev = marker.CreateElevation(doc, FloorPlan.InternalElement.Id, 0);
                westElev.Name = rname + " - A";
                var westcrsm = westElev.GetCropRegionShapeManager();
                var west = surfaces.ElementAt(0).PerimeterCurves();
                var westcurve = new List<Autodesk.Revit.DB.Curve>();
                foreach (var w in west) { westcurve.Add(w.ToRevitType(true)); }
                CurveLoop wcloop = CurveLoop.Create(westcurve);
                westcrsm.SetCropShape(wcloop);

                var northElev = marker.CreateElevation(doc, FloorPlan.InternalElement.Id, 1);
                northElev.Name = rname + " - B";
                var northcrsm = northElev.GetCropRegionShapeManager();
                var north = surfaces.ElementAt(1).PerimeterCurves();
                var northcurve = new List<Autodesk.Revit.DB.Curve>();
                foreach (var w in north) { northcurve.Add(w.ToRevitType(true)); }
                CurveLoop ncloop = CurveLoop.Create(northcurve);
                northcrsm.SetCropShape(ncloop);

                var eastElev = marker.CreateElevation(doc, FloorPlan.InternalElement.Id, 2);
                eastElev.Name = rname + " - C";
                var eastcrsm = eastElev.GetCropRegionShapeManager();
                var east = surfaces.ElementAt(2).PerimeterCurves();
                var eastcurve = new List<Autodesk.Revit.DB.Curve>();
                foreach (var w in east) { eastcurve.Add(w.ToRevitType(true)); }
                CurveLoop ecloop = CurveLoop.Create(eastcurve);
                eastcrsm.SetCropShape(ecloop);

                var southElev = marker.CreateElevation(doc, FloorPlan.InternalElement.Id, 3);
                southElev.Name = rname + " - D";
                var southcrsm = southElev.GetCropRegionShapeManager();
                var south = surfaces.ElementAt(3).PerimeterCurves();
                var southcurve = new List<Autodesk.Revit.DB.Curve>();
                foreach (var w in south) { southcurve.Add(w.ToRevitType(true)); }
                CurveLoop scloop = CurveLoop.Create(southcurve);
                southcrsm.SetCropShape(scloop);

                list.Add(westElev.ToDSType(true));
                list.Add(northElev.ToDSType(true));
                list.Add(eastElev.ToDSType(true));
                list.Add(southElev.ToDSType(true));

                ElevationView.Add(list);
            }
            TransactionManager.Instance.TransactionTaskDone();
            return ElevationView;
        }

        /// <summary>
        /// Transfer View Templates from a link document to current document 
        /// with or without associate filters(including override settings). Default include filters.
        /// </summary>
        /// <param name="LinkDocument"> A Link document which includes view templates</param>
        /// <param name="IsIncludeFilters"> If true, filters and settings will include with view template.
        /// If false, filters and settings will not include, only view templates will tranfer.
        /// default value true.</param>
        /// <returns name="TemplateNames">Created template name list.</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static List<string> TransferViewTemplateAndFilter(Document LinkDocument, bool IsIncludeFilters = true)
        {
            var TemplateNames = new List<string>();
            var doc = DocumentManager.Instance.CurrentDBDocument;
            var views = new FilteredElementCollector(LinkDocument).OfClass(typeof(View)).Cast<View>().Where(x => x.IsTemplate).ToList();
            var ids = new List<ElementId>();
            foreach (var view in views) { ids.Add(view.Id); }
            TransactionManager.Instance.EnsureInTransaction(doc);

            var templates = ElementTransformUtils.CopyElements(LinkDocument, ids, doc, Transform.Identity, new CopyPasteOptions());
            foreach (var i in templates) { TemplateNames.Add(doc.GetElement(i).Name); }

            if (!IsIncludeFilters)
            {
                foreach (ElementId v in templates)
                {
                    View view = doc.GetElement(v) as View;
                    var filters = view.GetFilters();
                    foreach (ElementId f in filters)
                    {
                        view.RemoveFilter(f);
                    }

                }
            }

            TransactionManager.Instance.TransactionTaskDone();
            return TemplateNames;


        }

    }
}
