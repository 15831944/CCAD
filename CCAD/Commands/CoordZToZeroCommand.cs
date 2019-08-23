using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace CCAD.Commands
{
    /// <summary>
    /// Z轴归零
    /// </summary>
    /// <remarks>
    /// 注意本命令与平面投影操作的区别：
    /// 本命令不进行投影操作，不改变实体的法线向量，仅将实体关键点投影至平面。如果实体与平面不平行，则实体仍将不与平面共面，以便核查制图正确与否。
    /// </remarks>
    public class CoordZToZeroCommand : ICADCommand
    {
        private enum CoordTypeEnum
        {
            WCS,
            UCS
        }

        private static CoordTypeEnum _coordType = CoordTypeEnum.UCS;

        private static string _coordName = "用户坐标";
        private static string _coordKeyWord = "用户坐标";
        
        private string CoordKeyWord
        {
            get { return _coordKeyWord; }
            set
            {
                if (value == "UC")
                {
                    _coordName = "用户坐标";
                    _coordType = CoordTypeEnum.UCS;
                }
                else if (value == "WC")
                {
                    _coordName = "世界坐标";
                    _coordType = CoordTypeEnum.WCS;
                }
                else
                {
                    throw new ArgumentException("关键字不正确");
                }
            }
        }
        private CoordTypeEnum CoordType
        {
            get { return _coordType; }
        }
        private string CoordName
        {
            get { return _coordName; }
        }
        private string PromoteStatus
        {
            get
            {
                return $"\n 当前设置：对齐坐标 = {CoordName}\n";
            }
        }

        private Point3d GetZ0Point(Point3d ptWCS, Matrix3d wcs2ucs, Matrix3d ucs2wcs)
        {
            if (CoordType == CoordTypeEnum.UCS)
            {
                Point3d ptUCS = ptWCS.TransformBy(wcs2ucs);
                Point3d newPt = new Point3d(ptUCS.X, ptUCS.Y, 0);
                return newPt.TransformBy(ucs2wcs);
            }
            else
            {
                return  new Point3d(ptWCS.X, ptWCS.Y, 0);
            }
        }

        private double GetZ0elevation(Matrix3d wcs2ucs, Matrix3d ucsSystem, Vector3d normalInWCS)
        {
            double elevation = 0;
            if (CoordType == CoordTypeEnum.UCS)
            {
                // 求原点在UCS上的投影点，然后与OCS平面向量（单位向量）点乘则得标高。
                Point3d ptUZ = Point3d.Origin.TransformBy(wcs2ucs);
                ptUZ = new Point3d(ptUZ.X, ptUZ.Y, 0);
                Point3d ptWZ = ptUZ.TransformBy(ucsSystem);
                Vector3d vec = ptWZ - Point3d.Origin;
                elevation = vec.DotProduct(normalInWCS);
            }
            return elevation;
        }
        public void Execute()
        {
            /// promote:
            /// 
            /// CMD
            /// 当前设置：对齐坐标 = 世界坐标
            /// 选择对象或改变坐标系[世界坐标(W)/用户坐标(U)]:
            /// 
            /// 修改完成，共修改[x]个对象
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database database = doc.Database;
            Editor ed = doc.Editor;
            Matrix3d wcs2ucs = ed.CurrentUserCoordinateSystem.Inverse();

            ed.WriteMessage(PromoteStatus);

            using (Transaction acTrans = database.TransactionManager.StartTransaction())
            {
                PromptSelectionOptions selOptions = new PromptSelectionOptions();
                selOptions.Keywords.Add("WC", "WC", "世界坐标(WC)");
                selOptions.Keywords.Add("UC", "UC", "用户坐标(UC)");
                selOptions.MessageForAdding = "\n选择对象或" + selOptions.Keywords.GetDisplayString(true);

                selOptions.KeywordInput += delegate (object sender, SelectionTextInputEventArgs args)
                {
                    CoordKeyWord = args.Input;
                    ed.WriteMessage(PromoteStatus);
                };

                PromptSelectionResult selResult = doc.Editor.GetSelection(selOptions);
                if (selResult.Status != PromptStatus.OK)
                {
                    return;
                }

                SelectionSet selectionSet = selResult.Value;
                foreach (SelectedObject selectedObject in selectionSet)
                {
                    if (selectedObject != null)
                    {
                        Entity ent = acTrans.GetObject(selectedObject.ObjectId, OpenMode.ForWrite) as Entity;
                        Debug.Assert(ent != null, nameof(ent) + " != null");
                        BlockReference blockRef = ent as BlockReference;
                        if (blockRef != null)
                        {

                            blockRef.Position = GetZ0Point(blockRef.Position, wcs2ucs, ed.CurrentUserCoordinateSystem);
                            continue;
                        }

                        DBPoint point = ent as DBPoint;
                        if (point != null)
                        {
                            point.Position = GetZ0Point(point.Position, wcs2ucs, ed.CurrentUserCoordinateSystem);
                            continue;
                        }

                        Circle circle = ent as Circle;
                        if (circle != null)
                        {
                            circle.Center = GetZ0Point(circle.Center, wcs2ucs, ed.CurrentUserCoordinateSystem);
                            continue;
                        }

                        Line line = ent as Line;
                        if (line != null)
                        {
                            line.StartPoint = GetZ0Point(line.StartPoint, wcs2ucs, ed.CurrentUserCoordinateSystem);
                            line.EndPoint = GetZ0Point(line.EndPoint, wcs2ucs, ed.CurrentUserCoordinateSystem); 
                            continue;
                        }

                        Polyline pline = ent as Polyline;
                        if (pline != null)
                        {
                            double elevation = 0;
                            if (CoordType == CoordTypeEnum.UCS)
                            {
                                // 求原点在UCS上的投影点，然后与OCS平面向量（单位向量）点乘则得标高。
                                Point3d ptUZ = Point3d.Origin.TransformBy(wcs2ucs);
                                ptUZ = new Point3d(ptUZ.X, ptUZ.Y, 0);
                                Point3d ptWZ = ptUZ.TransformBy(ed.CurrentUserCoordinateSystem);
                                Vector3d vec = ptWZ - Point3d.Origin;
                                elevation = vec.DotProduct(pline.Normal);
                            }

                            pline.Elevation = elevation;
                            continue;
                        }

                        RotatedDimension rd = ent as RotatedDimension;
                        if (rd != null)
                        {
                            rd.DimLinePoint = GetZ0Point(rd.DimLinePoint, wcs2ucs, ed.CurrentUserCoordinateSystem);
                            rd.XLine1Point = GetZ0Point(rd.XLine1Point, wcs2ucs, ed.CurrentUserCoordinateSystem);
                            rd.XLine2Point = GetZ0Point(rd.XLine2Point, wcs2ucs, ed.CurrentUserCoordinateSystem);
                            rd.Elevation = GetZ0elevation(wcs2ucs, ed.CurrentUserCoordinateSystem, rd.Normal);
                            continue;
                        }

                        AlignedDimension ad = ent as AlignedDimension;
                        if (ad != null)
                        {
                            ad.DimLinePoint = GetZ0Point(ad.DimLinePoint, wcs2ucs, ed.CurrentUserCoordinateSystem);
                            ad.XLine1Point = GetZ0Point(ad.XLine1Point, wcs2ucs, ed.CurrentUserCoordinateSystem);
                            ad.XLine2Point = GetZ0Point(ad.XLine2Point, wcs2ucs, ed.CurrentUserCoordinateSystem);
                            ad.Elevation = GetZ0elevation(wcs2ucs, ed.CurrentUserCoordinateSystem, ad.Normal);
                            continue;
                        }

                    }
                }
                acTrans.Commit();
            }
        }
    }
}
