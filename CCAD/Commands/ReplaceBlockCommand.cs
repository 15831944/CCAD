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
using Autodesk.AutoCAD.Runtime;

namespace CCAD.Commands
{
    public class ReplaceBlockCommand: ICADCommand
    {
        private class BlockData
        {
            public BlockData(ObjectId id, Matrix3d m)
            {
                Id = id;
                Transform = m;
            }
            public ObjectId Id { get; set; }
            public Matrix3d Transform { get; set; }

        }
        public void Execute()
        {
            /// promote:
            /// 
            /// CMD
            /// 选择要被替换的图块:
            /// 共选择X类图块，块名:[xxx,ccc]
            /// 选择替换图块
            /// 插入完成,共替换X个图块
            
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database database = doc.Database;
            Editor ed = doc.Editor;
            using (Transaction acTrans = database.TransactionManager.StartTransaction())
            {
                #region 选择图块
                PromptSelectionOptions selOptions = new PromptSelectionOptions();
                SelectionFilter filter = new SelectionFilter(new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"INSERT")
                });
                PromptSelectionResult selResult = doc.Editor.GetSelection(selOptions, filter);
                if (selResult.Status != PromptStatus.OK)
                {
                    return;
                }

                Dictionary<string, List<BlockData>> dic = new Dictionary<string, List<BlockData>>();
                SelectionSet acSSet = selResult.Value;
                foreach (SelectedObject acSSObj in acSSet)
                {
                    if (acSSObj != null)
                    {
                        using (BlockReference ent = acTrans.GetObject(acSSObj.ObjectId, OpenMode.ForRead) as BlockReference)
                        {
                            Debug.Assert(ent != null, nameof(ent) + " != null");
                            BlockData data = new BlockData(ent.Id, ent.BlockTransform);
                            if (dic.ContainsKey(ent.Name))
                            {
                                dic[ent.Name].Add(data);
                            }
                            else
                            {
                                dic.Add(ent.Name, new List<BlockData>()
                                {
                                    data
                                });
                            }
                        }
                    }
                }

                StringBuilder sb = new StringBuilder();
                foreach (string dicKey in dic.Keys)
                {
                    sb.Append(dicKey);
                    sb.Append(" ");
                }

                ed.WriteMessage($"\n共选择{dic.Keys.Count}类图块，块名:[{sb.ToString().Trim()}]");

                PromptEntityOptions entOptions = new PromptEntityOptions("\n选择替换图块");
                entOptions.SetRejectMessage("\n选择块参照");
                entOptions.AddAllowedClass(typeof(BlockReference), true);

                PromptEntityResult entResult = ed.GetEntity(entOptions);
                if (entResult.Status != PromptStatus.OK)
                {
                    return;
                }
                BlockReference blockRef = acTrans.GetObject(entResult.ObjectId, OpenMode.ForRead) as BlockReference;

                int replaceCount = 0;
                foreach (var pair in dic)
                {
                    if (!pair.Key.Equals(blockRef.Name))
                    {
                        //非同名块，开始替换
                        foreach (BlockData blockData in pair.Value)
                        {
                            //Transaction acTrans = database.TransactionManager.StartTransaction()
                            BlockReference formerBlockRef = acTrans.GetObject(blockData.Id, OpenMode.ForWrite) as BlockReference;
                            formerBlockRef.BlockTableRecord = blockRef.BlockTableRecord;
                            formerBlockRef.BlockTransform = blockData.Transform;
                            replaceCount++;
                        }
                    }
                }

                acTrans.Commit();
                ed.WriteMessage($"\n替换完成，共替换{replaceCount}个图块");
                #endregion
            }
        }
    }
}
