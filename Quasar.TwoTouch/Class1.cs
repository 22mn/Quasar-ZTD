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
{
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
    }
}
