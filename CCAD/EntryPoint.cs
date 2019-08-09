using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: ExtensionApplication(typeof(CCAD.EntryPoint))]

namespace CCAD
{
    public class EntryPoint : IExtensionApplication
    {
        void IExtensionApplication.Initialize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Version ver = Assembly.GetExecutingAssembly().GetName().Version;
            ed.WriteMessage("\n===   CCAD Ver " + ver.ToString() + " 已加载    ====");
        }

        void IExtensionApplication.Terminate()
        {
        }
    }
}
