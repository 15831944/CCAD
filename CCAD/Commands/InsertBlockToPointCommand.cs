using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CCAD.Commands
{
    public class InsertBlockToPointCommand : ICADCommand
    {
        private enum DeleteType
        {
            SelectedEntity,
            InsertedEntity,
            NotDelete
        }

        private static DeleteType _deleteType = DeleteType.SelectedEntity;
        private static double offsetX = 0;
        private static double offsetY = 0;

        private string PromoteStatus
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\n 当前设置：删除模式 = 删除");
                switch (_deleteType)
                {
                    case DeleteType.SelectedEntity:
                        sb.Append("选中对象");
                        break;
                    case DeleteType.InsertedEntity:
                        sb.Append("插入点对象");
                        break;
                    case DeleteType.NotDelete:
                        sb.Append("不删除");
                        break;
                }
                sb.Append($"，插入点偏移：({offsetX:F3},{offsetY:F3})\n");
                return sb.ToString();
            }
        }

        private string DeleteKeyWord
        {
            get
            {
                switch (_deleteType)
                {
                    case DeleteType.SelectedEntity:
                        return "S";
                    case DeleteType.InsertedEntity:
                        return "I";
                    case DeleteType.NotDelete:
                        return "N";
                }
                throw new InvalidOperationException();
            }
            set
            {
                if (value == "S")
                {
                    _deleteType = DeleteType.SelectedEntity;
                }else if (value == "I")
                {
                    _deleteType = DeleteType.InsertedEntity;
                }
                else if (value == "D")
                {
                    _deleteType = DeleteType.NotDelete;
                }
                else
                {
                    throw new ArgumentException("关键字不正确");
                }
            }
        }

        private bool _AddCenterPointToList(Entity ent, ref List<Point3d> lists)
        {
            Circle circle = ent as Circle;
            if (circle != null)
            {
                if (!lists.Contains(circle.Center))
                {
                    lists.Add(circle.Center);
                    return true;
                }

                return false;
            }

            DBPoint point = ent as DBPoint;
            if (point != null)
            {

                if (!lists.Contains(point.Position))
                {
                    lists.Add(point.Position);
                    return true;
                }

                return false;
            }

            return false;
        }

        public PromptStatus OffsetBlock(BlockReference blockRef)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptPointOptions ptOptions = new PromptPointOptions("\n选择新插入点");
            ptOptions.AllowNone = true;
            ptOptions.UseBasePoint = true;
            ptOptions.BasePoint = blockRef.Position;
            PromptPointResult ptResult = ed.GetPoint(ptOptions);
            PromptStatus returnStatus = ptResult.Status;
            if (returnStatus == PromptStatus.OK)
            {
                Vector3d vec = blockRef.Position - ptResult.Value;
                offsetX = vec.X;
                offsetY = vec.Y;
            }else if (returnStatus == PromptStatus.None)
            {
                returnStatus = PromptStatus.OK;
                offsetX = 0;
                offsetY = 0;
            }

            return returnStatus;
        }
        public PromptStatus SetDeleteMode()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptKeywordOptions kwOptions = new PromptKeywordOptions("\n选择删除模式");
            kwOptions.Keywords.Add("S","S", "选中对象(S)");
            kwOptions.Keywords.Add("I", "I", "插入对象(I)");
            kwOptions.Keywords.Add("N", "N", "不删除(N)");
            kwOptions.Keywords.Default = DeleteKeyWord;
            kwOptions.AllowNone = true;
            PromptResult pKeyRes = ed.GetKeywords(kwOptions);
            if (pKeyRes.Status == PromptStatus.OK)
            {
                // 测试情况：空格 ESC 回车 输入其他值
                DeleteKeyWord = pKeyRes.StringResult;
            }

            return pKeyRes.Status;
        }
        public void Execute()
        {
            /// promote:
            /// 
            /// CMD
            /// 当前设置：删除模式 = 删除插入点对象，插入点：（200,200）
            /// 选择块参照:
            /// 选择要插入的点、圆或 [偏移图块(O)/删除模式(D)]:
            /// 
            /// ---> 偏移图块
            /// 选择新插入点：
            /// 当前设置：删除模式 = 删除插入点对象，插入点：（200,200）
            /// ---> 删除模式：
            /// 选择删除模式 [选中对象(S) 插入对象(I) 不删除(N)]:<插入对象>
            ///
            /// 插入完成，共插入[x]个块参照，删除对象[y]个
            
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database database = doc.Database;
            Editor ed = doc.Editor;

            ed.WriteMessage(PromoteStatus);
            using (Transaction acTrans = database.TransactionManager.StartTransaction())
            {
                #region 选择图块
                PromptEntityOptions entOptions = new PromptEntityOptions("\n选择块参照");
                entOptions.SetRejectMessage("\n选择块参照");
                entOptions.AddAllowedClass(typeof(BlockReference), true);

                PromptEntityResult entResult = ed.GetEntity(entOptions);
                if (entResult.Status != PromptStatus.OK)
                {
                    return;
                }
                BlockReference blockRef = acTrans.GetObject(entResult.ObjectId, OpenMode.ForRead) as BlockReference;

                #endregion


                #region 选择点

                PromptSelectionOptions selOptions = new PromptSelectionOptions();
                selOptions.Keywords.Add("O", "O", "偏移图块(O)");
                selOptions.Keywords.Add("D", "D", "删除模式(D)");
                selOptions.MessageForAdding = "\n选择要插入的点、圆或" + selOptions.Keywords.GetDisplayString(true);

                PromptStatus kwStatus = PromptStatus.OK;
                selOptions.KeywordInput += delegate (object sender, SelectionTextInputEventArgs args)
                {
                    if (args.Input == "O")
                    {
                        kwStatus = OffsetBlock(blockRef);
                    }
                    else if (args.Input == "D")
                    {
                        kwStatus = SetDeleteMode();
                    }
                    ed.WriteMessage(PromoteStatus);
                };
                
                SelectionFilter filter = new SelectionFilter(new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"POINT,CIRCLE")
                });
                PromptSelectionResult selResult = doc.Editor.GetSelection(selOptions, filter);
                
                if (selResult.Status != PromptStatus.OK)
                {
                    return;
                }

                List<ObjectId> insertedIds = new List<ObjectId>();
                List<ObjectId> selectedIds = new List<ObjectId>();
                List<Point3d> insertPoints = new List<Point3d>();


                SelectionSet acSSet = selResult.Value;
                foreach (SelectedObject acSSObj in acSSet)
                {
                    if (acSSObj != null)
                    {
                        Entity ent = acTrans.GetObject(acSSObj.ObjectId, OpenMode.ForRead) as Entity;
                        Debug.Assert(ent != null, nameof(ent) + " != null");
                        selectedIds.Add(ent.ObjectId);
                        if (_AddCenterPointToList(ent, ref insertPoints))
                        {
                            insertedIds.Add(ent.ObjectId);
                        }
                    }
                }
                #endregion

                #region 插入图块

                int insertCount = 0;
                int deleteCount = 0;
                Matrix3d mat = Matrix3d.Displacement(new Vector3d(offsetX, offsetY, 0));
                foreach (Point3d inertPoint in insertPoints)
                {
                    using (BlockReference acBlkRef = new BlockReference(inertPoint.TransformBy(mat), blockRef.BlockTableRecord))
                    {
                        acBlkRef.ScaleFactors = new Scale3d(blockRef.ScaleFactors.X, blockRef.ScaleFactors.Y, blockRef.ScaleFactors.Z);

                        BlockTableRecord acCurSpaceBlkTblRec = acTrans.GetObject(database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                        acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                        acTrans.AddNewlyCreatedDBObject(acBlkRef, true);
                        insertCount++;
                    }
                }

                List<ObjectId> ids = new List<ObjectId>();
                if (_deleteType == DeleteType.SelectedEntity)
                {
                    ids = selectedIds;
                }else if (_deleteType == DeleteType.InsertedEntity)
                {
                    ids = insertedIds;
                }

                foreach (ObjectId objectId in ids)
                {
                    Entity ent = acTrans.GetObject(objectId, OpenMode.ForWrite) as Entity;
                    ent.Erase(true);
                    deleteCount++;
                }

                #endregion

                ed.WriteMessage($"\n插入完成，共插入[{insertCount}]个块参照，删除对象[{deleteCount}]个");
                acTrans.Commit();
            }
        }
    }
}
