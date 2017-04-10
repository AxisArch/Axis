using System;
using System.Collections.Generic;
using System.IO;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.Core
{
    public class CodeGenerator : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconRGen;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{6dccd5dc-520b-482d-bbb2-93607ba5166f}"); }
        }

        public CodeGenerator() : base("Code Generator", "Code", "Generate robot code from an Axis program.", "Axis", "Core")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Module", "Name of module to generate.", GH_ParamAccess.item, "MainModule");
            pManager.AddTextParameter("Declarations", "Declarations", "List of RAPID declarations [speed, zone, tool data].", GH_ParamAccess.list, "! No Declarations");
            pManager.AddTextParameter("Program", "Program", "Axis robot program for RAPID formatting.", GH_ParamAccess.list);
            pManager.AddTextParameter("Procedures", "Procedures", "Custom RAPID procedures.", GH_ParamAccess.list, "! No Custom Procedures");
            pManager.AddTextParameter("Overrides", "Overrides", "RAPID overrides as list of strings.", GH_ParamAccess.list, "! No Custom Overrides");
            pManager.AddBooleanParameter("Mode", "Mode", "Mode switch for code generation.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Type", "Type", "Robot brand to use for target creation. [0 = ABB, 1 = KUKA].", GH_ParamAccess.item, false);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "Code", "Robot code.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string strModName = null;
            bool mode = false;
            bool type = false;
            List<string> strHeaders = new List<string>();
            List<string> strDeclarations = new List<string>();
            List<string> strProgram = new List<string>();
            List<string> strProcedures = new List<string>();
            List<string> strOverrides = new List<string>();

            // Initialize lists to store the robot export code.
            List<string> RAPID = new List<string>();
            List<string> KRL = new List<string>();

            if (!DA.GetData(0, ref strModName)) return;
            if (!DA.GetDataList(1, strDeclarations)) return;
            if (!DA.GetDataList(2, strProgram)) return;
            if (!DA.GetDataList(3, strProcedures)) return;
            if (!DA.GetDataList(4, strOverrides)) return;
            if (!DA.GetData(5, ref mode)) return;
            if (!DA.GetData(6, ref type)) return;

            if (strProgram != null)
            {
                if (type)
                {
                    // Headers
                    
                    KRL.Add("; Generated with Axis Parametric Robot Control v0.1");
                    KRL.Add("; Aarhus School of Architecture");
                    KRL.Add("; Created: " + DateTime.Now.ToString());
                    KRL.Add(" ");

                    KRL.Add("&ACCESS RVP");
                    KRL.Add("&REL 26");
                    
                    // Open Main Proc
                    KRL.Add("DEF Axis_Program ( )");
                    KRL.Add(" ");

                    KRL.Add("DECL AXIS HOME");
                    KRL.Add("DECL INT i");
                    KRL.Add(" ");

                    KRL.Add("BAS(#INITMOV, 0)");
                    KRL.Add(" ");

                    KRL.Add("HOME = {AXIS: A1 -90, A2 -90, A3 90, A4 -90, A5 -30.0, A6 0}");
                    KRL.Add(" ");

                    KRL.Add("$APO.CPTP=1");
                    KRL.Add("$APO.CVEL=100");
                    KRL.Add("$APO.CDIS=1");

                    KRL.Add(" ");
                    KRL.Add("FOR i = 1 TO 6");
                    KRL.Add(@"   $VEL_AXIS[i] = 100.0");
                    KRL.Add(@"   $ACC_AXIS[i] = 30");
                    KRL.Add("ENDFOR");
                    KRL.Add(" ");

                    if (mode)
                    {
                        // Commands
                        KRL.Add("; Programmed Movement");
                        if (strProgram.Count > 5000)
                        {
                            KRL.Add("; Warning: Procedure length exceeds recommend maximum. Advise splitting main proc into sub-procs.");
                            KRL.Add(" ");
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommend maximum. Advise splitting main proc into sub-procs.");
                        }
                        for (int i = 0; i < strProgram.Count; i++)
                        {
                            KRL.Add(strProgram[i]);
                        }
                    }
                    else
                    {
                        KRL.Add("; Warning: Preview mode enabled. Switch to export mode to generate KRL code.");
                    }

                    KRL.Add("END");

                    DA.SetDataList(0, KRL);
                }
                else
                {
                    // Headers
                    RAPID.Add("MODULE " + strModName);
                    RAPID.Add("! Author: Ryan Hughes");
                    RAPID.Add("! Generated with Axis Parametric Robot Control v0.1");
                    RAPID.Add("! Aarhus School of Architecture");
                    RAPID.Add("! Created: " + DateTime.Now.ToString());
                    RAPID.Add(" ");

                    // Declarations
                    RAPID.Add("! Declarations");
                    RAPID.Add("VAR confdata cData := [0,-1,-1,0];");
                    RAPID.Add("VAR extjoint eAxis := [9E9,9E9,9E9,9E9,9E9,9E9];");

                    for (int i = 0; i < strDeclarations.Count; i++)
                    {
                        RAPID.Add(strDeclarations[i]);
                    }

                    RAPID.Add(" ");

                    // Open Main Proc
                    RAPID.Add("! Main Procedure");
                    RAPID.Add("PROC main()");
                    RAPID.Add(@"ConfL \Off;");
                    RAPID.Add(@"ConfJ \Off;");

                    // Machine overrides
                    if (strOverrides != null) // If we have machine override data, then add it.
                    {
                        for (int i = 0; i < strOverrides.Count; i++)
                        {
                            RAPID.Add(strOverrides[i]);
                        }
                    }
                    RAPID.Add(" ");

                    if (mode)
                    {
                        // Commands
                        RAPID.Add("! Programmed Movement");
                        if (strProgram.Count > 5000)
                        {
                            RAPID.Add("! Warning: Procedure length exceeds recommend maximum. Advise splitting main proc into sub-procs.");
                            RAPID.Add(" ");
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommend maximum. Advise splitting main proc into sub-procs.");
                        }
                        for (int i = 0; i < strProgram.Count; i++)
                        {
                            RAPID.Add(strProgram[i]);
                        }
                    }
                    else
                    {
                        RAPID.Add("! Warning: Preview mode enabled. Switch to export mode to generate RAPID code.");
                    }


                    // Close Main Proc
                    RAPID.Add(" ");
                    RAPID.Add("!");
                    RAPID.Add(@"ConfL \On;");
                    RAPID.Add(@"ConfJ \On;");
                    RAPID.Add("ENDPROC");
                    RAPID.Add(" ");

                    // Custom Procs
                    if (strProcedures != null) // If custom RAPID procedures have been specified, add them here.
                    {
                        RAPID.Add("! Custom Procedures");
                        for (int i = 0; i < strProcedures.Count; i++)
                        {
                            RAPID.Add(strProcedures[i]);
                        }
                    }

                    RAPID.Add("ENDMODULE");

                    if (export)
                    {
                        if (type)
                        {
                            using (StreamWriter writer = new StreamWriter(path + @"\" + filename + ".cnc", false))
                            {
                                for (int i = 0; i < gCode.Count; i++)
                                {
                                    writer.WriteLine(gCode[i]);
                                }
                            }
                        }
                        else
                        {
                            using (StreamWriter writer = new StreamWriter(path + @"\" + filename + ".cnc", false))
                            {
                                for (int i = 0; i < gCode.Count; i++)
                                {
                                    writer.WriteLine(gCode[i]);
                                }
                            }
                        }                        
                    }

                    DA.SetDataList(0, RAPID);
                }
            }
        }
    }
}
 
 
 