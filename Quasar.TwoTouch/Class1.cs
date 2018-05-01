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


/// <summary>
/// This is a prototype for Quasar package.
/// </summary>
namespace Quasar
{   /// <summary>
    /// Utility class for tools methods
    /// </summary>
    public static class Utility
    {   
       
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
    ///     Main class for Quasar, later class will be named based on node category.
    /// </summary>
    public static class TwoTouch
    {
        /// <summary>
        ///     Return all the views from current project.
        /// </summary>
        /// <returns name="views">All views</returns>

        [IsVisibleInDynamoLibrary(false)]
        public static List<Revit.Elements.Element> Views()
        {
            var doc = DocumentManager.Instance.CurrentDBDocument;
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Views);
            var elements = collector.ToElements();
            var views = new List<Revit.Elements.Element>();
            foreach (var elem in elements)
            {
                views.Add(elem.ToDSType(true));
            }
            return views;
        }

        /// <summary>
        /// Return current view from this document.
        /// </summary>
        [IsVisibleInDynamoLibrary(true)]
        public static Revit.Elements.Element ActiveView()
        {
            var ActiveView = DocumentManager.Instance.CurrentDBDocument.ActiveView.ToDSType(true);
            return ActiveView;
        }
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
        public static List<Revit.Elements.Element> CopyPasteFilter (Revit.Elements.Views.View ViewToCopy, List<Revit.Elements.Views.View> ViewToPaste)
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
            RevitServices.Transactions.TransactionManager.Instance.EnsureInTransaction(DocumentManager.Instance.CurrentDBDocument);
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

            RevitServices.Transactions.TransactionManager.Instance.TransactionTaskDone();
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
        public static String HideUnHideElement(List<Revit.Elements.Element> Elements, List<Revit.Elements.Element> Views,Boolean HideUnhide=false)
        {
            var ids = new List<ElementId>();
            RevitServices.Transactions.TransactionManager.Instance.EnsureInTransaction(RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument);
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
            RevitServices.Transactions.TransactionManager.Instance.TransactionTaskDone();
            return "Done!";
        }
        /// <summary>
        /// Create 3D Views for given room.
        /// </summary>
        /// <param name="Rooms">Rooms elements</param>
        /// <param name="Names">Name for new views</param>
        /// <param name="Offset">Offset value for section box. Default is 500.</param>
        /// <returns name="ThreeDView">New 3D Views</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static List<Revit.Elements.Element> ThreeDViewByRoom(List<Revit.Elements.Room> Rooms, List<String> Names,double Offset=500/304.8)
        {
            var ThreeDViews = new List<Revit.Elements.Element>();
            var doc = DocumentManager.Instance.CurrentDBDocument;
            var vtype = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().FirstOrDefault( a=>a.ViewFamily == ViewFamily.ThreeDimensional);
            RevitServices.Transactions.TransactionManager.Instance.EnsureInTransaction(doc);
            foreach(var elem in Rooms.Zip(Names, Tuple.Create))
            {
                BoundingBoxXYZ bbox = elem.Item1.InternalElement.get_BoundingBox(doc.ActiveView);
                var newbbox = Utility.crop_box(bbox, Offset);
                View3D ThreeDView = View3D.CreateIsometric(doc, vtype.Id);
                ThreeDView.Name = elem.Item2;
                ThreeDView.SetSectionBox(newbbox);
                ThreeDView.CropBoxActive = true;
                ThreeDView.CropBoxVisible = true;
                ThreeDView.Scale = 50;
                ThreeDView.DetailLevel = ViewDetailLevel.Fine;
                ThreeDView.DisplayStyle = DisplayStyle.Realistic;
                ThreeDViews.Add(ThreeDView.ToDSType(true));

            }
            RevitServices.Transactions.TransactionManager.Instance.TransactionTaskDone();

            return ThreeDViews;
        }
    }
}
