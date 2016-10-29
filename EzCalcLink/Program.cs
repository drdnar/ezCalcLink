using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink
{
    class Program
    {
        static void Main(string[] args)
        {
            //Omf695 obj = Omf695.LoadObjectFile("nokernel.lib");
            //Object.ObjectFile obj = Object.LoadOmf.FromFile("CHECKERS.lod");
            //Object.ObjectFile obj = Object.LoadOmf.FromFile("main.obj");
            //Object.LoadOmf.FromFile("graphx.lib");
            
            //Object.LoadOmf.FromFile("main.obj");

            Linker.Linker linker = new Linker.Linker();

            DefineData[] defines = { 
                                       new DefineData() { Name = "__low_bss", Value = 0xD031F6, AddressSpace = "RAM", Section = "BSS" },  
                                   };
            
            
            string[] inFiles = new string[] { "cstartup.obj", "fileioc.obj", "graphx.obj", "libheader.obj", "logo.obj", "logo_gfx.obj", "main.obj", "simplech.obj", "fileioc.lib", "graphx.lib" };
            foreach (var f in inFiles)
            {
                var o = Object.LoadOmf.FromFile(f);
                if (o.Count() == 1 && !o[0].IsLibraryMember)
                    linker.ObjectFiles.AddRange(o);
                else
                    linker.Libraries.AddRange(o);
            }

            foreach (var d in defines)
            {
                linker.DefinesObject.LocalSymbols.Add(new Object.Symbol()
                {
                    Section = linker.DefinesObject.Sections[d.Section],
                    AddressSpace = linker.DefinesObject.AddressSpaces[d.AddressSpace],
                    ObjectFile = linker.DefinesObject,
                    External = false,
                    Resolved = true,
                    Offset = d.Value,
                    Name = d.Name
                });
            }

            linker.LinkTest();

            Console.ReadKey();
        }

        private struct DefineData
        {
            public string Name;
            public int Value;
            public string AddressSpace;
            public string Section;
        }
    }
}
