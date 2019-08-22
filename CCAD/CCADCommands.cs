using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CCAD;
using CCAD.Commands;

[assembly: CommandClass(typeof(CCAD.CCADCommands))]


namespace CCAD
{
    public class CCADCommands 
    {
        [CommandMethod("CCAD", "INSERTBLOCKTOPOINT", CommandFlags.Modal)]
        public void InsertBlockToPoint()
        {
            ICADCommand cmd = new InsertBlockToPointCommand();
            cmd.Execute();
        }

        [CommandMethod("CCAD", "COORDZTOZERO", CommandFlags.Modal)]
        public void CoordZToZero()
        {
            ICADCommand cmd = new CoordZToZeroCommand();
            cmd.Execute();
        }

        [CommandMethod("CCAD", "REPLACEBLOCK", CommandFlags.Modal)]
        public void ReplaceBlock()
        {
            ICADCommand cmd = new ReplaceBlockCommand();
            cmd.Execute();
        }
    }
}
