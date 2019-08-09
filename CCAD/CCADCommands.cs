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
            InsertBlockToPointCommand cmd = new InsertBlockToPointCommand();
            cmd.Execute();
        }
    }
}
