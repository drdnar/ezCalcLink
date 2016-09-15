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
            Object.LoadOmf.FromFile("main.obj");

            Console.ReadKey();
        }
    }
}
