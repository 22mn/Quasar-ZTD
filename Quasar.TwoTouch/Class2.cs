using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;


namespace Quasar
{
    /// <summary>
    /// Class contains dynamo nodes.
    /// </summary>
    public static class DynamoNodes
    {
        /// <summary>
        /// Surface divides to Quad Panels
        /// </summary>
        /// <param name="Surface">Surface</param>
        /// <param name="Udivision">Number of division</param>
        /// <param name="Vdivision">Number of division</param>
        /// <returns>Returns Quad Panels and Polygons</returns>
        [IsVisibleInDynamoLibrary(true)]
        [MultiReturn(new[] { "Panels", "Polygons" })]
        public static Dictionary<string, object> QuadPanel(Surface Surface, double Udivision, double Vdivision)
        {

            var panels = new List<Surface>();
            var polygons = new List<Polygon>();

            for (var i = 0; i < Udivision; i++)
            {
                for (var j = 0; j < Vdivision; j++)
                {
                    var points = new List<Point>();

                    var ustep = 1.0 / Udivision;
                    var vstep = 1.0 / Vdivision;

                    var pA = Surface.PointAtParameter(i * ustep, j * vstep);
                    var pB = Surface.PointAtParameter((i + 1) * ustep, j * vstep);
                    var pC = Surface.PointAtParameter((i + 1) * ustep, (j + 1) * vstep);
                    var pD = Surface.PointAtParameter(i * ustep, (j + 1) * vstep);

                    points.Add(pA);
                    points.Add(pB);
                    points.Add(pC);
                    points.Add(pD);

                    panels.Add(Surface.ByPerimeterPoints(points));
                    polygons.Add(Polygon.ByPoints(points));

                    pA.Dispose();
                    pB.Dispose();
                    pC.Dispose();
                    pD.Dispose();
                }
            }

            return new Dictionary<string, object> { { "Panels", panels }, { "Polygons", polygons } };

        }

        /// <summary>
        /// Surface divides to Diamond Panel and Triangle Panel
        /// </summary>
        /// <param name="Surface"></param>
        /// <param name="Udivision"></param>
        /// <param name="Vdivision"></param>
        /// <returns></returns>


        [IsVisibleInDynamoLibrary(true)]
        [MultiReturn(new[] { "DiamondPanel", "TrianglePanel" })]
        public static Dictionary<string,object> DiamondPanel(Surface Surface, double Udivision, double Vdivision)
        {
            var dpanels = new List<Surface>();
            var tpanels = new List<Surface>();

            var ustep = 1.0 / Udivision;
            var vstep = 1.0 / Vdivision;

            for (var i = 0; i < (Udivision+1); i++)
            {
                for (var j = 0; j < (Vdivision+1); i++)
                {
                    if ((i + j) % 2 == 0)
                    {
                        var pointA = Surface.PointAtParameter(0, 0);
                        var pointB = Surface.PointAtParameter(0, 0);
                        var pointC = Surface.PointAtParameter(0, 0);
                        var pointD = Surface.PointAtParameter(0, 0);

                        if (i > 0)
                        {
                            pointA = Surface.PointAtParameter((i - 1) * ustep, j * vstep);
                        }
                        else
                        {
                            pointB = Surface.PointAtParameter(i * ustep, j * vstep);
                        }

                        if (j > 0)
                        {
                            pointB = Surface.PointAtParameter(i * ustep, (j - 1) * vstep);
                        }
                        else
                        {
                            pointB = Surface.PointAtParameter(i * ustep, j * vstep);
                        }

                        if (i < Udivision)
                        {
                            pointC = Surface.PointAtParameter((i + 1) * ustep, j * vstep);
                        }
                        else
                        {
                            pointC = Surface.PointAtParameter(i * ustep, j * vstep);
                        }

                        if (j <= (Vdivision - 1))
                        {
                            pointD = Surface.PointAtParameter(i * ustep, (j + 1) * vstep);
                        }
                        else
                        {
                            pointD = Surface.PointAtParameter(i * ustep, j * vstep);
                        }

                        if (i > 0 && j > 0 && i < Udivision && j <= (Vdivision - 1))
                        {
                            var points = new List<Point>();
                            points.Add(pointA);
                            points.Add(pointB);
                            points.Add(pointC);
                            points.Add(pointD);

                            var panel = Surface.ByPerimeterPoints(points);
                            dpanels.Add(panel);

                            pointA.Dispose();
                            pointB.Dispose();
                            pointC.Dispose();
                            pointD.Dispose();
                        }

                        if (i > 0 && j>0 && i < Udivision && j < Vdivision)
                        {
                            var points = new List<Point>();
                        }

                        else
                        {
                            if (i == 0 && j == 0)
                            {
                                var points = new List<Point>();
                                points.Add(pointB);
                                points.Add(pointC);
                                points.Add(pointD);

                                var panel = Surface.ByPerimeterPoints(points);
                                tpanels.Add(panel);

                                pointA.Dispose();
                                pointB.Dispose();
                                pointC.Dispose();
                                pointD.Dispose();


                            }

                            if (i == 0 && j == Vdivision)
                            {
                                var points = new List<Point>();
                                points.Add(pointB);
                                points.Add(pointC);
                                points.Add(pointD);

                                var panel = Surface.ByPerimeterPoints(points);
                                tpanels.Add(panel);

                                pointA.Dispose();
                                pointB.Dispose();
                                pointC.Dispose();
                                pointD.Dispose();

                            }

                            if (i == Udivision && j == 0)
                            {
                                var points = new List<Point>();
                                points.Add(pointC);
                                points.Add(pointD);
                                points.Add(pointA);

                                var panel = Surface.ByPerimeterPoints(points);
                                tpanels.Add(panel);

                                pointA.Dispose();
                                pointB.Dispose();
                                pointC.Dispose();
                                pointD.Dispose();

                            }

                            if (i == Udivision && j == Vdivision)
                            {
                                var points = new List<Point>();
                                points.Add(pointA);
                                points.Add(pointB);
                                points.Add(pointC);

                                var panel = Surface.ByPerimeterPoints(points);
                                tpanels.Add(panel);

                                pointA.Dispose();
                                pointB.Dispose();
                                pointC.Dispose();
                                pointD.Dispose();
                            }

                            if (i == 0 && j >0 && j < Vdivision)
                            {
                                var points = new List<Point>();
                                points.Add(pointB);
                                points.Add(pointC);
                                points.Add(pointD);

                                var panel = Surface.ByPerimeterPoints(points);
                                tpanels.Add(panel);

                                pointA.Dispose();
                                pointB.Dispose();
                                pointC.Dispose();
                                pointD.Dispose();

                            }

                            if (i == Udivision && j > 0 && j < Vdivision)
                            {
                                var points = new List<Point>();
                                points.Add(pointA);
                                points.Add(pointB);
                                points.Add(pointD);

                                var panel = Surface.ByPerimeterPoints(points);
                                tpanels.Add(panel);

                                pointA.Dispose();
                                pointB.Dispose();
                                pointC.Dispose();
                                pointD.Dispose();
                            }

                            if (j == 0 && i > 0 && j < Vdivision)
                            {
                                var points = new List<Point>();
                                points.Add(pointA);
                                points.Add(pointC);
                                points.Add(pointD);

                                var panel = Surface.ByPerimeterPoints(points);
                                tpanels.Add(panel);
                                pointA.Dispose();
                                pointB.Dispose();
                                pointC.Dispose();
                                pointD.Dispose();
                            }

                            if (j == Vdivision && i > 0 && i < Udivision)
                            {
                                var points = new List<Point>();
                                points.Add(pointA);
                                points.Add(pointB);
                                points.Add(pointC);

                                var panel = Surface.ByPerimeterPoints(points);
                                tpanels.Add(panel);

                                pointA.Dispose();
                                pointB.Dispose();
                                pointC.Dispose();
                                pointD.Dispose();
                            }
                        }

                    }
                   
                }


            }

            return new Dictionary<string, object> { { "DiamondPanel", dpanels }, { "Triangle", tpanels } };
        }
        
    }
}

